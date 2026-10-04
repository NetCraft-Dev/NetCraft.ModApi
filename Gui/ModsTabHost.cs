using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using NetCraft.Logging;

namespace NetCraft.ModApi.Gui;

//ModsTabHost 模组页挂载 由注入进 ServerWindow 的调用点驱动
//首次调用把 MODS 页插进标签栏 之后每拍只刷新它
//单独成一个类是为了让 Avalonia 类型只在这条路径上被解析
public static class ModsTabHost
{
    private const string TabHeader = "MODS";

    //Injected 记下自己插进去的那一项 按窗口实例存
    //不靠遍历标签栏按表头认领 宿主里若残留同名页面会认错并每拍重插一页
    private static readonly ConditionalWeakTable<Window, TabItem> Injected = new();

    //AttachOrRefresh 已插过就只刷新 没插过就补一页进去
    public static void AttachOrRefresh(object window)
    {
        if (window is not Window host) return;

        var tabs = host.FindControl<TabControl>("Tabs");
        if (tabs is null) return;

        if (!Injected.TryGetValue(host, out var item) || !tabs.Items.Contains(item))
        {
            item = new TabItem
            {
                Header = TabHeader,
                Content = new ContentControl { Content = new ServerModsPage(), Margin = new Thickness(4) }
            };
            tabs.Items.Add(item);
            Injected.AddOrUpdate(host, item);
            Log.Debug("NetCraft-ModApi injected MODS page into the server UI");
        }

        if ((item.Content as ContentControl)?.Content is ServerModsPage page)
            page.Refresh();
    }
}
