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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnServerRun(object self)
    {
        var server = (MinecraftServer)self;
        NcServer.Capture(server);
        server.Run();
    }
}
