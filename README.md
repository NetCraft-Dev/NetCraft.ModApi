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


## Documentation

- [Mod API reference](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/mod-api.md)
- [Modding guide](https://github.com/NetCraft-Dev/NetCraft/blob/main/docs/modding-guide.md)

## License

Apache-2.0 — see [LICENSE](./LICENSE).
