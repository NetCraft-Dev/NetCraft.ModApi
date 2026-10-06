namespace NetCraft.ModApi.Extension;

//Inject 声明本方法是某处调用点的顶替实现
//与 ncmod.json 的 hooks 条目等价 两者可以混用 同一个注入点两边都声明时以注解为准
//替换类与替换方法就是标注它的那个类与方法 这两个字段不必再写 也就不可能拼错
//规则是加载器静态读元数据得来的 与清单走同一条路 不在场也能读 不会提前加载任何程序集
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InjectAttribute : Attribute
{
    //Inject target 被顶替的类型 method 被顶替的方法名
    public InjectAttribute(Type target, string method)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Method = method ?? throw new ArgumentNullException(nameof(method));
    }

    //Target 被顶替的类型
    //编译进元数据的是类型的序列化名 只是一段字符串 读取规则不需要那个类型被加载
    public Type Target { get; }

    //Method 被顶替的方法名 用 nameof 写可以跟着改名走
    public string Method { get; }

    //HookType 注入形态 取 CallSite/MethodBody/NewObj 等 默认 CallSite
    public string HookType { get; set; } = "CallSite";

    //PatchMode 落地方式 取 ILRewrite 或 RuntimeInject 默认 ILRewrite
    //两者锚点参数完全一样 区别只在时机 ILRewrite 赶在目标程序集加载前改字节
    //RuntimeInject 借 CLR 的 ReJIT 改已经加载的代码 要求进程启动时挂了原生注入层
    public string PatchMode { get; set; } = "ILRewrite";

    //Label 探针标签 仅 Probe 与 Mark 用
    public string? Label { get; set; }

    //Ordinal 同一锚点在宿主方法里匹配到多处时只认第几处 0 基
    //不写表示每一处都改 宿主里没有这么多处时这条规则不落地
    public int? Ordinal { get; set; }

    //ArgumentIndex 改第几个实参 0 基 实例调用的 this 算第 0 个 仅 HookType 为 CallArg 时用
    public int? ArgumentIndex { get; set; }

    //SliceFrom/SliceTo 方法内区间限定 写成"类型全名::方法名"
    //把匹配收窄到宿主方法里第一次调用 SliceFrom 到第一次调用 SliceTo 之间 任一端都可留空
    //两端都不写就是原来的"整个方法体内匹配"
    public string? SliceFrom { get; set; }

    public string? SliceTo { get; set; }

    //Environment 该规则适用的运行端 取 both/client/server 默认 both
    public string Environment { get; set; } = "both";
}
