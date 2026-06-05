# Contributing to Loadout

Thanks for your interest in the project! Loadout aims to become an open source
reference for gaming PC optimization. Every contribution is welcome: code,
documentation, tests, ideas, bug reports.

## The non-negotiable principle: safe and reversible

Loadout rests on one absolute rule:

> **Every optimization must be safe and fully reversible.**

Concretely, any feature that changes system state must:

1. **Back up the original state** before any modification.
2. **Offer a reliable restore** of that state.
3. Favor reversible actions (suspend a process rather than kill it, disable a
   service rather than delete it, etc.).

A PR that introduces an irreversible system change with no backup will be
rejected, regardless of its technical quality.

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download) or newer
- Windows 10/11 (the app targets Windows)
- An editor: Visual Studio 2022, Rider, or VS Code

## Getting started

```bash
git clone https://github.com/Zedoww/loadout.git
cd loadout
dotnet build
dotnet test
dotnet run --project src/Loadout.App
```

> The app requires administrator rights (hardware sensors, power plan,
> registry). Launch it from an elevated terminal during development.

## Structure

```
src/
├─ Loadout.Core/        System logic, no UI dependency (testable)
├─ Loadout.App/         WPF (Fluent) UI, MVVM
└─ Loadout.Core.Tests/  Core unit tests
```

The golden architecture rule: **all testable logic lives in `Loadout.Core`**;
the UI only orchestrates.

## Contribution workflow

1. Create a branch: `git checkout -b feat/my-feature`
2. Code, following `.editorconfig` and the existing style.
3. Add tests for any logic in `Loadout.Core`.
4. Make sure everything passes: `dotnet build` then `dotnet test`.
5. Open a Pull Request describing **what changes on the system and how it is
   restored**.

## Commit style

[Conventional Commits](https://www.conventionalcommits.org/) format is
recommended:

```
feat: add process suspension during Surge
fix: correct AMD GPU temperature reading
docs: update the README
test: cover power plan parsing
```

## Report a bug or suggest an idea

Open an [issue](https://github.com/Zedoww/loadout/issues) using the appropriate
template.
