using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using MaouSamaTD.UI;

namespace MaouSamaTD.Editor
{
    public static class RenameAndBindUI
    {
        [MenuItem("Maou-TD/UI/Clean and Bind Inspector Panels")]
        public static void CleanAndBind()
        {
            var root = Object.FindAnyObjectByType<UnitInspectorFullScreenUI>(FindObjectsInactive.Include);
            if (root == null)
            {
                Debug.LogError("Could not find UnitInspectorFullScreenUI in scene.");
                return;
            }

            // 1. First, search and RENAME the ugly objects so they look clean in the inspector
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = t.name.ToLower();
                if (n == "val_atk") { t.gameObject.name = "Atk Text"; EditorUtility.SetDirty(t.gameObject); }
                else if (n == "val_def") { t.gameObject.name = "Def Text"; EditorUtility.SetDirty(t.gameObject); }
                else if (n.Contains("stat_aspd_value_inspector")) { t.gameObject.name = "Aspd Text"; EditorUtility.SetDirty(t.gameObject); }
                else if (n == "val_range") { t.gameObject.name = "Range Text"; EditorUtility.SetDirty(t.gameObject); }
                else if (n == "amity_level_text") { t.gameObject.name = "Amity Level Text"; EditorUtility.SetDirty(t.gameObject); }
                else if (n == "rarity_txt") { t.gameObject.name = "Rarity Text Label"; EditorUtility.SetDirty(t.gameObject); }
            }

            // 2. Now manually bind them into the Inspector scripts
            var stats = Object.FindAnyObjectByType<UnitInspectorStatsPanel>(FindObjectsInactive.Include);
            if (stats != null)
            {
                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    string n = t.name.ToLower();
                    if (n.Contains("atk text")) { SetField(stats, "_atkText", t); }
                    else if (n.Contains("def text")) { SetField(stats, "_defText", t); }
                    else if (n.Contains("aspd text")) { SetField(stats, "_aspdText", t); }
                    else if (n.Contains("range text")) { SetField(stats, "_rangeText", t); }
                    else if (n.Contains("amity level text")) { SetField(stats, "_amityLevelText", t); }
                    else if (n.Contains("rarity text label")) { SetField(stats, "_rarityTextLabel", t); }
                }

                // Bind Amity Fill Image
                var images = root.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.name == "Fill" && img.transform.parent != null && img.transform.parent.name.Contains("Amity"))
                    {
                        img.gameObject.name = "Amity Fill Image"; // rename it too
                        SetField(stats, "_amityFillImage", img);
                        EditorUtility.SetDirty(img.gameObject);
                        break;
                    }
                }
                EditorUtility.SetDirty(stats);
            }

            Debug.Log("UI Names cleaned and successfully bound!");
        }

        private static void SetField(object obj, string fieldName, object val)
        {
            if (val == null || val.Equals(null)) return;
            var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null)
            {
                f.SetValue(obj, val);
            }
        }
    }
}
