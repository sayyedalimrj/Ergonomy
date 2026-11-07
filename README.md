# Ergonomy — Right Edge Assistant

Windows 11 x64 · Persian · IRANYekanX · Offline

[Download the current candidate](https://github.com/sayyedalimrj/Ergonomy/releases).

1. Exit any older Ergonomy instance from its tray menu.
2. Install the unsigned per-user setup, or extract the portable ZIP and run `Ergonomy.exe`.
3. The assistant starts fully hidden. Hover briefly at the right edge, below the title-bar controls to reveal it. It slides horizontally out of the right edge automatically for a reminder and retreats when you leave or dismiss it.

The compact card uses short instructions, **Done/Later**, and a **+** menu with three quick actions. Each movement has an independent timer, Previous/Next and pause/repeat controls. About includes the requested developer attribution. The character demonstrates blinking/far focus, seated posture, standing up, walking and gentle shoulder movement. Three preferences live in the tray: startup, motion and optional water. Eye/posture/movement schedules still start automatically at 20/30/55 active minutes. No camera, input logging, network service or posture detection.

Yekan is embedded; no font installation is needed. Windows 11 backdrop is requested through DWM, with an opaque contrast fallback if unavailable. This is a release candidate; physical Windows 11 multi-monitor/blur/accessibility and long-run resource acceptance remain pending.

- [Design, implementation and test scope](docs/RIGHT-EDGE-V5.md)
- [Windows pilot](docs/WINDOWS-PILOT.md)
- [HSE sources](docs/ERGONOMICS-EVIDENCE.md)
- [Changelog](CHANGELOG.md)

```powershell
dotnet run --project tests/Ergonomy.EngineTests -c Release
dotnet build src/Ergonomy/Ergonomy.csproj -c Release
```

Development branch: `feat/right-edge-v5`. Previous branches/releases are preserved.
