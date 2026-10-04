using NetCraft;

namespace NetCraft.ModApi.Wrapper;

//NcStartupArgs 启动参数就绪事件参数
public sealed class NcStartupArgs
{
    //Arguments 内核认不下来的全部原始 token 保持命令行里的原顺序
    //--flag 与 --opt value 与 --opt=value 与裸位置参数都在里面 怎么解释由模组自己定
    public required IReadOnlyList<string> Arguments { get; init; }
}

//NcStartup 启动参数访问点
//这一列 API 与 Minecraft 无关 是 NetCraft 自己的启动流程
//内核解析完参数后把没认下来的 token 原样转出来 模组自行决定怎么解释
//模组加载早于参数解析 所以在 Init 里订阅一定赶得上 解析完成时统一分发
public static class NcStartup
{
    //RawTokens 收到的全部原始 token
    private static readonly List<string> RawTokens = new();
    //Declared 解析后的参数名到取值 一个名字出现多次就有多个值
    private static readonly Dictionary<string, List<string?>> Declared = new(StringComparer.Ordinal);
    //Watchers 等着就绪的按名订阅
    private static readonly List<Watcher> Watchers = new();
    private static readonly object Gate = new();
    private static volatile bool _ready;
    private static int _hooked;

    //Ready 启动参数就绪事件
    //内核解析完成时触发 此刻宿主还没有进入正式运行 适合在这里按参数做准备
    public static readonly NcEvent<NcStartupArgs> Ready = new();

    //Arguments 全部原始 token 就绪前是空列表
    public static IReadOnlyList<string> Arguments
    {
        get { lock (Gate) return RawTokens.ToArray(); }
    }

    //IsReady 参数是否已解析完成
    public static bool IsReady => _ready;

    //Initialize 挂上内核的参数广播 由模组入口在加载时调一次
    internal static void Initialize()
    {
        if (Interlocked.Exchange(ref _hooked, 1) == 1)
            return;
        LaunchOptions.UnhandledArgument += OnArgument;
    }

    //Has 该参数是否出现过
    public static bool Has(string name)
    {
        lock (Gate) return Declared.ContainsKey(name);
    }

    //Value 取参数的值 无值形式与没出现都返回 null 出现多次时取最后一个
    public static string? Value(string name)
    {
        lock (Gate) return Declared.TryGetValue(name, out var values) ? values[^1] : null;
    }

    //Values 同一参数出现多次时的全部取值 没出现过返回空
    public static IReadOnlyList<string?> Values(string name)
    {
        lock (Gate) return Declared.TryGetValue(name, out var values) ? values.ToArray() : Array.Empty<string?>();
    }

    //Subscribe 声明关心某个参数 就绪时它出现过才回调 没出现什么也不发生
    //回调收到该参数的值 无值形式传 null
    //就绪之后再订阅且参数存在时立即回调一次
    public static IDisposable Subscribe(string name, Action<string?> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(handler);

        var watcher = new Watcher(name, handler);
        string? immediate = null;
        var fireNow = false;

        lock (Gate)
        {
            if (!_ready)
            {
                Watchers.Add(watcher);
                return watcher;
            }

            if (Declared.TryGetValue(name, out var values))
            {
                immediate = values[^1];
                fireNow = true;
            }
        }

        if (fireNow)
            watcher.Invoke(immediate);
        return watcher;
    }

    //MarkReady 参数已解析完成 分发一次
    //由 SignalProbe 在事件发布之前调 保证就绪一定排在用户的订阅回调前面
    //重复调用只生效一次 客户端每 tick 都会走到这里 所以先做一次无锁短路
    internal static void MarkReady()
    {
        if (_ready)
            return;

        NcStartupArgs args;
        var pending = new List<(Watcher Watcher, string? Value)>();

        lock (Gate)
        {
            if (_ready)
                return;

            _ready = true;
            BuildIndex();
            args = new NcStartupArgs { Arguments = RawTokens.ToArray() };

            foreach (var watcher in Watchers)
            {
                if (!watcher.Disposed && Declared.TryGetValue(watcher.Name, out var values))
                    pending.Add((watcher, values[^1]));
            }
        }

        Ready.Publish(args);
        foreach (var (watcher, value) in pending)
            watcher.Invoke(value);
    }

    //OnArgument 内核广播一条未识别 token 先原样存下来
    //成对关系要等后面的 token 到齐才知道 所以解析留到就绪时统一做
    private static void OnArgument(object? sender, LaunchArgEventArgs e)
    {
        lock (Gate) RawTokens.Add(e.Token);
    }

    //BuildIndex 把原始 token 序列解析成参数名到取值
    //--opt 后面跟的非 -- 开头 token 算它的值 与内核 Parse 的判断保持一致
    private static void BuildIndex()
    {
        Declared.Clear();

        for (var i = 0; i < RawTokens.Count; i++)
        {
            var token = RawTokens[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
                continue;

            var body = token.AsSpan(2);
            var equals = body.IndexOf('=');
            string name;
            string? value;

            if (equals >= 0)
            {
                name = body[..equals].ToString();
                value = body[(equals + 1)..].ToString();
            }
            else
            {
                name = body.ToString();
                value = null;
                if (i + 1 < RawTokens.Count && !RawTokens[i + 1].StartsWith("--", StringComparison.Ordinal))
                    value = RawTokens[++i];
            }

            if (name.Length == 0)
                continue;

            if (!Declared.TryGetValue(name, out var values))
                Declared[name] = values = new List<string?>();
            values.Add(value);
        }
    }

    //Watcher 按名的启动参数订阅 释放即不再回调
    private sealed class Watcher : IDisposable
    {
        private readonly Action<string?> _handler;

        public Watcher(string name, Action<string?> handler)
        {
            Name = name;
            _handler = handler;
        }

        //Name 关心的参数名
        public string Name { get; }

        //Disposed 是否已注销
        public bool Disposed { get; private set; }

        public void Invoke(string? value)
        {
            if (!Disposed)
                _handler(value);
        }

        public void Dispose()
        {
            Disposed = true;
            lock (Gate) Watchers.Remove(this);
        }
    }
}
