using NetCraft.Game.World.Crafting;
using NetCraft.Game.World.Items;
using NetCraft.Registry;

namespace NetCraft.ModApi.Wrapper;

//NcRecipes 配方查询入口
//数据包重载会整表换掉 查到的配方与 holder 别跨重载缓存
public static class NcRecipes
{
    //Manager 当前生效的配方管理器 资源尚未装载时为 null
    public static RecipeManager? Manager => RecipeManager.Active;

    //IsAvailable 配方表是否已装载
    public static bool IsAvailable => Manager is not null;

    //Require 取配方管理器 未装载直接抛 免得调用方到处判空
    public static RecipeManager Require()
        => Manager ?? throw new InvalidOperationException("配方表尚未装载 请在服务端事件回调里访问");

    //Count 已加载的配方总数
    public static int Count => Require().Count;

    //StonecutterCount 已加载的切石机配方数
    public static int StonecutterCount => Require().StonecutterRecipeCount;

    //CookingCount 已加载的烹饪配方数 熔炉烟熏高炉篝火合计
    public static int CookingCount => Require().CookingRecipeCount;

    //Craft 按合成网格算产出 没命中返回空栈
    public static ItemStack Craft(CraftingInput input) => Require().GetCraftingResult(input);

    //Find 按配方 id 找配方 找不到返回 null
    public static RecipeHolder? Find(Identifier id) => Require().GetRecipe(id);

    //Find 按配方 id 找配方 例如 minecraft:oak_planks 找不到返回 null
    public static RecipeHolder? Find(string id) => Find(Identifier.Parse(id));

    //StonecuttingFor 取该输入能用的全部切石机配方 顺序即数据包加载顺序
    public static IReadOnlyList<StonecutterRecipe> StonecuttingFor(ItemStack input)
        => Require().GetStonecutterRecipes(input);

    //CookingFor 按烹饪类型与输入查配方 类型是 smelting 这类注册名路径 没命中返回 null
    public static AbstractCookingRecipe? CookingFor(string type, ItemStack input)
        => Require().GetCookingRecipe(type, input);
}
