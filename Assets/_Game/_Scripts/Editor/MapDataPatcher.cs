using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using System.Collections.Generic;

public class MapDataPatcher
{
    [MenuItem("MaouSamaTD/Patch Map Data")]
    public static void PatchMaps()
    {
        PatchMap("Level4", new string[] {
            "###############",
            "S...###.......#",
            "###.###.#####.#",
            "###.....#####.#",
            "#############.#",
            "#.............B",
            "#.###########.#",
            "#.#####.......#",
            "#.#####.#######",
            "S.......#######",
            "###############"
        });

        PatchMap("Level5", new string[] {
            "###############",
            "S...#...#.....#",
            "###.#.#.#.###.#",
            "#.....#.......B",
            "#.###########.#",
            "S.............#",
            "###############"
        });

        PatchMap("Level6", new string[] {
            "###############",
            "#.............#",
            "#.###########.#",
            "#.#########.#.#",
            "S.#.#######.#.#",
            "###.###B..#.#.#",
            "#...#####.#...#",
            "#.#######.#####",
            "#.............#",
            "###############"
        });

        PatchMap("Level7", new string[] {
            "###############",
            "#####.#####.###",
            "#####.#####.###",
            "S.............B",
            "#####.#####.###",
            "#####.#####.###",
            "S.............B",
            "###############"
        });

        PatchMap("Level8", new string[] {
            "###############",
            "S..#.....#....#",
            "##.#.###.#.##.#",
            "#......#...#..B",
            "######.########",
            "S....#........#",
            "###############"
        });

        PatchMap("Level9", new string[] {
            "###############",
            "#.............#",
            "S.###########.#",
            "#.#########.#.B",
            "#.#.#######.#.#",
            "S.#.........#.#",
            "#.###########.#",
            "#.............#",
            "###############"
        });

        PatchMap("Level10", new string[] {
            "###############",
            "S.............#",
            "#.............B",
            "S.....###.....#",
            "#.....###.....B",
            "S.............#",
            "###############"
        });

        Debug.Log("Finished patching maps.");
    }

    private static void PatchMap(string levelName, string[] layoutRaw)
    {
        string path = $"Assets/_Game/Data/Maps/MapData_{levelName}.asset";
        MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
        
        if (mapData == null)
        {
            Debug.LogError("Could not find " + path);
            return;
        }

        // Expand the raw layout to 15x15 if needed by duplicating or padding
        string[] layout = new string[15];
        int startY = (15 - layoutRaw.Length) / 2;
        for (int i = 0; i < 15; i++)
        {
            if (i >= startY && i < startY + layoutRaw.Length)
            {
                layout[i] = layoutRaw[i - startY].PadRight(15, '#');
            }
            else
            {
                layout[i] = "###############";
            }
        }

        mapData.ManualLayoutData = new List<TileLayoutData>();
        mapData.SpawnPoints = new List<SpawnPointData>();
        mapData.ExitPoints = new List<Vector2Int>();

        for (int y = 0; y < 15; y++)
        {
            for (int x = 0; x < 15; x++)
            {
                char c = layout[14 - y][x]; // Reverse Y because Unity UI grids usually start from bottom-left (0,0) in our code
                int type = 2; // Wall
                if (c == '.') type = 1; // Path
                else if (c == 'S') type = 4; // Spawn
                else if (c == 'B') type = 5; // Base

                mapData.ManualLayoutData.Add(new TileLayoutData { 
                    Coordinate = new Vector2Int(x, y), 
                    Type = (TileType)type 
                });

                if (type == 4)
                {
                    mapData.SpawnPoints.Add(new SpawnPointData {
                        Coordinate = new Vector2Int(x, y),
                        TargetExitIndex = 0 // Assuming index 0 for all for now, can be updated later
                    });
                }
                else if (type == 5)
                {
                    mapData.ExitPoints.Add(new Vector2Int(x, y));
                }
            }
        }

        // Fix TargetExitIndex for spawns if multiple exits exist
        if (mapData.ExitPoints.Count > 1)
        {
            for (int i = 0; i < mapData.SpawnPoints.Count; i++)
            {
                var spawn = mapData.SpawnPoints[i];
                // simple heuristic: pair spawn i with exit i % exits
                spawn.TargetExitIndex = i % mapData.ExitPoints.Count;
                mapData.SpawnPoints[i] = spawn;
            }
        }

        EditorUtility.SetDirty(mapData);
        AssetDatabase.SaveAssets();
        Debug.Log($"Patched {levelName}");
    }
}
