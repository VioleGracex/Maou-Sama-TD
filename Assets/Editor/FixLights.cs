using UnityEditor;
using UnityEngine;

namespace MaouSamaTD.EditorTools
{
    public class FixLights
    {
        [MenuItem("Maou-TD/Tools/Fix Pedestal Lights")]
        public static void FixPedestalLights()
        {
            string[] targetPrefabs = {
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Pedestal_01.prefab",
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Pedestal_02.prefab",
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Torch_01.prefab",
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Torch_02.prefab",
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Campfire_01.prefab",
                "Assets/Dungeon/URP/Prefabs/Misc/Light/NGF_Light_Campfire_02.prefab"
            };

            string fireParticlePath = "Assets/Dungeon/Animations_and_particle/NGF_particle_Fire.prefab";
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fireParticlePath);

            if (firePrefab == null)
            {
                Debug.LogError($"Could not find fire particle at {fireParticlePath}");
                return;
            }

            int fixedCount = 0;

            foreach (string path in targetPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Could not find prefab at {path}");
                    continue;
                }

                // Start editing the prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                bool modified = false;

                // 1. Add Point Light
                Light pointLight = instance.GetComponentInChildren<Light>();
                if (pointLight == null)
                {
                    GameObject lightObj = new GameObject("FireLight");
                    lightObj.transform.SetParent(instance.transform);
                    
                    // Approximate height of the object top
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                    float topY = 0;
                    foreach (var r in renderers) {
                        if (r.bounds.max.y - instance.transform.position.y > topY)
                            topY = r.bounds.max.y - instance.transform.position.y;
                    }
                    if (topY < 0.1f) topY = 1.5f;
                    
                    lightObj.transform.localPosition = new Vector3(0, topY, 0);

                    pointLight = lightObj.AddComponent<Light>();
                    pointLight.type = LightType.Point;
                    pointLight.color = new Color(1f, 0.6f, 0.2f); // Warm fire orange
                    pointLight.intensity = 2f;
                    pointLight.range = 10f;
                    pointLight.shadows = LightShadows.Soft;
                    
                    modified = true;
                }

                // 2. Add Fire Particles
                Transform existingFire = instance.transform.Find("NGF_particle_Fire");
                if (existingFire == null)
                {
                    GameObject fireInst = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
                    fireInst.name = "NGF_particle_Fire";
                    fireInst.transform.SetParent(instance.transform);
                    
                    // Same top Y position
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                    float topY = 0;
                    foreach (var r in renderers) {
                        if (r.bounds.max.y - instance.transform.position.y > topY)
                            topY = r.bounds.max.y - instance.transform.position.y;
                    }
                    if (topY < 0.1f) topY = 1.5f;
                    
                    fireInst.transform.localPosition = new Vector3(0, topY, 0);
                    fireInst.transform.localScale = Vector3.one;
                    fireInst.transform.localRotation = Quaternion.identity;

                    modified = true;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    fixedCount++;
                }
                
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Fixed {fixedCount} pedestal prefabs by adding lights and fire particles!");
        }
    }
}
