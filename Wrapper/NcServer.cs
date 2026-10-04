using NetCraft;
using NetCraft.Game.Commands;
using NetCraft.Game.Server;
using NetCraft.Game.World.Level;
using NetCraft.Network;
using NetCraft.Network.Chat;
using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Storage;

namespace NetCraft.ModApi.Wrapper;

//NcServer 服务端查询入口
//这一列 API 与 Minecraft 的模组生态无关 是 NetCraft 自己的服务端门面
//实例由 ServerProbe 在内核启动主循环时捕获 那之前所有成员都不可用
//未就绪时 Require 抛异常 想先判可用性用 IsAvailable
public static class NcServer
{
    private static MinecraftServer? _current;

    //Current 当前服务端实例 未就绪时为 null
    public static MinecraftServer? Current => Volatile.Read(ref _current);

    //IsAvailable 服务端是否已就绪
    public static bool IsAvailable => Current is not null;

    //Require 取服务端实例 未就绪直接抛 免得调用方到处判空
    public static MinecraftServer Require()
        => Current ?? throw new InvalidOperationException("服务端尚未就绪 请在服务端事件回调里访问");

    //Capture 由探针在内核启动主循环时调一次
    internal static void Capture(MinecraftServer server) => Volatile.Write(ref _current, server);

    //Players 在线玩家名单 包装层内部取用 模组走 NcPlayers
    internal static PlayerList Players => Require().PlayerList;

    //Settings 服务端配置
    public static ServerSettings Settings => Require().Settings;

    //Commands 命令管理器
    public static CommandManager Commands => Require().Commands;

    //TickRate 刻速率管理器 每秒刻数冻结世界与加速跑都从它走
    public static ServerTickRateManager TickRate => Require().TickRate;

    //EntityTracker 实体追踪 查某个实体对哪些玩家可见
    public static EntityTracker EntityTracker => Require().EntityTracker;

    //CommandStorage 命令方块与告示牌命令的持久存储
    public static CommandStorage CommandStorage => Require().CommandStorage;

    //GameRules 游戏规则 查与改都走它
    public static GameRuleMapData GameRules => Require().GameRules;

    //PlayerData 玩家数据的读写入口
    public static PlayerDataStorage PlayerData => Require().PlayerData;

    //Stopwatches 计时器面板的数据源
    public static Stopwatches Stopwatches => Require().Stopwatches;

    //DebugPlayers 调试假人管理
    public static DebugPlayerManager DebugPlayers => Require().DebugPlayers;

    //Connections 当前全部连接 含未进入游戏的连接
    public static IReadOnlyList<Connection> Connections => Require().Connections;

    //BlockEntities 方块实体集合
    public static BlockEntityManager BlockEntities => Require().BlockEntities;

    //Overworld 主世界
    public static NcLevel Overworld => NcLevel.Get(Require().Overworld);

    //Nether 下界 未装载时为 null
    public static NcLevel? Nether => Wrap(Require().GetLevel(LevelKeys.NETHER));

    //End 末地 未装载时为 null
    public static NcLevel? End => Wrap(Require().GetLevel(LevelKeys.END));

    //GetLevel 按维度键取内核关卡 包装层内部取用
    internal static PersistentServerLevel? GetLevel(ResourceKey<Level> key) => Require().GetLevel(key);

    //FindLevel 按维度标识找关卡 模组走 NcWorld.Get
    internal static NcLevel? FindLevel(string dimension)
        => Wrap(GetLevel(ResourceKey<Level>.Create(Registries.DIMENSION, Identifier.Parse(dimension))));

    //Wrap 内核关卡包成句柄 null 原样透传
    private static NcLevel? Wrap(PersistentServerLevel? level) => level is null ? null : NcLevel.Get(level);

    //Running 服务端是否在主循环中
    public static bool Running => Require().Running;

    //TickCount 本次启动以来的累计 tick 数
    public static long TickCount => Require().TickCount;

    //AverageTickTimeNanos 最近样本的平均单拍耗时
    public static long AverageTickTimeNanos => Require().AverageTickTimeNanos;

    //Tps 当前每秒刻数 由平均单拍耗时折算 上限是目标值
    public static double Tps
    {
        get
        {
            var nanos = Require().AverageTickTimeNanos;
            return nanos <= 0
                ? MinecraftServer.TargetTps
                : Math.Min(MinecraftServer.TargetTps, 1_000_000_000.0 / nanos);
        }
    }

    //WorldSeed 世界种子
    public static long WorldSeed => Require().WorldSeed;

    //SpawnPos 世界出生点
    public static Vec3 SpawnPos => Require().SpawnPos;

    //SetSpawnPos 设置世界出生点
    public static void SetSpawnPos(Vec3 pos) => Require().SetSpawnPos(pos);

    //DefaultGameType 默认游戏模式
    public static GameType DefaultGameType => Require().DefaultGameType;

    //SetDefaultGameType 设置默认游戏模式 只影响之后加入的玩家
    public static void SetDefaultGameType(GameType gameType) => Require().SetDefaultGameType(gameType);

    //WhiteListEnabled 白名单开关
    public static bool WhiteListEnabled
    {
        get => Require().IsWhiteListEnabled;
        set => Require().IsWhiteListEnabled = value;
    }

    //SavingEnabled 是否允许存档
    public static bool SavingEnabled
    {
        get => Require().IsSavingEnabled;
        set => Require().SetSavingEnabled(value);
    }

    //SetWeather 设置天气 clearTime 与 rainTime 是持续刻数
    public static void SetWeather(int clearTime, int rainTime, bool raining, bool thundering)
        => Require().SetWeatherParameters(clearTime, rainTime, raining, thundering);

    //SaveAll 立即全量存盘
    public static void SaveAll() => Require().SaveAllNow();

    //Broadcast 给全部在线玩家广播一条系统消息
    public static void Broadcast(string message) => Broadcast(Component.Literal(message));

    //Broadcast 给全部在线玩家广播一条组件消息
    public static void Broadcast(Component message) => Players.BroadcastSystemMessage(message, false);

    //Execute 以控制台身份执行一条命令 命令可不带前导斜杠 返回值即命令的返回值
    public static int Execute(string command)
    {
        var server = Require();
        return server.Commands.Execute(ServerCommandSource.Console(server), command);
    }

    //Execute 以指定玩家身份执行一条命令 权限按该玩家算
    public static int Execute(NcPlayer player, string command)
        => Require().Commands.Execute(player.Inner, command);
}
