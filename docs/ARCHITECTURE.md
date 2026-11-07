# Ergonomy 0.3 architecture

Keep .NET 10 WPF. Existing application, Persian content and settings survive; migration to WinUI 3 would require replacing XAML, tray/window interop and release tooling, with no demonstrated reliability benefit for this small utility. No camera, AI inference service or browser engine is needed.

- `WellnessSchedule`: deterministic active-work clock, one pending reminder, 90 active-second delivery cooldown, per-kind snooze, injectable inactive/quiet flags. Pure C#, linked into a portable behavioral test executable.
- `ReminderEngine`: two-second dispatcher adapter, GetLastInputInfo, session switch, power event. Long gaps never count as work. Session events marshal to the UI thread. Dispose detaches static events.
- `WindowsEnvironment`: foreground window bounds compared against its monitor; public shell notification policy; nonactivating compact tool window; monitor work-area clamp with WPF transforms. No screen pixels, window titles, microphone or keyboard events collected.
- `MainWindow`: compact 92-DIP dock and expandable dashboard; tray control, drag and persisted placement, manual practice independent of schedule. Auto-open does not activate; deliberate panel interaction restores keyboard focus. Header brand hides on narrow work areas, navigation wraps.
- `BuddyControl`: original native vector geometry, 4 kinds / 7 states, 12fps dispatcher, two instances (dock/coach). Timer stops on Unloaded, hidden, idle, pause, disabled motion and system reduced animation. Body/arm/eye variation, gentle movement demonstration; no claim of biomechanical simulation. No runtime video or remote rendering.
- `SettingsStore`: same-user LocalAppData, JSON temporary write plus atomic move; startup is opt-in HKCU. `HistoryStore`: up to 200 reminder outcomes, removable in UI. History is not a work log and contains no input contents.
- `App`: single instance, crash diagnostics, bounded self-test modes.

## Glass implementation and limits

Current renderer retains WPF transparent layered window, translucent navy/lilac gradient and border/shadow. This is a composited transparency fallback; it is **not native Mica/Acrylic backdrop blur**. Avoid unsupported composition hooks. True backdrop blur needs a separately tested non-layered HWND design and remains a visual enhancement. Dark theme only in RC1.

DPI is declared PerMonitorV2. Clamp uses the current monitor, not primary screen dimensions. Mixed-DPI coordinate behavior, monitor removal, focus and shell policy still require actual Windows 11 pilot evidence. Shell policy is best effort; no undocumented Focus Assist registry inspection. No Teams/Zoom presence API or guaranteed detection of windowed meetings. Manual one-hour pause covers those cases.

## Build and validation

`dotnet run --project tests/Ergonomy.EngineTests -c Release`

`dotnet publish src/Ergonomy/Ergonomy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o release/Ergonomy-win-x64`

Published `--ui-self-test` opens/layouts real WPF controls on Windows, visits pages and renders all 28 character combinations. Set `ERGONOMY_UI_OUTPUT` to retain PNG evidence. It does not simulate pointer interactions, mixed-DPI hardware, sleep or real meetings. CI evidence and physical-desktop acceptance are separate gates.
