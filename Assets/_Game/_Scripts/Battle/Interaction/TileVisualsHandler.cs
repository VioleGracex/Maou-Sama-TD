using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;
using MaouSamaTD.Skills;
using MaouSamaTD.Levels;
using System.Collections.Generic;

namespace MaouSamaTD.Managers.Interaction
{
    public class TileVisualsHandler
    {
        private GridManager _gridManager;

        // Settings (Passed from Manager Serialized Fields)
        public Color RangeColor;
        public Color ValidColor;
        public Color InvalidColor;
        public bool UseFullFillRange;
        public bool UseFullFillPlacement;
        public bool UseFullFillSkills;
        public List<Vector2Int> AllowedTiles = new List<Vector2Int>();

        public TileVisualsHandler(GridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public void UpdateVisuals(
            UnitData activeUnit, 
            bool isDragging, 
            bool isSkillTargeting, 
            SovereignRiteData selectedSkill,
            Tile hoverTile,
            PlayerUnit inspectedUnit,
            EnemyUnit inspectedEnemy = null)
        {
            if (_gridManager == null) return;

            bool isUnitActive = activeUnit != null;
            bool isSkillActive = isSkillTargeting && selectedSkill != null;

            foreach (var tile in _gridManager.GetAllTiles())
            {
                bool shouldHighlight = false;
                Color highlightColor = Color.black;
                bool useFullFill = false;

                if (isUnitActive)
                {
                    bool isValidType = IsTileValidForUnit(tile, activeUnit);
                    bool isValidPlacement = isValidType && !tile.IsOccupied;
                    bool isInRange = false;

                    if (hoverTile != null && activeUnit.Range > 0)
                    {
                        // Use pattern for ghost range if available (or default to circular)
                        float dist = Vector2.Distance(
                            new Vector2(tile.Coordinate.x, tile.Coordinate.y), 
                            new Vector2(hoverTile.Coordinate.x, hoverTile.Coordinate.y));
                        if (dist <= activeUnit.Range) isInRange = true;
                    }

                    if (tile == hoverTile)
                    {
                        shouldHighlight = true;
                        highlightColor = isValidPlacement ? ValidColor : InvalidColor;
                        useFullFill = UseFullFillPlacement;
                    }
                    else if (isInRange)
                    {
                        shouldHighlight = true;
                        highlightColor = RangeColor;
                        useFullFill = UseFullFillRange;
                    }
                    else if (isValidPlacement)
                    {
                        shouldHighlight = true;
                        highlightColor = ValidColor * 0.7f;
                        useFullFill = UseFullFillPlacement;
                    }
                    else if (isValidType && AllowedTiles.Count > 0)
                    {
                        // Explicitly show restricted tiles as red during tutorial
                        shouldHighlight = true;
                        highlightColor = InvalidColor * 0.5f;
                        useFullFill = UseFullFillPlacement;
                    }
                }
                else if (inspectedUnit != null)
                {
                    if (inspectedUnit.IsTargetInPattern(inspectedUnit.CurrentTile.Coordinate, tile.Coordinate, inspectedUnit.Data.AttackPattern, inspectedUnit.Range))
                    {
                        bool canAttack = true;
                        {
                            // A melee unit cannot attack tiles that no enemy can ever stand on.
                            // This covers:
                            //   - HighGround/DecoHighGround: only reachable by ranged classes
                            //   - NonWalkableDecor / Wall / None: impassable scenery, never occupied
                            var tileType = tile.Type;
                            bool tileIsUnreachable =
                                tileType == TileType.NonWalkableDecor ||
                                tileType == TileType.Wall             ||
                                tileType == TileType.None;

                            bool iAmHighGround = inspectedUnit.CurrentTile != null && inspectedUnit.CurrentTile.IsHighGround;
                            bool tileIsHighGround = tile.IsHighGround; // HighGround / DecoHighGround / SpawnPointHigh / ExitPointHigh

                            if (tileIsUnreachable)
                            {
                                // Impassable scenery — no unit can ever stand here, so it can't be attacked
                                canAttack = false;
                            }
                            else if (tileIsHighGround && !iAmHighGround)
                            {
                                // High ground tile, and I'm on low ground.
                                // Only Ranger / Warlock / Sage / Support are placed on high ground and can
                                // fire across height difference. Everyone else cannot.
                                bool isLongRangeClass = false;
                                if (inspectedUnit is PlayerUnit pu && pu.Data != null)
                                {
                                    // Mirror UnitData.ViableTiles: only these classes are placed on high ground.
                                    var cls = pu.Data.Class;
                                    isLongRangeClass = cls == UnitClass.Ranger  ||
                                                       cls == UnitClass.Warlock ||
                                                       cls == UnitClass.Sage    ||
                                                       cls == UnitClass.Support;
                                }
                                if (!isLongRangeClass) canAttack = false;
                            }
                        }

                        shouldHighlight = true;
                        if (canAttack)
                        {
                            // Valid attack tile: use the configured range color (blue) at its configured alpha
                            highlightColor = RangeColor;
                        }
                        else
                        {
                            // Invalid attack tile (e.g. melee can't hit high ground): faint red overlay.
                            // We use InvalidColor RGB but clamp its alpha to match the visual weight of the
                            // range indicator so the red reads clearly without dominating the screen.
                            highlightColor = InvalidColor;
                            highlightColor.a = Mathf.Max(RangeColor.a, 0.15f); // at least 15% opacity for readability
                        }
                        useFullFill = UseFullFillRange;
                    }
                }
                else if (inspectedEnemy != null)
                {
                    Vector2Int myCoord = _gridManager.WorldToGridCoordinates(inspectedEnemy.transform.position);
                    if (inspectedEnemy.IsTargetInPattern(myCoord, tile.Coordinate, inspectedEnemy.EnemyData.AttackPattern, inspectedEnemy.Range))
                    {
                        shouldHighlight = true;
                        highlightColor = RangeColor;
                        highlightColor.a = RangeColor.a; 
                        useFullFill = UseFullFillRange;
                    }
                }
                else if (isSkillActive && hoverTile != null)
                {
                    if (selectedSkill.IsInShape(hoverTile.Coordinate, tile.Coordinate))
                    {
                        shouldHighlight = true;
                        highlightColor = selectedSkill.BaseVisuals.RangeIndicatorColor;
                        // INCREASED visibility for the AOE shape
                        highlightColor.a = (tile == hoverTile) ? 0.6f : 0.4f; 
                        useFullFill = UseFullFillSkills;
                    }
                }

                tile.SetHighlight(shouldHighlight, highlightColor, useFullFill);
            }
        }

        private bool IsTileValidForUnit(Tile tile, UnitData unit)
        {
            if (unit == null || tile == null) return false;
            
            if (AllowedTiles != null && AllowedTiles.Count > 0)
            {
                if (!AllowedTiles.Contains(tile.Coordinate)) return false;
            }

            return unit.ViableTiles != null && unit.ViableTiles.Contains(tile.Type);
        }
    }
}
