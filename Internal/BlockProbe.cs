using System.Runtime.CompilerServices;
using NetCraft.Game.Server;
using NetCraft.Game.World.Items;
using NetCraft.ModApi.Wrapper;
using NetCraft.Primitives;
using NetCraft.Registry.State;
using NetCraft.Storage;
using NetCraft.Storage.Updates;

namespace NetCraft.ModApi.Internal;

//BlockProbe 方块事件探针
//三条挂点都落在被注入的程序集里 Game 与 Storage
//参数里的 BlockPos/BlockState 是值类型 只能按真身声明 不能像引用类型那样退化成 object
//两者分别来自 Primitives 与 Registry 不在注入名单 装配期解析不会把待改写的那批提前拉起来
public static class BlockProbe
{
    //OnBlockChanged 方块状态已写进去 正要同步客户端
    //挂的是接口方法 实现方可能是服务端出口也可能是客户端关卡 探针按虚分派补回原调用
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnBlockChanged(object self, BlockPos pos, BlockState state)
    {
        ((IBlockUpdateSink)self).BlockChanged(pos, state);
        ServerEvents.BlockChanged.Publish(new BlockChangedArgs { Pos = pos, State = state });
    }

    //OnBreakBlock 破坏方块 只在真的换掉了才发事件
    //player 为 null 表示红石那类非玩家因素破坏
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool OnBreakBlock(object level, object players, object? player, BlockPos pos)
    {
        var changed = ServerBlockUpdates.BreakBlock((PersistentServerLevel)level, (PlayerList)players,
            player as ServerPlayer, pos);
        if (changed)
        {
            ServerEvents.BlockBroken.Publish(new BlockBrokenArgs
            {
                Pos = pos,
                Player = player is ServerPlayer sp ? NcPlayer.Get(sp) : null,
            });
        }
        return changed;
    }

    //OnSpawnDrop 掉落物实体生成 空物品堆不生成实体也就没有事件
    //先补回原调用再发事件 模组抛异常不至于把掉落打断
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnSpawnDrop(object level, BlockPos pos, object stack)
    {
        var item = (ItemStack)stack;
        ServerBlockUpdates.SpawnDrop((PersistentServerLevel)level, pos, item);
        if (!item.IsEmpty())
            ServerEvents.ItemDropped.Publish(new ItemDroppedArgs { Pos = pos, Stack = item });
    }
}
