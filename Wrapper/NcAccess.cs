using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace NetCraft.ModApi.Wrapper;

//NcAccess 内核私有成员访问入口 门面没开到的那部分从这里走
//成员按名字解析 解析结果带缓存 拿到句柄后反复调用不再走反射绑定
//这是逃生舱 内核改了成员名就会失效 能用 Nc* 门面就用门面
public static class NcAccess
{
    //Flags 找成员时公开与非公开 实例与静态都算在内
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Instance | BindingFlags.Static;

    //Types 类型全名到内核类型的解析结果 找不到记空 免得每次都把已加载程序集搜一遍
    private static readonly ConcurrentDictionary<string, Type?> Types = new();

    //Fields/Properties 成员句柄缓存 键是类型加成员名
    private static readonly ConcurrentDictionary<(Type, string), NcField?> Fields = new();
    private static readonly ConcurrentDictionary<(Type, string), NcProperty?> Properties = new();

    //Methods 方法句柄缓存 键里多一段参数类型签名 重载靠它区分
    private static readonly ConcurrentDictionary<(Type, string, string), NcMethod?> Methods = new();

    //Unwrap 把 Nc* 句柄还原成内核对象 传进来的不是句柄就原样返回
    //内核对象不进公开面 这里是有意开的缺口 换来的引用只能按当前内核版本用
    public static object Unwrap(object handle) => handle switch
    {
        NcPlayer value => value.Inner,
        NcLevel value => value.Inner,
        NcGameRules value => value.Inner,
        NcServerSettings value => value.Inner,
        NcTickRate value => value.Inner,
        NcWorldBorder value => value.Inner,
        NcWhiteList value => value.Inner,
        NcOpList value => value.Inner,
        NcBanList value => value.Inner,
        NcIpBanList value => value.Inner,
        _ => handle,
    };

    //FindType 按全名找已加载程序集里的类型 找不到返回 null
    public static Type? FindType(string fullName) => Types.GetOrAdd(fullName, static name =>
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetType(name) is { } type)
                return type;
        }
        return null;
    });

    //Field 取字段句柄 找不到返回 null
    public static NcField? Field(Type type, string name)
        => Fields.GetOrAdd((type, name), static key =>
            key.Item1.GetField(key.Item2, Flags) is { } field ? new NcField(field) : null);

    //Field 按类型全名取字段句柄
    public static NcField? Field(string typeName, string name)
        => FindType(typeName) is { } type ? Field(type, name) : null;

    //Property 取属性句柄 找不到返回 null
    public static NcProperty? Property(Type type, string name)
        => Properties.GetOrAdd((type, name), static key =>
            key.Item1.GetProperty(key.Item2, Flags) is { } property ? new NcProperty(property) : null);

    //Property 按类型全名取属性句柄
    public static NcProperty? Property(string typeName, string name)
        => FindType(typeName) is { } type ? Property(type, name) : null;

    //Method 取方法句柄 同名重载要精确到某一个就得把参数类型一并给上 留空取第一个同名的
    public static NcMethod? Method(Type type, string name, params Type[] parameterTypes)
    {
        var signature = string.Join(",", parameterTypes.Select(t => t.FullName));
        return Methods.GetOrAdd((type, name, signature), _ =>
            ResolveMethod(type, name, parameterTypes) is { } method ? new NcMethod(method) : null);
    }

    //Method 按类型全名取方法句柄
    public static NcMethod? Method(string typeName, string name, params Type[] parameterTypes)
        => FindType(typeName) is { } type ? Method(type, name, parameterTypes) : null;

    //ResolveMethod 找方法 给了参数类型就走精确匹配
    //基类的 private 方法不在继承成员里 那类的只能写到声明它的类型上
    private static MethodInfo? ResolveMethod(Type type, string name, Type[] parameterTypes)
        => parameterTypes.Length == 0
            ? type.GetMethods(Flags).FirstOrDefault(m => m.Name == name)
            : type.GetMethod(name, Flags, null, parameterTypes, null);
}

//NcField 字段句柄 解析一次反复读写
//目标对象传 null 表示访问静态字段
public sealed class NcField
{
    private readonly FieldInfo _field;
    private readonly Func<object, object?> _get;
    private readonly Action<object, object?>? _set;

    internal NcField(FieldInfo field)
    {
        _field = field;
        _get = BuildGetter(field);
        //readonly 字段表达式树里赋不了值 留空等调用时再报
        _set = field.IsInitOnly ? null : BuildSetter(field);
    }

    //Name 字段名
    public string Name => _field.Name;

    //FieldType 字段声明的类型
    public Type FieldType => _field.FieldType;

    //CanWrite 是不是可写 系统字段与 readonly 字段是 false
    public bool CanWrite => _set is not null;

    //Get 取字段值
    public object? Get(object? target) => _get(target!);

    //Get<T> 取字段值并转成指定类型
    public T Get<T>(object? target) => (T)Get(target)!;

    //Set 写字段值
    public void Set(object? target, object? value)
        => (_set ?? throw new InvalidOperationException($"字段 {_field.Name} 不可写"))(target!, value);

    //Set<T> 按指定类型写字段值 值先装箱 免得又落回泛型这一版
    public void Set<T>(object? target, T value) => Set(target, (object?)value);

    //BuildGetter 编译取值委托 静态字段不看目标对象
    private static Func<object, object?> BuildGetter(FieldInfo field)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var access = field.IsStatic ? null : Expression.Convert(target, field.DeclaringType!);
        var body = Expression.Convert(Expression.Field(access, field), typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, target).Compile();
    }

    //BuildSetter 编译写值委托 静态字段不看目标对象
    private static Action<object, object?> BuildSetter(FieldInfo field)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var access = field.IsStatic ? null : Expression.Convert(target, field.DeclaringType!);
        var body = Expression.Assign(Expression.Field(access, field), Expression.Convert(value, field.FieldType));
        return Expression.Lambda<Action<object, object?>>(body, target, value).Compile();
    }
}

//NcProperty 属性句柄 解析一次反复读写
//目标对象传 null 表示访问静态属性 只有 get 或只有 set 的那一侧不可用
public sealed class NcProperty
{
    private readonly PropertyInfo _property;
    private readonly Func<object, object?>? _get;
    private readonly Action<object, object?>? _set;

    internal NcProperty(PropertyInfo property)
    {
        _property = property;
        _get = property.GetGetMethod(nonPublic: true) is null ? null : BuildGetter(property);
        _set = property.GetSetMethod(nonPublic: true) is null ? null : BuildSetter(property);
    }

    //Name 属性名
    public string Name => _property.Name;

    //PropertyType 属性声明的类型
    public Type PropertyType => _property.PropertyType;

    //CanRead 有没有取值访问器
    public bool CanRead => _get is not null;

    //CanWrite 有没有写值访问器
    public bool CanWrite => _set is not null;

    //Get 取属性值
    public object? Get(object? target)
        => (_get ?? throw new InvalidOperationException($"属性 {_property.Name} 没有取值访问器"))(target!);

    //Get<T> 取属性值并转成指定类型
    public T Get<T>(object? target) => (T)Get(target)!;

    //Set 写属性值
    public void Set(object? target, object? value)
        => (_set ?? throw new InvalidOperationException($"属性 {_property.Name} 没有写值访问器"))(target!, value);

    //Set<T> 按指定类型写属性值 值先装箱 免得又落回泛型这一版
    public void Set<T>(object? target, T value) => Set(target, (object?)value);

    //BuildGetter 编译取值委托 静态属性不看目标对象
    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var getter = property.GetGetMethod(nonPublic: true)!;
        var access = getter.IsStatic ? null : Expression.Convert(target, property.DeclaringType!);
        var body = Expression.Convert(Expression.Property(access, property), typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, target).Compile();
    }

    //BuildSetter 编译写值委托 静态属性不看目标对象
    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var setter = property.GetSetMethod(nonPublic: true)!;
        var access = setter.IsStatic ? null : Expression.Convert(target, property.DeclaringType!);
        var body = Expression.Assign(Expression.Property(access, property), Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<object, object?>>(body, target, value).Compile();
    }
}

//NcMethod 方法句柄 解析一次反复调用
//目标对象传 null 表示调用静态方法 返回值是 void 时调用结果为 null
//带 ref/out 参数的方法与泛型方法定义编译不出委托 调用时直接报不支持
public sealed class NcMethod
{
    private readonly MethodInfo _method;
    private readonly Func<object, object?[], object?>? _invoke;

    internal NcMethod(MethodInfo method)
    {
        _method = method;
        _invoke = IsSupported(method) ? BuildInvoker(method) : null;
    }

    //Name 方法名
    public string Name => _method.Name;

    //ReturnType 返回值类型 void 方法为 typeof(void)
    public Type ReturnType => _method.ReturnType;

    //ParameterCount 形参个数
    public int ParameterCount => _method.GetParameters().Length;

    //Invoke 调用方法 实参按位置对应 个数对不上一律拒绝 免得错位传参
    public object? Invoke(object? target, params object?[] args)
    {
        if (_invoke is null)
            throw new NotSupportedException($"方法 {_method.Name} 带 ref/out 参数或是泛型方法定义 调用不了");
        if (args.Length != ParameterCount)
            throw new ArgumentException($"方法 {_method.Name} 要 {ParameterCount} 个参数 给了 {args.Length} 个");
        return _invoke(target!, args);
    }

    //Invoke<T> 调用方法并把返回值转成指定类型
    public T Invoke<T>(object? target, params object?[] args) => (T)Invoke(target, args)!;

    //IsSupported 泛型方法定义与带 ref/out 参数的方法编译不出委托
    private static bool IsSupported(MethodInfo method)
        => !method.IsGenericMethodDefinition
           && method.GetParameters().All(p => !p.ParameterType.IsByRef);

    //BuildInvoker 编译调用委托 实参从数组按位置取 静态方法不看目标对象
    private static Func<object, object?[], object?> BuildInvoker(MethodInfo method)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var args = Expression.Parameter(typeof(object[]), "args");
        var parameters = method.GetParameters();
        var callArgs = new Expression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            callArgs[i] = Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), parameters[i].ParameterType);

        var instance = method.IsStatic ? null : Expression.Convert(target, method.DeclaringType!);
        var call = Expression.Call(instance, method, callArgs);
        Expression body = method.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(null, typeof(object)))
            : Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<object, object?[], object?>>(body, target, args).Compile();
    }
}
