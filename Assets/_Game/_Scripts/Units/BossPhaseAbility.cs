using UnityEngine;
using MaouSamaTD.Grid;
using System.Linq;
using System.Collections.Generic;

namespace MaouSamaTD.Units
{
    [CreateAssetMenu(fileName = "BossPhaseAbility", menuName = "MaouSamaTD/Abilities/Boss Phase")]
    public class BossPhaseAbility : EnemyAbility
    {
        [Header("Phase Settings")]
        public float PhaseHpThreshold = 0.7f; // 70% HP
        public int PhasingChargesToGrant = 5;
        
        private bool _hasPhased = false;

        public static event System.Action<EnemyUnit> OnPhaseTriggered;

        public override void OnInitialize(EnemyUnit owner)
        {
            _hasPhased = false;
        }

        public override void OnTakeDamage(EnemyUnit owner, float amount, DamageType type)
        {
            if (_hasPhased) return;

            float hpPercent = owner.CurrentHp / owner.MaxHp;
            if (hpPercent <= PhaseHpThreshold)
            {
                TriggerPhase(owner);
            }
        }

        private void TriggerPhase(EnemyUnit owner)
        {
            _hasPhased = true;
            Debug.Log($"[BossPhase] {owner.gameObject.name} triggering Phase Shift!");

            // 1. Grant Phasing Charges so it can pass through units
            // We need a way to set current phasing charges in EnemyUnit
            // I'll add a public method or property for that
            owner.SetPhasingCharges(PhasingChargesToGrant);

            // 2. Add Melee, Ranged, and Magic Immunities during phase to prevent HP from draining down to 1 HP
            if (!owner.Immunities.Contains(DamageType.Melee)) owner.Immunities.Add(DamageType.Melee);
            if (!owner.Immunities.Contains(DamageType.Ranged)) owner.Immunities.Add(DamageType.Ranged);
            if (!owner.Immunities.Contains(DamageType.Magic)) owner.Immunities.Add(DamageType.Magic);

            // Reset HP ratio to the phase threshold so it stays clean
            owner.SetHpRatio(PhaseHpThreshold);

            // 3. Teleport behind Ignis
            TeleportBehindTarget(owner);

            // Notify anyone listening (e.g., TutorialManager)
            OnPhaseTriggered?.Invoke(owner);
        }

        private void TeleportBehindTarget(EnemyUnit owner)
        {
            // Find Ignis
            var target = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && u.name.Contains("Ignis"));
            if (target == null) return;

            GridManager grid = FindAnyObjectByType<GridManager>();
            if (grid == null) return;

            Vector2Int targetCoord = grid.WorldToGridCoordinates(target.transform.position);
            Vector2Int exitCoord = grid.ExitPoint;

            // Use pathfinding to find the next tile from target to exit
            Queue<Tile> pathToExit = grid.GetPath(targetCoord, exitCoord, owner.EnemyData.MovementType, true);

            Vector2Int teleportCoord = targetCoord;
            bool foundTile = false;

            if (pathToExit != null && pathToExit.Count > 0)
            {
                Tile nextTile = pathToExit.Peek();
                if (nextTile != null && nextTile.Type != MaouSamaTD.Levels.TileType.HighGround)
                {
                    teleportCoord = nextTile.Coordinate;
                    foundTile = true;
                }
            }

            // Fallback to exit-direction calculation if pathfinding fails
            if (!foundTile)
            {
                Vector2Int spawnCoord = grid.SpawnPoint;
                Vector2Int dirToExit = new Vector2Int(
                    System.Math.Sign(exitCoord.x - spawnCoord.x),
                    System.Math.Sign(exitCoord.y - spawnCoord.y)
                );

                teleportCoord = targetCoord + dirToExit;
                Tile targetTile = grid.GetTileAt(teleportCoord);
                if (targetTile == null || targetTile.Type == MaouSamaTD.Levels.TileType.HighGround)
                {
                    teleportCoord = targetCoord + new Vector2Int(dirToExit.x, 0);
                }
            }

            Tile finalTile = grid.GetTileAt(teleportCoord);
            if (finalTile != null)
            {
                owner.transform.position = grid.GridToWorldPosition(teleportCoord);
                owner.RecalculatePath();
                Debug.Log($"[BossPhase] {owner.gameObject.name} teleported to {teleportCoord} (Behind {target.name}) via pathfinding.");
            }
        }
    }
}
