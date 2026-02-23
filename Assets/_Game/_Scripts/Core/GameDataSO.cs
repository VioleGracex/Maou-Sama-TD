using UnityEngine;
using NaughtyAttributes;

namespace MaouSamaTD.Core
{
    public class GameDataSO : ScriptableObject
    {
        [HideInInspector] public bool useDefaultInspector = false;

        [Header("Identity")]
        [ReadOnly]
        [Tooltip("Permanent unique identifier generated upon creation.")]
        public string UniqueID;

        protected virtual void OnValidate()
        {
            // If the ID is empty, generate a new Guid and mark as dirty to save it permanently
            if (string.IsNullOrEmpty(UniqueID))
            {
                UniqueID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
}
