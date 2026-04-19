using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using MaouSamaTD.Units;

public class GenerateUnitDataTemp {
    
    [MenuItem("Tools/Generate Unit Data")]
    public static void Run() {
        var log = new System.Text.StringBuilder();
        string docsPath = Path.Combine(Application.dataPath, "_Game/docs~/Math_and_Balance/Mythic_Batches");
        if (!Directory.Exists(docsPath)) {
            Debug.LogError("Docs path not found.");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("UnitData");
        if (group == null) {
            group = settings.CreateGroup("UnitData", false, false, true, settings.DefaultGroup.Schemas);
        }
        settings.AddLabel("UnitData");

        string[] exclude = { "Shade", "Lilith", "Ignis", "Aquila" };

        var dirs = Directory.GetDirectories(docsPath, "*", SearchOption.AllDirectories);
        bool needsRefresh = false;

        // Pass 1: Create folders, copy images, create UnitData empty assets
        foreach (var dir in dirs) {
            string dirName = Path.GetFileName(dir);
            if (dir.Contains("Batch_") && !dirName.StartsWith("Batch_")) {
                if (exclude.Any(e => dirName.Contains(e))) {
                    continue;
                }

                string rarityFolder = GetRarityFolder(dir);
                MaouSamaTD.Units.UnitRarity rarityEnum = GetRarityEnum(dir);

                string artDestFolder = $"Assets/_Game/Art/Characters/{rarityFolder}/{dirName}";
                string dataDestFolder = $"Assets/_Game/Data/Units/Vassals/{rarityFolder}";

                if (!AssetDatabase.IsValidFolder(artDestFolder)) CreateFolderRecursively(artDestFolder);
                if (!AssetDatabase.IsValidFolder(dataDestFolder)) CreateFolderRecursively(dataDestFolder);

                var pngs = Directory.GetFiles(dir, "*.png");
                foreach (var png in pngs) {
                    string fileName = Path.GetFileName(png);
                    string destPngPath = $"{artDestFolder}/{fileName}";
                    if (!File.Exists(destPngPath)) {
                        File.Copy(png, destPngPath);
                        needsRefresh = true;
                    }
                }
            }
        }

        if (needsRefresh) {
            AssetDatabase.Refresh();
        }

        // Pass 2: Set importers, create/update UnitData, assign Addressables
        foreach (var dir in dirs) {
            string dirName = Path.GetFileName(dir);
            if (dir.Contains("Batch_") && !dirName.StartsWith("Batch_")) {
                if (exclude.Any(e => dirName.Contains(e))) continue;

                string rarityFolder = GetRarityFolder(dir);
                MaouSamaTD.Units.UnitRarity rarityEnum = GetRarityEnum(dir);
                string artDestFolder = $"Assets/_Game/Art/Characters/{rarityFolder}/{dirName}";
                string dataDestFolder = $"Assets/_Game/Data/Units/Vassals/{rarityFolder}";
                
                // Addressables setup for Data
                string dataPath = $"{dataDestFolder}/Char_{dirName}_UnitData.asset";
                UnitData ud = AssetDatabase.LoadAssetAtPath<UnitData>(dataPath);
                if (ud == null) {
                    ud = ScriptableObject.CreateInstance<UnitData>();
                    AssetDatabase.CreateAsset(ud, dataPath);
                }

                ud.UnitName = dirName.Replace("_", " ");
                ud.Rarity = rarityEnum;

                string guid = AssetDatabase.AssetPathToGUID(dataPath);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry != null) {
                    entry.SetLabel("UnitData", true, true);
                    entry.address = $"Char_{dirName}_UnitData";
                }

                // Process Images
                var artPngs = AssetDatabase.FindAssets("t:texture2d", new[] { artDestFolder });
                bool modifiedImporter = false;

                foreach (var artGuid in artPngs) {
                    string assetPath = AssetDatabase.GUIDToAssetPath(artGuid);
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite) {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.SaveAndReimport();
                        modifiedImporter = true;
                    }
                }

                var baseSkin = ud.BaseSkin;

                foreach (var artGuid in artPngs) {
                    string p = AssetDatabase.GUIDToAssetPath(artGuid).ToLower();
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(artGuid));
                    if (sprite != null) {
                        if (p.Contains("chibi")) baseSkin.Chibi = sprite;
                        else if (p.Contains("full_body") || p.Contains("fullbody")) baseSkin.FullBodyCutout = sprite;
                        else if (p.Contains("waist_up") || p.Contains("waistup")) baseSkin.WaistUp = sprite;
                        else if (p.Contains("splash")) baseSkin.FullSplashArt = sprite;
                    }
                }

                ud.BaseSkin = baseSkin;
                EditorUtility.SetDirty(ud);
                log.AppendLine($"Configured {dirName}");
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("UnitData generation complete:\n" + log.ToString());
    }

    static string GetRarityFolder(string dir) {
        if (dir.Contains("\\UR\\") || dir.Contains("/UR/")) return "06_UR";
        if (dir.Contains("\\SSR\\") || dir.Contains("/SSR/")) return "05_SSR";
        if (dir.Contains("\\SR\\") || dir.Contains("/SR/")) return "04_SR";
        if (dir.Contains("\\R\\") || dir.Contains("/R/")) return "03_R";
        if (dir.Contains("\\UC\\") || dir.Contains("/UC/")) return "02_UC";
        return "01_Common";
    }

    static MaouSamaTD.Units.UnitRarity GetRarityEnum(string dir) {
        if (dir.Contains("\\UR\\") || dir.Contains("/UR/")) return MaouSamaTD.Units.UnitRarity.Legendary;
        if (dir.Contains("\\SSR\\") || dir.Contains("/SSR/")) return MaouSamaTD.Units.UnitRarity.Master;
        if (dir.Contains("\\SR\\") || dir.Contains("/SR/")) return MaouSamaTD.Units.UnitRarity.Elite;
        if (dir.Contains("\\R\\") || dir.Contains("/R/")) return MaouSamaTD.Units.UnitRarity.Rare;
        if (dir.Contains("\\UC\\") || dir.Contains("/UC/")) return MaouSamaTD.Units.UnitRarity.Uncommon;
        return MaouSamaTD.Units.UnitRarity.Common;
    }

    static void CreateFolderRecursively(string path) {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++) {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
