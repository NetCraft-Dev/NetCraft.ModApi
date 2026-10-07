using System.Runtime.CompilerServices;
using NetCraft.Game.Server;
using NetCraft.ModApi.Wrapper;

namespace NetCraft.ModApi.Internal;

//ServerProbe 服务端实例探针
//CallSite 规则把内核启动主循环的那次调用换成这里的方法 原调用由探针自己补回去
//self 就是服务端本身 模组的查询与操作都从这一处拿到同一个实例
//签名只吃 object 装配期反射找方法时不会解析内核类型 理由同 PlayerProbe
public static class ServerProbe
{
    //OnServerRun 捕获服务端实例再放行原调用
    //捕获要排在 Run 之前 它方法体入口的 Mark 会立刻发 Started 事件 用户回调里就得能读到实例
    //Run 返回即主循环退出 用 finally 补发 Stopped 无论正常关停还是崩溃退出都不漏
    //Stopped 回调里只该做纯内存收尾 存档刷盘已在内核 Stop 里走完
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnServerRun(object self)
    {
        var server = (MinecraftServer)self;
        NcServer.Capture(server);
        try
        {
            server.Run();
        }
        finally
        {
            //先发事件再失效实例 回调里还可能读 NcServer 上的统计
            ServerEvents.Stopped.Publish(new ServerPhaseArgs { Phase = "stopped" });
            NcServer.Release();
        }
    }
}
