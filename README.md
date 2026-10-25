# SysGadget

**What:** A small always-on-top Windows gadget that shows CPU load per core, memory, and GPU load, memory and temperature — by default a floating strip with one column per group at the top of the screen; it can also dock to a screen edge.

**Why:** Replaces the old Windows desktop gadgets that gave useful system information at a glance. Every group and item can be shown or hidden, and the dock edge and always-on-top are configurable.

**Status:** working — CPU, memory, GPU (load, VRAM, temperature), storage, network, system and optional LibreHardwareMonitor temperatures; floating window (columns or stack) or docking (overlay, or reserving the edge); settings window; tray icon. No installer yet.

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

## Use

- Right-click the gadget or its tray icon: Settings, Always on top, Exit.
- Drag the floating window by any part of it; its place is kept.
- Thin bar (window or tray menu, or Settings): one line per group, showing the first visible item of each group. Order the items in the settings tree to choose which one.
- Settings: dock edge (floating, left, right, top, bottom), monitor, width of a card (200 to 800), opacity, update interval, columns or stack for the floating window, reserve screen space when docked, compact core strip, and a tree of groups and items with checkboxes and Up/Down ordering.

## Layout

- `src/SysGadget.Core/` — engine-free library (netstandard2.1): providers (counters, DXGI, NVML), the catalogue, registry, sampler, settings and row formatting.
- `src/SysGadget.LibreHardware/` — optional temperature source on LibreHardwareMonitorLib.
- `src/SysGadget.App/` — WPF app: window, docking (AppBar), tray icon, settings window.
- `tests/SysGadget.Core.Tests/`, `tests/SysGadget.App.Tests/` — xUnit tests; some read the real counters of the machine.
- `nuget.config` — clears the global feed, uses nuget.org only.

## Adding an item

1. Add the item (and a source if it needs a new one) to `MeterCatalog` — data only.
2. Add the source factory and the row function in `MeterRegistry`; its constructor fails while either is missing.
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

---

Keep this file honest: it is the only project document that is committed. `LOG.md` is
gitignored, so anything that must survive a fresh clone belongs here, not in the log.
