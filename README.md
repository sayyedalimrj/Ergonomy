# Ergonomy — Glass Dock for Windows

Version 0.2 technical preview — always-on-top ergonomic and workplace-HSE assistant for Windows 11.

## What changed

The original dashboard has been replaced with a floating top-center glass-styled island, visible while using other desktop programs. Expand the island to open a colorful, animated RTL Persian control center. Auto reminders start immediately: there are no interval fields to fill in.

- 20-min eye reminder with optional 20-second countdown (does not assume user completed it).
- 30-min posture self-check; this is a product heuristic, not an ISO legal timing requirement.
- 55-min activity change / break reminder inspired by UK HSE short frequent breaks.
- Optional 120-min hydration nudge, disabled by default.
- Curated HSE guidance catalog with source links on eye health, computer equipment, posture, breaks, cables, electricity, exits, lifting, thermal comfort, mental wellbeing, workplace assessment and management.
- Interactive workstation checklist.
- Topmost capsule, animated expand/collapse, animated pulse, gradients and translucent layered glass styling.
- Pause/resume, automatic idle and locked-session suppression.
- Startup with Windows enabled by default if policy allows; single instance.
- Startup diagnostics in a local log; optional tray access.
- No webcam, screenshots, content monitoring, employee tracking, account, network API or cloud storage.

## Download and test

Open the GitHub Actions Windows build workflow, select a successful run on branch feat/glass-dock-v2, and download artifact Ergonomy-GlassDock-v2-Windows-x64.

Extract the entire ZIP to a stable folder, quit any prior Ergonomy version, and launch Ergonomy.exe. A self-contained build should not require installing the .NET runtime.

To compile locally on Windows 11 with .NET 10 SDK:

    dotnet run --project src/Ergonomy/Ergonomy.csproj

To publish:

    dotnet publish src/Ergonomy/Ergonomy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

## If the program vanishes or fails to launch

The new version writes diagnostics to:

    %LOCALAPPDATA%\Ergonomy\logs\app.log

In File Explorer address bar paste this exact path. Include this log and Windows 11 build version in any issue report. A Windows compile and headless self-test are not proof of functional behavior under a real logged-in desktop. Multi-monitor, focus mode, full-screen, DPI and security software tests are still needed.

## Important boundaries

Glassy here means a translucent gradient layered WPF dock with glowing highlights. True live Acrylic blur of desktop content behind the dock is not yet included; layered windows and DWM backdrops have limitations, so this version deliberately uses a stable visual fallback. No ISO certification or full international legal compliance is claimed. Copyrighted ISO standards are referenced by scope, not reproduced.

The app cannot detect whether a person is sitting correctly without sensors; posture check-ins are self-assessment prompts. The built-in guidance covers essential office-HSE topics but does not replace a site risk assessment or qualified HSE evaluation.

See docs/ERGONOMICS-EVIDENCE.md and docs/OFFICE-HSE-CHECKLIST.md.
