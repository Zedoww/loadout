# Politique de sécurité

## Surface d'exposition

Loadout s'exécute avec les **droits administrateur** et modifie des paramètres
système (plan d'alimentation, registre, processus). La sécurité est donc une
préoccupation de premier ordre.

Garanties du projet :

- **Aucune communication réseau** n'est effectuée à des fins de télémétrie ou de
  collecte de données. Les seules actions réseau possibles sont explicites et
  initiées par l'utilisateur.
- **Toute modification système est sauvegardée** et réversible (voir le principe
  directeur dans `CONTRIBUTING.md`).
- Le code est entièrement open source et auditable.

## Signaler une vulnérabilité

Si tu découvres une faille de sécurité, **n'ouvre pas d'issue publique**.

Utilise plutôt la fonction
[GitHub Security Advisories](https://github.com/Zedoww/loadout/security/advisories/new)
pour un signalement privé, ou contacte directement le mainteneur.

Tu peux t'attendre à :

- une première réponse sous **72 heures** ;
- une évaluation et, si confirmée, un correctif priorisé ;
- un crédit dans les notes de version (sauf si tu préfères l'anonymat).

## Versions supportées

Le projet étant en développement actif, seule la dernière version de la branche
`main` reçoit des correctifs de sécurité.
