# Opti — Optimisation PC Gaming

Application desktop Windows pour optimiser ton PC pour le jeu, avec un principe non négociable : **tout est sûr et réversible**.

## Stack

- **.NET 9** / **C#**
- **WPF + WPF-UI** (design Fluent / Mica)
- **MVVM** via CommunityToolkit.Mvvm
- **LibreHardwareMonitor** pour les capteurs (CPU/GPU/RAM, températures)

## Structure

```
Opti.sln
└─ src/
   ├─ Opti.Core/   Logique système (monitoring, optimisations, backups)
   └─ Opti.App/    Interface WPF Fluent
```

## Fonctionnalités (MVP)

| Page           | Ce que ça fait                                                              | Réversible |
|----------------|----------------------------------------------------------------------------|------------|
| Tableau de bord | Charge et température CPU/GPU, utilisation RAM en temps réel               | —          |
| Mode jeu        | Plan d'alimentation hautes performances + libération de la RAM            | ✅ snapshot |
| Nettoyage       | Suppression des fichiers temporaires + création d'un point de restauration | ✅          |

Le mode jeu sauvegarde l'état précédent dans `%APPDATA%\Opti\boost-state.json` :
le bouton « Désactiver et restaurer » rétablit le plan d'alimentation d'origine.

## Lancer

L'application requiert les droits administrateur (gestion du plan d'alimentation,
capteurs matériels, point de restauration).

```powershell
# Compiler
dotnet build

# Lancer (accepter l'invite UAC)
.\src\Opti.App\bin\Debug\net9.0-windows\Opti.exe
```

## Roadmap

- Gestion des processus et services Windows superflus pendant le jeu
- Tweaks registre réversibles (Game DVR, planification GPU matérielle, etc.)
- Overlay FPS / monitoring en jeu
- Profils d'optimisation par jeu

## Contribuer

Les contributions sont les bienvenues. Le principe directeur est non négociable :
**toute optimisation doit être sûre et entièrement réversible** (sauvegarde de
l'état avant modification + possibilité de restaurer).

## Licence

Distribué sous licence [MIT](LICENSE). Tu es libre de l'utiliser, le modifier et
le redistribuer, y compris dans un cadre commercial.
