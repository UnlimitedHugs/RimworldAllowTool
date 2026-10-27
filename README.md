The AllowTool mod for Rimworld.

A set of tools to make your life on the Rim a bit easier.
Easily forbid and unforbid items, select similar things, have things hauled urgently and affect the entire map with powerful new tool extensions.  
See the forum and Steam pages for more info.

Forum page: https://ludeon.com/forums/index.php?topic=17218.0  
Steam workshop page: https://steamcommunity.com/sharedfiles/filedetails/?id=761421485

## RimWorld 1.7 build (Waylith / .NET 10)

RimWorld 1.7 runs on Ludeon's Waylith engine with a .NET 10 CoreCLR runtime instead of Unity/Mono.
The project was converted to an SDK-style `net10.0` csproj; game references come from the `Krafs.Rimworld.Ref` NuGet package, Harmony is 3.0.

Build (needs .NET 10 SDK): `dotnet build -c Release` — output goes to `Mods/AllowTool/v1.7/Assemblies/`.

HugsLib comes from the 1.7 port at https://github.com/anatolix/RimworldHugsLib (net10.0); its assembly is vendored under `lib/HugsLib/` because the upstream NuGet package is still the net472 build.

The 1.6 assembly under `v1.6/` is kept as-is for older game versions.
