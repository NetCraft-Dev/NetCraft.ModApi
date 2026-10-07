using System.Runtime.CompilerServices;
using System.Threading;
using NetCraft.ModApi.Wrapper;

namespace NetCraft.ModApi.Internal;

//SignalProbe 信号探针 所有"某件事发生了"的 Mark 钩子都汇到这一个方法
//标签在 ncmod.json 的 label 字段里写 这里按标签分发到对应事件
//签名只吃 string 装配期靠反射找方法时不会连带解析内核类型
//标 NoInlining 免得被内联进调用方后提前把内核类型解析出来
public static class SignalProbe
{
    private static long _serverTicks;
    private static long _clientTicks;

    //OnSignal 按标签分发信号
    //启动参数先派发 用户的就绪回调里要能读到参数 顺序反了就只看到一份空表
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnSignal(string label)
    {
        switch (label)
        {
            case "server_starting":
                NcStartup.MarkReady();
                ServerEvents.Starting.Publish(new ServerPhaseArgs { Phase = "starting" });
                break;
            case "server_tick":
                NcStartup.MarkReady();
                ServerEvents.Tick.Publish(new ServerTickArgs
                {
                    TickCount = Interlocked.Increment(ref _serverTicks),
                });
                break;
            case "server_started":
                NcStartup.MarkReady();
                ServerEvents.Started.Publish(new ServerPhaseArgs { Phase = "started" });
                break;
            case "server_stopping":
                ServerEvents.Stopping.Publish(new ServerPhaseArgs { Phase = "stopping" });
                break;
            case "client_tick":
                NcStartup.MarkReady();
                ClientEvents.Tick.Publish(new ClientTickArgs
                {
                    TickCount = Interlocked.Increment(ref _clientTicks),
                });
                break;
        }
    }
}
