# NetCraft.ModApi

The mod API surface for [NetCraft](https://github.com/NetCraft-Dev/NetCraft) — a from-scratch reimplementation of the Minecraft 26.2 kernel in C# / .NET 10.

`NetCraft.ModApi` has two identities. It is the API surface other mods build against (event subscription, `Nc*` handles, command registration), and it is itself an ordinary mod: it ships an embedded `ncmod.json`, and the loader loads it like any other mod. This mirrors how `fabric-api` is a normal mod in the Fabric ecosystem.

## Layout

| Path | Contents |
|---|---|
| `Wrapper/` | `NcEvent<T>`, the `Nc*` facades and object handles — the public surface |
| `Extension/` | `[Inject]` and `[Mixin]` attributes |
| `Internal/` | Probe types the loader injects; not part of the public surface |
| `Gui/` | Mods page injected into the server GUI (Avalonia) |

## Wrapper purity policy

Every kernel type that could show up on the public surface falls into one of three tiers:

| Tier | Rule | Types |
|---|---|---|
| Whitelisted | May appear on the public surface under a **stable promise**: if the kernel changes them, ModApi ships a compat shim instead of breaking mods | brigadier (`NetCraft.Commands.*`), value types from `NetCraft.Primitives` (`BlockPos`, `Vec3`, `Identifier`, `GameType`, `BlockState`), `ItemStack`, `GameProfile` |
| Wrapped | Kernel types with their own lifecycle or mutable state never appear on the public surface; mods reach them through `Nc*` facades and handles | everything else |
| Internal | Injection probes and plumbing; not an API | `Internal/*`, `Gui/*` |

The judgement rule behind the whitelist: value types and cross-mod data currencies are frozen by contract; service types that carry their own state are wrapped.

One deliberate exception lives in the wrapper layer: `NcAccess` unwraps a handle back to the kernel object and resolves members by name, so a mod can reach whatever the facades do not cover. It is an escape hatch rather than a promise — the names it uses are kernel implementation details, and a rename there breaks a mod relying on them.

Known debt, tracked for upcoming batches: `NcRecipes` (recipe types), `NcRegistries` (`Registry<T>` handles), and the `Block`/`Item` content classes are settled by the content-registration batch; storage APIs by the storage batch. Event handler exceptions are isolated (logged, never propagated into the kernel), and cancellable events exist as a mechanism (`INcCancellable`); the first cancellable events land with the interaction batch.

## Building

This project is configured by `NetCraft.ModApi.ncproj`, not by a csproj, so `dotnet build` does not apply. Build it with `ncm`, the NetCraft mod development CLI:

```powershell
dotnet tool install -g NetCraft.ModBuild.Tools
```

Then, from this directory:

```powershell
ncm build       # check, compile and deploy, this is the one to run
ncm restore     # fetch the declared packages into Build/packages without building
ncm clean       # drop the build cache, --all drops the downloads as well
```

`ncm build` syncs the kernel reference assemblies and the packages declared in the `.ncproj` under `Build`, compiles the project with Roslyn, and copies the built dll into `run/mods` right after a successful build, as `<Deploy>` says. The build is incremental: rerunning it with nothing changed reports `Up to date` and writes nothing.

## Documentation

- [Mod API reference](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/mod-api.md)
- [Modding guide](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/modding-guide.md)

## License

Apache-2.0 — see [LICENSE](./LICENSE).
