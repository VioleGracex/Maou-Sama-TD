# Modular Vassal Architecture

To ensure scalability for the 100+ vassal roster, Maou-Sama: TD uses a modular folder-based architecture for character data. This separates concerns between combat mechanics, visual assets, and progression logic.

## Directory Structure
Each character is located in `game/data/units/char_[id]/`.

```text
char_ignis/
├── assets.ts  # Image paths (portrait, card, chibi, sprite, cg)
├── combat.ts  # Base stats, class, primary passive, and active skills
├── stellar.ts # Stellar Gate (Soul Fusion) nodes and awakening mechanical changes
└── index.ts   # The aggregator that exports the UNIT_[ID] definition
```

## File Responsibilities

### 1. `assets.ts`
Exports a `UnitAssets` object. All paths should follow the `/images/characters/...` convention.
```typescript
export const assets: UnitAssets = { ... };
```

### 2. `combat.ts`
Exports a `Partial<UnitDefinition>` containing game-mechanic fields.
- `baseStats`: Native values before level/star modifiers.
- `passive`: The primary ability inherent to the vassal.
- `skills`: Active abilities triggered by SP.

### 3. `stellar.ts`
Exports a `StellarNode[]` array. 
- Nodes 1-5 usually provide flat stat bonuses or skill resonances.
- Node 6 is the **Awakening Node**, often linked to a `mechanicalChangeId` parsed by the battle engine.

### 4. `index.ts`
Combines the partials into a complete `UnitDefinition` and adds identity fields (`id`, `name`, `rarity`).

## Registry Pattern
The `game/data/units/registry.ts` file acts as the central hub. It imports the aggregated `UNIT_X` definitions and reduces them into the global `CHARACTERS` record. 

**Rule:** Do not add raw data directly to `registry.ts`. Always create the modular folder first.