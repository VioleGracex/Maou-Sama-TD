using UnityEngine;
using UnityEngine.UI;
using Zenject;
using NaughtyAttributes;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Managers
{
    public class DebugManager : MonoBehaviour
    {
        #region Fields
        [Header("Debug UI")]
        [SerializeField] private Button _spawnEnemyButton;
        [SerializeField] private Button _addSealsButton;
        [SerializeField] private EnemyData _testEnemyData; 

        [Inject] private EnemyManager _enemyManager; 
        [Inject] private GameManager _gameManager;
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private BattleCurrencyManager _currencyManager;
        #endregion

        #region Lifecycle
        private void Start()
        {
            if (_spawnEnemyButton != null)
            {
                _spawnEnemyButton.onClick.AddListener(OnSpawnEnemyClicked);
            }
            if (_addSealsButton != null)
            {
                _addSealsButton.onClick.AddListener(AddAuthSeal);
            }
        }
        #endregion

        #region Private Methods
        [Button("Spawn Test Enemy")] 
        private void OnSpawnEnemyClicked()
        {
            Debug.Log("[DebugManager] Spawn Enemy Clicked");
            if (_enemyManager != null && _testEnemyData != null)
            {
                _enemyManager.SpawnEnemy(_testEnemyData, -1, -1);
            }
            else
            {
                if (_enemyManager == null) Debug.LogError("[DebugManager] EnemyManager is missing!");
                if (_testEnemyData == null) Debug.LogWarning("[DebugManager] No Test Enemy Data assigned in Inspector!");
            }
        }

        [Button("Global Heal (Base +999)")]
        private void GlobalHeal()
        {
             Debug.Log("[DebugManager] Global Heal Triggered");
             if (_gameManager != null)
             {
                 _gameManager.TakeBaseDamage(-999);
             }
        }

        [Button("Global Damage (Kill All Enemies)")]
        private void GlobalDamageEnemy()
        {
            Debug.Log("[DebugManager] Global Damage Triggered");
            var enemies = new List<EnemyUnit>(EnemyUnit.ActiveEnemies);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(99999);
                }
            }
        }

        [Button("Retreat All Units")]
        private void RetreatAllUnits()
        {
            Debug.Log("[DebugManager] Retreat All Units Triggered");
            if (_gridManager != null)
            {
                List<PlayerUnit> unitsToRetreat = new List<PlayerUnit>();
                
                foreach (var tile in _gridManager.GetAllTiles())
                {
                    if (tile.IsOccupied && tile.Occupant is PlayerUnit pUnit)
                    {
                        unitsToRetreat.Add(pUnit);
                    }
                }

                foreach (var unit in unitsToRetreat)
                {
                    if (unit != null) unit.Retreat();
                }
            }
        }

        [Button("Add Auth Seal (+5)")]
        private void AddAuthSeal()
        {
            if (_currencyManager != null)
            {
                _currencyManager.AddSeals(5);
                Debug.Log("[DebugManager] Added 5 Auth Seals");
            }
        }
        #endregion
    }
}
