# Sovereign Rites — Description Architecture & Color Palette

> **Architecture (Updated 2026-05-13)**  
> The description UI is split into two separate TMP text objects inside `Description_BG`:
> - **`Skill_Info_Txt`** — lore/flavour text only, italic, auto-size 8–13pt. Sourced from `SovereignRiteData.Description` (plain text, no tags).  
> - **`Skill_Stats_Txt`** — colored stats block, generated at runtime in `SkillPanelUI.UpdateSkillDescriptionUI()` from SO fields. Auto-size 7–12pt.
>
> Do **not** add rich text tags to the SO `Description` field — stats are code-driven.

---

## 🎨 Color Token Reference

| Token | Hex | Used For |
|---|---|---|
| **Damage** | `#FF4444` | Damage values |
| **Buff / Positive** | `#44FF88` | Positive stat bonuses |
| **Debuff** | `#FF8844` | Negative stat effects |
| **Duration / Time** | `#44CCFF` | Duration, cooldown info |
| **Area / Range** | `#FFDD44` | Radius, area, shape info |
| **Cost** | `#CC88FF` | Seal cost |
| **Label** | `#AAAAAA` | Dim labels (e.g. "Targets:") |
| **Header** | `#FFCC00` | Section header / rite name |

---

## 📜 Rite Descriptions (Archetype: Female)

### Dark Blessing (`Empower_Female`)
- **Cost:** 15 SP | **Cooldown:** 15s | **Effect:** Buff | **Target:** Single Unit | **Duration:** 20s
- **Modifiers:** Attack +50%, AttackSpeed +30%

```
Infuse a vassal with your dark Authority, greatly increasing their combat prowess.\n\n<color=#AAAAAA>Target:</color> <color=#44CCFF><b>Single Vassal</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>20s</b></color>\n<color=#44FF88><b>+50% Attack</b></color>\n<color=#44FF88><b>+30% Attack Speed</b></color>
```

---

### Event Horizon (`EventHorizon_Female`)
- **Cost:** 75 SP | **Cooldown:** 30s | **Effect:** Damage | **Target:** Tile | **Radius:** 0 (Single Point)

```
Condensing dark matter into a microscopic singularity, dealing devastating damage.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Single Tile</b></color>\n<color=#FF4444><b>2,500 Magic DMG</b></color>\n<color=#AAAAAA>Cooldown:</color> <color=#44CCFF><b>30s</b></color>
```

---

### Sovereign's Domain (`SovereignDomain_Female`)
- **Cost:** 15 SP | **Cooldown:** 60s | **Effect:** Buff | **Target:** Tile | **Radius:** 5 Circle | **Duration:** 15s
- **Modifiers:** Range +20%, AttackSpeed +30%

```
A majestic aura that empowers all vassals within the domain, significantly increasing their combat effectiveness.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Circle Area (Radius 5)</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>15s</b></color>\n<color=#44FF88><b>+20% Range</b></color>\n<color=#44FF88><b>+30% Attack Speed</b></color>
```

---

### Star-Fall Requiem (`StarFallRequiem_Female`)
- **Cost:** 25 SP | **Cooldown:** 45s | **Effect:** Damage | **Target:** Tile | **Radius:** 3 Circle | **Duration:** 5s (DoT)
- **Value:** 150 per hit

```
Apocalyptic meteor shower of dark-crystal projectiles over a designated area.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Circle Area (Radius 3)</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>5s</b></color>\n<color=#FF4444><b>150 Magic DMG</b></color> <color=#AAAAAA>per strike</color>
```

---

## 📜 Rite Descriptions (Archetype: Male)

### Empower (`Empower_Male`)
- **Cost:** 15 SP | **Cooldown:** 15s | **Effect:** Buff | **Target:** Single Unit | **Duration:** 20s
- **Modifiers:** Attack +50%, AttackSpeed +30%

```
Infuse a vassal with your demonic Authority, greatly increasing their combat prowess.\n\n<color=#AAAAAA>Target:</color> <color=#44CCFF><b>Single Vassal</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>20s</b></color>\n<color=#44FF88><b>+50% Attack</b></color>\n<color=#44FF88><b>+30% Attack Speed</b></color>
```

---

### Abyssal Guillotine (`AbyssalGuillotine_Male`)
- **Cost:** 75 SP | **Cooldown:** 30s | **Effect:** Damage | **Target:** Tile | **Radius:** 0 (Single Point)
- **Value:** 2,500

```
A focused, high-speed execution strike. Deals massive burst Magic Damage to a single target area.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Single Tile</b></color>\n<color=#FF4444><b>2,500 Magic DMG</b></color>\n<color=#AAAAAA>Cooldown:</color> <color=#44CCFF><b>30s</b></color>
```

---

### Grand Cross (`GrandCross_Male`)
- **Cost:** 25 SP | **Cooldown:** 45s | **Effect:** Damage | **Target:** Tile | **Radius:** 3 Custom Cross | **Duration:** 2s
- **Value:** 500

```
Pillars of crimson fire erupt from the ground in a massive cross formation.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Custom Cross (Radius 3)</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>2s</b></color>\n<color=#FF4444><b>500 Magic DMG</b></color> <color=#AAAAAA>per enemy</color>
```

---

### Tyrant's Awakening (`TyrantsAwakening_Male`)
- **Cost:** 15 SP | **Cooldown:** 60s | **Effect:** Buff | **Target:** Tile | **Radius:** 10 Circle | **Duration:** 15s
- **Modifiers:** Attack +100%, AttackSpeed +50%

```
A command that resonates with the Maou's authority, empowering all nearby vassals with increased Attack and Attack Speed.\n\n<color=#AAAAAA>Target:</color> <color=#FFDD44><b>Circle Area (Radius 10)</b></color>\n<color=#AAAAAA>Duration:</color> <color=#44CCFF><b>15s</b></color>\n<color=#44FF88><b>+100% Attack</b></color>\n<color=#44FF88><b>+50% Attack Speed</b></color>
```

---

*Last updated: 2026-05-13 — Keep in sync with `SovereignRiteData` assets whenever balancing changes.*
