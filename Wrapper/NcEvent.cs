using NetCraft.Logging;

namespace NetCraft.ModApi.Wrapper;

//NcEvent 事件 订阅后拿到句柄 释放句柄即注销
//对齐原版 Fabric 的 Event 用法
//事件线与门面同在包装层 模组订阅后拿到的参数也全是包装类型
public sealed class NcEvent<T>
{
    private readonly List<Action<T>> _handlers = new();
    private readonly object _lock = new();

    //Subscribe 订阅事件 返回的句柄释放时注销
    public IDisposable Subscribe(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock) _handlers.Add(handler);
        return new Subscription(this, handler);
    }

    //Publish 分发事件 由注入探针调用
    //无订阅者时直接返回 包级与 tick 级事件每拍都发 一次 ToArray 就是一份白扔的分配
    //回调异常就地接住记日志 一份模组抛异常不许穿透进内核调用栈 也不许打断别的订阅者
    internal void Publish(T args)
    {
        Action<T>[] snapshot;
        lock (_lock)
        {
            if (_handlers.Count == 0) return;
            snapshot = _handlers.ToArray();
        }
        foreach (var handler in snapshot)
        {
            try
            {
                handler(args);
            }
            catch (Exception e)
            {
                Log.Error($"NetCraft-ModApi event handler exception: {e}");
            }
        }
    }

    //Unsubscribe 注销订阅
    private void Unsubscribe(Action<T> handler)
    {
        lock (_lock) _handlers.Remove(handler);
    }

    //Subscription 订阅句柄
    private sealed class Subscription : IDisposable
    {
        private readonly NcEvent<T> _owner;
        private readonly Action<T> _handler;
        private bool _disposed;

        public Subscription(NcEvent<T> owner, Action<T> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Unsubscribe(_handler);
        }
    }
}

//INcCancellable 可取消事件参数接口 参数类实现它 回调里置 Cancelled 即否决
//走 Bukkit 式裁决 分发完由探针读回结果决定放不放行 内核原调用照旧由探针补回
public interface INcCancellable
{
    //Cancelled 是否被某个回调取消 任一回调置真即生效
    bool Cancelled { get; set; }
}

//NcIntercept 可取消事件的分发收口 探针专用
//先照常分发再读裁决 返回 false 表示有回调否决 探针据此跳过内核原调用
internal static class NcIntercept
{
    //Through 分发并裁决 true 放行 false 拦截
    public static bool Through<T>(NcEvent<T> evt, T args) where T : class, INcCancellable
    {
        evt.Publish(args);
        return !args.Cancelled;
    }
}
