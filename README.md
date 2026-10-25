# SysGadget

**What:** A small always-on-top Windows gadget, docked at a screen edge, that shows CPU load per core, memory, and GPU load and memory.

**Why:** Replaces the old Windows desktop gadgets that gave useful system information at a glance. Every meter can be shown or hidden, and the dock edge and always-on-top are configurable.

**Status:** scaffold only — repository, README and NuGet config exist; no code yet.

## How to run

Not buildable yet. Planned: a .NET 10 WPF app on top of an engine-free core library.

```
dotnet build
dotnet run --project src/SysGadget.App
dotnet test
```

**Verify:** none yet. `/sw-quality:sw-quality setup` creates one.

**Prerequisites:** Windows 11, .NET SDK 10.0 (10.0.400 installed). The default sensor source (performance counters) needs no admin rights. The optional LibreHardwareMonitorLib source needs admin rights and loads a driver.

## Layout

- `nuget.config` — clears the global feed, uses nuget.org only.

Planned: `src/SysGadget.Core/` (meters, providers, settings; no UI), `src/SysGadget.App/` (WPF window, docking, tray), `tests/`.

## Decisions

- 2026-10-25 — .NET 10 + WPF over C++/Qt/web: native transparent topmost window and AppBar docking are easy, and the machine already has the .NET toolchain.
- 2026-10-25 — Sensors: performance counters are the default (no admin, no driver); LibreHardwareMonitorLib is an optional opt-in provider for temperatures and power.
- 2026-10-25 — Every meter has its own visibility setting; a hidden meter is not sampled.

---

Keep this file honest: it is the only project document that is committed. `LOG.md` is
gitignored, so anything that must survive a fresh clone belongs here, not in the log.
