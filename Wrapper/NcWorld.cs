using NetCraft.Game.Server;
using NetCraft.Game.World.Clock;
using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Registry.State;
using NetCraft.Storage;

namespace NetCraft.ModApi.Wrapper;

//NcWorld 世界控制与方块操作入口
//不传关卡的成员一律作用于主世界 要操作别的维度用带 level 参数的重载
public static class NcWorld
{
    //Overworld 主世界
    public static PersistentServerLevel Overworld => NcServer.Overworld;

    //Nether 下界 未装载时为 null
    public static PersistentServerLevel? Nether => NcServer.Nether;

    //End 末地 未装载时为 null
    public static PersistentServerLevel? End => NcServer.End;

    //Get 按维度标识找关卡 例如 minecraft:the_nether 未装载时为 null
    public static PersistentServerLevel? Get(Identifier dimension)
        => NcServer.GetLevel(ResourceKey<Level>.Create(Registries.DIMENSION, dimension));

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

    //Clock 世界时钟管理器 时钟的暂停倍率与总刻数都从它走
    public static ServerClockManager Clock => NcServer.Require().ClockManager;

    //RainLevel 主世界雨量 取值 0 到 1
    public static float RainLevel
    {
        get => Overworld.RainLevel;
        set => Overworld.SetRainLevel(value);
    }

    //ThunderLevel 主世界雷量 取值 0 到 1
    public static float ThunderLevel
    {
        get => Overworld.ThunderLevel;
        set => Overworld.SetThunderLevel(value);
    }

    //IsRaining 主世界是否在下雨
    public static bool IsRaining => Overworld.IsRaining;

    //IsThundering 主世界是否在打雷
    public static bool IsThundering => Overworld.IsThundering;

    //Border 主世界世界边界 大小中心与伤害都从它改
    public static WorldBorder Border => Overworld.WorldBorder;

    //Entities 主世界当前载入的实体
    public static IEnumerable<Entity> Entities => Overworld.Entities;

    //GetBlock 读主世界指定坐标的方块状态 区块未加载返回 null
    public static BlockState? GetBlock(int x, int y, int z)
        => Overworld.GetBlockState(new BlockPos(x, y, z));

    //GetBlock 读指定关卡的方块状态 区块未加载返回 null
    public static BlockState? GetBlock(PersistentServerLevel level, BlockPos pos)
        => level.GetBlockState(pos);

    //SetBlock 写主世界方块 走完整联动链与客户端同步 返回是否真的发生变更
    public static bool SetBlock(BlockPos pos, BlockState state, bool notifyNeighbors = true)
        => ServerBlockUpdates.SetBlock(Overworld, NcServer.Players, pos, state, notifyNeighbors);

    //SetBlock 写指定关卡的方块
    public static bool SetBlock(PersistentServerLevel level, BlockPos pos, BlockState state, bool notifyNeighbors = true)
        => ServerBlockUpdates.SetBlock(level, NcServer.Players, pos, state, notifyNeighbors);

    //BreakBlock 破坏主世界方块 依次走破坏表现 掉落与破坏回调
    //player 为 null 表示非玩家因素破坏 不触发玩家破坏回调
    public static bool BreakBlock(BlockPos pos, NcPlayer? player = null)
        => ServerBlockUpdates.BreakBlock(Overworld, NcServer.Players, player?.Inner, pos);

    //GetBlockEntity 取主世界的方块实体 位置没有或类型不符返回 null
    public static T? GetBlockEntity<T>(BlockPos pos) where T : class
        => Overworld.GetBlockEntity<T>(pos);

    //PlaySound 在主世界的指定方块处播一个音效 以方块中心为坐标
    public static void PlaySound(SoundEvent sound, SoundSource source, BlockPos pos, float volume, float pitch)
        => Overworld.PlaySound(sound, source, pos, volume, pitch);

    //LevelEvent 广播一个关卡事件 破坏粒子用 ServerBlockUpdates.ParticleBlockBreak
    public static void LevelEvent(int kind, BlockPos pos, int data) => Overworld.LevelEvent(kind, pos, data);

    //FindBlock 按注册名找方块 例如 minecraft:stone 找不到返回 null
    //物品实体粒子这些别的类型看 NcRegistries
    public static Block? FindBlock(string id) => NcRegistries.FindBlock(id);

    //FindState 按注册名找方块的默认状态 找不到返回 null
    public static BlockState? FindState(string id) => NcRegistries.FindState(id);
}
