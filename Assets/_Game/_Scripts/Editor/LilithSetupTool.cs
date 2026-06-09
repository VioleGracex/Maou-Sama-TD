using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Utils;

public static class LilithSetupTool
{
    [MenuItem("Tools/MaouSamaTD/Setup Lilith Sealed Prefab")]
    public static void SetupLilith()
    {
        string targetFile = "Assets/_Game/Art/Props/Lilith_Sealed.png";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetFile);
        if (sprite == null)
        {
            Debug.LogError("Could not load the Lilith sprite from " + targetFile);
            return;
        }

        string prefabsDir = "Assets/_Game/Prefabs/Map/Props";
        if (!Directory.Exists(prefabsDir)) Directory.CreateDirectory(prefabsDir);

        GameObject go = new GameObject("Lilith_Sealed");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Add billboarding so it faces camera
        Billboard billboard = go.AddComponent<Billboard>();
        
        string prefabPath = prefabsDir + "/Lilith_Sealed.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);

        // Assign to Level 2 Map Data at 7, 6
        MapData level2 = AssetDatabase.LoadAssetAtPath<MapData>("Assets/_Game/Data/Maps/MapData_Level2.asset");
        if (level2 != null && prefab != null)
        {
            Vector2Int coord = new Vector2Int(7, 6);

            if (level2.VisualOverrides == null) level2.VisualOverrides = new List<TileVisualOverride>();

            int idx = level2.VisualOverrides.FindIndex(o => o.Coordinate == coord);
            TileVisualOverride ov;
            if (idx == -1)
            {
                ov = new TileVisualOverride { Coordinate = coord, Decorations = new List<DecorationData>() };
            }
            else
            {
                ov = level2.VisualOverrides[idx];
                if (ov.Decorations == null) ov.Decorations = new List<DecorationData>();
            }

            DecorationData deco = DecorationData.Default;
            deco.Prefab = prefab;
            deco.Scale = Vector3.one;
            
            ov.Decorations.Add(deco);

            if (idx == -1) level2.VisualOverrides.Add(ov);
            else level2.VisualOverrides[idx] = ov;

            EditorUtility.SetDirty(level2);
            AssetDatabase.SaveAssets();

            Debug.Log("Successfully created Lilith_Sealed prefab and placed it in Level 2 at (7, 6)!");
        }
        else
        {
            Debug.LogError("Could not find Level 2 MapData or prefab was not created.");
        }
    }
}
