using NetCraft.Game.World.Level;
using NetCraft.Network.Chat;
using NetCraft.Primitives;

namespace NetCraft.ModApi.Wrapper;

//NcPlayers 玩家操作入口
//查到的与改的都是内核那份在线名单 模组自己造一个句柄不产生任何效果
public static class NcPlayers
{
    //All 当前在线玩家
    public static IReadOnlyList<NcPlayer> All
    {
        get
        {
            var players = NcServer.Players.Players;
            var result = new NcPlayer[players.Count];
            for (var i = 0; i < players.Count; i++)
                result[i] = NcPlayer.Get(players[i]);
            return result;
        }
    }

    //Count 在线人数
    public static int Count => NcServer.Players.PlayerCount;

    //Max 人数上限
    public static int Max => NcServer.Players.MaxPlayers;

    //Find 按名字查在线玩家 忽略大小写 不在线返回 null
    public static NcPlayer? Find(string name)
    {
        var player = NcServer.Players.GetPlayerByName(name);
        return player is null ? null : NcPlayer.Get(player);
    }

    //Find 按实体 id 查在线玩家 不在线返回 null
    public static NcPlayer? Find(int entityId)
    {
        var player = NcServer.Players.GetPlayerByEntityId(entityId);
        return player is null ? null : NcPlayer.Get(player);
    }

    //Send 给该玩家发一条系统聊天消息
    public static void Send(NcPlayer player, string message)
        => player.Inner.SendSystemMessage(Component.Literal(message));

    //SendOverlay 给该玩家发一条栏上提示消息
    public static void SendOverlay(NcPlayer player, string message)
        => player.Inner.SendOverlayMessage(Component.Literal(message));

    //Kick 踢出该玩家 会发断连包并关闭连接
    public static void Kick(NcPlayer player, string reason) => player.Inner.Disconnect(reason);

    //SetGameMode 改游戏模式并同步客户端
    public static void SetGameMode(NcPlayer player, GameType gameType)
        => NcServer.Players.ChangeGameMode(player.Inner, gameType);

    //SetPermissionLevel 设权限等级 自动钳到 0 到 4 并同步客户端
    public static void SetPermissionLevel(NcPlayer player, int level)
        => NcServer.Players.ApplyPermissionLevel(player.Inner, level);

    //Teleport 传送到指定坐标 朝向一并给出 玩家尚未进入游戏时什么也不做
    public static void Teleport(NcPlayer player, double x, double y, double z, float yaw, float pitch)
        => player.Inner.Listener?.Teleport(new Vec3(x, y, z), yaw, pitch, 0, 0, 0, 0f, 0f, 0);

    //TeleportTo 只给坐标 朝向保持当前值
    public static void TeleportTo(NcPlayer player, double x, double y, double z)
        => Teleport(player, x, y, z, player.Yaw, player.Pitch);

    //SetHealth 设血量并同步客户端 值会被钳到 0 与血量上限之间
    public static void SetHealth(NcPlayer player, float value)
    {
        player.Inner.SetHealth(value);
        NcServer.Players.SyncHealth(player.Inner);
    }

    //Heal 回满血并同步客户端
    public static void Heal(NcPlayer player) => SetHealth(player, player.MaxHealth);

    //Hurt 造成伤害并同步 返回是否真的造成了伤害 无敌帧内与已死亡为 false
    //走这条路径会照常触发 PlayerHurt 事件
    public static bool Hurt(NcPlayer player, float amount, NcPlayer? attacker = null)
        => NcServer.Players.HurtPlayer(player.Inner, attacker?.Inner, amount);
}
