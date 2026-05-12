# Stage 3: Validation & UX

Focus: Transparency in combat results and UI cleanup.

## Proposed Changes

### 1. Combat Logging
- Inject `Debug.Log` or `BattleLogManager` entries in `TakeDamage` to show:
  - `Raw Damage`
  - `Defense Reduction`
  - `Final Damage`

### 2. UI Polish
- Ensure the Unit Inspector UI looks clean after removing Resistance.
- Align stat icons if gaps are created.

## Verification
- Check Console for combat logs during Level 2 Boss fight.
