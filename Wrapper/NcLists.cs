using NetCraft.Game.Server;
using NetCraft.Network.Protocol.Login;

namespace NetCraft.ModApi.Wrapper;

//NcWhiteList 白名单句柄 落盘 whitelist.json
//每次取到的是同一内核名单的实时视图 改动即刻生效并写盘
public sealed class NcWhiteList
{
    private readonly WhiteList _inner;

    internal NcWhiteList(WhiteList inner) => _inner = inner;

    //Inner 内核名单本体 包装层内部取用 模组看不到
    internal WhiteList Inner => _inner;

    //Count 名单条目数
    public int Count => _inner.Count;

    //Names 名单里的名字快照
    public IReadOnlyList<string> Names => _inner.Names;

    //Contains 该档案是否在名单内
    public bool Contains(GameProfile profile) => _inner.IsAllowed(profile);

    //Add 加入名单并落盘 返回是否新增条目
    public bool Add(GameProfile profile) => _inner.Add(profile);

    //Remove 移出名单并落盘 返回是否命中条目
    public bool Remove(GameProfile profile) => _inner.Remove(profile);
}

//NcOpList 管理员名单句柄 落盘 ops.json
public sealed class NcOpList
{
    private readonly OpList _inner;

    internal NcOpList(OpList inner) => _inner = inner;

    //Inner 内核名单本体 包装层内部取用 模组看不到
    internal OpList Inner => _inner;

    //Count 名单条目数
    public int Count => _inner.Count;

    //Names 名单里的名字快照
    public IReadOnlyList<string> Names => _inner.Names;

    //Contains 该档案是否在名单内
    public bool Contains(GameProfile profile) => _inner.IsOp(profile);

    //PermissionLevel 该档案在名单里的权限等级 不在名单返回 0
    public int PermissionLevel(GameProfile profile) => _inner.GetPermissionLevel(profile);

    //Add 加入或更新名单并落盘 等级自动钳到 0 到 4 返回是否新增条目
    public bool Add(GameProfile profile, int level) => _inner.Add(profile, level);

    //Remove 移出名单并落盘 返回是否命中条目
    public bool Remove(GameProfile profile) => _inner.Remove(profile);
}

//NcBanList 玩家封禁名单句柄 落盘 banned-players.json
//只填名字不填 uuid 也能命中 离线模式下 uuid 随名字生成
public sealed class NcBanList
{
    private const string DefaultSource = "NetCraft-ModApi";

    private readonly BanList _inner;

    internal NcBanList(BanList inner) => _inner = inner;

    //Inner 内核名单本体 包装层内部取用 模组看不到
    internal BanList Inner => _inner;

    //IsBanned 该档案是否被封禁
    public bool IsBanned(GameProfile profile) => _inner.IsBanned(profile);

    //Ban 封禁该档案并落盘 reason 缺省用内核默认文案
    public bool Ban(GameProfile profile, string? reason = null)
        => _inner.Add(profile, DefaultSource, reason ?? BanList.DefaultReason);

    //Unban 解除封禁并落盘 返回是否命中记录
    public bool Unban(GameProfile profile) => _inner.Remove(profile);
}

//NcIpBanList IP 封禁名单句柄 落盘 banned-ips.json
public sealed class NcIpBanList
{
    private const string DefaultSource = "NetCraft-ModApi";

    private readonly IpBanList _inner;

    internal NcIpBanList(IpBanList inner) => _inner = inner;

    //Inner 内核名单本体 包装层内部取用 模组看不到
    internal IpBanList Inner => _inner;

    //IsBanned 该地址是否被封禁
    public bool IsBanned(string address) => _inner.IsBanned(address);

    //Ban 封禁该地址并落盘 reason 缺省用内核默认文案
    public bool Ban(string address, string? reason = null)
        => _inner.Add(address, DefaultSource, reason ?? IpBanList.DefaultReason);

    //Unban 解除封禁并落盘 返回是否命中记录
    public bool Unban(string address) => _inner.Remove(address);
}

//NcLists 名单入口
//名单本体是内核运行时那份实例 模组自己 new 一份会与内核互相覆盖写盘
public static class NcLists
{
    //WhiteList 白名单
    public static NcWhiteList WhiteList => new(NcServer.Require().WhiteList);

    //Ops 管理员名单
    public static NcOpList Ops => new(NcServer.Require().OpList);

    //Bans 玩家封禁名单
    public static NcBanList Bans => new(NcServer.Require().BanList);

    //IpBans IP 封禁名单
    public static NcIpBanList IpBans => new(NcServer.Require().IpBanList);

    //Settings 服务端配置视图 读写入口都在句柄上
    public static NcServerSettings Settings => NcServerSettings.Get(NcServer.Require().Settings);

    //IsOp 该档案是否在管理员名单里
    public static bool IsOp(GameProfile profile) => Ops.Contains(profile);

    //IsBanned 该档案是否被封禁
    public static bool IsBanned(GameProfile profile) => Bans.IsBanned(profile);

    //IsWhitelisted 该档案是否在白名单里
    public static bool IsWhitelisted(GameProfile profile) => WhiteList.Contains(profile);
}
