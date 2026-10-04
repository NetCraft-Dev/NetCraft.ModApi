using NetCraft.Commands;
using NetCraft.Commands.Tree;

namespace NetCraft.ModApi.Internal;

//NcCommandInfo 一条经由 ModApi 注册的命令
internal sealed class NcCommandInfo(string name, string description, IReadOnlyList<string> usages)
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    //Usages 该命令每条可执行路径的用法文本 参数用尖括号包着
    public IReadOnlyList<string> Usages { get; } = usages;
}

//NcCommandRegistry 登记经由 ModApi 注册的命令 给 /ncmapi 展示用
//命令本身照旧挂在内核分派器上 这份清单只是账本 不参与执行
internal static class NcCommandRegistry
{
    private static readonly List<NcCommandInfo> _entries = new();

    public static IReadOnlyList<NcCommandInfo> Entries => _entries;

    //Add 登记一条命令 用法从节点树现算
    public static void Add(string name, string description, CommandNode<CommandSourceStack> node)
        => _entries.Add(new NcCommandInfo(name, description, BuildUsages(node)));

    //WriteList 把清单写给命令源 控制台走文本 玩家走聊天回执
    public static void WriteList(CommandSourceStack source)
    {
        if (_entries.Count == 0)
        {
            source.SendSuccess("当前没有经由NetCraft-ModApi注册的命令");
            return;
        }
        source.SendSuccess($"通过NetCraft-ModApi注册的命令,共{_entries.Count}个");
        foreach (var entry in _entries)
        {
            source.SendSuccess($"/{entry.Name}  {entry.Description}");
            foreach (var usage in entry.Usages)
                source.SendSuccess($"  /{usage}");
        }
    }

    //BuildUsages 列出命令的每条叶子路径
    //字面量直接写名字 参数用尖括号包起来 中间节点自己可执行时也算一条
    private static List<string> BuildUsages(CommandNode<CommandSourceStack> node)
    {
        var results = new List<string>();
        Walk(node, node.GetName(), results);
        return results;
    }

    private static void Walk(CommandNode<CommandSourceStack> node, string path, List<string> results)
    {
        var children = node.GetChildren();
        //能执行或有子节点都算 一个节点既自己可执行又带子参数时两条路径都要列
        if (node.GetCommand() is not null || children.Count == 0)
            results.Add(path);
        foreach (var child in children)
        {
            var token = child is LiteralCommandNode<CommandSourceStack>
                ? child.GetName()
                : "<" + child.GetName() + ">";
            Walk(child, path + " " + token, results);
        }
    }
}
