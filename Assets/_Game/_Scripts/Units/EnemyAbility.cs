using UnityEngine;
using System;

namespace MaouSamaTD.Units
{
    public abstract class EnemyAbility : ScriptableObject
    {
        [Header("Ability Info")]
        public string AbilityName;
        [TextArea] public string Description;

        /// <summary>
        /// Called when the EnemyUnit is initialized.
        /// </summary>
        public virtual void OnInitialize(EnemyUnit owner) { }

        /// <summary>
        /// Called every Update on the EnemyUnit.
        /// </summary>
        public virtual void OnTick(EnemyUnit owner) { }

        /// <summary>
        /// Called when the EnemyUnit takes damage.
        /// </summary>
        public virtual void OnTakeDamage(EnemyUnit owner, float amount, DamageType type) { }

        /// <summary>
        /// Called when the EnemyUnit starts an attack.
        /// </summary>
        public virtual void OnAttack(EnemyUnit owner, UnitBase target) { }

        /// <summary>
        /// Called when the EnemyUnit dies.
        /// </summary>
        public virtual void OnDeath(EnemyUnit owner) { }
    }
}
