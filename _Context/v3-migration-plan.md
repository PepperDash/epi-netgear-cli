# v3 Migration Plan — epi-netgear-cli

> Generated 2026-08-13 by essentials-epi agent (read-only `scan-plugin-v3-readiness.ps1`, Mode V3). Re-run the scan before starting work in case the repo has drifted since this was written.
> Work happens on branch `feature/v3-migration`.

## Program Context

This repo is 1 of 5 EPIs being converted to Essentials v3 in this effort:
`epi-videoCodec-ciscoExtended`, `epi-cisco-cli`, `epi-crestron-nvx`, `epi-epson-projector`, `epi-netgear-cli`.
`epi-videoCodec-ciscoExtended` is on hold until its in-progress feature branches are merged to `main` — do not start that one from this plan set.

## Current State

| | |
|---|---|
| Target framework | `net472` |
| Essentials version | `2.29.0-feature-add-network-switch-poe-interfaces.4` |
| `SERIES4` define | present |
| C# files | 8 (smallest of the 5) |
| `#if SERIES4` conditionals | 0 |
| Removed .NET APIs | 0 |
| Factory | 1 — `src\NetgearCliFactory.cs`, class `NetgearCliFactory`, `MinimumEssentialsFrameworkVersion = "2.0.0"` |
| 3-Series artifacts | `GetPackages.BAT` |
| Debug.Console remaining | 0 (18 calls already migrated to Serilog) |

## Risk: **Low**

Smallest repo of the 5, no conditionals, no removed APIs.

⚠️ **Scanner false-positive, already investigated — no action needed on this:** the automated scan flagged `NetgearCliFactory`'s `TypeNames`/`MinimumEssentialsFrameworkVersion` as suspicious (`"SamsungMdc"`, garbled version string). Confirmed by direct read: this is leftover **template XML doc-comment/`<example>` text**, not real code. The actual constructor fields are correct (`MinimumEssentialsFrameworkVersion = "2.0.0"`, `TypeNames = { "NetgearCli" }`). No fix required — only the real version bump below is needed. Optionally clean up the stale doc-comment `<example>` block while touching this file (cosmetic, not required for v3).

## Skill to Use

[`sub-agents/essentials-epi/skills/migrate-v2-to-v3/SKILL.md`](../../../skills/migrate-v2-to-v3/SKILL.md) — standard 5-phase workflow. No routing migration needed.

## Pre-Flight (do first, every session — per essentials-epi's mandatory git pre-flight rule)

1. `git fetch`, confirm branch is `feature/v3-migration`, confirm 0 behind upstream, confirm clean working tree.
2. Confirm this branch is still correctly based on an up-to-date `main`.

## Task Checklist

- [ ] Re-run `analyze-plugin` scan to confirm nothing changed since this plan was written
- [ ] Phase 2a: `<TargetFramework>net472</TargetFramework>` → `net8` in `src\epi-netgear-cli.4Series.csproj`
- [ ] Phase 2a: remove both `SERIES4` `DefineConstants` `PropertyGroup` blocks
- [ ] Phase 2a: bump `PepperDashEssentials` PackageReference to `3.0.0`
- [ ] Phase 2b: `MinimumEssentialsFrameworkVersion = "2.0.0"` → `"3.0.0"` in `NetgearCliFactory.cs`
- [ ] Phase 2c: delete `GetPackages.BAT`
- [ ] Phase 3a: no `#if SERIES4` conditionals to remove (verify still true)
- [ ] Phase 3b: no `Debug.Console` calls to migrate (verify still true — grep to confirm 0 remaining)
- [ ] Phase 3c: check `Initialize()`/`CustomActivate()` visibility and any external callers
- [ ] Phase 4: verify join map — all fields `public`, `base(joinStart, typeof(...))`, `[JoinName]` attributes match
- [ ] Phase 5: `dotnet restore` + `dotnet build` on `epi-netgear-cli.4Series.sln`, fix any compile errors
- [ ] Update README's "Minimum Essentials Framework Versions" section if present
- [ ] Commit with `feat!:` / `BREAKING CHANGE:` footer (major version bump)
- [ ] Verify `output/` `.cplz` builds

## Notes / Flags

- See scanner false-positive note above — no real defect, just verify while in the file.
