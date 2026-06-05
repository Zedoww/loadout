# Security Policy

## Attack surface

Loadout runs with **administrator rights** and modifies system settings (power
plan, registry, processes). Security is therefore a first-order concern.

Project guarantees:

- **No network communication** is performed for telemetry or data collection.
  The only possible network actions are explicit and user-initiated.
- **Every system change is backed up** and reversible (see the guiding
  principle in `CONTRIBUTING.md`).
- The code is fully open source and auditable.

## Reporting a vulnerability

If you discover a security flaw, **do not open a public issue**.

Instead use
[GitHub Security Advisories](https://github.com/Zedoww/loadout/security/advisories/new)
for a private report, or contact the maintainer directly.

You can expect:

- a first response within **72 hours**;
- an assessment and, if confirmed, a prioritized fix;
- credit in the release notes (unless you prefer to remain anonymous).

## Supported versions

As the project is under active development, only the latest version of the
`main` branch receives security fixes.
