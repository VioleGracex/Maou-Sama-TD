using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;

namespace MaouSamaTD.Editor
{
    public class GenerateClassDataUtility
    {
        [MenuItem("MaouSamaTD/Generate Base Class Scaling")]
        public static void GenerateClassScalingData()
        {
            string path = "Assets/_Game/Data/ClassScalingData.asset";
            
            ClassScalingData asset = AssetDatabase.LoadAssetAtPath<ClassScalingData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ClassScalingData>();
                
                // Ensure directory
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                {
                    AssetDatabase.CreateFolder("Assets/_Game", "Data");
                }

                AssetDatabase.CreateAsset(asset, path);
            }

            // Get all enum values
            System.Array classes = System.Enum.GetValues(typeof(UnitClass));
            asset.ClassScalings = new ClassStatMultipliers[classes.Length];

            for (int i = 0; i < classes.Length; i++)
            {
                UnitClass uClass = (UnitClass)classes.GetValue(i);
                asset.ClassScalings[i] = new ClassStatMultipliers
                {
                    ClassType = uClass,
                    OverrideClassName = uClass.ToString(),
                    BaseHpMultiplier = 1.0f,
                    BaseAtkMultiplier = 1.0f,
                    BaseDefMultiplier = 1.0f,
                    RarityGrowths = new RarityStatGrowth[]
                    {
                        new() { Rarity = UnitRarity.Common, HpGrowthPerLevel = 10, AtkGrowthPerLevel = 1, DefGrowthPerLevel = 0 },
                        new() { Rarity = UnitRarity.Uncommon, HpGrowthPerLevel = 25, AtkGrowthPerLevel = 2, DefGrowthPerLevel = 1 },
                        new() { Rarity = UnitRarity.Rare, HpGrowthPerLevel = 50, AtkGrowthPerLevel = 4, DefGrowthPerLevel = 2 },
                        new() { Rarity = UnitRarity.Elite, HpGrowthPerLevel = 80, AtkGrowthPerLevel = 6, DefGrowthPerLevel = 3 },
                        new() { Rarity = UnitRarity.Master, HpGrowthPerLevel = 120, AtkGrowthPerLevel = 8, DefGrowthPerLevel = 4 },
                        new() { Rarity = UnitRarity.Legendary, HpGrowthPerLevel = 180, AtkGrowthPerLevel = 12, DefGrowthPerLevel = 5 }
                    }
                };

                // Add slight flavor bounds for standard classes
                if (uClass == UnitClass.Bastion) { asset.ClassScalings[i].BaseHpMultiplier = 1.5f; asset.ClassScalings[i].BaseDefMultiplier = 1.5f; }
                else if (uClass == UnitClass.Executioner) { asset.ClassScalings[i].BaseHpMultiplier = 0.8f; asset.ClassScalings[i].BaseAtkMultiplier = 1.4f; }
                else if (uClass == UnitClass.Gunner) { asset.ClassScalings[i].BaseHpMultiplier = 0.7f; asset.ClassScalings[i].BaseAtkMultiplier = 1.3f; }
                else if (uClass == UnitClass.EnemyBoss) { asset.ClassScalings[i].BaseHpMultiplier = 5.0f; asset.ClassScalings[i].BaseAtkMultiplier = 2.0f; asset.ClassScalings[i].BaseDefMultiplier = 2.0f; }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated base ClassScalingData at {path} with {classes.Length} entries.");
        }
    }
}
