# Contribuer à Loadout

Merci de l'intérêt que tu portes au projet ! Loadout vise à devenir une
référence open source d'optimisation PC pour le gaming. Toute contribution est
la bienvenue : code, documentation, tests, idées, signalements de bugs.

## Le principe non négociable : sûr et réversible

Loadout repose sur une règle absolue :

> **Toute optimisation doit être sûre et entièrement réversible.**

Concrètement, toute fonctionnalité qui modifie l'état du système doit :

1. **Sauvegarder l'état d'origine** avant toute modification.
2. **Proposer une restauration** fiable de cet état.
3. Privilégier les actions réversibles (suspendre un processus plutôt que le
   tuer, désactiver un service plutôt que le supprimer, etc.).

Une PR qui introduit une modification système irréversible sans backup sera
refusée, quelle que soit sa qualité technique.

## Prérequis

- [.NET SDK 9](https://dotnet.microsoft.com/download) ou supérieur
- Windows 10/11 (l'application cible Windows)
- Un éditeur : Visual Studio 2022, Rider ou VS Code

## Démarrer

```bash
git clone https://github.com/Zedoww/loadout.git
cd loadout
dotnet build
dotnet test
dotnet run --project src/Loadout.App
```

> L'application requiert les droits administrateur (capteurs matériels, plan
> d'alimentation, registre). Lance-la depuis un terminal élevé en dev.

## Structure

```
src/
├─ Loadout.Core/        Logique système, sans dépendance à l'UI (testable)
├─ Loadout.App/         Interface WPF (Fluent), MVVM
└─ Loadout.Core.Tests/  Tests unitaires du cœur
```

La règle d'or d'architecture : **toute la logique testable vit dans
`Loadout.Core`**, l'UI ne fait qu'orchestrer.

## Workflow de contribution

1. Crée une branche : `git checkout -b feat/ma-fonctionnalite`
2. Code, en respectant le `.editorconfig` et le style existant.
3. Ajoute des tests pour toute logique dans `Loadout.Core`.
4. Vérifie que tout passe : `dotnet build` puis `dotnet test`.
5. Ouvre une Pull Request en décrivant **ce qui est modifié sur le système et
   comment c'est restauré**.

## Style de commits

Format [Conventional Commits](https://www.conventionalcommits.org/) recommandé :

```
feat: ajout de la suspension des processus pendant Surge
fix: correction de la lecture de température GPU AMD
docs: mise à jour du README
test: couverture du parsing des plans d'alimentation
```

## Signaler un bug ou proposer une idée

Ouvre une [issue](https://github.com/Zedoww/loadout/issues) en utilisant le
template approprié.
