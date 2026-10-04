using System.Collections.Concurrent;
using System.Reflection;
using NetCraft.Logging;
using NetCraft.ModApi.Gui;

namespace NetCraft.ModApi.Internal;

//GuiProbe 服务端界面注入的落点
//注入的是 ServerWindow.RefreshNow 的调用点 构造末尾与每拍轮询都会走到
//替换后原方法不再被直接调用 所以这里要自己转发一次 否则状态刷新会整块丢掉
//本类刻意不声明任何 Avalonia 类型的成员 界面代码全在 Gui.ModsTabHost 里
//客户端宿主没装 Avalonia 只要不碰那一边就不会去解析它
public static class GuiProbe
{
    //RefreshMethods 按窗口类型缓存原刷新方法 免得每拍都反射查找
    private static readonly ConcurrentDictionary<Type, MethodInfo?> RefreshMethods = new();

    //OnWindowRefresh 替换 ServerWindow.RefreshNow 的调用点
    //self 就是窗口本身 注入的是实例调用所以第一个参数是 this
    public static void OnWindowRefresh(object self)
    {
        try
        {
            ModsTabHost.AttachOrRefresh(self);
        }
        catch (Exception ex)
        {
            //界面扩展出问题不能连累原刷新逻辑
            Log.Warning($"Mod page injection failed {ex.Message}");
        }

        var method = RefreshMethods.GetOrAdd(self.GetType(),
            static type => type.GetMethod("RefreshNow", BindingFlags.Instance | BindingFlags.NonPublic));
        method?.Invoke(self, null);
    }
}
