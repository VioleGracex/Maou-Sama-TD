using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class FixWidthEditor
{
    [MenuItem("Tools/MaouSamaTD/Fix ScrollView Width")]
    public static void Execute()
    {
        string scenePath = "Assets/_Game/Scenes/Home_New.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        GameObject scrollViewObj = GameObject.Find("RiteSlots_ScrollView");
        if (scrollViewObj != null)
        {
            LayoutElement le = scrollViewObj.GetComponent<LayoutElement>();
            if (le == null) le = scrollViewObj.AddComponent<LayoutElement>();
            
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            
            // Just to be safe, also check if we need to force update the layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewObj.transform.parent.GetComponent<RectTransform>());
            
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Added LayoutElement with flexible width to RiteSlots_ScrollView");
        }
        else
        {
            Debug.LogError("Could not find RiteSlots_ScrollView");
        }
    }
}
