# QA & Testing Checklist

## Daily Development Checks
- [ ] **Build Check**: Does `npm run dev` start without errors?
- [ ] **Type Check**: Does the IDE show red squigglies? (Fix TS errors immediately).
- [ ] **Responsiveness**: Check UI on Mobile View (Chrome DevTools) and Desktop.

## Gameplay Logic Testing

### Battle
- [ ] **Pathing**: Do enemies stick to the lanes?
- [ ] **Leak**: Does Castle HP reduce when enemies reach the end?
- [ ] **Game Over**: Does the "Defeat" screen trigger at 0 HP?
- [ ] **Win**: Does "Victory" trigger after the last wave?
- [ ] **Resources**: Does Mana regenerate/drop correctly?

### Gacha
- [ ] **Deduction**: Are crystals subtracted correctly?
- [ ] **Rates**: (Manual Test) Pull 100 times, ensure roughly 2 SSRs (if 2% rate).
- [ ] **Pity**: Does the 90th pull guarantee SSR?
- [ ] **Duplicates**: Do duplicates convert to Shards instead of a new unit?

### Save System
- [ ] **Reload**: Reload the page. Is the squad selection and currency preserved?
- [ ] **Version Migration**: If we add a new field to `PlayerSaveData`, does the old save file crash the app? (Need migration logic).

## Performance
- [ ] **Entity Count**: Spawn 50 enemies. Does FPS drop below 30?
- [ ] **Memory**: Monitor heap usage during long sessions.
