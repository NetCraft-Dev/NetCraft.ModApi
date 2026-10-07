using NetCraft.Commands;
using NetCraft.Logging;
using NetCraft.ModApi.Internal;
using NetCraft.ModApi.Wrapper;

namespace NetCraft.ModApi;

//ModApiEntry 模组入口 对应原版 fabric.mod.json 的 entrypoints
//加载器按 ncmod.json 的 entry 字段找到这个类并调 Init
public sealed class ModApiEntry
{
    //Init 模组加载时调用一次
    //事件表是静态就绪的 注入探针可能在它之前就被内核触发 这里不做事件相关初始化
    public Task Init()
    {
        Log.Info("NetCraft-ModApi loaded");

        //启动参数要等内核解析 加载时只挂广播 解析完成由 SignalProbe 在事件发布前统一分发
        NcStartup.Initialize();

        //首个 tick 打一条日志后注销 用来确认注入探针与事件通路都已接通
        IDisposable? tickHandle = null;
        tickHandle = ServerEvents.Tick.Subscribe(args =>
        {
            Log.Debug($"NetCraft-ModApi event channel ready, first tick {args.TickCount}");
            tickHandle?.Dispose();
        });

        ServerEvents.Starting.Subscribe(_ => Log.Debug("NetCraft-ModApi received server starting"));
        ServerEvents.Started.Subscribe(_ => Log.Debug("NetCraft-ModApi received server started"));
        ServerEvents.Stopping.Subscribe(_ => Log.Debug("NetCraft-ModApi received server stopping"));
        ServerEvents.Stopped.Subscribe(_ => Log.Debug("NetCraft-ModApi received server stopped"));

        //新增四条挂点的自检 每条只留第一份就注销
        //挂点错位的表现是事件一直不来 而不是报错 留一行日志才能分辨这两种失败
        SelfCheck(NetworkEvents.PacketReceived,
            args => $"packet {args.Packet.GetType().Name} serverbound={args.IsServerbound}");
        SelfCheck(ServerEvents.LevelTick,
            args => $"level tick {args.Level.Dimension} runs={args.RunsNormally}");
        SelfCheck(ServerEvents.CommandExecuted,
            args => $"command {args.Command} result={args.Result}");
        SelfCheck(ServerEvents.SavedDataSaving, _ => "saved data flushed");
        SelfCheck(ServerEvents.BlockChanged, args => $"block changed {args.Pos}");
        SelfCheck(ServerEvents.BlockBroken, args => $"block broken {args.Pos}");
        SelfCheck(ServerEvents.ItemDropped, args => $"item dropped {args.Pos}");

        //命令注册时机挂一个自检命令 敲 /ncmapi 列出所有经由 ModApi 注册的命令
        ServerEvents.CommandRegister.Subscribe(args =>
        {
            args.Register("ncmapi", "列出经由 NetCraft-ModApi 注册的命令", builder =>
                builder.Requires(s => s.HasPermission(2))
                    .Executes(context =>
                    {
                        NcCommandRegistry.WriteList(context.GetSource());
                        return 1;
                    }));
            Log.Debug($"NetCraft-ModApi registered self-check command /ncmapi, ledger {NcCommandRegistry.Entries.Count} entries");
        });

        return Task.CompletedTask;
    }

    //SelfCheck 订阅一条事件 收到第一份就注销并打日志
    //回调里注销是安全的 分发时对回调表做了快照
    private static void SelfCheck<T>(NcEvent<T> evt, Func<T, string> describe)
    {
        IDisposable? handle = null;
        handle = evt.Subscribe(args =>
        {
            Log.Debug($"NetCraft-ModApi event channel ready, {describe(args)}");
            handle?.Dispose();
        });
    }
}
