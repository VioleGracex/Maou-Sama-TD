using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiImageGenerator
{
    [RequireComponent(typeof(Image))]
    public class AiImageGenerator : MonoBehaviour
    {
        public enum GenerationState { Ready, Pending, Generating, Success, Error }

        [Header("Global Settings")]
        public AiImageGeneratorConfig config;

        [Header("Image Parameters")]
        [TextArea(3, 10)]
        public string prompt;
        public Vector2Int resolution = new Vector2Int(512, 512);

        [Header("Overrides")]
        public StylePreset styleOverride;
        public ColorPalette paletteOverride;
        public bool usePaletteOverride;

        [Header("Storage")]
        public string assetPathOverride;
        public string fileName;

        [Header("Status")]
        public GenerationState state = GenerationState.Ready;
        [TextArea(2, 5)] public string statusMessage;

        private Image _image;

        private void OnValidate()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (string.IsNullOrEmpty(fileName)) fileName = gameObject.name.Replace(" ", "_");
        }

        public void RequestGeneration()
        {
            state = GenerationState.Pending;
            statusMessage = "Waiting for Antigravity to sync...";
            Debug.Log($"<color=cyan>[Antigravity-SYNC]</color> Requesting generation for {gameObject.name}. Prompt: {prompt}");
        }

        public void CancelRequest()
        {
            state = GenerationState.Ready;
            statusMessage = "Request cancelled.";
            Debug.Log($"<color=cyan>[Antigravity]</color> Image generation for {gameObject.name} was cancelled.");
        }

        public void CompleteGeneration(string path, bool success)
        {
            state = success ? GenerationState.Success : GenerationState.Error;
            statusMessage = success ? "Successfully generated and assigned!" : "Generation failed.";
            
            if (config != null)
            {
                config.AddHistoryRecord(gameObject.name, prompt, path, state.ToString());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(config);
#endif
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public string GetFullPrompt()
        {
            string fullPrompt = "";
            if (config != null)
            {
                fullPrompt += config.projectContext + " ";
            }
            
            if (styleOverride != null && !string.IsNullOrEmpty(styleOverride.includePrompts))
                fullPrompt += styleOverride.includePrompts + " ";
                
            fullPrompt += prompt + " ";

            if (usePaletteOverride && paletteOverride != null)
            {
                fullPrompt += paletteOverride.GetDescription();
            }

            return fullPrompt.Trim();
        }

        public string GetNegativePrompt()
        {
            string neg = config != null ? config.globalNegativePrompt : "";
            if (styleOverride != null && !string.IsNullOrEmpty(styleOverride.excludePrompts))
                neg += ", " + styleOverride.excludePrompts;
            return neg;
        }

        public string GetSavePath()
        {
            string path = !string.IsNullOrEmpty(assetPathOverride) ? assetPathOverride : (config != null ? config.defaultSavePath : "_Game/Art/Generated/");
            if (!path.EndsWith("/")) path += "/";
            return path;
        }
    }
}
