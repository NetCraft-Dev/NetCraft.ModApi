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
    internal void Publish(T args)
    {
        Action<T>[] snapshot;
        lock (_lock)
        {
            if (_handlers.Count == 0) return;
            snapshot = _handlers.ToArray();
        }
        foreach (var handler in snapshot)
            handler(args);
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
