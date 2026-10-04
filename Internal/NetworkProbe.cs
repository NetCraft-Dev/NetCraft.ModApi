using System.Runtime.CompilerServices;
using NetCraft.ModApi.Wrapper;
using NetCraft.Network;

namespace NetCraft.ModApi.Internal;

//NetworkProbe 网络收包探针
//挂 PacketProcessor 的两个入口 它们互补 一个入站包只会走其中一条
//ScheduleIfPossible 是排进主线程队列的那条 游戏阶段走它
//HandleNow 是当场处理的 握手与状态阶段走它
//两条都挂才覆盖全部入站包 比逐个挂 HandleXxx 省得多
//泛型重载内部转调非泛型版 两处调用点都会被换成本方法 但只会发一次事件
public static class NetworkProbe
{
    //OnScheduleIfPossible 包排进处理队列 此刻还没进业务层
    //高频 每拍可能有几十颗 回调里别做耗时的事
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnScheduleIfPossible(object self, object listener, object packet)
    {
        ((PacketProcessor)self).ScheduleIfPossible(listener, packet);
        Publish(listener, packet);
    }

    //OnHandleNow 包在调用线程直接处理 握手与状态阶段走这条
    //这段时期主循环还没起步 走队列的话包要等到第一拍才被处理 会卡住客户端
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnHandleNow(object self, object listener, object packet)
    {
        ((PacketProcessor)self).HandleNow(listener, packet);
        Publish(listener, packet);
    }

    //Publish 两条路径共用一份事件发布 包对象与监听器原样透传
    private static void Publish(object listener, object packet)
        => NetworkEvents.PacketReceived.Publish(new PacketReceivedArgs
        {
            Listener = listener,
            Packet = packet,
            IsServerbound = listener is ServerboundPacketListener,
        });
}
