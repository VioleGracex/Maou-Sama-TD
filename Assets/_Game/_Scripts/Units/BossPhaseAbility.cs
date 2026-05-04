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

            // 2. Add Melee Immunity during phase (optional, can be handled by charges logic)
            if (!owner.Immunities.Contains(DamageType.Melee))
            {
                owner.Immunities.Add(DamageType.Melee);
            }

            // 3. Teleport behind Ignis
            TeleportBehindTarget(owner);
        }

        private void TeleportBehindTarget(EnemyUnit owner)
        {
            // Find Ignis
            var target = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && u.name.Contains("Ignis"));
            if (target == null) return;

            GridManager grid = FindFirstObjectByType<GridManager>();
            if (grid == null) return;

            Vector2Int targetCoord = grid.WorldToGridCoordinates(target.transform.position);
            
            // Determine "Behind" based on exit direction
            Vector2Int exitCoord = grid.ExitPoint;
            Vector2Int spawnCoord = grid.SpawnPoint;
            
            Vector2Int dirToExit = new Vector2Int(
                System.Math.Sign(exitCoord.x - spawnCoord.x),
                System.Math.Sign(exitCoord.y - spawnCoord.y)
            );

            // Behind is 1 tile closer to exit than target
            Vector2Int teleportCoord = targetCoord + dirToExit;
            
            // Validate if tile exists and is walkable
            Tile targetTile = grid.GetTileAt(teleportCoord);
            if (targetTile == null || targetTile.Type == MaouSamaTD.Levels.TileType.HighGround)
            {
                // Try nearby tiles if blocked
                teleportCoord = targetCoord + new Vector2Int(dirToExit.x, 0);
                targetTile = grid.GetTileAt(teleportCoord);
            }

            if (targetTile != null)
            {
                owner.transform.position = grid.GridToWorldPosition(teleportCoord);
                owner.RecalculatePath();
                Debug.Log($"[BossPhase] {owner.gameObject.name} teleported to {teleportCoord} (Behind {target.name})");
            }
        }
    }
}
