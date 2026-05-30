using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MaouSamaTD.Levels;

namespace MaouSamaTD.EditorTools
{
    public class AutoDecorateTool : EditorWindow
    {
        private MapData _targetMapData;
        private string _prefabFolderPath = "Assets/Dungeon/URP/Prefabs/Misc";
        private float _spawnChance = 0.5f;
        private float _yOffset = 0.25f;

        [MenuItem("Maou-TD/Tools/Auto Decorate Map")]
        public static void ShowWindow()
        {
            GetWindow<AutoDecorateTool>("Auto Decorate Map");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scatter Random Decor on MapData", EditorStyles.boldLabel);
            
            _targetMapData = (MapData)EditorGUILayout.ObjectField("Target Map Data", _targetMapData, typeof(MapData), false);
            _prefabFolderPath = EditorGUILayout.TextField("Prefab Folder Path", _prefabFolderPath);
            _spawnChance = EditorGUILayout.Slider("Spawn Chance", _spawnChance, 0f, 1f);
            _yOffset = EditorGUILayout.FloatField("Y Height Offset", _yOffset);

            if (GUILayout.Button("Scatter Decor"))
            {
                ScatterDecor();
            }
        }

        private void ScatterDecor()
        {
            if (_targetMapData == null)
            {
                Debug.LogError("Please assign a Target Map Data.");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _prefabFolderPath });
            List<GameObject> allPrefabs = new List<GameObject>();
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) allPrefabs.Add(prefab);
            }

            // Categorize
            var pedestals = allPrefabs.Where(p => p.name.Contains("Pedestal") || p.name.Contains("Campfire") || p.name.Contains("Torch")).ToList();
            var chests = allPrefabs.Where(p => p.name.Contains("Chest")).ToList();
            var cratesBarrels = allPrefabs.Where(p => p.name.Contains("Crate") || p.name.Contains("Barrel")).ToList();
            var crystals = allPrefabs.Where(p => p.name.Contains("Crystal")).ToList();

            // Find key locations (Spawns and Exits)
            List<Vector2Int> keyPoints = new List<Vector2Int>();
            foreach (var tile in _targetMapData.ManualLayoutData)
            {
                if (tile.Type == TileType.SpawnPoint || tile.Type == TileType.SpawnPointHigh ||
                    tile.Type == TileType.ExitPoint || tile.Type == TileType.ExitPointHigh)
                {
                    keyPoints.Add(tile.Coordinate);
                }
            }

            Undo.RecordObject(_targetMapData, "Scatter Decor on Map");

            int addedCount = 0;
            foreach (var layoutTile in _targetMapData.ManualLayoutData)
            {
                if (layoutTile.Type == TileType.NonWalkableDecor || layoutTile.Type == TileType.DecoHighGround)
                {
                    // Check distance to nearest key point
                    float minDistance = float.MaxValue;
                    foreach (var kp in keyPoints)
                    {
                        float dist = Vector2Int.Distance(layoutTile.Coordinate, kp);
                        if (dist < minDistance) minDistance = dist;
                    }

                    GameObject selectedPrefab = null;

                    // Logical Placement
                    if (minDistance <= 2.5f) // Near spawn or exit
                    {
                        if (Random.value < 0.6f && pedestals.Count > 0)
                        {
                            selectedPrefab = pedestals[Random.Range(0, pedestals.Count)];
                        }
                    }
                    else // Further away
                    {
                        if (Random.value <= _spawnChance)
                        {
                            float roll = Random.value;
                            if (roll < 0.15f && chests.Count > 0) selectedPrefab = chests[Random.Range(0, chests.Count)];
                            else if (roll < 0.50f && cratesBarrels.Count > 0) selectedPrefab = cratesBarrels[Random.Range(0, cratesBarrels.Count)];
                            else if (roll < 0.80f && crystals.Count > 0) selectedPrefab = crystals[Random.Range(0, crystals.Count)];
                            else selectedPrefab = allPrefabs[Random.Range(0, allPrefabs.Count)];
                        }
                    }

                    if (selectedPrefab != null)
                    {
                        int overrideIndex = _targetMapData.VisualOverrides.FindIndex(v => v.Coordinate == layoutTile.Coordinate);
                        TileVisualOverride visOverride;
                        
                        if (overrideIndex >= 0)
                        {
                            visOverride = _targetMapData.VisualOverrides[overrideIndex];
                            if (visOverride.Decorations == null) visOverride.Decorations = new List<DecorationData>();
                        }
                        else
                        {
                            visOverride = new TileVisualOverride 
                            { 
                                Coordinate = layoutTile.Coordinate,
                                Decorations = new List<DecorationData>() 
                            };
                            _targetMapData.VisualOverrides.Add(visOverride);
                            overrideIndex = _targetMapData.VisualOverrides.Count - 1;
                        }

                        visOverride.Decorations.Add(new DecorationData
                        {
                            Prefab = selectedPrefab,
                            Offset = new Vector3(0, _yOffset, 0),
                            Rotation = new Vector3(0, Random.Range(0, 4) * 90f, 0),
                            Scale = Vector3.one
                        });

                        _targetMapData.VisualOverrides[overrideIndex] = visOverride;
                        addedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(_targetMapData);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully added {addedCount} logical decorations to {_targetMapData.name}!");
        }
    }
}
