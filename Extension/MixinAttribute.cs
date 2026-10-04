namespace NetCraft.ModApi.Extension;

//Mixin 把标注这个类的字段与方法搬进目标类型
//与 ncmod.json 的 mixins 条目等价 同一个目标两边都声明时以注解为准
//来源类搬完只剩空壳 模组代码不该再用它 这一点跟 Mixin 的 mixin 类一样
//规则是加载器静态读元数据得来的 不在场也能读 不会提前加载任何程序集
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MixinAttribute : Attribute
{
    //Mixin target 成员搬进哪个类型
    public MixinAttribute(Type target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    //Target 目标类型
    //编译进元数据的是类型的序列化名 只是一段字符串 读取规则不需要那个类型被加载
    public Type Target { get; }

    //Interfaces 顺带让目标类型实现的接口
    //被搬过去的实例方法会标成虚方法 接口分派只认虚表
    public Type[] Interfaces { get; set; } = Array.Empty<Type>();
}
