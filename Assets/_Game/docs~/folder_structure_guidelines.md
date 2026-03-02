---
title: Folder Structure & Addressables Guidelines
description: Standardized directory layout for Maou-Sama-TD to keep assets organized and ready for Addressables.
---

# 📂 Project Folder Structure & Addressables Conventions

As the project scales, maintaining a strict folder hierarchy is crucial for preventing a messy `Assets/` folder and making Addressables management pain-free.

---

## 🏗️ Root Directories (`Assets/_Game/`)

All project-specific files should be placed inside `Assets/_Game/` to separate them from third-party plugins or external tools.

### 1. 📊 `Data/` (Scriptable Objects & Setup Data)
This folder holds all your **ScriptableObject instances** (Data). This is the **primary folder you will mark as Addressable** since game design data often needs fast loading or remote updating.

```
Assets/_Game/Data/
│
├── Levels/                   # LevelData SOs (e.g., 1-1, 1-2)
│   ├── Chapter1/
│   └── Chapter2/
│
├── Units/                    # UnitData SOs (Stats, basic info)
│   ├── Vassals/              # Player-controlled units
│   ├── Enemies/              # Enemy units (Lesser-Shadow, etc.)
│   └── Assistants/           # 11th support units
│
├── Maps/                     # MapData SOs (Pathing, nodes)
└── Waves/                    # WaveData SOs (Specific wave patterns if separated)
```

**Addressables Convention for `Data/`:**
*   **Groups:** Put all unit data in a `UnitData` Addressables Group, and Level data in a `LevelData` Group.
*   **Labels:** Use labels like `Vassal`, `Enemy`, `Chapter1`.
*   **Address:** Simplify addresses from the long path to just the filename (e.g., `IgnisUnitData`).

---

### 2. 🎨 `Art/` (Visual Assets)
Everything purely visual goes here. Keep textures, materials, and source models close to each other, grouped by the "Subject".

```
Assets/_Game/Art/
│
├── Characters/
│   ├── Vassals/
│   │   └── Ignis/            # Ignis's Sprites, Spine files, Materials
│   └── Enemies/
│       └── LesserShadow/
│
├── Environments/             # Tilesets, Backgrounds, Props
├── UI/                       # Icons, Panels, Buttons (Sprites only)
└── VFX/                      # Particle Systems, related textures/materials
```

---

### 3. 📦 `Prefabs/` (Constructed GameObjects)
The assembled GameObjects that combine Scripts, Art, and Logic.

```
Assets/_Game/Prefabs/
│
├── Units/
│   ├── Vassals/              # The actual GameObject for Ignis
│   └── Enemies/              # The actual GameObject for Lesser-Shadow
│
├── Levels/                   # Prefabs for map visualization
├── UI/                       # UI Canvases, Modals, Panels
└── Projectiles/              # Fireballs, Arrows, Magic blasts
```

**Addressables Convention for `Prefabs/`:**
*   Many prefabs (like units or maps) will be spawned at runtime via Addressables.
*   **Address:** Keep it clean -> `Ignis_Prefab`, `Level_1-1_Map_Prefab`.

---

### 4. 📜 `_Scripts/` (C# Code)
Code is organized by system, not by what GameObject it attaches to.

```
Assets/_Game/_Scripts/
│
├── Levels/                   # LevelData.cs, WaveData.cs, MapData.cs
├── Units/                    # Unit.cs, UnitData.cs, EnemyController.cs
├── UI/                       # MainMenu/, InGame/, Generic/
├── Managers/                 # GameManager.cs, SaveManager.cs
├── Economy/                  # Currency, Gacha rules
└── Utils/                    # Generic helpers, Extensions
```

---

### 5. 🔊 `Audio/` (Sound Assets)
```
Assets/_Game/Audio/
│
├── BGM/                      # Background Music
├── SFX/                      # Sound Effects (UI clicks, impacts)
└── Voices/                   # Character voicelines
```

---

## 📌 General Naming Rules
1.  **PascalCase for Folders & Scripts:** `EnemyUnits`, `MainMenu`, `LevelData`.
2.  **Suffixes for Clarity:** 
    *   Prefabs: `Ignis_Prefab`
    *   Data: `Ignis_Data` or `IgnisUnitData`
    *   Controllers: `IgnisController`
3.  **No Spaces:** Use `_` or PascalCase (e.g., `Level_1-1` instead of `Level 1-1`). This prevents pathing errors in some build pipelines.
