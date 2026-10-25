# SysGadget

**What:** A small always-on-top Windows gadget that shows CPU load per core, memory, and GPU load, memory and temperature — by default a floating strip with one column per group at the top of the screen; it can also dock to a screen edge.

**Why:** Replaces the old Windows desktop gadgets that gave useful system information at a glance. Every group and item can be shown or hidden, and the dock edge and always-on-top are configurable.

**Status:** working — CPU (total, per core), memory, GPU (load, VRAM, temperature), storage (read, write and free space per drive), network (down, up against a set bandwidth), temperatures of all sensors (LibreHardwareMonitor, needs administrator rights) and system; floating window (columns or stack, thin bar) or docking (overlay, or reserving the edge); settings window; tray icon; start with Windows; installer.

## Install

Run `SysGadget-Setup-<version>.exe` from the GitHub release. The setup asks whether SysGadget starts with Windows (ticked by default); Settings has the same switch ("Start SysGadget with Windows", the `SysGadget` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`). The installed app is self-contained: no .NET runtime is needed.

## How to run

```
dotnet build
dotnet run --project src/SysGadget.App
dotnet test tests/SysGadget.Core.Tests
dotnet test tests/SysGadget.App.Tests
```

**Verify:** none yet. `/sw-quality:sw-quality setup` creates one.

**Prerequisites:** Windows 11, .NET SDK 10.0 (10.0.400 tested). The default sources (performance counters, DXGI, NVML) need no admin rights. GPU temperature needs the NVIDIA driver (nvml.dll). The optional LibreHardwareMonitor source needs admin rights and loads a driver; the app offers to restart itself elevated.

**Files at run time:** `%APPDATA%\SysGadget\settings.json` (settings), `%APPDATA%\SysGadget\crash.log` (written only on a crash).

## Build the installer

`.\build-installer.ps1` publishes the app self-contained for win-x64 and compiles `installer\SysGadget.iss` with Inno Setup (6 or 7). The setup lands in `out\installer\`; the script prints its path and SHA-256. The version comes from `Directory.Build.props`.

## Release

1. Raise `<Version>` in `Directory.Build.props`, commit and push to master.
2. Run `.\release.ps1`. It stops before tagging when master is not clean and in step with origin, or when the tag or release exists. It builds, runs every test (the Hardware ones too), builds the installer, pushes the annotated tag `v<version>` and creates the GitHub release with the setup and its SHA-256.

CI (`.github/workflows/build.yml`) builds, tests and makes the installer on every push; on a `v*` tag it uploads the setup to the release (creating it when missing). CI leaves out tests marked `Category=Hardware` (real GPU, NVIDIA driver, two disks). Until the Actions quota is available again, releases are made from the desk with `release.ps1`.

## Use

- Right-click the gadget or its tray icon: Settings, Always on top, Thin bar, Exit.
- Drag the floating window by any part of it; it stops flush with the screen edges and its place is kept.
- Double-click the gadget to switch between the cards and the thin bar (one line per group: the first ticked item of each group, marked "thin bar" in the settings tree).
- Settings: position (floating, left, right, top, bottom), monitor, card width (200 to 800), opacity, always on top, thin bar, columns or stack (floating), reserve screen space (docked), update interval, network bandwidth in Mbit/s (a full Down/Up bar), cores as vertical bars, and a tree of groups and items: tick to show, drag to reorder (items stay in their group). Each drive has its own Read, Write and Free entries.
- Temperatures: tick "All sensors" in the Temperatures group. The gadget asks to restart as administrator (LibreHardwareMonitor needs it) and asks again at start while the temperatures are on. Values turn amber near and red at the device's own limits (drive warning/critical, GPU slow-down/shut-down), else at 75/90 °C. CPU temperatures may be missing when Windows blocks LibreHardwareMonitor's driver.

## Layout

- `src/SysGadget.Core/` — engine-free library (netstandard2.1): providers (counters, DXGI, NVML), the catalogue, registry, sampler, settings and row formatting.
- `src/SysGadget.LibreHardware/` — optional temperature source on LibreHardwareMonitorLib.
- `src/SysGadget.App/` — WPF app: window, docking (AppBar), tray icon, settings window.
- `tests/SysGadget.Core.Tests/`, `tests/SysGadget.App.Tests/` — xUnit tests; some read the real counters of the machine.
- `installer/SysGadget.iss`, `build-installer.ps1`, `release.ps1`, `.github/workflows/build.yml` — installer and release.
- `Directory.Build.props` — the one version of all projects.
- `nuget.config` — clears the global feed, uses nuget.org only.

## Adding an item

1. Add the item (and a source if it needs a new one) to `MeterCatalog` — data only.
2. Add the row function in `MeterRegistry`, and a new hardware source as a factory in `MachineSources` (the real provider in `Real`, a fixed snapshot in the tests' `FakeMachine`); the registry's constructor fails while either is missing.
3. Add the formatting to `MeterRowBuilder`, with a test.

## Decisions

- 2026-10-25 — .NET 10 + WPF over C++/Qt/web: native transparent topmost window and AppBar docking are easy, and the machine already has the .NET toolchain.
- 2026-10-25 — Sensors: performance counters are the default (no admin, no driver); LibreHardwareMonitor is an optional opt-in source for temperatures.
- 2026-10-25 — Display as groups of items; the catalogue is data and the behaviour sits in a registry keyed by ids, so the sampler has no per-item code.
- 2026-10-25 — A hidden item is not sampled; its source is closed when no visible item needs it.
- 2026-10-25 — GPU temperature through NVML: works without admin rights, NVIDIA only.
- 2026-10-25 — Minimum width 200: label, a bar at its 36 px minimum and the value always fit on one line.
- 2026-10-25 — Docking does not reserve the edge by default: Windows reserves an app bar's whole edge, which left half the strip empty; the overlay is only as tall as its cards.
- 2026-10-25 — Default is a floating window with one column per group, centred at the top; the cards are coloured as a rainbow by position.
- 2026-10-25 — No collapsing of groups: in columns a collapsed card stayed as an empty column; the thin bar replaces it.
- 2026-10-25 — Per-instance items: a drive's Read, Write and Free are separate entries ("id|instance" keys), found at start and when Settings opens; retired item ids are dropped on load.
- 2026-10-25 — Temperatures have one switch (the tree item); the elevation question is owned by an on-screen window; settings are saved before an elevated restart.
- 2026-10-25 — Temperatures show no bar and no peak; colour comes from the device's own limits.
- 2026-10-25 — Installer with Inno Setup on a self-contained publish (the ParaPad/PassKit pattern); release from the desk with `release.ps1` while the GitHub Actions quota is used up, CI on tags afterwards.

---

Keep this file honest: it is the only project document that is committed. `LOG.md` is
gitignored, so anything that must survive a fresh clone belongs here, not in the log.
