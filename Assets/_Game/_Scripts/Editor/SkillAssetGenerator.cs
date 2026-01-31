using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;
using System.IO;

public class SkillAssetGenerator
{
    [MenuItem("MaouSamaTD/Generate Example Rites")]
    public static void GenerateRites()
    {
        EnsureFolderExists("Assets/Data/Skills");

        // 1. Thunderbolt
        CreateRite("Thunderbolt", rite => {
            rite.SkillName = "Thunderbolt";
            rite.Description = "Strikes a single enemy with high damage.";
            rite.SealCost = 50;
            rite.Cooldown = 15f;
            rite.TargetType = SkillTargetType.Unit;
            rite.EffectType = SkillEffectType.Damage;
            rite.Value = 100f; // High Damage
            rite.Radius = 0f; // Single Target
            rite.Range = 100f;
        });

        // 2. Fireball
        CreateRite("Fireball", rite => {
            rite.SkillName = "Fireball";
            rite.Description = "Explodes in an area, damaging all enemies.";
            rite.SealCost = 30;
            rite.Cooldown = 10f;
            rite.TargetType = SkillTargetType.Ground; // Can target ground
            rite.EffectType = SkillEffectType.Damage;
            rite.Value = 40f; // Medium Damage
            rite.Radius = 3f; // AOE
            rite.Range = 100f;
        });

        // 3. Empower
        CreateRite("Empower", rite => {
            rite.SkillName = "Empower";
            rite.Description = "Restores health to a friendly unit.";
            rite.SealCost = 20;
            rite.Cooldown = 8f;
            rite.TargetType = SkillTargetType.Unit;
            rite.EffectType = SkillEffectType.Buff; // Heals friendly
            rite.Value = 50f;
            rite.Radius = 0f;
            rite.Range = 100f;
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Skill Generator", "Created 3 Example Rites in Assets/Data/Skills/", "OK");
    }

    private static void CreateRite(string name, System.Action<SovereignRiteData> configure)
    {
        string path = $"Assets/Data/Skills/{name}.asset";
        SovereignRiteData rite = ScriptableObject.CreateInstance<SovereignRiteData>();
        
        configure(rite);

        AssetDatabase.CreateAsset(rite, path);
    }

    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
