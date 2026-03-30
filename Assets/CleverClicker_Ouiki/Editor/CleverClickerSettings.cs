using UnityEngine;
using UnityEditor;

namespace CleverClicker.Ouiki
{
    public static class CleverClickerSettings
    {
        private const string Prefix = "CleverClicker_";

        public static EventModifiers ModifierKeys
        {
            get => (EventModifiers)EditorPrefs.GetInt(Prefix + "ModifierKeys", (int)(EventModifiers.Control | EventModifiers.Shift));
            set => EditorPrefs.SetInt(Prefix + "ModifierKeys", (int)value);
        }

        public static bool FocusOnSelect
        {
            get => EditorPrefs.GetBool(Prefix + "FocusOnSelect", true);
            set => EditorPrefs.SetBool(Prefix + "FocusOnSelect", value);
        }

        public static int ExcludeLayers
        {
            get => EditorPrefs.GetInt(Prefix + "ExcludeLayers", 0);
            set => EditorPrefs.SetInt(Prefix + "ExcludeLayers", value);
        }

        public static Color HighlightColor
        {
            get
            {
                string colorStr = EditorPrefs.GetString(Prefix + "HighlightColor", "0.2,0.7,1,0.5");
                string[] split = colorStr.Split(',');
                if (split.Length == 4 && 
                    float.TryParse(split[0], out float r) && 
                    float.TryParse(split[1], out float g) && 
                    float.TryParse(split[2], out float b) && 
                    float.TryParse(split[3], out float a))
                {
                    return new Color(r, g, b, a);
                }
                return new Color(0.2f, 0.7f, 1f, 0.5f);
            }
            set => EditorPrefs.SetString(Prefix + "HighlightColor", $"{value.r},{value.g},{value.b},{value.a}");
        }

        public static bool ShowIcons
        {
            get => EditorPrefs.GetBool(Prefix + "ShowIcons", true);
            set => EditorPrefs.SetBool(Prefix + "ShowIcons", value);
        }

        public static bool IsFirstRun
        {
            get => EditorPrefs.GetBool(Prefix + "IsFirstRun", true);
            set => EditorPrefs.SetBool(Prefix + "IsFirstRun", value);
        }

        public static bool ShowOnStartup
        {
            get => EditorPrefs.GetBool(Prefix + "ShowOnStartup", true);
            set => EditorPrefs.SetBool(Prefix + "ShowOnStartup", value);
        }

        public static bool ShowOnlyActive
        {
            get => EditorPrefs.GetBool(Prefix + "ShowOnlyActive", false);
            set => EditorPrefs.SetBool(Prefix + "ShowOnlyActive", value);
        }

        public static void ResetToDefaults()
        {
            ModifierKeys = EventModifiers.Control | EventModifiers.Shift;
            FocusOnSelect = true;
            ExcludeLayers = 0;
            HighlightColor = new Color(0.2f, 0.7f, 1f, 0.5f);
            ShowIcons = true;
        }

        public static void ApplyPreset(string preset)
        {
            switch (preset)
            {
                case "Minimalist":
                    ModifierKeys = EventModifiers.Shift;
                    ShowIcons = false;
                    FocusOnSelect = false;
                    break;
                case "Pro":
                    ModifierKeys = EventModifiers.Alt | EventModifiers.Shift;
                    FocusOnSelect = true;
                    HighlightColor = new Color(1f, 0.5f, 0f, 0.7f);
                    break;
                case "Standard":
                default:
                    ResetToDefaults();
                    break;
            }
        }
    }
}
