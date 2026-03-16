using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Vassals;

public class ApplyUIFixes : EditorWindow
{
    [InitializeOnLoadMethod]
    public static void ApplyFixes()
    {
        if (EditorPrefs.GetBool("FixesApplied1654_V4", false)) return;
        EditorPrefs.SetBool("FixesApplied1654_V4", true);
        
        try
        {
            // 1. Fix Button Normal Colors for Slots
            var slots = GameObject.FindObjectsByType<MaouSamaTD.UI.UnitCardSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int fixedSlots = 0;
            foreach (var slot in slots)
            {
                var btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    var c = btn.colors;
                    if (c.normalColor != Color.white || c.colorMultiplier != 1f)
                    {
                        c.normalColor = Color.white;
                        c.colorMultiplier = 1f; // Fix visibility
                        btn.colors = c;
                        EditorUtility.SetDirty(btn);
                        fixedSlots++;
                    }
                }
                
                // Rename MissionReadinessSlot_X to CohortSlot_X
                if (slot.gameObject.name.StartsWith("MissionReadinessSlot_"))
                {
                    slot.gameObject.name = slot.gameObject.name.Replace("MissionReadinessSlot_", "CohortSlot_");
                    EditorUtility.SetDirty(slot.gameObject);
                }
            }
            Debug.Log($"Fixed {fixedSlots} slot buttons and applied renames.");

        // 2. Attach RangeGridVisualizer to RangeGrid prefabs (if not already)
        // The prefab itself is enough, but it's instantiated in the scene probably.
        var rangeGrids = GameObject.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        foreach (var rg in rangeGrids)
        {
            if (rg.name == "RangeGrid")
            {
                var vis = rg.gameObject.GetComponent<RangeGridVisualizer>();
                if (vis == null)
                {
                    vis = rg.gameObject.AddComponent<RangeGridVisualizer>();
                    EditorUtility.SetDirty(rg.gameObject);
                }
            }
        }
        
        // 3. Update Vassals_Page_UI
        var vassalsPanel = GameObject.FindAnyObjectByType<VassalsBarracksPanel>(FindObjectsInactive.Include);
        var cohortPanel = GameObject.FindAnyObjectByType<MaouSamaTD.UI.CohortManagerPanel>(FindObjectsInactive.Include);
        
        if (vassalsPanel != null && cohortPanel != null)
        {
            Transform vassalsRoot = vassalsPanel.transform;
            Transform cohortRoot = cohortPanel.transform;

            // Delete ActionsContainer
            Transform actionsContainer = vassalsRoot.Find("ActionsContainer");
            if (actionsContainer != null)
            {
                GameObject.DestroyImmediate(actionsContainer.gameObject);
            }

            // Copy Header from Cohort_Page_UI if Vassals doesn't have one
            Transform vassalsHeader = vassalsRoot.Find("Header");
            if (vassalsHeader == null)
            {
                Transform cohortHeader = cohortRoot.Find("Header");
                if (cohortHeader != null)
                {
                    GameObject newHeader = PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(cohortHeader.gameObject)) as GameObject;
                    if (newHeader == null) // If it's not a prefab, just instantiate
                    {
                        newHeader = GameObject.Instantiate(cohortHeader.gameObject);
                    }
                    newHeader.name = "Header";
                    newHeader.transform.SetParent(vassalsRoot, false);
                    newHeader.transform.SetAsFirstSibling(); // Put it at top
                    
                    // Update Title
                    Transform titleObj = newHeader.transform.Find("MissionReadiness_Title");
                    if (titleObj != null)
                    {
                        var text = titleObj.GetComponent<TextMeshProUGUI>();
                        if (text != null) text.text = "VASSALS";
                    }

                    // Wire Back Button
                    var backObj = newHeader.transform.Find("Back_MissionReady_Btn");
                    if (backObj != null)
                    {
                        var backBtn = backObj.GetComponent<Button>();
                        if (backBtn != null)
                        {
                            var so = new SerializedObject(vassalsPanel);
                            so.FindProperty("_btnClose").objectReferenceValue = backBtn;
                            
                            // Also we might need to remove CohortManagerPanel listener and add VassalsBarracksPanel close?
                            // Actually it's cleaner to handle via scripting in VassalsBarracksPanel Awake, but setting the reference is good.
                            so.ApplyModifiedProperties();
                        }
                    }
                    
                    EditorUtility.SetDirty(vassalsPanel);
                }
            }
        }

        // 4. Wire UnitInspectorPanel references
        var inspectors = GameObject.FindObjectsByType<UnitInspectorPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inspector in inspectors)
        {
            var so = new SerializedObject(inspector);
            
            // Try tracking down _classIcon
            var classIconObj = FindDeepChild(inspector.transform, "ClassFilterScroll"); // Wait, class icon is in the header near close button usually.
            // Actually it's usually inside TopArea or somewhere near _nameText.
            // Let's dynamically find images with Class or icon in name if missing.
            // Let user assign it if we can't find it precisely, but let's try.
            
            var passiveIcon = FindDeepChild(inspector.transform, "Passive_Icon");
            var passiveName = FindDeepChild(inspector.transform, "Passive_Name");
            var passiveDesc = FindDeepChild(inspector.transform, "Passive_Desc");
            
            var activeIcon = FindDeepChild(inspector.transform, "Active_Icon");
            var activeName = FindDeepChild(inspector.transform, "Active_Name");
            var activeDesc = FindDeepChild(inspector.transform, "Active_Desc");
            
            var ultIcon = FindDeepChild(inspector.transform, "Ultimate_Icon");
            var ultName = FindDeepChild(inspector.transform, "Ultimate_Name");
            var ultDesc = FindDeepChild(inspector.transform, "Ultimate_Desc");
            
            var classIcon = FindDeepChild(inspector.transform, "Class_Icon");
            var rangeGrid = inspector.GetComponentInChildren<RangeGridVisualizer>(true);

            if (passiveIcon) so.FindProperty("_passiveIcon").objectReferenceValue = passiveIcon.GetComponent<Image>();
            if (passiveName) so.FindProperty("_passiveName").objectReferenceValue = passiveName.GetComponent<TextMeshProUGUI>();
            if (passiveDesc) so.FindProperty("_passiveDesc").objectReferenceValue = passiveDesc.GetComponent<TextMeshProUGUI>();

            if (activeIcon) so.FindProperty("_activeIcon").objectReferenceValue = activeIcon.GetComponent<Image>();
            if (activeName) so.FindProperty("_activeName").objectReferenceValue = activeName.GetComponent<TextMeshProUGUI>();
            if (activeDesc) so.FindProperty("_activeDesc").objectReferenceValue = activeDesc.GetComponent<TextMeshProUGUI>();

            if (ultIcon) so.FindProperty("_ultimateIcon").objectReferenceValue = ultIcon.GetComponent<Image>();
            if (ultName) so.FindProperty("_ultimateName").objectReferenceValue = ultName.GetComponent<TextMeshProUGUI>();
            if (ultDesc) so.FindProperty("_ultimateDesc").objectReferenceValue = ultDesc.GetComponent<TextMeshProUGUI>();
            
            if (classIcon) so.FindProperty("_classIcon").objectReferenceValue = classIcon.GetComponent<Image>();
            if (rangeGrid) so.FindProperty("_rangeGrid").objectReferenceValue = rangeGrid;

            // Stats matching
            var rnt = FindDeepChild(inspector.transform, "Range_Txt");
            if (rnt) so.FindProperty("_rangeText").objectReferenceValue = rnt.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(inspector);
        }

        Debug.Log("UI Fixes V3 applied successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("ApplyUIFixes Exception: " + ex);
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
