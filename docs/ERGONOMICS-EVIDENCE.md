# Ergonomy: evidence and boundaries (2026-11-07)

## Regulatory versus optional scheduling

The EU Display Screen Equipment Directive 90/270/EEC (Article 7) requires periodic interruption of screen work by breaks or changes of activity. It does not set a single universal reminder interval. UK HSE guidance suggests short frequent breaks, for example 5-10 minutes after 50-60 minutes of screen/keyboard work, rather than 20 minutes every two hours, while also emphasizing user discretion. The reminder plan is a configurable product default, not a jurisdiction-wide legal requirement.

- EU Directive: https://eur-lex.europa.eu/eli/dir/1990/270/oj/eng
- UK HSE breaks: https://www.hse.gov.uk/msd/dse/work-routine.htm
- UK HSE workstation assessment: https://www.hse.gov.uk/msd/dse/assessment.htm
- UK HSE workstation posture: https://www.hse.gov.uk/msd/dse/good-posture.htm
- US OSHA monitor: https://www.osha.gov/etools/computer-workstations/components/monitors/
- US OSHA general setup: https://www.osha.gov/etools/computer-workstations/
- CDC NIOSH breaks: https://www.cdc.gov/niosh/bulletin/2020/working-from-home.html
- AOA 20-20-20: https://www.aoa.org/healthy-eyes/eye-and-vision-conditions/computer-vision-syndrome/20-20-20-rule
- ISO 9241-5:2024 scope: https://www.iso.org/standard/86222.html
- ISO 45001:2018 scope: https://www.iso.org/standard/45001

## Default reminder policy

| Category | Default | Rationale | Mandatory? |
|---|---|---|---|
| Eye refocus | 20 minutes | Look >=20 feet/6m away >=20 seconds, AOA/NIOSH | No; widely recommended |
| Posture check | 30 minutes | Product heuristic to vary posture, avoid staying fixed | No; product choice |
| Move/change activity | 55 minutes | Offer 5-10 minutes away from screen; UK HSE example | No fixed legal minute |
| Hydration | 120 minutes, disabled by default | Optional wellness nudge, no prescribed fluid dose | No |

Intervals count active user time, stop after 3 min input inactivity, and pause when the session is locked. A snooze delays reminders 10 wall-clock minutes. The prototype does not verify that a break occurred.

## Workstation setup

- Monitor directly in front, top at/below eye level; OSHA preferred view distance 50-100cm, with individual adjustments. AOA often recommends roughly 51-71cm. Center of screen approximately 15-20 degrees below horizontal gaze.
- Neck balanced, shoulders relaxed, elbows near torso, wrists aligned with forearms.
- Back supported, feet supported; space behind knees; chair/work surface adjusted to fit the individual.
- Avoid glare and strong reflected light; change posture rather than imposing one static posture.
- On laptops used for prolonged work, consider separate keyboard/mouse and riser/external display.

## Special cases and limitations

Users with pain, visual symptoms, disability, pregnancy, post-surgical limitations, or other individual needs should receive tailored occupational-health assessment. Never give aggressive stretch instructions or collect health details without need. The app does not inspect camera, microphone, screen contents, or keystrokes, and cannot know whether posture is correct.

The ISO text is paywalled beyond its public scope: do not claim product certification or reproduce proprietary technical clauses. This research is not a legal compliance determination for any particular country.
