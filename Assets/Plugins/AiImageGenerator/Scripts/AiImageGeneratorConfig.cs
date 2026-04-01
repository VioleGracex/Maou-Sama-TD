using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AiImageGenerator
{
    [Serializable]
    public class ColorPalette
    {
        public string name;
        public List<Color> colors = new List<Color>();
        public bool active = true;

        public string GetDescription()
        {
            if (colors == null || colors.Count == 0) return "";
            string desc = "Color palette: ";
            foreach (var c in colors)
            {
                desc += ColorUtility.ToHtmlStringRGB(c) + " ";
            }
            return desc.Trim();
        }
    }

    [Serializable]
    public class StylePreset
    {
        public string name;
        [TextArea(2, 5)] public string includePrompts;
        [TextArea(2, 5)] public string excludePrompts;
    }

    [System.Serializable]
    public class GenerationHistoryRecord
    {
        public string objectName;
        public string prompt;
        public string resultPath;
        public string timestamp;
        public string state;
    }

    [CreateAssetMenu(fileName = "AiImageGeneratorConfig", menuName = "AI Image Generator/AI Image Generator Config")]
    public class AiImageGeneratorConfig : ScriptableObject
    {
        public enum GeminiModel
        {
            Gemini_1_5_Flash,
            Gemini_1_5_Pro,
            Gemini_2_0_Flash,
            Gemini_2_0_Flash_Lite,
            Gemini_2_0_Pro
        }

        [Header("AI Model Settings")]
        public GeminiModel selectedModel = GeminiModel.Gemini_1_5_Flash;

        [Header("Project Context")]
        [TextArea(5, 20)]
        public string projectContext = "Enter general project description, art style, and context here.";

        [Header("Global Generation Rules")]
        [TextArea(3, 10)]
        public string globalNegativePrompt = "low quality, blurry, distorted, text, watermark, signature, finger, hand, face, person";

        [Header("Global Storage Settings")]
        public string defaultSavePath = "_Game/Art/Generated/";
        public bool useAutomaticNaming = true;

        [Header("Tool Settings")]
        public bool openOnStartup = true;

        [Header("History")]
        public List<GenerationHistoryRecord> history = new List<GenerationHistoryRecord>();

        [Header("Presets")]
        public List<ColorPalette> palettes = new List<ColorPalette>();
        public List<StylePreset> styles = new List<StylePreset>();
        
        public void AddHistoryRecord(string objName, string prompt, string path, string status)
        {
            history.Add(new GenerationHistoryRecord
            {
                objectName = objName,
                prompt = prompt,
                resultPath = path,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                state = status
            });
            
            // Limit history to last 500 entries to prevent SO bloat
            if (history.Count > 500) history.RemoveAt(0);
        }
    }
}
