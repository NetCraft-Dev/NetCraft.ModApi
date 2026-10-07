namespace NetCraft.ModApi.Wrapper;

using NetCraft.Commands;
using NetCraft.Commands.Builder;
using NetCraft.Game.Server;
using NetCraft.Game.World.Items;
using NetCraft.ModApi.Internal;
using NetCraft.Primitives;
using NetCraft.Registry.State;

//ServerTickArgs 服务器 tick 事件参数
public sealed class ServerTickArgs
{
    //TickCount 自本次启动以来的 tick 计数
    public long TickCount { get; init; }
}

//ClientTickArgs 客户端 tick 事件参数
public sealed class ClientTickArgs
{
    //TickCount 自本次启动以来的 tick 计数
    public long TickCount { get; init; }
}

//ServerPhaseArgs 服务端生命周期节点参数
public sealed class ServerPhaseArgs
{
    //Phase 节点名 starting started stopping stopped
    public required string Phase { get; init; }
}

//CommandRegisterArgs 命令注册事件参数
//内核内置命令全部注册完的那一刻触发 模组拿分派器挂自己的命令
public sealed class CommandRegisterArgs
{
    //Dispatcher 命令分派器 对应原版 Commands 的那个
    public required CommandDispatcher<CommandSourceStack> Dispatcher { get; init; }

    //Register 模组注册命令的入口
    //除了挂到分派器上 还会登记进 ModApi 的账本 /ncmapi 就是照这份账本列的
    //name 命令字面量 不带斜杠
    //description 一句话说明 会随账本显示出来
    //build 继续往上挂参数与执行体 权限谓词也在这里加
    public void Register(string name, string description, Action<LiteralArgumentBuilder<CommandSourceStack>> build)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(build);
        var builder = LiteralArgumentBuilder<CommandSourceStack>.Literal(name);
        build(builder);
        var node = Dispatcher.Register(builder);
        NcCommandRegistry.Add(name, description, node);
    }
}

//CommandExecutedArgs 命令执行事件参数
//只在执行完发一次 不带"开始执行"那半段 命令都是同步跑完的 分两次反而要模组自己配状态
public sealed class CommandExecutedArgs
{
    //Command 命令原文 聊天命令不带斜杠 控制台与代码发起的一般也不带
    public required string Command { get; init; }

    //Result 命令返回值 0 表示失败或被拒 具体含义由各命令自己定
    public required int Result { get; init; }

    //Source 命令来源 玩家命令源与控制台源都在这里
    //玩家重载进来的那次调用在内部才建命令源 事件里拿不到 那种情况只有 Player 有值
    public CommandSourceStack? Source { get; init; }

    //Player 发起命令的玩家 控制台发起时为 null
    public NcPlayer? Player { get; init; }
}

//PlayerJoinArgs 玩家加入事件参数
public sealed class PlayerJoinArgs
{
    //Player 刚加入的玩家 入场包序列已经发完 状态可安全读
    public required NcPlayer Player { get; init; }

    //ProfileName 玩家名
    public required string ProfileName { get; init; }
}

//PlayerLeaveArgs 玩家离开事件参数
public sealed class PlayerLeaveArgs
{
    //Player 离开的玩家 此刻已不在在线列表里
    public required NcPlayer Player { get; init; }

    //Removed 是否真的从列表里移出 重复移除时为 false
    public required bool Removed { get; init; }
}

//PlayerDisconnectArgs 玩家断线事件参数
//断连包已发 连接已关 这里拿到的玩家对象只用于读状态
public sealed class PlayerDisconnectArgs
{
    //Player 断线的玩家
    public required NcPlayer Player { get; init; }

    //Reason 断开理由 组件形式取的是其明文
    public required string Reason { get; init; }
}

//PlayerHurtArgs 玩家受伤事件参数
public sealed class PlayerHurtArgs
{
    //Player 受伤的玩家
    public required NcPlayer Player { get; init; }

    //Attacker 造成伤害的玩家 环境伤害与命令伤害为 null
    public NcPlayer? Attacker { get; init; }

    //Amount 本次伤害量 无敌帧内与已死亡不会触发本事件
    public required float Amount { get; init; }
}

//PlayerDeathArgs 玩家死亡事件参数
//内核在血量归零后立刻复位 本事件即死亡时刻 此刻玩家已满血回到重生点
public sealed class PlayerDeathArgs
{
    //Player 死亡的玩家
    public required NcPlayer Player { get; init; }

    //Attacker 凶手 无凶手时为 null
    public NcPlayer? Attacker { get; init; }
}

//PlayerChatArgs 玩家聊天事件参数
public sealed class PlayerChatArgs
{
    //SenderName 发送者名字
    public required string SenderName { get; init; }

    //Message 消息明文
    public required string Message { get; init; }
}

//ChunkLoadedArgs 区块加载事件参数
public sealed class ChunkLoadedArgs
{
    //X 区块坐标 X
    public required int X { get; init; }

    //Z 区块坐标 Z
    public required int Z { get; init; }
}

//ChunkUnloadedArgs 区块卸载事件参数
public sealed class ChunkUnloadedArgs
{
    //X 区块坐标 X
    public required int X { get; init; }

    //Z 区块坐标 Z
    public required int Z { get; init; }
}

//ChunkSavedArgs 区块落盘事件参数
//触发时内核刚取好快照 真正写盘是异步的 事件里拿不到"写完"的时刻
public sealed class ChunkSavedArgs
{
    //X 区块坐标 X
    public required int X { get; init; }

    //Z 区块坐标 Z
    public required int Z { get; init; }
}

//LevelTickArgs 维度级 tick 事件参数
//已装载的维度各发一次 主世界在关卡表里排第一
public sealed class LevelTickArgs
{
    //Level 本拍推进的维度
    public required NcLevel Level { get; init; }

    //RunsNormally 本拍是否正常推进 执行过 /tick freeze 时为 false
    //冻结下关卡照样 tick 只是实体与随机刻被过滤 所以本事件在冻结期间仍会触发
    public required bool RunsNormally { get; init; }
}

//SavedDataSavingArgs 存档数据落盘事件参数
//数据是同步写完才发这个事件的 回调里读到的就是最终文件内容
public sealed class SavedDataSavingArgs
{
    //目前不携带内容 存储批次再补 存储表本体是内核易变类型不进公开面
}

//PacketReceivedArgs 收包事件参数
//包刚排进处理器 还没进业务层 这里既拿不到解码后的结果也改不了包
//真要用包内容得自己按类型判断 内核对包的解码在排队的另一端
public sealed class PacketReceivedArgs
{
    //Listener 收这个包的监听器 握手/状态/配置/游戏四个阶段各有各的实现
    public required object Listener { get; init; }

    //Packet 包对象本体 类型是各阶段自己的包类型
    public required object Packet { get; init; }

    //IsServerbound 是否服务端收到的包 客户端收到的为 false
    public required bool IsServerbound { get; init; }
}

//BlockChangedArgs 方块状态变更事件参数
//触发在状态已经写进去、正要同步客户端那一刻 这里改不了变更本身
public sealed class BlockChangedArgs
{
    //Pos 发生变更的方块位置
    public required BlockPos Pos { get; init; }

    //State 变更后的方块状态
    public required BlockState State { get; init; }
}

//BlockBrokenArgs 方块被破坏事件参数
//只在真的换掉了才发 空位置与被拒绝的破坏不会触发
public sealed class BlockBrokenArgs
{
    //Pos 被破坏的方块位置
    public required BlockPos Pos { get; init; }

    //Player 破坏者 红石那类非玩家因素破坏时为 null
    public NcPlayer? Player { get; init; }
}

//ItemDroppedArgs 掉落物生成事件参数
//方块破坏掉落与篝火烤成品都走这里 空物品堆不生成实体也就没有事件
public sealed class ItemDroppedArgs
{
    //Pos 掉落物出现的位置 按方块中心算
    public required BlockPos Pos { get; init; }

    //Stack 掉落的那份物品堆
    public required ItemStack Stack { get; init; }
}

//ServerEvents 服务端事件
public static class ServerEvents
{
    //Tick 服务器主循环每 tick 触发一次 数据来自挂在 DedicatedServer.Tick 上的注入探针
    public static readonly NcEvent<ServerTickArgs> Tick = new();

    //Starting 服务端开始启动 世界尚未加载 完成于 Started
    public static readonly NcEvent<ServerPhaseArgs> Starting = new();

    //Started 服务端启动完成 端口已开始监听
    public static readonly NcEvent<ServerPhaseArgs> Started = new();

    //Stopping 服务端开始关闭 玩家连接会被断开
    public static readonly NcEvent<ServerPhaseArgs> Stopping = new();

    //Stopped 主循环退出 存档刷盘已完成 之后进程不保证还在
    //回调里只做纯内存收尾 别再碰世界与玩家
    public static readonly NcEvent<ServerPhaseArgs> Stopped = new();

    //CommandRegister 内置命令注册完成 模组在这里往分派器里挂自己的命令
    public static readonly NcEvent<CommandRegisterArgs> CommandRegister = new();

    //CommandExecuted 一条命令跑完 语法错误与权限被拒也算 看 Result 分辨
    public static readonly NcEvent<CommandExecutedArgs> CommandExecuted = new();

    //PlayerJoin 玩家加入完成 入场包已发
    public static readonly NcEvent<PlayerJoinArgs> PlayerJoin = new();

    //PlayerLeave 玩家移出在线列表
    public static readonly NcEvent<PlayerLeaveArgs> PlayerLeave = new();

    //PlayerDisconnect 玩家断线 断连包已发
    public static readonly NcEvent<PlayerDisconnectArgs> PlayerDisconnect = new();

    //PlayerHurt 玩家实际受到伤害 无敌帧内不算
    public static readonly NcEvent<PlayerHurtArgs> PlayerHurt = new();

    //PlayerDeath 玩家死亡 内核在归零后立刻复位 本事件与复位同刻
    public static readonly NcEvent<PlayerDeathArgs> PlayerDeath = new();

    //PlayerChat 玩家聊天 原方法广播之后触发
    public static readonly NcEvent<PlayerChatArgs> PlayerChat = new();

    //ChunkLoaded 区块首次进入内存 实体载入之前
    public static readonly NcEvent<ChunkLoadedArgs> ChunkLoaded = new();

    //ChunkUnloaded 区块离开内存 方块实体已随之内核清理
    public static readonly NcEvent<ChunkUnloadedArgs> ChunkUnloaded = new();

    //ChunkSaved 区块落盘前取了快照 破坏性操作别放这个回调里
    public static readonly NcEvent<ChunkSavedArgs> ChunkSaved = new();

    //LevelTick 维度级 tick 主世界与已装载维度各发一次 每拍都发
    public static readonly NcEvent<LevelTickArgs> LevelTick = new();

    //SavedDataSaving 存档数据写完落盘 世界时钟与游戏规则这类数据走它
    //与区块落盘是两条路 区块那边是异步的 这个已经写完
    public static readonly NcEvent<SavedDataSavingArgs> SavedDataSaving = new();

    //BlockChanged 方块状态变更已写进去 正要同步客户端
    //红石元件在行为回调里自己改状态也走这条 高频 回调里别做耗时的事
    public static readonly NcEvent<BlockChangedArgs> BlockChanged = new();

    //BlockBroken 方块被破坏 玩家挖掘与红石自毁都在内 非玩家破坏时 Player 为 null
    public static readonly NcEvent<BlockBrokenArgs> BlockBroken = new();

    //ItemDropped 掉落物实体生成 方块破坏掉落与烤制产物都在内
    public static readonly NcEvent<ItemDroppedArgs> ItemDropped = new();
}

//ClientEvents 客户端事件
public static class ClientEvents
{
    //Tick 客户端主循环每 tick 触发一次 数据来自挂在 MinecraftClient.Tick 上的注入探针
    public static readonly NcEvent<ClientTickArgs> Tick = new();
}

//NetworkEvents 网络事件 收发两端共用一套 方向由参数里的 IsServerbound 分辨
public static class NetworkEvents
{
    //PacketReceived 每个入站包都会走一次 移动包这类每拍就有好几颗
    //挂点在包处理器的入口 排队那一刻就发 还没进业务层
    public static readonly NcEvent<PacketReceivedArgs> PacketReceived = new();
}
