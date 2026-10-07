using System.Runtime.CompilerServices;
using NetCraft.ModApi.Wrapper;
using NetCraft.Storage;

namespace NetCraft.ModApi.Internal;

//StorageProbe 存档数据探针
//只管 SavedDataStorage 这一层 区块落盘是另外两条路 不在这里
//签名只用 BCL 类型 理由同 PlayerProbe
public static class StorageProbe
{
    //OnScheduleSave 存档数据落盘 原方法同步写完才返回 所以发事件时文件已经是新的
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task OnScheduleSave(object self)
    {
        var task = ((SavedDataStorage)self).ScheduleSave();
        ServerEvents.SavedDataSaving.Publish(new SavedDataSavingArgs());
        return task;
    }
}
