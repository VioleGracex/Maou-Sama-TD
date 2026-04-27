using UnityEngine;
using UnityEditor;
using MaouSamaTD.Tutorial;
using MaouSamaTD.Story;

namespace MaouSamaTD.Editor.Story
{
    [CustomEditor(typeof(DialogueData))]
    public class DialogueDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Dialogue Tester", GUILayout.Height(40)))
            {
                DialogueTesterWindow.ShowWithAsset((ScriptableObject)target);
            }
            
            EditorGUILayout.Space(10);
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(StoryDataSO))]
    public class StoryDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Dialogue Tester", GUILayout.Height(40)))
            {
                DialogueTesterWindow.ShowWithAsset((ScriptableObject)target);
            }
            
            EditorGUILayout.Space(10);
            base.OnInspectorGUI();
        }
    }
}
