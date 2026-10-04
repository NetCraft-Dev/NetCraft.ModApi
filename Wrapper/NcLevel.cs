using System.Runtime.CompilerServices;
using NetCraft.Storage;

namespace NetCraft.ModApi.Wrapper;

//NcLevel 维度句柄
//把内核的 PersistentServerLevel 挡在公开面之后 内核改这个类型时模组不用重编译
//句柄由包装层内部构造 同一个内核关卡始终对应同一个句柄
public sealed class NcLevel
{
    //Cache 内核关卡到句柄的映射 弱引用跟随内核对象生命周期 关卡卸载后自动失效
    private static readonly ConditionalWeakTable<PersistentServerLevel, NcLevel> Cache = new();

    //_inner 内核关卡本体 私有 不进公开面
    private readonly PersistentServerLevel _inner;

    private NcLevel(PersistentServerLevel inner) => _inner = inner;

    //Get 取句柄 同一内核关卡复用同一份
    internal static NcLevel Get(PersistentServerLevel level) => Cache.GetValue(level, static l => new NcLevel(l));

    //Inner 内核关卡本体 包装层内部取用 模组看不到
    internal PersistentServerLevel Inner => _inner;

    //Dimension 维度标识 形如 minecraft:overworld
    public string Dimension => _inner.Dimension.ToString();

    //DayTime 白天时间 0 到 23999 循环
    public long DayTime
    {
        get => _inner.DayTime;
        set => _inner.DayTime = value;
    }

    //GameTime 游戏总时间
    public long GameTime
    {
        get => _inner.GameTime;
        set => _inner.GameTime = value;
    }

    //RainLevel 雨量 取值 0 到 1
    public float RainLevel
    {
        get => _inner.RainLevel;
        set => _inner.SetRainLevel(value);
    }

    //ThunderLevel 雷量 取值 0 到 1
    public float ThunderLevel
    {
        get => _inner.ThunderLevel;
        set => _inner.SetThunderLevel(value);
    }

    //IsRaining 是否在下雨 只有能下雨的维度才为真
    public bool IsRaining => _inner.IsRaining;

    //IsThundering 是否在打雷
    public bool IsThundering => _inner.IsThundering;

    //MinBuildHeight 最低可建造高度
    public int MinBuildHeight => _inner.MinBuildHeight;

    //MaxBuildHeight 最高可建造高度
    public int MaxBuildHeight => _inner.MaxBuildHeight;

    //Ticks 本维度累计推进的刻数
    public long Ticks => _inner.LevelTick;

    //ForceLoadChunk 强制加载区块 返回是否发生变更
    public bool ForceLoadChunk(int x, int z) => _inner.SetChunkForced(x, z, true);

    //UnloadForceLoadedChunk 取消强制加载 返回是否发生变更
    public bool UnloadForceLoadedChunk(int x, int z) => _inner.SetChunkForced(x, z, false);
}
