using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class FixCohortRiteScrollView
{
    [MenuItem("Tools/MaouSamaTD/Convert Rite Slots to Scroll View")]
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
        Transform parentTransform = container.transform.parent; // Center_Rites
        
        // Create ScrollView
        GameObject scrollView = new GameObject("RiteSlots_ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollView.transform.SetParent(parentTransform, false);
        scrollView.transform.SetSiblingIndex(container.transform.GetSiblingIndex());
        
        RectTransform svRect = scrollView.GetComponent<RectTransform>();
        RectTransform oldRect = container.GetComponent<RectTransform>();
        
        // Copy the rect from the original container to the scroll view
        svRect.anchorMin = oldRect.anchorMin;
        svRect.anchorMax = oldRect.anchorMax;
        svRect.anchoredPosition = oldRect.anchoredPosition;
        svRect.sizeDelta = oldRect.sizeDelta;
        svRect.pivot = oldRect.pivot;

        // Create Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        vpRect.anchoredPosition = Vector2.zero;
        
        Image vpImg = viewport.GetComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Move container into viewport
        container.transform.SetParent(viewport.transform, false);
        
        // Update container to expand based on content
        oldRect.anchorMin = new Vector2(0, 1);
        oldRect.anchorMax = new Vector2(1, 1);
        oldRect.pivot = new Vector2(0.5f, 1);
        oldRect.anchoredPosition = Vector2.zero;
        oldRect.sizeDelta = new Vector2(0, oldRect.sizeDelta.y);

        // Change layout to Grid Layout so it wraps nicely
        var vertical = container.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) Object.DestroyImmediate(vertical);
        
        var horizontal = container.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) Object.DestroyImmediate(horizontal);
        
        var grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(400, 90); // Use a wide cell size suitable for the container width
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
        }

        // Add ContentSizeFitter
        ContentSizeFitter csf = container.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Hook up ScrollRect
        ScrollRect sr = scrollView.GetComponent<ScrollRect>();
        sr.content = oldRect;
        sr.viewport = vpRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.inertia = true;
        sr.scrollSensitivity = 20f;
        
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
            newSlotObj.name = "RiteSlot_" + (i + 1);
            
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
        
        Debug.Log("Successfully converted RiteSlots to ScrollView and expanded to 10 in Home_New!");
    }
}
