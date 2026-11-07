# Ergonomy product roadmap

## MVP capabilities (feature branch)

- Windows WPF (.NET 10), right-to-left Persian dashboard
- Eye, posture, movement and optional hydration timers
- Active-input heuristic, stops counting after 180 seconds idle
- Windows session lock suppression; 90-second anti-spam separation
- Animated topmost popup; done / snooze / automatic timeout
- System tray, startup option, local JSON settings
- No camera, screen capture, telemetry, accounts, or cloud services

## Explicit limitations of the first MVP

- Reminder only; no computer-vision pose inference
- Primary display popup only; no sophisticated per-monitor placement
- Full-screen presentations, Focus Assist / Do Not Disturb, video calls and remote desktop require explicit integration and validation
- No accessibility review with screen readers or reduced-motion setting yet
- No MSI/MSIX installer, code signing, device management policy or company dashboard
- No medical-device claims, employee scoring or supervision

## Next production stages

1. Test on Windows 11 real devices, several DPI values, screen sizes and multi-monitor layouts.
2. Respect Focus Assist, exclusive full-screen apps, meetings, and sleep/resume; implement quiet hours.
3. Accessibility: keyboard focus, screen reader automation, high contrast, reduced motion, localization.
4. Enterprise deployment: signed MSI/MSIX, managed policy, optional machine-wide installation, Intune/GPO.
5. Privacy/security review and transparent opt-in anonymous statistics only if actually needed.
6. Workplace pilot with volunteer users and HSE assessor; collect qualitative acceptability rather than worker rankings.

## Build

Install .NET 10 SDK (Windows 11) and run:

    dotnet run --project src/Ergonomy/Ergonomy.csproj

For portable Windows x64 publishing:

    dotnet publish src/Ergonomy/Ergonomy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish

Use Windows Actions artifact after CI success. This branch has not been executed on a physical Windows desktop by the assistant.
