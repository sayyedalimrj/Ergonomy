# Ergonomy 0.5 — right edge and independent movements

## Interaction

The hidden host now reveals from the RIGHT, below title-bar close/maximize controls. The activation strip is four physical-scaled pixels at the right work-area edge, around the dock's header, with a 350ms dwell. The old upper-right corner does not activate it. Both entry and retreat animate the horizontal Left coordinate; Top stays anchored. Quiet/fullscreen policy still overrides activation.

The + button opens exactly three quick actions: Eye, Body and About. Settings remain in the tray. About displays the user-requested attribution `Mirjafari` and `09186441801`; these are static text and do not initiate calls or send data.

## Movement controls

Each movement owns its countdown. Reaching zero stops that movement and waits for the user's Next command; it never automatically skips forward. Next/Previous select a movement and reset that movement's full duration. Clicking its countdown pauses/resumes it, or restarts it after completion. A per-movement progress bar, Persian digits and movement number make the state clear. Done remains an explicit self-report for the entire reminder.

Opening the + menu pauses progression/animation behind it. Selection resets the UI tick baseline so a newly selected movement does not inherit elapsed time from the previous one. Inactivity/lock, suspend gaps, quiet mode and reduced motion continue to be respected. Abandoned prompts expire after 180 active seconds unless the user is interacting.

Animation improvements include articulated elbows, alternating knee flexion and foot clearance for walking, eased sitting-to-standing, and preserving the demonstration pose while its timer is paused. The hidden renderer stops its animation timer.

## Short reminders and research

Posture prompts rotate among sitting, neck and relaxed-shoulder self-checks. Eye rest and movement remain automatic. Hydration uses the existing optional 120-active-minute setting; an existing user's preference is preserved rather than silently changed. Wording is a reminder, never sensor-based detection.

Primary-source recheck (2026-11-07):
- https://www.osha.gov/etools/computer-workstations/positions — balanced head/neck, relaxed shoulders, supported posture.
- https://www.osha.gov/etools/computer-workstations/checklists/evaluation — workstation adjustability, comfortable alignment.
- https://www.hse.gov.uk/msd/dse/work-routine.htm — short frequent breaks/activity changes and flexible timing.

The demo lengths and hover timing are product choices, not legal or clinical standards. No new unsolicited reminder timer was added: short self-checks reuse the existing posture cycle to avoid reminder overload. Comfortable variation is preferred over holding a rigid posture.

## Verification scope

40 portable behavioral assertions cover the existing schedule plus manual step boundaries, independent/reset timers, pause/repeat, right-edge activation at two DPIs, exclusion of title-bar corner and edge-strip width. Windows smoke additionally exercises three quick buttons, About, Previous/Next/pause, live WPF frame advancement and hidden-timer shutdown, glyphs, physical edge hover, normal lifecycle, installation and uninstallation. Physical Windows 11 mixed-monitor positioning, perceived backdrop and long-run resource validation remain a manual pilot.
