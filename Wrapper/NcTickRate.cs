using System.Runtime.CompilerServices;
using NetCraft.Game.Server;

namespace NetCraft.ModApi.Wrapper;

//NcTickRate 刻速率句柄 每秒刻数 冻结 步进与加速跑都从它走
//实例由包装层内部构造 管理器只有一份 同一内核管理器始终对应同一句柄
public sealed class NcTickRate
{
    //Cache 内核管理器到句柄的映射 弱引用跟随内核对象生命周期
    private static readonly ConditionalWeakTable<ServerTickRateManager, NcTickRate> Cache = new();

    //_inner 内核管理器本体 私有 不进公开面
    private readonly ServerTickRateManager _inner;

    private NcTickRate(ServerTickRateManager inner) => _inner = inner;

    //Get 取句柄 同一内核管理器复用同一份
    internal static NcTickRate Get(ServerTickRateManager manager)
        => Cache.GetValue(manager, static m => new NcTickRate(m));

    //Inner 内核管理器本体 包装层内部取用 模组看不到
    internal ServerTickRateManager Inner => _inner;

    //Rate 当前每秒刻数
    public float Rate => _inner.TickRate;

    //SetRate 设每秒刻数 下限 1
    public void SetRate(float rate) => _inner.SetTickRate(rate);

    //IsFrozen 世界是否冻结 冻结下关卡照常 tick 实体与随机刻被过滤
    public bool IsFrozen => _inner.IsFrozen;

    //SetFrozen 冻结或解冻世界
    public void SetFrozen(bool frozen) => _inner.SetFrozen(frozen);

    //IsSprinting 是否在加速跑
    public bool IsSprinting => _inner.IsSprinting;

    //Sprint 请求加速跑指定刻数 加速期间主循环不睡 全速推进
    public bool Sprint(int ticks) => _inner.RequestGameToSprint(ticks);

    //StopSprint 提前结束加速跑
    public bool StopSprint() => _inner.StopSprinting();

    //Step 冻结状态下推进指定刻数 未冻结返回 false
    public bool Step(int ticks) => _inner.StepGameIfPaused(ticks);

    //StopStep 停止步进
    public bool StopStep() => _inner.StopStepping();
}
