# 0.4 — Minimal corner assistant

## Interaction

The normal state is completely concealed, with no window rectangle or visible edge strip intercepting clicks. A 200ms position-only cursor poll checks a 128-DIP-wide, 4-DIP-high zone at the top-right of the current monitor's work area. A 350ms dwell reveals a 320×76-DIP pill. Moving away retreats after 1.1s. No mouse hook, input recording or network transmission is used.

An automatic reminder slides the 320-DIP-wide coaching card out of the top-right, without taking keyboard focus. It remains until Done, Later, Hide/Escape or prompt expiry. Fullscreen/shell quiet policy overrides hover and reminders. The manual tray menu provides Show, Eye practice, Body practice, Pause/resume, Settings and Exit. The main card has two response actions. Preferences contain only startup, motion and optional water.

Retain the tested active-work scheduler, lock/power adapter, defaults, snooze semantics and local outcome persistence from 0.3. The presentation layer has been replaced; this is not a scheduler rewrite. Old source branches and settings are preserved. Old freely dragged coordinates are intentionally ignored: the new interaction is anchored to the top-right.

## Typeface

User-supplied IRANYekanX Regular and Bold WOFFs were losslessly decompressed to TTF containers with fontTools and embedded as WPF Resource assets. No system font installation is required. The Windows render smoke resolves the embedded glyph typeface and checks the Persian Yeh glyph. These are user-supplied font assets; no independent open-font license is asserted.

## Glass

A non-layered WindowChrome window requests Windows 11's documented DWM transient backdrop, immersive dark appearance and rounded corners. The WPF surface uses a translucent navy tint. If DWM rejects the backdrop, use an opaque high-contrast navy fallback. A successful HRESULT confirms request acceptance, not the perceived blur quality on every GPU/Windows build. Actual desktop blur, multi-monitor edge positions and animation require physical Windows 11 review. There is no always-visible desktop dock anymore.

## Motion is instructional

- Eye: five seconds of gentle blinking, then a full 20-second far-focus step with a visible approximately six-metre target. No circular or forced eye rotations.
- Sitting: shoulder release, comfortable neutral neck alignment, feet supported. Side-view chair, head/spine guide and feet/ground affordances.
- Move: articulate hip/knee/ankle positions from sitting to standing, alternate arm/leg stride while walking, then slow shoulder movement. The final message still suggests 5–10 minutes of activity change; the short demonstration does not replace the break.
- Water: simple glass cue, optional and no fixed dose.

Done remains an explicit self-report. Pain-free/comfortable range is stated briefly in the step hint. Reduced motion keeps static instructional poses and timed captions. Hidden, paused and idle lessons do not keep a frame timer running. The animation renderer uses no AI inference or cloud assets.

## Verification

29 portable behavioral assertions include concealed default, dwell, retreat, quiet override, reminder pin, exact eye far-focus duration, suspended/pause time and staged body coaching. Windows CI additionally tests font resolution, native compact/lesson render, physical SetCursorPos corner reveal with cursor restored, normal hidden-host/tray lifecycle and resource samples, portable self-test, installer/install/run/uninstall. These are bounded automated checks, not a long-run memory-leak or physical multi-monitor certification.
