# v2 glass dock QA and next steps

## UX

- Main window is a slim (766 x 92 DIP) topmost capsule anchored at screen center.
- Expand/collapse with the chevron; no desktop activation requested.
- Today / Standards / Desk / Settings tabs; built-in catalog, no timing forms.
- Reminder automatically expands the dock and shows the relevant educational prompt.
- Manual eye countdown does not automatically mark the break complete; user confirms.
- User can snooze 10 min or pause reminders 1 hour, and resume.
- Hydration disabled by default, autostart enabled by default on new installations.
- All data remains on device, error logs to LocalAppData only.
- Native Acrylic/live background blur is not currently implemented; layered WPF visual fallback.

## Reliability and limitations

- Fatal startup errors and dispatcher errors are recorded and a visible message points to the log.
- Failures in tray icon creation or registry autostart do not stop app startup.
- Startup self-test covers catalog and configuration; it cannot verify interactive WPF rendering.
- Never automatically conclude a reminder was acted upon.
- App runs in single instance and suppresses work time while idle 180 seconds or workstation locked.
- No audio, webcam, screenshot or keyboard-hook monitoring.
- Default Windows display only; if display settings change, dock recenters on primary screen.
- Needs Windows 11 desktop QA for mixed DPI, docking, accessibility, taskbar, fullscreen and sleep/resume.
- Signed MSI/MSIX/Intune deployment is pending.

## If the prior version crashed

A specific root cause cannot be verified without the old Windows error message or crash log. The original executable had no startup exception handling and offered no diagnostic output. Do not present the new code as a proven fix for an unidentified crash before physical validation.

## Design and ergonomic sources

- Microsoft Fluent design materials: https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops
- WPF transparent windows: https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.allowstransparency
- WPF layering and HWND constraints: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/technology-regions-overview
- UK HSE screen breaks: https://www.hse.gov.uk/msd/dse/work-routine.htm
- UK HSE workstation assessment: https://www.hse.gov.uk/msd/dse/assessment.htm
