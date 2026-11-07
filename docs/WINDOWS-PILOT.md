# Windows 11 acceptance pilot — NOT YET EXECUTED

Use a non-elevated Windows 11 x64 session. Extract the portable ZIP before running Ergonomy.exe, or use the unsigned per-user setup. No internet connection is needed. The executable is self-contained, no .NET install required. Keep the old version in a different folder; exit it from tray before starting the new one.

Run `Test-ErgonomyPilot.ps1 -Exe <absolute path>` with your normal organizational script policy. The script renders startup screenshots, samples resources for one minute and leaves the app open. It does not declare the manual items passed.

| Test | Procedure / pass criterion | Status |
|---|---|---|
| Startup | Visible compact dock; no admin prompt or unexpected exit | Pending |
| Overlay/focus | Type in another app as auto reminder opens; no input theft; only dock rectangle intercepts clicks | Pending |
| Expansion | Toggle, double-click header, tray reopen, close to compact; keyboard Tab/Shift-Tab/Space visible focus | Pending |
| Characters | Select all four, trigger eye/posture/move practice; blink/arms/movement; Done/Snooze/idle reactions | Pending |
| Schedule | Set eye/posture/move to 1/2/3 min for test; verify one at a time; restore 20/30/55 | Pending |
| Practice | Practice just before an automatic due time must not reset automatic eye cycle | Pending |
| Actions | Done resets kind; Snooze reoffers kind after ten elapsed minutes; Skip and 90s expiry clear it | Pending |
| Idle | After 180 seconds without input, timer stops; resumes on return | Pending |
| Session/power | Lock 5 min; sleep 5 min; unlock/resume without adding sleep to active work or producing burst | Pending |
| Quiet/fullscreen | F11 browser, PowerPoint presentation, game; dock hidden, no auto reminder; return restores it | Pending |
| Windowed meeting | Use pause (automatic provider meeting detection is not implemented) | Pending |
| DPI/monitors | Drag across 100/150/200% displays; change resolution/scale, unplug monitor; keep controls reachable | Pending |
| Position | Exit/restart; saved position restored/clamped; taskbar work area respected | Pending |
| Offline | Disconnect network; all coaching, settings and reminders continue | Pending |
| Settings | Restart; preferences persist; malformed settings safely fall back; startup default off | Pending |
| History | Done/Snooze/Skip logged locally, practice excluded, clear retained after restart | Pending |
| Motion | Disable animations/reduced motion; characters static; no decorative continuous repaint | Pending |
| Resources | Sample normal/hidden/reduced-motion for 30 min each, compare CPU/handles/private bytes; investigate persistent growth | Pending |
| Accessibility | Narrator, keyboard, high contrast, Persian reading order, 200% scaling; no clipping or lost focus | Pending |
| Packaging | Fresh standard user install/uninstall; retained data; launch extracted portable; verify checksums | Pending |

Record Windows build, GPU, display scales, timestamps, result and reproducible steps for failures. Attach real screenshots and resource CSV. Do not promote the candidate to stable until these pass.
