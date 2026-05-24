using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.Skills;
using System.Linq;

namespace MaouSamaTD.Editor
{
    public static class PopulateRitesPlaceholders
    {
        [MenuItem("Maou-TD/UI/Populate Rites Placeholders")]
        public static void Populate()
        {
            var cohortUI = Object.FindAnyObjectByType<CohortSquadUI>();
            if (cohortUI == null)
            {
                Debug.LogError("CohortSquadUI not found in scene!");
                return;
            }

            var squadType = typeof(CohortSquadUI);
            var containerField = squadType.GetField("_availableRitesContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var prefabField = squadType.GetField("_riteItemPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (containerField == null || prefabField == null)
            {
                Debug.LogError("Could not find fields.");
                return;
            }

            var container = containerField.GetValue(cohortUI) as RectTransform;
            var prefab = prefabField.GetValue(cohortUI) as GameObject;

            if (container == null)
            {
                // Fallback to searching by name
                var go = GameObject.Find("OwnedRitesContainer") ?? GameObject.Find("AvailableRites_Container");
                if (go != null) container = go.transform as RectTransform;
            }

            if (container == null || prefab == null)
            {
                Debug.LogError("Container or Prefab is null!");
                return;
            }

            // Clear container
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(container.GetChild(i).gameObject);
            }

            // Load all SovereignRiteData
            string[] guids = AssetDatabase.FindAssets("t:SovereignRiteData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<SovereignRiteData>(path);
                if (data != null)
                {
                    GameObject instance;
                    if (PrefabUtility.IsPartOfPrefabAsset(prefab))
                    {
                        instance = PrefabUtility.InstantiatePrefab(prefab, container) as GameObject;
                    }
                    else
                    {
                        instance = Object.Instantiate(prefab, container);
                    }
                    
                    if (instance != null)
                    {
                        instance.SetActive(true);
                        var itemUI = instance.GetComponent<CohortRiteItemUI>();
                        if (itemUI != null)
                        {
                            itemUI.Setup(data, false);
                        }
                    }
                }
            }

            // Ensure grid layout is somewhat fitting, or let RebuildRitesLayout handle it.
            EditorUtility.SetDirty(container.gameObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            
            Debug.Log($"Populated {guids.Length} rites!");
        }
    }
}
