using NetCraft;
using NetCraft.Game.Server;
using NetCraft.Network.Protocol.Login;

namespace NetCraft.ModApi.Wrapper;

//NcLists 名单与配置入口
//暴露的是内核运行时那份名单实例 模组自己 new 一份会与内核互相覆盖写盘
public static class NcLists
{
    //WhiteList 白名单
    public static WhiteList WhiteList => NcServer.Require().WhiteList;

    //Ops 管理员名单
    public static OpList Ops => NcServer.Require().OpList;

    //Bans 玩家封禁名单
    public static BanList Bans => NcServer.Require().BanList;

    //IpBans IP 封禁名单
    public static IpBanList IpBans => NcServer.Require().IpBanList;

    //Settings 服务端配置 端口与人数上限这类只读 可写的只有下面四个 SetXxx
    public static ServerSettings Settings => NcServer.Settings;

    //IsOp 该档案是否在管理员名单里
    public static bool IsOp(GameProfile profile) => Ops.IsOp(profile);

    //IsBanned 该档案是否被封禁
    public static bool IsBanned(GameProfile profile) => Bans.IsBanned(profile);

    //IsWhitelisted 该档案是否在白名单里
    public static bool IsWhitelisted(GameProfile profile) => WhiteList.IsAllowed(profile);

    //SetGameMode 改默认游戏模式 只写内存 要落盘再调 SaveSettings
    public static void SetGameMode(string name) => Settings.SetGamemode(name);

    //SetDifficulty 改默认难度 只写内存 要落盘再调 SaveSettings
    public static void SetDifficulty(string name) => Settings.SetDifficulty(name);

    //SetPlayerIdleTimeout 改空闲踢出分钟数 0 表示不踢 只写内存
    public static void SetPlayerIdleTimeout(int minutes) => Settings.SetPlayerIdleTimeout(minutes);

    //SetWhiteList 改配置里的白名单开关 只写内存 运行时开关在 NcServer.WhiteListEnabled
    public static void SetWhiteList(bool enabled) => Settings.SetWhiteList(enabled);

    //SaveSettings 把当前配置写回 server.properties
    public static void SaveSettings() => Settings.SaveCurrent();
}
