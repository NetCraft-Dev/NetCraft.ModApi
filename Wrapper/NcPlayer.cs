using System.Runtime.CompilerServices;
using NetCraft.Game.Server;
using NetCraft.Game.World.Level;

namespace NetCraft.ModApi.Wrapper;

//NcPlayer 在线玩家句柄
//把内核的 ServerPlayer 挡在公开面之后 内核改这个类型时模组不用重编译
//句柄只由探针构造 模组拿到的是只读视图 同一个内核玩家始终对应同一个句柄
public sealed class NcPlayer
{
    //Cache 内核玩家到句柄的映射 弱引用跟随内核对象生命周期 玩家下线后自动失效
    private static readonly ConditionalWeakTable<ServerPlayer, NcPlayer> Cache = new();

    //_inner 内核玩家本体 私有 不进公开面
    private readonly ServerPlayer _inner;

    private NcPlayer(ServerPlayer inner) => _inner = inner;

    //Get 取句柄 同一内核玩家复用同一份
    internal static NcPlayer Get(ServerPlayer player) => Cache.GetValue(player, static p => new NcPlayer(p));

    //Inner 内核玩家本体 包装层内部取用 模组看不到
    internal ServerPlayer Inner => _inner;

    //Name 玩家名
    public string Name => _inner.Profile.Name;

    //Uuid 玩家唯一标识
    public Guid Uuid => _inner.Profile.Id;

    //EntityId 实体 id 实体追踪与包同步都用它
    public int EntityId => _inner.EntityId;

    //X/Y/Z 当前坐标
    public double X => _inner.Position.X;
    public double Y => _inner.Position.Y;
    public double Z => _inner.Position.Z;

    //Yaw/Pitch 当前朝向
    public float Yaw => _inner.Yaw;
    public float Pitch => _inner.Pitch;

    //Health 当前血量
    public float Health => _inner.Health;

    //MaxHealth 血量上限
    public float MaxHealth => _inner.MaxHealth;

    //GameType 当前游戏模式
    public GameType GameType => _inner.GameType;

    //PermissionLevel 权限等级 0 到 4
    public int PermissionLevel => _inner.PermissionLevel;

    //HasPermission 是否达到指定权限等级
    public bool HasPermission(int level) => _inner.HasPermissions(level);

    //IsAlive 是否存活
    public bool IsAlive => !_inner.IsDeadOrDying;

    //IsSneaking 是否潜行
    public bool IsSneaking => _inner.IsSneaking;

    //IsSprinting 是否疾跑
    public bool IsSprinting => _inner.IsSprinting;
}
