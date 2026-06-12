using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class ExpandRiteSlotsEditor
{
    [MenuItem("Tools/MaouSamaTD/Expand Cohort Rite Slots to 10")]
    public static void Execute()
    {
        string scenePath = "Assets/_Game/Scenes/Home_New.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        var cohortSquad = Object.FindObjectOfType<MaouSamaTD.UI.Cohorts.CohortSquadUI>(true);
        if (cohortSquad == null)
        {
            Debug.LogError("CohortSquadUI not found in Home_New scene.");
            return;
        }

        var so = new SerializedObject(cohortSquad);
        var riteSlotsProp = so.FindProperty("_riteSlots");
        
        if (riteSlotsProp.arraySize < 1)
        {
            Debug.LogError("No rite slots found to duplicate.");
            return;
        }
        
        // Get the first slot to duplicate
        var firstSlotRef = riteSlotsProp.GetArrayElementAtIndex(0).objectReferenceValue as MaouSamaTD.UI.Cohorts.CohortRiteSlot;
        if (firstSlotRef == null) return;
        
        GameObject container = firstSlotRef.gameObject.transform.parent.gameObject;
        
        // Change layout to Grid Layout so it wraps nicely
        var horizontal = container.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) Object.DestroyImmediate(horizontal);
        
        var grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90, 90); // Guessing a size, adjust if necessary
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;
        }
        
        // Collect current slots
        List<MaouSamaTD.UI.Cohorts.CohortRiteSlot> allSlots = new List<MaouSamaTD.UI.Cohorts.CohortRiteSlot>();
        for (int i = 0; i < riteSlotsProp.arraySize; i++)
        {
            var s = riteSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue as MaouSamaTD.UI.Cohorts.CohortRiteSlot;
            if (s != null) allSlots.Add(s);
        }
        
        // Duplicate until we have 10
        int targetCount = 10;
        int currentCount = allSlots.Count;
        
        for (int i = currentCount; i < targetCount; i++)
        {
            GameObject newSlotObj = PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(firstSlotRef.gameObject)) as GameObject;
            if (newSlotObj == null) 
            {
                newSlotObj = Object.Instantiate(firstSlotRef.gameObject);
            }
            
            newSlotObj.transform.SetParent(container.transform, false);
            newSlotObj.name = "RiteSlot_" + i;
            
            var newSlotComp = newSlotObj.GetComponent<MaouSamaTD.UI.Cohorts.CohortRiteSlot>();
            allSlots.Add(newSlotComp);
        }
        
        // Update property array
        riteSlotsProp.arraySize = targetCount;
        for (int i = 0; i < targetCount; i++)
        {
            riteSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = allSlots[i];
        }
        
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("Successfully expanded Cohort Rite Slots to 10 in Home_New!");
    }
}
