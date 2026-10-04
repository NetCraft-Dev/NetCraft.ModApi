using System.Runtime.CompilerServices;
using NetCraft.Commands;
using NetCraft.Game.Commands;
using NetCraft.Game.Server;
using NetCraft.ModApi.Wrapper;

namespace NetCraft.ModApi.Internal;

//CommandProbe 命令注册扩展点
//挂在 EffectCommand::Register 的调用点上 那是内核构造函数里最后一批注册之一
//替换方法的参数声明成 object 装配期反射找方法时不会解析到内核类型
//真正把内核类型引出来是在方法体里 那时代码已经在跑 内核早就加载好了
public static class CommandProbe
{
    //OnCommandsReady 接管那次调用 先补回内置的 effect 命令 再放行模组注册
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnCommandsReady(object dispatcher)
    {
        var typed = (CommandDispatcher<CommandSourceStack>)dispatcher;
        EffectCommand.Register(typed);
        ServerEvents.CommandRegister.Publish(new CommandRegisterArgs { Dispatcher = typed });
    }

    //OnExecute 命令执行 两个 Execute 重载共用这一条规则 它们的参数个数相同
    //第一个参数可能是 ServerPlayer 也可能是 CommandSourceStack 按真身分派
    //语法错误被内核接住后照样返回 所以事件在成功与失败两种情况下都会发
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int OnExecute(object self, object source, string command)
    {
        var manager = (CommandManager)self;
        var result = source is ServerPlayer player
            ? manager.Execute(player, command)
            : manager.Execute((CommandSourceStack)source, command);

        ServerEvents.CommandExecuted.Publish(new CommandExecutedArgs
        {
            Command = command,
            Result = result,
            Source = source as CommandSourceStack,
            Player = source is ServerPlayer sp ? NcPlayer.Get(sp) : null,
        });
        return result;
    }
}
