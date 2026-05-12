# Stage 1: Combat Standardization & Stat Cleanup (COMPLETED)

Focus: Fixing the "0 damage" issue and removing redundant stats.

## Proposed Changes

### 1. Stat Removal (Resistance)
- [x] **UnitData.cs**: Delete `Resistance` float.
- [x] **UnitDataEditor.cs**: Remove UI field for Resistance.
- [x] **UI Panels**: Remove Resistance text updates in `UnitInspectorStatsPanel` and `VassalDetailPanel`.

### 2. Damage Formula (UnitBase.cs)
- [x] Standardize all damage to use **Defense** (flat reduction).
- [x] Implement `Mathf.Max(1, finalDamage)` to ensure a minimum floor.

### 3. Boss Balancing
- [x] **EnemySO_Abyssal-Shade-Boss.asset**: Set `AttackPower` to 110.
- [x] **EnemyData.cs**: Add `DamageType` field.
- [x] **EnemyDataEditor.cs**: Expose `DamageType`.

## Verification
- [x] Attack Ignis with the Boss; verify damage is ~26 (110 Attack - 84 Defense).
