using System.Runtime.CompilerServices;
using NetCraft;

namespace NetCraft.ModApi.Wrapper;

//NcServerSettings 服务端配置句柄 server.properties 的运行时只读视图
//写路径只有下面那几个 Set 内核没给模组开的键不该从这里绕过去改
//实例由包装层内部构造 配置只有一份 同一内核配置始终对应同一句柄
public sealed class NcServerSettings
{
    //Cache 内核配置到句柄的映射 弱引用跟随内核对象生命周期
    private static readonly ConditionalWeakTable<ServerSettings, NcServerSettings> Cache = new();

    //_inner 内核配置本体 私有 不进公开面
    private readonly ServerSettings _inner;

    private NcServerSettings(ServerSettings inner) => _inner = inner;

    //Get 取句柄 同一内核配置复用同一份
    internal static NcServerSettings Get(ServerSettings settings)
        => Cache.GetValue(settings, static s => new NcServerSettings(s));

    //Inner 内核配置本体 包装层内部取用 模组看不到
    internal ServerSettings Inner => _inner;

    //Port 监听端口
    public int Port => _inner.ServerPort;

    //MaxPlayers 人数上限
    public int MaxPlayers => _inner.MaxPlayers;

    //LevelName 世界名
    public string LevelName => _inner.LevelName;

    //Gamemode 默认游戏模式 survival/creative/adventure/spectator
    public string Gamemode => _inner.Gamemode;

    //Difficulty 默认难度 peaceful/easy/normal/hard
    public string Difficulty => _inner.Difficulty;

    //OnlineMode 正版验证开关
    public bool OnlineMode => _inner.OnlineMode;

    //AllowPvp 是否允许 PVP
    public bool AllowPvp => _inner.AllowPvp;

    //AllowFlight 是否允许飞行
    public bool AllowFlight => _inner.AllowFlight;

    //ViewDistance 视距 单位 chunk
    public int ViewDistance => _inner.ViewDistance;

    //SimulationDistance 模拟距离 单位 chunk 决定真正参与 tick 的范围
    public int SimulationDistance => _inner.SimulationDistance;

    //Motd 服务器描述
    public string Motd => _inner.Motd;

    //WhiteListConfig 配置文件里的白名单开关 运行时开关看 NcServer.WhiteListEnabled
    public bool WhiteListConfig => _inner.WhiteList;

    //PlayerIdleTimeout 挂机踢出分钟数 0 表示不踢
    public int PlayerIdleTimeout => _inner.PlayerIdleTimeout;

    //NcLanguage 服务端面板与日志的语言码 nc 专属
    public string NcLanguage => _inner.NcLanguage;

    //SetGamemode 改默认游戏模式 只写内存 要落盘再调 Save
    public void SetGamemode(string name) => _inner.SetGamemode(name);

    //SetDifficulty 改默认难度 只写内存 要落盘再调 Save
    public void SetDifficulty(string name) => _inner.SetDifficulty(name);

    //SetPlayerIdleTimeout 改挂机踢出分钟数 只写内存 要落盘再调 Save
    public void SetPlayerIdleTimeout(int minutes) => _inner.SetPlayerIdleTimeout(minutes);

    //SetWhiteList 改配置里的白名单开关 只写内存 运行时开关在 NcServer.WhiteListEnabled
    public void SetWhiteList(bool enabled) => _inner.SetWhiteList(enabled);

    //Save 把当前配置写回 server.properties
    public void Save() => _inner.SaveCurrent();
}
