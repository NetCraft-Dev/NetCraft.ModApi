using System.Runtime.CompilerServices;
using NetCraft.ModApi.Wrapper;
using NetCraft.Primitives;
using NetCraft.Storage;

namespace NetCraft.ModApi.Internal;

//LevelProbe 区块生命周期探针
//挂点选在 ServerChunkCache 三个回调属性的赋值处 不是在读取处
//那三个属性是单播的 内核自己在 PersistentServerLevel 构造时注入了存档与实体逻辑
//模组若直接赋值会把内核那份顶掉 —— 卸载不落盘 方块实体也不清 属于静默破坏
//所以在赋值那一刻套一层包装委托 事件发完再转调内核原本的回调
//包装只在赋值时做一次 之后每次触发多一层转发 开销可忽略
//签名只用 BCL 类型 理由同 PlayerProbe
public static class LevelProbe
{
    //OnChunkLoadedAssigned 区块首次进内存
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnChunkLoadedAssigned(object self, object value)
    {
        var cache = (ServerChunkCache)self;
        var kernel = value as Action<ChunkPos>;
        cache.ChunkLoaded = pos =>
        {
            ServerEvents.ChunkLoaded.Publish(new ChunkLoadedArgs { X = pos.X, Z = pos.Z });
            kernel?.Invoke(pos);
        };
    }

    //OnChunkUnloadedAssigned 区块离开内存
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnChunkUnloadedAssigned(object self, object value)
    {
        var cache = (ServerChunkCache)self;
        var kernel = value as Action<ChunkPos>;
        cache.ChunkUnloaded = pos =>
        {
            ServerEvents.ChunkUnloaded.Publish(new ChunkUnloadedArgs { X = pos.X, Z = pos.Z });
            kernel?.Invoke(pos);
        };
    }

    //OnChunkSaveAssigned 区块落盘前的快照回调
    //内核约定这个回调里要同步取好快照 所以事件也在同一时刻发 回调里别做耗时操作
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnChunkSaveAssigned(object self, object value)
    {
        var cache = (ServerChunkCache)self;
        var kernel = value as Action<ChunkAccess>;
        cache.ChunkSaveSink = chunk =>
        {
            ServerEvents.ChunkSaved.Publish(new ChunkSavedArgs { X = chunk.Pos.X, Z = chunk.Pos.Z });
            kernel?.Invoke(chunk);
        };
    }

    //OnLevelTick 维度级 tick 每拍每个已装载维度各一次
    //先补回原调用再发事件 模组抛异常不至于把世界推进打断
    //高频 回调里别做耗时的事
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnLevelTick(object self, bool runsNormally)
    {
        var level = (PersistentServerLevel)self;
        level.Tick(runsNormally);
        ServerEvents.LevelTick.Publish(new LevelTickArgs
        {
            Level = level,
            RunsNormally = runsNormally,
        });
    }
}
