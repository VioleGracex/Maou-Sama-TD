using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Units
{
    public class EnemyUnit : UnitBase
    {
        [Header("Enemy Stats")]
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private int _damageToPlayer = 1;

        private Queue<Tile> _path;
        private Tile _targetTile;
        private bool _isMoving = false;
        private PlayerUnit _blockedBy = null;

        public void SetPath(Queue<Tile> path)
        {
            _path = path;
            if (_path != null && _path.Count > 0)
            {
                _targetTile = _path.Dequeue();
                _isMoving = true;
            }
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
            
            if (_blockedBy != null)
            {
                // Attack logic against blocked player unit would go here
                return;
            }

            if (_isMoving && _targetTile != null)
            {
                MoveTowardsTarget(); // Move logic logic
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 targetPos = _targetTile.transform.position;
            // Ignore Y for simple movement for now
            Vector3 currentPos = transform.position;
            Vector3 dir = (targetPos - currentPos).normalized;
            dir.y = 0; 

            transform.position += dir * _moveSpeed * Time.deltaTime;

            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.1f)
            {
                // Reached tile
                if (_path.Count > 0)
                {
                    _targetTile = _path.Dequeue();
                    // LookAtTarget(); // Removed LookAtTarget as we might rely on Billboard? Or we can flip X.
                    // For now, let's keep movement simple.
                }
                else
                {
                    ReachedExit();
                }
            }
        }

        private void LookAtTarget()
        {
            if (_targetTile != null)
            {
                Vector3 dir = (_targetTile.transform.position - transform.position).normalized;
                dir.y = 0;
                if(dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private void ReachedExit()
        {
            _isMoving = false;
            // Deal damage to "Player" (Base HP)
            Debug.Log("Enemy reached exit! Dealing damage.");
            GridManager.Instance?.SetTileType(_targetTile.Coordinate, TileType.Exit); // Just to verify we are at exit?
            Destroy(gameObject);
        }

        public void SetBlockedBy(PlayerUnit blocker)
        {
            _blockedBy = blocker;
            _isMoving = false;
        }

        public void ReleaseBlock()
        {
            _blockedBy = null;
            _isMoving = true;
        }
    }
}
