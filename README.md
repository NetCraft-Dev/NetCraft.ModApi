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
| `libs/` | Kernel reference assemblies this repo compiles against |
| `tools/` | Maintenance scripts |

## Building

The kernel source lives in the NetCraft repository, not here. This repo compiles against the reference assemblies in `libs/`, so it builds on its own:

```powershell
dotnet build -c Release
```

When the kernel API changes, refresh `libs/` from a kernel build output:

```powershell
./tools/sync-libs.ps1 -KernelOutput <path to NetCraft.ServerExe bin/Release/net10.0>
```

The output DLL is a mod. To deploy it into a host, point `ModHostDirs` at the host's `mods/` directory:

```powershell
dotnet build -c Release -p:ModHostDirs="<host>/mods"
```

## Documentation

- [Mod API reference](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/mod-api.md)
- [Modding guide](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/modding-guide.md)

## License

Apache-2.0 — see [LICENSE](./LICENSE).
