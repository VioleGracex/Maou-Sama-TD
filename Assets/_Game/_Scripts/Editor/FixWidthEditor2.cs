using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class FixWidthEditor2
{
    [MenuItem("Tools/MaouSamaTD/Fix ScrollView Width 2")]
    public static void Execute()
    {
        string scenePath = "Assets/_Game/Scenes/Home_New.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        var cohortSquad = Object.FindObjectOfType<MaouSamaTD.UI.Cohorts.CohortSquadUI>(true);
        if (cohortSquad != null)
        {
            var so = new SerializedObject(cohortSquad);
            var riteSlotsProp = so.FindProperty("_riteSlots");
            if (riteSlotsProp.arraySize > 0)
            {
                var firstSlotRef = riteSlotsProp.GetArrayElementAtIndex(0).objectReferenceValue as Component;
                if (firstSlotRef != null)
                {
                    Transform container = firstSlotRef.transform.parent; // RiteSlots_Container
                    Transform viewport = container.parent; // Viewport
                    Transform scrollView = viewport.parent; // RiteSlots_ScrollView
                    
                    LayoutElement le = scrollView.GetComponent<LayoutElement>();
                    if (le == null) le = scrollView.gameObject.AddComponent<LayoutElement>();
                    
                    le.flexibleWidth = 1f;
                    le.flexibleHeight = 1f;
                    
                    // Force rebuild
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.parent.GetComponent<RectTransform>());
                    
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("Added LayoutElement with flexible width to RiteSlots_ScrollView (Found via CohortSquadUI)");
                    return;
                }
            }
        }
        Debug.LogError("Could not find RiteSlots_ScrollView via CohortSquadUI");
    }
}
