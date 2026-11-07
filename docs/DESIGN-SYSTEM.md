# Ergonomy design implementation

Figma: https://www.figma.com/design/511QVJ92g9EGEMYPNMtDAS

MagicPath project: 469714546869825536; component 469714750478118912, revision 469714750478118913. Interactive source preserved in `design/magicpath`. This prototype does not replace native application behavior.

| Role | Color |
|---|---|
| Surface | #162238 |
| Character ink | #132035 |
| Primary text | #F2FCFF |
| Posture | #BCA7FF |
| Eye | #6FE5D0 |
| Move | #82B7FF |
| Wellness | #F5C58D |

Type: Segoe UI/Tahoma for native Persian system fallback; RTL content, LTR brand header. Spacing 8/12/16/24, rounded cards 17–23 DIP. Visible button keyboard focus. Seven states: Idle, Happy, Reminder, Attention, Resting, Completed, Snoozed. Figma component set IDs: 2:88, 2:166, 2:244, 2:322. Native code geometry is authoritative for playback. Figma state cards are editable static references, not exported animation files.

Contrast examples (solid colors, no transparency assumption): #F2FCFF on #162238 exceeds 14:1; #6FE5D0 on #162238 exceeds 10:1. Actual transparency, high contrast mode and all focus states need Windows visual review. Default gradient uses near-opaque layers for readability.
