using UnityEditor;
using UnityEngine;
using AiImageGenerator;

namespace AiImageGenerator.Editor
{
    [CustomEditor(typeof(AiImageGenerator))]
    public class AiImageGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AiImageGenerator gen = (AiImageGenerator)target;

            serializedObject.Update();

            // Draw everything except the status fields
            DrawPropertiesExcluding(serializedObject, "m_Script", "state", "statusMessage");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            
            // Draw status as read-only labels
            GUI.enabled = false;
            EditorGUILayout.EnumPopup("State", gen.state);
            EditorGUILayout.TextField("Message", gen.statusMessage);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI Actions", EditorStyles.boldLabel);

            bool isBusy = gen.state == AiImageGenerator.GenerationState.Pending || 
                          gen.state == AiImageGenerator.GenerationState.Generating;

            if (isBusy)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button($"SYNC MODE: {gen.state}...", GUILayout.Height(40));
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Cancel Request", GUILayout.Width(120)))
                {
                    gen.CancelRequest();
                    EditorUtility.SetDirty(gen);
                }
                
                EditorGUILayout.HelpBox("Antigravity is currently monitoring Unity for your request. It should be processed automatically within seconds.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("Generate with Antigravity", GUILayout.Height(40)))
                {
                    gen.RequestGeneration();
                    EditorUtility.SetDirty(gen);
                }
            }

            if (gen.state == AiImageGenerator.GenerationState.Error)
            {
                EditorGUILayout.HelpBox($"Error: {gen.statusMessage}", MessageType.Error);
                if (GUILayout.Button("Clear Error"))
                {
                    gen.state = AiImageGenerator.GenerationState.Ready;
                    gen.statusMessage = "";
                }
            }
            else if (gen.state == AiImageGenerator.GenerationState.Success)
            {
                EditorGUILayout.HelpBox("Successfully generated and synced!", MessageType.Info);
                if (GUILayout.Button("Reset State"))
                {
                    gen.state = AiImageGenerator.GenerationState.Ready;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
