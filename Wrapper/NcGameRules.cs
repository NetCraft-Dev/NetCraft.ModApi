using System.Runtime.CompilerServices;
using NetCraft.Game.World.Level;
using NetCraft.Registry;
using NetCraft.Storage;

namespace NetCraft.ModApi.Wrapper;

//NcGameRules 游戏规则句柄 按名字读写 规则值落在内核那份存档表上
//查规则名与默认值看 NcGameRules.All 内核加规则改规则都不需要重编译模组
public sealed class NcGameRules
{
    //Cache 内核规则表到句柄的映射 弱引用跟随内核对象生命周期
    private static readonly ConditionalWeakTable<GameRuleMapData, NcGameRules> Cache = new();

    //_inner 内核规则表本体 私有 不进公开面
    private readonly GameRuleMapData _inner;

    private NcGameRules(GameRuleMapData inner) => _inner = inner;

    //Get 取句柄 同一内核规则表复用同一份
    internal static NcGameRules Get(GameRuleMapData data)
        => Cache.GetValue(data, static d => new NcGameRules(d));

    //Inner 内核规则表本体 包装层内部取用 模组看不到
    internal GameRuleMapData Inner => _inner;

    //All 全部规则名 内核定义表按注册顺序给出
    public static IReadOnlyList<string> All =>
        GameRules.All.Select(r => r.Id.ToShortString()).ToList();

    //CanonicalName 把规则名归一成短名 带不带 minecraft: 前缀都能命中 找不到原样返回
    public static string CanonicalName(string name)
    {
        var rule = GameRules.Find(name);
        return rule is null ? name : rule.Id.ToShortString();
    }

    //GetBool 读布尔规则 未设置过返回规则默认值 规则名不存在返回 false
    public bool GetBool(string name)
    {
        var rule = GameRules.Find(name);
        return rule is null ? false : _inner.GetBool(rule);
    }

    //SetBool 写布尔规则 规则不存在或类型不符返回 false
    public bool SetBool(string name, bool value)
    {
        var rule = GameRules.Find(name);
        if (rule is null || rule.Type != GameRuleType.Bool) return false;
        _inner.SetRule(rule, value);
        return true;
    }

    //GetInt 读整数规则 未设置过返回规则默认值 规则名不存在返回 0
    public int GetInt(string name)
    {
        var rule = GameRules.Find(name);
        return rule is null ? 0 : _inner.GetInt(rule);
    }

    //SetInt 写整数规则 值按规则声明的上下限钳位 规则不存在或类型不符返回 false
    public bool SetInt(string name, int value)
    {
        var rule = GameRules.Find(name);
        if (rule is null || rule.Type != GameRuleType.Int) return false;
        _inner.SetRule(rule, Math.Clamp(value, rule.Min, rule.Max));
        return true;
    }
}
