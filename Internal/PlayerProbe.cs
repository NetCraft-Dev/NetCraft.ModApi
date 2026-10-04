using System.Reflection;
using System.Runtime.CompilerServices;
using NetCraft.Game.Network.Protocol.Game;
using NetCraft.Game.Server;
using NetCraft.ModApi.Wrapper;
using NetCraft.Network;
using NetCraft.Network.Chat;
using NetCraft.Network.Protocol.Login;

namespace NetCraft.ModApi.Internal;

//PlayerProbe 玩家事件探针
//CallSite 规则会把内核里对这几个方法的调用换成这里的方法
//原调用由探针自己补回去 内核行为因此一点没变 只是在外面包了一层事件
//签名只能用 BCL 类型 引用参数与返回值一律 object 值类型参数保留真身 否则栈上的表示对不上
//签名里出现内核类型会在装配期就被反射解析 那时代码还没注入 会把未改写的内核提前拉起来
public static class PlayerProbe
{
    //OnPlaceNewPlayer 玩家加入 满员被拒时原方法返回 null 那种情况不发事件
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? OnPlaceNewPlayer(object self, object connection, object profile)
    {
        var player = ((PlayerList)self).PlaceNewPlayer((Connection)connection, (GameProfile)profile);
        if (player is not null)
        {
            ServerEvents.PlayerJoin.Publish(new PlayerJoinArgs
            {
                Player = NcPlayer.Get(player),
                ProfileName = player.Profile.Name,
            });
        }
        return player;
    }

    //OnRemovePlayer 玩家离开 不论是否真的移出都发事件 结果由 Removed 说明
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool OnRemovePlayer(object self, object player)
    {
        var removed = ((PlayerList)self).RemovePlayer((ServerPlayer)player);
        ServerEvents.PlayerLeave.Publish(new PlayerLeaveArgs
        {
            Player = NcPlayer.Get((ServerPlayer)player),
            Removed = removed,
        });
        return removed;
    }

    //OnDisconnect 玩家断线 两个重载共用一个替换方法 它们的参数个数一样
    //参数可能是 string 也可能是 Component 补回原调用时要按真身分派
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnDisconnect(object self, object reason)
    {
        var player = (ServerPlayer)self;
        if (reason is Component component)
            player.Disconnect(component);
        else
            player.Disconnect((string)reason);

        ServerEvents.PlayerDisconnect.Publish(new PlayerDisconnectArgs
        {
            Player = NcPlayer.Get(player),
            Reason = reason is Component part ? part.GetString() : reason as string ?? "",
        });
    }

    //OnHurtPlayer 玩家受伤 原方法返回 false 表示无敌帧内或已死亡 那种情况不算一次受伤
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool OnHurtPlayer(object self, object target, object attacker, float amount)
    {
        var player = (ServerPlayer)target;
        var source = attacker as ServerPlayer;
        var hurt = ((PlayerList)self).HurtPlayer(player, source, amount);
        if (hurt)
        {
            ServerEvents.PlayerHurt.Publish(new PlayerHurtArgs
            {
                Player = NcPlayer.Get(player),
                Attacker = source is null ? null : NcPlayer.Get(source),
                Amount = amount,
            });
        }
        return hurt;
    }

    //OnRespawnPlayer 玩家死亡复位 内核在血量归零后立刻走这里 所以它等价于死亡事件
    //RespawnPlayer 是 private 内核没给模组开 InternalsVisibleTo 补回原调用只能反射进去
    //一次死亡才走一次 反射那点开销无所谓
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnRespawnPlayer(object self, object player, object attacker)
    {
        var victim = (ServerPlayer)player;
        var source = attacker as ServerPlayer;
        RespawnMethod?.Invoke(self, new object?[] { victim, source });
        ServerEvents.PlayerDeath.Publish(new PlayerDeathArgs
        {
            Player = NcPlayer.Get(victim),
            Attacker = source is null ? null : NcPlayer.Get(source),
        });
    }

    //RespawnMethod 首次调用时才解析 private 方法
    private static MethodInfo? RespawnMethod =>
        _respawn ??= typeof(PlayerList).GetMethod("RespawnPlayer",
            BindingFlags.NonPublic | BindingFlags.Instance);

    private static MethodInfo? _respawn;

    //OnChat 玩家聊天 原方法会广播 chat.type.text
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnChat(object self, object packet)
    {
        var listener = (ServerGamePacketListenerImpl)self;
        var chat = (ServerboundChatPacket)packet;
        listener.HandleChat(chat);
        if (!string.IsNullOrEmpty(chat.Message))
        {
            ServerEvents.PlayerChat.Publish(new PlayerChatArgs
            {
                SenderName = listener.Player?.Profile.Name ?? "",
                Message = chat.Message,
            });
        }
    }
}
