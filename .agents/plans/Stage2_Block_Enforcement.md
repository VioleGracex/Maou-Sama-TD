# Stage 2: Block Enforcement

Focus: Respecting the `BlockCount` property defined in UnitData.

## Proposed Changes

### 1. Player Capacity (PlayerUnit.cs)
- Track `_currentBlockedCount`.
- Add `CanBlockMore()` helper.
- Update `OnDeath` to release all blocked enemies.

### 2. Enemy Interaction (EnemyUnit.cs)
- Modify movement logic to only assign `_blockedBy` if the target `CanBlockMore()`.
- Ensure enemies resume movement if their blocker dies or reaches capacity.

## Verification
- Spawn 5 enemies; verify only 3 are blocked by a unit with BlockCount 3.
