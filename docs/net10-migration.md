# .NET 10 and SLNX migration

This repository now includes a **side-by-side, Windows-only .NET 10** build.

- Modern solution: `FrostsGen5Editor.slnx`
- SDK-style WinForms/WPF project: `FrostsGen5Editor.Net10.csproj`
- Target: `net10.0-windows` / `win-x64`
- Original .NET Framework 4.8 project: `FrostsGen5Editor.csproj` (retained unchanged for stable releases)
- CI: `.github/workflows/dotnet10-migration.yml`

## Build locally on Windows

Install the **.NET 10 SDK**, Visual Studio/MSBuild Windows Desktop build tools, and `nuget.exe`.

From a checkout of this repository, in PowerShell:

```powershell
nuget restore packages.config -PackagesDirectory ..\packages -NonInteractive
dotnet restore FrostsGen5Editor.slnx
dotnet build FrostsGen5Editor.slnx -c Release --no-restore
dotnet publish FrostsGen5Editor.Net10.csproj -c Release -r win-x64 --self-contained false -o publish/net10
```

The published build requires **Windows** and the **.NET 10 Windows Desktop Runtime**. It is a framework-dependent WinForms application, not a cross-platform executable.

## Migration state and remaining compatibility gates

The compiler and `dotnet publish` have been verified in GitHub Actions.
The modern `WinForms.DataVisualization` package replaces .NET Framework's built-in charting API.
Some existing binary components are deliberately retained for now:
`DSDecmp.dll`, `HelixToolkit.dll`, `OpenTK.dll`, `Tao.OpenGl.dll`,
`Tao.Platform.Windows.dll`, `SharpDX`, and legacy `MonoGame.Forms.DX`.

Important: **compilation is not equivalent to runtime correctness.**
The legacy MonoGame.Forms.DX 3.2.0 framework binary remains a significant
runtime compatibility risk. A trial upgrade to 3.2.1 did not compile against
the existing `MonoGameControl` code and brought outdated transitive packages;
the project kept the compiling legacy references for now.

Before promoting the modern build to the default/stable release, test:
1. Launch and close the GUI on an actual Windows desktop.
2. Load and save a known-good clean BW and BW2 ROM, then compare the saved ROM output.
3. Render sprite/model/texture previews and animations using MonoGame, SharpDX, OpenTK and WPF.
4. Exercise scripting, text editing, trainer/encounter editors and patch output.
5. Verify all runtime files (including `Resources`, `ScriptHeaders`, `Images`, `Patches`) are present in the zip.

Do not treat an Actions green build as proof those workflows work.
