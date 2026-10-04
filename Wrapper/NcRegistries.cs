using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Registry.State;

namespace NetCraft.ModApi.Wrapper;

//NcRegistries 内置注册表查询入口
//注册表在启动期逐步装配 模组加载早于装配完成 别在 Init 里查完就缓存下来
//要按 id 找单个别用 Find 系列 要遍历或按标签查直接拿下面的表
public static class NcRegistries
{
    //Blocks 方块注册表
    public static Registry<Block> Blocks => BuiltInRegistries.BLOCK;

    //Items 物品注册表
    public static Registry<Item> Items => BuiltInRegistries.ITEM;

    //Fluids 流体注册表
    public static Registry<Fluid> Fluids => BuiltInRegistries.FLUID;

    //MobEffects 状态效果注册表
    public static Registry<MobEffect> MobEffects => BuiltInRegistries.MOB_EFFECT;

    //Biomes 生物群系注册表
    public static Registry<Biome> Biomes => BuiltInRegistries.BIOME;

    //Particles 粒子类型注册表
    public static Registry<NetCraft.Registry.ParticleType<object>> Particles => BuiltInRegistries.PARTICLE_TYPE;

    //EntityTypes 实体类型注册表
    public static Registry<EntityType<object>> EntityTypes => BuiltInRegistries.ENTITY_TYPE;

    //BlockEntityTypes 方块实体类型注册表
    public static Registry<NetCraft.Registry.BlockEntityType<object>> BlockEntityTypes
        => BuiltInRegistries.BLOCK_ENTITY_TYPE;

    //FindBlock 按注册名找方块 例如 minecraft:stone 找不到返回 null
    public static Block? FindBlock(string id) => BuiltInRegistries.BLOCK.GetValue(Identifier.Parse(id));

    //FindState 按注册名找方块的默认状态 找不到返回 null
    public static BlockState? FindState(string id) => FindBlock(id)?.DefaultBlockState;

    //FindItem 按注册名找物品 找不到返回 null
    public static Item? FindItem(string id) => BuiltInRegistries.ITEM.GetValue(Identifier.Parse(id));

    //FindFluid 按注册名找流体 找不到返回 null
    public static Fluid? FindFluid(string id) => BuiltInRegistries.FLUID.GetValue(Identifier.Parse(id));

    //FindMobEffect 按注册名找状态效果 找不到返回 null
    public static MobEffect? FindMobEffect(string id) => BuiltInRegistries.MOB_EFFECT.GetValue(Identifier.Parse(id));

    //FindBiome 按注册名找生物群系 找不到返回 null
    public static Biome? FindBiome(string id) => BuiltInRegistries.BIOME.GetValue(Identifier.Parse(id));

    //FindParticle 按注册名找粒子类型 参数形态未移植的类型也会返回
    public static NetCraft.Registry.ParticleType<object>? FindParticle(string id)
        => BuiltInRegistries.PARTICLE_TYPE.GetValue(Identifier.Parse(id));

    //FindEntityType 按注册名找实体类型 找不到返回 null
    public static EntityType<object>? FindEntityType(string id)
        => BuiltInRegistries.ENTITY_TYPE.GetValue(Identifier.Parse(id));

    //FindBlockEntityType 按注册名找方块实体类型 找不到返回 null
    public static NetCraft.Registry.BlockEntityType<object>? FindBlockEntityType(string id)
        => BuiltInRegistries.BLOCK_ENTITY_TYPE.GetValue(Identifier.Parse(id));
}
