<div align="center">

# ⚡ Loadout

**L'optimisation PC pour le gaming — puissante, mais sûre et 100 % réversible.**

[![CI](https://github.com/Zedoww/loadout/actions/workflows/ci.yml/badge.svg)](https://github.com/Zedoww/loadout/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6.svg)](#)

*Compose ton « loadout » d'optimisations, déclenche un **Surge**, joue. Puis restaure tout en un clic.*

</div>

---

## Pourquoi Loadout ?

La plupart des « optimiseurs PC » sont des boîtes noires qui charcutent ton système
sans filet. Loadout part d'un principe **non négociable** :

> **Toute optimisation est sûre et entièrement réversible.**
> L'état d'origine est sauvegardé avant chaque modification, et restaurable à tout moment.

Le code est open source, sans télémétrie, et entièrement auditable.

## Fonctionnalités

| Module | Ce que ça fait | Réversible |
|--------|----------------|:----------:|
| 📊 **Tableau de bord** | Charge et température CPU/GPU, utilisation RAM en temps réel (LibreHardwareMonitor) | — |
| 🚀 **Surge** | Mode performance en un clic : plan d'alimentation haute perf, libération RAM, mise en pause des applis d'arrière-plan | ✅ snapshot |
| 🧩 **Processus** | Liste des applis les plus gourmandes ; suspension/reprise réversible (`NtSuspendProcess`) | ✅ |
| ⚙️ **Tweaks** | Optimisations registre connues (Game DVR, HAGS, réactivité système, bridage réseau…) avec sauvegarde de la valeur d'origine | ✅ backup |
| 🧹 **Nettoyage** | Suppression des fichiers temporaires + point de restauration Windows | ✅ |
| 🎨 **Réglages** | Thème clair/sombre, à propos | — |

Tout ce qui modifie le système écrit d'abord son état dans `%APPDATA%\Loadout\`
(`surge-state.json`, `tweaks-backup.json`) pour garantir la restauration.

## Architecture

```
Loadout.sln
└─ src/
   ├─ Loadout.Core/        Logique système, sans dépendance UI (100 % testable)
   │  ├─ Monitoring/       Capteurs matériels
   │  ├─ Optimization/     Surge, processus, tweaks, plan d'alim, mémoire, temp
   │  └─ Backup/           Points de restauration
   ├─ Loadout.App/         Interface WPF Fluent (MVVM)
   │  ├─ ViewModels/       Un ViewModel par écran
   │  ├─ Views/            Pages XAML
   │  └─ Services/         Composition root (injection de dépendances)
   └─ Loadout.Core.Tests/  Tests unitaires xUnit
```

**Choix techniques :**

- **.NET 9 / WPF** avec **[WPF-UI](https://github.com/lepoco/wpfui)** (design Fluent / Mica)
- **MVVM** via [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Injection de dépendances** (`Microsoft.Extensions.DependencyInjection`)
- **Journalisation structurée** via [Serilog](https://serilog.net/) → `%APPDATA%\Loadout\logs`
- **Tests** xUnit sur toute la logique métier, **CI GitHub Actions** sur chaque PR

## Démarrer

Prérequis : [.NET SDK 9+](https://dotnet.microsoft.com/download) et Windows 10/11.

```bash
git clone https://github.com/Zedoww/loadout.git
cd loadout

dotnet build          # compiler
dotnet test           # lancer les tests

# Lancer l'application (droits administrateur requis — accepter l'UAC)
dotnet run --project src/Loadout.App
```

> Loadout requiert l'élévation pour gérer plan d'alimentation, registre, processus
> et capteurs matériels. Aucune donnée n'est envoyée sur le réseau.

## Outils de développement

Le repo intègre trois outils pour maximiser l'efficacité du développement avec les agents IA (Claude Code, etc.) :

| Outil | Rôle | Gain |
|-------|------|------|
| [CodeGraph](https://github.com/colbymchenry/codegraph) | Indexe le code en un graph sémantique (MCP) — les agents IA naviguent la structure sans scanner les fichiers | Moins d'appels, contexte précis |
| [RTK](https://github.com/rtk-ai/rtk) | Compresse la sortie terminal (`dotnet build`, `git status`…) avant envoi à l'IA | –60–90 % tokens d'entrée |
| [Caveman](https://github.com/juliusbrussee/caveman) | Skill qui force l'IA à répondre ultra-concis | –65–75 % tokens de sortie |

**Installation rapide (tout-en-un) :**

```powershell
.\tools\setup-dev-tools.ps1
```

**Ou manuellement :**

```bash
# CodeGraph
npm i -g @colbymchenry/codegraph
codegraph init -i
codegraph install          # connecte à Claude Code

# RTK (nécessite Rust ou binaire depuis GitHub Releases)
cargo install --git https://github.com/rtk-ai/rtk
rtk init -g                # hooks pour Claude Code

# Caveman
npx skills add JuliusBrussee/caveman
# Activer dans la conversation : /caveman
```

## Roadmap

- [ ] Overlay FPS / monitoring en jeu
- [ ] Profils d'optimisation par jeu (détection automatique du jeu lancé)
- [ ] Gestion des services Windows superflus
- [ ] Graphiques d'historique sur le tableau de bord
- [ ] Icône de barre des tâches + démarrage avec Windows

## Contribuer

Les contributions sont les bienvenues — voir [CONTRIBUTING.md](CONTRIBUTING.md).
Rappel du principe directeur : **toute modification système doit être réversible**.

## Licence

[MIT](LICENSE) — libre d'utilisation, modification et redistribution, y compris commerciale.
