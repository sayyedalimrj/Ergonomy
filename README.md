# Ergonomy — Windows Wellness Dock

A Persian/RTL, offline Windows 11 x64 wellness assistant built with WPF/.NET 10. This repository contains a **0.3.0 release candidate**, not a fully desktop-validated stable release.

## Download and run

Download the installer or portable ZIP from [Releases](https://github.com/sayyedalimrj/Ergonomy/releases) once the Windows build is green. Check `SHA256SUMS.txt` if verifying integrity. Both packages are unsigned.

- Installer: `Ergonomy-0.3.0-rc.1-setup-x64.exe`, installs for your user without Administrator.
- Portable: extract `Ergonomy-0.3.0-rc.1-win-x64.zip`, then run `Ergonomy.exe`. No separate .NET installation needed.
- Exit older Ergonomy instances from their tray menu before launching this version.
- The dock opens at the top of the display. Drag its header to move it; use the arrow or double-click header to expand/collapse.
- Closing the panel keeps the compact dock active; use the tray menu or Settings → Exit to quit.

## Included

- Compact topmost transparent dock, expandable Persian dashboard, guidance catalog and workstation self-check.
- Four original native vector companions: Posture Buddy, Eye Guardian, Move Buddy and Wellness Buddy.
- Seven character states, blinking and gentle limb/body motion, selectable buddy, reduced motion and animation toggle.
- Eye 20 min, posture self-check 30 min, movement/activity change 55 min; optional hydration 120 min off by default. Intervals and enabled categories editable.
- Idle/lock/power-aware active-work scheduler, per-kind ten-minute Snooze, Done/Skip and prompt expiry, one pending reminder and anti-burst cooldown.
- Fullscreen/public shell quiet-policy suppression, local history capped at 200 outcomes, optional Windows startup, tray control and persisted placement.
- Local diagnostics, published executable self-tests, rendered WPF smoke, portable installation scripts and per-user installer.

The transparency fallback is **not native Mica/Acrylic backdrop blur**. Windowed meeting detection is not integrated; use Pause. No camera, microphone, screen capture, keystroke logging, employee surveillance or posture detection. Self-reporting completion is not proof an exercise occurred.

## Build and test

```powershell
dotnet run --project tests/Ergonomy.EngineTests -c Release
dotnet build src/Ergonomy/Ergonomy.csproj -c Release
dotnet publish src/Ergonomy/Ergonomy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o release/Ergonomy-win-x64
```

On Windows, the published executable supports `--self-test` (settings/history round trip, corrupt JSON fallback, catalog) and `--ui-self-test` (show/layout pages and 28 character combinations). Self-tests use isolated temporary data, not the user's settings/history. Set `ERGONOMY_UI_OUTPUT` to save UI PNGs. These do **not** certify physical desktop interaction.

## Validation and development

- [Quality report](docs/QUALITY-REPORT.md)
- [Windows 11 pilot checklist](docs/WINDOWS-PILOT.md)
- [Architecture and technical limits](docs/ARCHITECTURE.md)
- [HSE sources and distinction from mandatory standards](docs/ERGONOMICS-EVIDENCE.md)
- [Design system, Figma and MagicPath references](docs/DESIGN-SYSTEM.md)
- [Changelog](CHANGELOG.md)

Source development branch: `feat/wellness-dock-v3`; earlier MVP and glass-dock-v2 branches are preserved. Keep the PR draft until real focus/tray/sleep/mixed-DPI/accessibility/resource acceptance passes. No ISO certification or medical diagnosis is claimed.
