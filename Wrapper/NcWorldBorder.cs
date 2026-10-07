using System.Runtime.CompilerServices;
using NetCraft.Storage;

namespace NetCraft.ModApi.Wrapper;

//NcWorldBorder 世界边界句柄 尺寸 中心 伤害与警告参数都从它走
//边界随存档持久化 改动即刻生效并同步客户端
public sealed class NcWorldBorder
{
    //Cache 内核边界到句柄的映射 弱引用跟随内核对象生命周期
    private static readonly ConditionalWeakTable<WorldBorder, NcWorldBorder> Cache = new();

    //_inner 内核边界本体 私有 不进公开面
    private readonly WorldBorder _inner;

    private NcWorldBorder(WorldBorder inner) => _inner = inner;

    //Get 取句柄 同一内核边界复用同一份
    internal static NcWorldBorder Get(WorldBorder border)
        => Cache.GetValue(border, static b => new NcWorldBorder(b));

    //Inner 内核边界本体 包装层内部取用 模组看不到
    internal WorldBorder Inner => _inner;

    //Size 当前边界边长
    public double Size => _inner.GetSize();

    //SetSize 设边界边长 立即生效
    public void SetSize(double size) => _inner.SetSize(size);

    //CenterX 边界中心 X
    public double CenterX => _inner.CenterX;

    //CenterZ 边界中心 Z
    public double CenterZ => _inner.CenterZ;

    //SetCenter 设边界中心
    public void SetCenter(double x, double z) => _inner.SetCenter(x, z);

    //MinX/MaxX/MinZ/MaxZ 边界四至
    public double MinX => _inner.GetMinX();
    public double MaxX => _inner.GetMaxX();
    public double MinZ => _inner.GetMinZ();
    public double MaxZ => _inner.GetMaxZ();

    //DamagePerBlock 每格越界伤害
    public double DamagePerBlock
    {
        get => _inner.DamagePerBlock;
        set => _inner.SetDamagePerBlock(value);
    }

    //SafeZone 越界免伤缓冲格数
    public double SafeZone
    {
        get => _inner.SafeZone;
        set => _inner.SetSafeZone(value);
    }

    //WarningTime 越界预警提前刻数
    public int WarningTime
    {
        get => _inner.WarningTime;
        set => _inner.SetWarningTime(value);
    }

    //WarningBlocks 越界预警距离
    public int WarningBlocks
    {
        get => _inner.WarningBlocks;
        set => _inner.SetWarningBlocks(value);
    }

    //AbsoluteMaxSize 边界绝对尺寸上限
    public int AbsoluteMaxSize
    {
        get => _inner.AbsoluteMaxSize;
        set => _inner.SetAbsoluteMaxSize(value);
    }

    //Contains 坐标是否在边界内
    public bool Contains(double x, double z) => _inner.IsWithinBounds(x, z);

    //DistanceTo 边界到指定坐标最近一条边的距离 边界外为负
    public double DistanceTo(double x, double z) => _inner.GetDistanceToBorder(x, z);
}
