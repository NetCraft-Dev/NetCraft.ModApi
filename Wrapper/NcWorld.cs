using NetCraft.Game.Server;
using NetCraft.Game.World.Clock;
using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Registry.State;

namespace NetCraft.ModApi.Wrapper;

//NcWorld 世界控制与方块操作入口
//不传关卡的成员一律作用于主世界 要操作别的维度用带 level 参数的重载
//坐标一律拆成 x y z 三个 int 公开面上不出现 BlockPos
public static class NcWorld
{
    //Overworld 主世界
    public static NcLevel Overworld => NcServer.Overworld;

    //Nether 下界 未装载时为 null
    public static NcLevel? Nether => NcServer.Nether;

    //End 末地 未装载时为 null
    public static NcLevel? End => NcServer.End;

    //Get 按维度标识找关卡 例如 minecraft:the_nether 未装载时为 null
    public static NcLevel? Get(string dimension) => NcServer.FindLevel(dimension);

    //DayTime 主世界白天时间 0 到 23999 循环
    public static long DayTime
    {
        get => Overworld.DayTime;
        set => Overworld.DayTime = value;
    }

    //GameTime 主世界游戏总时间
    public static long GameTime
    {
        get => Overworld.GameTime;
        set => Overworld.GameTime = value;
    }

    //Clock 世界时钟管理器 内核多时钟键控实体 时钟批次再定公开面
    internal static ServerClockManager Clock => NcServer.Require().ClockManager;

    //RainLevel 主世界雨量 取值 0 到 1
    public static float RainLevel
    {
        get => Overworld.RainLevel;
        set => Overworld.RainLevel = value;
    }

    //ThunderLevel 主世界雷量 取值 0 到 1
    public static float ThunderLevel
    {
        get => Overworld.ThunderLevel;
        set => Overworld.ThunderLevel = value;
    }

    //IsRaining 主世界是否在下雨
    public static bool IsRaining => Overworld.IsRaining;

    //IsThundering 主世界是否在打雷
    public static bool IsThundering => Overworld.IsThundering;

    //Border 主世界世界边界 大小中心与伤害都从它改
    public static NcWorldBorder Border => NcWorldBorder.Get(Overworld.Inner.WorldBorder);

    //Entities 主世界当前载入的实体 内核易变类型 实体批次再定公开面
    internal static IEnumerable<Entity> Entities => Overworld.Inner.Entities;

    //GetBlock 读主世界指定坐标的方块状态 区块未加载返回 null
    public static BlockState? GetBlock(int x, int y, int z) => GetBlock(Overworld, x, y, z);

    //GetBlock 读指定关卡的方块状态 区块未加载返回 null
    public static BlockState? GetBlock(NcLevel level, int x, int y, int z)
        => level.Inner.GetBlockState(new BlockPos(x, y, z));

    //SetBlock 写主世界方块 走完整联动链与客户端同步 返回是否真的发生变更
    public static bool SetBlock(int x, int y, int z, BlockState state, bool notifyNeighbors = true)
        => SetBlock(Overworld, x, y, z, state, notifyNeighbors);

    //SetBlock 写指定关卡的方块
    public static bool SetBlock(NcLevel level, int x, int y, int z, BlockState state, bool notifyNeighbors = true)
        => ServerBlockUpdates.SetBlock(level.Inner, NcServer.Players, new BlockPos(x, y, z), state, notifyNeighbors);

    //BreakBlock 破坏主世界方块 依次走破坏表现 掉落与破坏回调
    //player 为 null 表示非玩家因素破坏 不触发玩家破坏回调
    public static bool BreakBlock(int x, int y, int z, NcPlayer? player = null)
        => ServerBlockUpdates.BreakBlock(Overworld.Inner, NcServer.Players, player?.Inner, new BlockPos(x, y, z));

    //GetBlockEntity 取主世界的方块实体 内核易变类型 方块实体批次再定公开面
    internal static T? GetBlockEntity<T>(int x, int y, int z) where T : class
        => Overworld.Inner.GetBlockEntity<T>(new BlockPos(x, y, z));

    //PlaySound 在主世界的指定方块处播一个音效 以方块中心为坐标 内核易变类型 声音批次再定公开面
    internal static void PlaySound(SoundEvent sound, SoundSource source, int x, int y, int z, float volume, float pitch)
        => Overworld.Inner.PlaySound(sound, source, new BlockPos(x, y, z), volume, pitch);

    //LevelEvent 广播一个关卡事件 破坏粒子用 ServerBlockUpdates.ParticleBlockBreak
    public static void LevelEvent(int kind, int x, int y, int z, int data)
        => Overworld.Inner.LevelEvent(kind, new BlockPos(x, y, z), data);

    //FindBlock 按注册名找方块 例如 minecraft:stone 找不到返回 null
    //物品实体粒子这些别的类型看 NcRegistries
    public static Block? FindBlock(string id) => NcRegistries.FindBlock(id);

    //FindState 按注册名找方块的默认状态 找不到返回 null
    public static BlockState? FindState(string id) => NcRegistries.FindState(id);
}
