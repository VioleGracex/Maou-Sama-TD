using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.UI.Common
{
    /// <summary>
    /// Interface for any UI item that can be managed by GenericListView.
    /// </summary>
    /// <typeparam name="TData">The data type this item represents.</typeparam>
    public interface IListItem<TData>
    {
        /// <summary>
        /// Setup the UI with data. Should handle internal pooling/refresh logic.
        /// </summary>
        void Setup(TData data, Action<UnityEngine.Component> onClick = null);
        
        /// <summary>
        /// Returns a unique identifier for the data currently bound to this UI.
        /// Used for smart updates.
        /// </summary>
        string GetContentID();
        
        /// <summary>
        /// Optional version/hash check to see if visual refresh is needed even if ID is same.
        /// </summary>
        int GetContentVersion() => 0; 
    }

    /// <summary>
    /// A generic, pooled scroll view content manager.
    /// Efficiently reuses UI prefabs based on data list.
    /// </summary>
    /// <typeparam name="TData">Data model type</typeparam>
    /// <typeparam name="TView">UI component type (must implement IListItem)</typeparam>
    public class GenericListView<TData, TView> where TView : MonoBehaviour, IListItem<TData>
    {
        private readonly Transform _container;
        private readonly TView _prefab;
        private readonly List<TView> _pool = new List<TView>();
        
        public List<TView> ActiveItems => _pool.FindAll(x => x.gameObject.activeSelf);

        public GenericListView(Transform container, TView prefab)
        {
            _container = container;
            _prefab = prefab;
            
            // Initial hide of prefab if it's a template in the container
            if (_prefab != null && _prefab.transform.parent == _container)
            {
                _prefab.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the list content. Reuses existing items, instantiates new ones if needed,
        /// and hides surplus. Performs "Smart Update" to avoid redundant UI bindings.
        /// </summary>
        public void UpdateContent(IEnumerable<TData> dataList, Action<TView> onClick = null, bool forceRefresh = false)
        {
            if (_container == null || _prefab == null) return;

            int index = 0;
            if (dataList != null)
            {
                foreach (var data in dataList)
                {
                    TView item = GetOrSpawn(index);
                    item.gameObject.SetActive(true);
                    
                    // Smart Update: Check if we actually need to call Setup
                    // We compare the view's current content with the new data
                    if (forceRefresh || ShouldUpdate(item, data))
                    {
                        item.Setup(data, (comp) => onClick?.Invoke(comp as TView));
                    }
                    
                    index++;
                }
            }

            // Hide extra items in the pool
            for (int i = index; i < _pool.Count; i++)
            {
                _pool[i].gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach (var item in _pool)
            {
                item.gameObject.SetActive(false);
            }
        }

        private TView GetOrSpawn(int index)
        {
            while (_pool.Count <= index)
            {
                TView newItem = UnityEngine.Object.Instantiate(_prefab, _container);
                _pool.Add(newItem);
            }
            return _pool[index];
        }

        private bool ShouldUpdate(TView item, TData data)
        {
            // If the item was inactive, it definitely needs setup
            if (!item.gameObject.activeSelf) return true;
            
            // Use the interface to check if content changed
            // This is safer than just assuming IDs match
            string currentID = item.GetContentID();
            string newDataID = GetIDFromData(data);
            
            if (currentID != newDataID) return true;
            
            // Optional: check version/hash for deep changes (level up, sprite swap)
            int currentVersion = item.GetContentVersion();
            int newDataVersion = GetVersionFromData(data);
            
            return currentVersion != newDataVersion;
        }

        private string GetIDFromData(TData data)
        {
            // Reflection or Type check for common ID property names
            if (data == null) return string.Empty;
            
            // Check for common ID properties
            var type = typeof(TData);
            var prop = type.GetProperty("UniqueID") ?? type.GetProperty("LevelID") ?? type.GetProperty("ID");
            if (prop != null) return prop.GetValue(data)?.ToString() ?? string.Empty;
            
            var field = type.GetField("UniqueID") ?? type.GetField("LevelID") ?? type.GetField("ID");
            if (field != null) return field.GetValue(data)?.ToString() ?? string.Empty;

            return data.GetHashCode().ToString();
        }

        private int GetVersionFromData(TData data)
        {
            if (data == null) return 0;
            var type = typeof(TData);
            var prop = type.GetProperty("Version") ?? type.GetProperty("DataVersion");
            if (prop != null) return (int)(prop.GetValue(data) ?? 0);
            
            return 0;
        }
    }
}
