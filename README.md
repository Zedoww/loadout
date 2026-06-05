<div align="center">

# ⚡ Loadout

**PC optimization for gaming — powerful, yet safe and 100% reversible.**

[![CI](https://github.com/Zedoww/loadout/actions/workflows/ci.yml/badge.svg)](https://github.com/Zedoww/loadout/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6.svg)](#)

*Compose your "loadout" of optimizations, trigger a **Surge**, play. Then restore everything in one click.*

</div>

---

## Why Loadout?

Most "PC optimizers" are black boxes that hack away at your system with no
safety net. Loadout starts from one **non-negotiable** principle:

> **Every optimization is safe and fully reversible.**
> The original state is saved before each change, and restorable at any time.

The code is open source, telemetry-free, and fully auditable.

### Loadout vs. CCleaner

CCleaner popularized one-click cleanup, but it is a general-purpose janitor —
not a gaming tool — and it carries baggage Loadout deliberately avoids:

| | CCleaner | Loadout |
|---|----------|---------|
| **Focus** | Generic cleanup for any user | Built for gaming performance |
| **Reversibility** | Cleanup is destructive; registry "fixes" are risky | Every change is snapshotted and restorable |
| **Telemetry / ads** | Bundled telemetry, upsells, paid tiers | None — open source, no network calls |
| **Performance mode** | None | **Surge**: one-click power/RAM/process boost |
| **Transparency** | Closed source | Fully auditable code |
| **Trust history** | Shipped a supply-chain malware build (2017) | No binaries you can't rebuild yourself |

In short: Loadout keeps the "one click and it's clean/fast" promise, but every
action is reversible, gaming-focused, and free of the telemetry and dark
patterns CCleaner is known for.

## Features

| Module | What it does | Reversible |
|--------|--------------|:----------:|
| 📊 **Dashboard** | Real-time CPU/GPU load and temperature, RAM usage (LibreHardwareMonitor) | — |
| 🚀 **Surge** | One-click performance mode: high-perf power plan, RAM release, pausing of background apps | ✅ snapshot |
| 🧩 **Processes** | List of the most resource-hungry apps; reversible suspend/resume (`NtSuspendProcess`) | ✅ |
| ⚙️ **Tweaks** | Known registry optimizations (Game DVR, HAGS, system responsiveness, network throttling…) with the original value saved | ✅ backup |
| 🧹 **Cleanup** | Temporary file removal + Windows restore point | ✅ |
| 🎨 **Settings** | Light/dark theme, about | — |

Anything that changes the system first writes its state to `%APPDATA%\Loadout\`
(`surge-state.json`, `tweaks-backup.json`) to guarantee restoration.

## Architecture

```
Loadout.sln
└─ src/
   ├─ Loadout.Core/        System logic, no UI dependency (100% testable)
   │  ├─ Monitoring/       Hardware sensors
   │  ├─ Optimization/     Surge, processes, tweaks, power plan, memory, temp
   │  └─ Backup/           Restore points
   ├─ Loadout.App/         WPF Fluent UI (MVVM)
   │  ├─ ViewModels/       One ViewModel per screen
   │  ├─ Views/            XAML pages
   │  └─ Services/         Composition root (dependency injection)
   └─ Loadout.Core.Tests/  xUnit unit tests
```

**Technical choices:**

- **.NET 9 / WPF** with **[WPF-UI](https://github.com/lepoco/wpfui)** (Fluent / Mica design)
- **MVVM** via [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Dependency injection** (`Microsoft.Extensions.DependencyInjection`)
- **Structured logging** via [Serilog](https://serilog.net/) → `%APPDATA%\Loadout\logs`
- **Tests** with xUnit over all business logic, **GitHub Actions CI** on every PR

## Getting started

Prerequisites: [.NET SDK 9+](https://dotnet.microsoft.com/download) and Windows 10/11.

```bash
git clone https://github.com/Zedoww/loadout.git
cd loadout

dotnet build          # compile
dotnet test           # run tests

# Launch the app (administrator rights required — accept the UAC prompt)
dotnet run --project src/Loadout.App
```

> Loadout requires elevation to manage the power plan, registry, processes,
> and hardware sensors. No data is sent over the network.

## Development tools

The repo integrates three tools to maximize development efficiency with AI
agents (Claude Code, etc.):

| Tool | Role | Gain |
|------|------|------|
| [CodeGraph](https://github.com/colbymchenry/codegraph) | Indexes the code into a semantic graph (MCP) — AI agents navigate the structure without scanning files | Fewer calls, precise context |
| [RTK](https://github.com/rtk-ai/rtk) | Compresses terminal output (`dotnet build`, `git status`…) before sending it to the AI | –60–90% input tokens |
| [Caveman](https://github.com/juliusbrussee/caveman) | A skill that forces the AI to answer ultra-concisely | –65–75% output tokens |

**Quick install (all-in-one):**

```powershell
.\tools\setup-dev-tools.ps1
```

**Or manually:**

```bash
# CodeGraph
npm i -g @colbymchenry/codegraph
codegraph init -i
codegraph install          # connect to Claude Code

# RTK (requires Rust or a binary from GitHub Releases)
cargo install --git https://github.com/rtk-ai/rtk
rtk init -g                # hooks for Claude Code

# Caveman
npx skills add JuliusBrussee/caveman
# Activate in the conversation: /caveman
```

## Roadmap

Items marked **(beats CCleaner)** are where Loadout aims to go beyond generic
cleaners with gaming-first, reversible features:

- [ ] FPS overlay / in-game monitoring **(beats CCleaner)**
- [ ] Per-game optimization profiles (auto-detect the running game) **(beats CCleaner)**
- [ ] Windows bloatware / superfluous service management (reversible) **(beats CCleaner)**
- [x] Smart cleanup: per-category preview & selection before deleting, with size estimates **(beats CCleaner)**
- [ ] Startup app manager (measure boot impact, disable reversibly) **(beats CCleaner)**
- [ ] Dashboard history charts
- [ ] Taskbar icon + start with Windows

## Contributing

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).
A reminder of the guiding principle: **every system change must be reversible**.

## License

[MIT](LICENSE) — free to use, modify, and redistribute, including commercially.
