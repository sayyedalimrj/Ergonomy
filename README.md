# Ergonomy — Workplace Wellness Assistant

Ergonomy is a privacy-first Windows desktop reminder for screen breaks, posture check-ins, and movement during office work.

## MVP preview

- 🖥️ Windows WPF (.NET 10) Persian / right-to-left control center
- 👁️ Eye refocus: 20 minutes / 20 seconds / at least 20 feet (6 m)
- 🪑 Posture self-check: every 30 active minutes (product default, **not** a regulatory requirement)
- 🚶 Movement / screen break: after 55 active minutes; suggested change of activity or 5–10 minute pause
- 💧 Optional hydration reminder, off by default
- ✨ Non-focus-stealing, animated topmost desktop popup with Snooze and Done
- 🕒 Tracks idle periods rather than full wall-clock time, suppresses reminders when the Windows session is locked
- 🔔 System tray background mode, local JSON settings, optional Windows login startup
- 🔒 No camera access, screenshot capture, cloud analytics, monitoring, or staff surveillance

## Run or publish

Windows 11 + .NET 10 SDK:

```powershell
dotnet run --project src/Ergonomy/Ergonomy.csproj
```

Self-contained publish for Windows x64:

```powershell
dotnet publish src/Ergonomy/Ergonomy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

When CI succeeds, the Windows build workflow also uploads a downloadable artifact.

## Medical and compliance boundaries

This MVP reminds users to check their own posture; it does **not** measure or infer body position. The time schedule is configurable, not a universal occupational safety obligation, and the app is not medical equipment or a substitute for workplace assessment by HSE professionals.

Read [evidence review](docs/ERGONOMICS-EVIDENCE.md) and [development plan](docs/PRODUCT-ROADMAP.md).

**Status:** Windows compilation/behavior pending CI and real Windows device tests. Not yet approved for organization-wide deployment.
