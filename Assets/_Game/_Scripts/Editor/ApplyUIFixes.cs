using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Vassals;

public class ApplyUIFixes : EditorWindow
{
    [InitializeOnLoadMethod]
    [MenuItem("Tools/Apply UI Fixes V7")]
    public static void ApplyFixes()
    {
        if (EditorPrefs.GetBool("FixesApplied1654_V7", false)) return;
        EditorPrefs.SetBool("FixesApplied1654_V7", true);

        try
        {
            // 1. Fix Button Normal Colors for Slots + rename legacy names
            var allSlots = GameObject.FindObjectsByType<MaouSamaTD.UI.UnitCardSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int fixedSlots = 0;
            foreach (var slot in allSlots)
            {
                var btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    var c = btn.colors;
                    if (c.normalColor != Color.white || c.colorMultiplier != 1f)
                    {
                        c.normalColor = Color.white;
                        c.colorMultiplier = 1f;
                        btn.colors = c;
                        EditorUtility.SetDirty(btn);
                        fixedSlots++;
                    }
                }
                if (slot.gameObject.name.StartsWith("MissionReadinessSlot_"))
                {
                    slot.gameObject.name = slot.gameObject.name.Replace("MissionReadinessSlot_", "CohortSlot_");
                    EditorUtility.SetDirty(slot.gameObject);
                }
            }
            Debug.Log($"Fixed {fixedSlots} slot buttons and applied renames.");

            // 2. Attach RangeGridVisualizer
            var rangeGrids = GameObject.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            foreach (var rg in rangeGrids)
            {
                if (rg.name == "RangeGrid")
                {
                    if (rg.gameObject.GetComponent<RangeGridVisualizer>() == null)
                    {
                        rg.gameObject.AddComponent<RangeGridVisualizer>();
                        EditorUtility.SetDirty(rg.gameObject);
                    }
                }
            }

            // 3. Vassals/Cohort panel
            var vassalsPanel = GameObject.FindAnyObjectByType<VassalsBarracksPanel>(FindObjectsInactive.Include);
            var cohortPanel  = GameObject.FindAnyObjectByType<MaouSamaTD.UI.CohortManagerPanel>(FindObjectsInactive.Include);

            if (vassalsPanel != null && cohortPanel != null)
            {
                Transform vassalsRoot = vassalsPanel.transform;
                Transform cohortRoot  = cohortPanel.transform;

                Transform actionsContainer = vassalsRoot.Find("ActionsContainer");
                if (actionsContainer != null)
                    GameObject.DestroyImmediate(actionsContainer.gameObject);

                Transform vassalsHeader = vassalsRoot.Find("Header");
                if (vassalsHeader == null)
                {
                    Transform cohortHeader = cohortRoot.Find("Header");
                    if (cohortHeader != null)
                    {
                        GameObject newHeader = PrefabUtility.InstantiatePrefab(
                            PrefabUtility.GetCorrespondingObjectFromSource(cohortHeader.gameObject)) as GameObject;
                        if (newHeader == null)
                            newHeader = GameObject.Instantiate(cohortHeader.gameObject);

                        newHeader.name = "Header";
                        newHeader.transform.SetParent(vassalsRoot, false);
                        newHeader.transform.SetAsFirstSibling();

                        Transform titleObj = newHeader.transform.Find("MissionReadiness_Title");
                        if (titleObj != null)
                        {
                            var text = titleObj.GetComponent<TextMeshProUGUI>();
                            if (text != null) text.text = "VASSALS";
                        }

                        var backObj = newHeader.transform.Find("Back_MissionReady_Btn");
                        if (backObj != null)
                        {
                            var backBtn2 = backObj.GetComponent<Button>();
                            if (backBtn2 != null)
                            {
                                var so2 = new SerializedObject(vassalsPanel);
                                so2.FindProperty("_btnClose").objectReferenceValue = backBtn2;
                                so2.ApplyModifiedProperties();
                            }
                        }
                        EditorUtility.SetDirty(vassalsPanel);
                    }
                }
            }

            // 4. Wire VassalDetailPanel references
            var inspectors = GameObject.FindObjectsByType<VassalDetailPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var inspector in inspectors)
            {
                var so = new SerializedObject(inspector);

                void TrySet(string prop, string childName, System.Func<Transform, Object> getter) {
                    var t = FindDeepChild(inspector.transform, childName);
                    if (t != null) { var obj = getter(t); if (obj != null) so.FindProperty(prop).objectReferenceValue = obj; }
                }

                TrySet("_passiveIcon", "Passive_Icon", t => t.GetComponent<Image>());
                TrySet("_passiveName", "Passive_Name", t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_passiveDesc", "Passive_Desc", t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_activeIcon",  "Active_Icon",  t => t.GetComponent<Image>());
                TrySet("_activeName",  "Active_Name",  t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_activeDesc",  "Active_Desc",  t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_ultimateIcon","Ultimate_Icon", t => t.GetComponent<Image>());
                TrySet("_ultimateName","Ultimate_Name", t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_ultimateDesc","Ultimate_Desc", t => t.GetComponent<TextMeshProUGUI>());
                TrySet("_classIcon",   "Class_Icon",   t => t.GetComponent<Image>());
                TrySet("_rangeText",   "Range_Txt",    t => t.GetComponent<TextMeshProUGUI>());

                var rangeGrid = inspector.GetComponentInChildren<RangeGridVisualizer>(true);
                if (rangeGrid) so.FindProperty("_rangeGrid").objectReferenceValue = rangeGrid;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(inspector);
            }

            // 5. CohortManagerPanel: rebuild lost references
            if (cohortPanel != null)
            {
                var cSO = new SerializedObject(cohortPanel);
                var root = GameObject.Find("Cohort_Page_UI");
                if (root) cSO.FindProperty("_visualRoot").objectReferenceValue = root;

                var vp = GameObject.FindAnyObjectByType<VassalsBarracksPanel>(FindObjectsInactive.Include);
                if (vp) cSO.FindProperty("_vassalsBarracksController").objectReferenceValue = vp;

                if (root != null)
                {
                    var header = root.transform.Find("Header");
                    if (header != null)
                    {
                        var title = FindDeepChild(header, "MissionReadiness_Title");
                        if (title) cSO.FindProperty("_titleText").objectReferenceValue = title.GetComponent<TextMeshProUGUI>();
                        var backBtn = FindDeepChild(header, "Back_MissionReady_Btn");
                        if (backBtn) cSO.FindProperty("_backButton").objectReferenceValue = backBtn.GetComponent<Button>();
                    }

                    var actContainer = root.transform.Find("ActionsContainer");
                    if (actContainer != null)
                    {
                        var saveBtn2 = actContainer.Find("Save_Btn");
                        if (saveBtn2) cSO.FindProperty("_saveButton").objectReferenceValue = saveBtn2.GetComponent<Button>();

                        var removeAllBtn = actContainer.Find("Remove_All_Btn");
                        if (removeAllBtn) cSO.FindProperty("_removeAllButton").objectReferenceValue = removeAllBtn.GetComponent<Button>();

                        var selectMultipleBtn = actContainer.Find("Select_Multiple_Btn");
                        if (selectMultipleBtn) cSO.FindProperty("_barracksButton").objectReferenceValue = selectMultipleBtn.GetComponent<Button>();

                        var startBtn = actContainer.Find("StartBattle_Btn");
                        if (startBtn == null && saveBtn2 != null)
                        {
                            var go2 = UnityEngine.Object.Instantiate(saveBtn2.gameObject, actContainer);
                            go2.name = "StartBattle_Btn";
                            var txt2 = go2.GetComponentInChildren<TextMeshProUGUI>();
                            if (txt2) txt2.text = "START BATTLE";
                            startBtn = go2.transform;
                        }
                        if (startBtn) cSO.FindProperty("_startBattleButton").objectReferenceValue = startBtn.GetComponent<Button>();
                    }

                    // Collect and sort CohortSlot_X objects
                    var cohortSlots2 = root.GetComponentsInChildren<MaouSamaTD.UI.UnitCardSlot>(true);
                    System.Array.Sort(cohortSlots2, (a, b) =>
                    {
                        string aStr = a.name.Replace("CohortSlot_", "");
                        string bStr = b.name.Replace("CohortSlot_", "");
                        int.TryParse(aStr, out int numA);
                        int.TryParse(bStr, out int numB);
                        return numA.CompareTo(numB);
                    });

                    var slotsProp = cSO.FindProperty("_slots");
                    slotsProp.ClearArray();
                    for (int i = 0; i < cohortSlots2.Length && i < 12; i++)
                    {
                        slotsProp.InsertArrayElementAtIndex(i);
                        slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cohortSlots2[i];

                        var slotSO   = new SerializedObject(cohortSlots2[i]);
                        var emptyVis = FindDeepChild(cohortSlots2[i].transform, "EmptyGraphic_Visual");
                        if (emptyVis) slotSO.FindProperty("_emptyVisual").objectReferenceValue = emptyVis.gameObject;

                        var emptyTxt = emptyVis ? emptyVis.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                        if (emptyTxt) slotSO.FindProperty("_emptySlotText").objectReferenceValue = emptyTxt;

                        var unitCard = cohortSlots2[i].GetComponentInChildren<MaouSamaTD.UI.MainMenu.UnitCardUI>(true);
                        if (unitCard) slotSO.FindProperty("_unitCardUI").objectReferenceValue = unitCard;

                        slotSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(cohortSlots2[i]);
                    }

                    var popup = root.transform.Find("UnsavedChangesPopup");
                    if (popup != null)
                    {
                        cSO.FindProperty("_unsavedChangesPopup").objectReferenceValue = popup.gameObject;
                        var cBtn  = FindDeepChild(popup, "Confirm_Btn");
                        if (cBtn)  cSO.FindProperty("_confirmLeaveButton").objectReferenceValue = cBtn.GetComponent<Button>();
                        var cxBtn = FindDeepChild(popup, "Cancel_Btn");
                        if (cxBtn) cSO.FindProperty("_cancelLeaveButton").objectReferenceValue = cxBtn.GetComponent<Button>();
                    }

                    var blocker = root.transform.Find("NoEditBlocker");
                    if (blocker) cSO.FindProperty("_noEditBlocker").objectReferenceValue = blocker.gameObject;

                    var cohortsRoot = root.transform.Find("Cohorts_Root");
                    if (cohortsRoot == null) cohortsRoot = FindDeepChild(root.transform, "Cohorts_Root");
                    if (cohortsRoot != null)
                    {
                        var teamBtnsList = new System.Collections.Generic.List<Button>();
                        foreach (var b in cohortsRoot.GetComponentsInChildren<Button>(true))
                            if (b.name.IndexOf("COHORT", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                teamBtnsList.Add(b);

                        var cohortBtnProp = cSO.FindProperty("_cohortButtons");
                        cohortBtnProp.ClearArray();
                        for (int i = 0; i < teamBtnsList.Count; i++)
                        {
                            cohortBtnProp.InsertArrayElementAtIndex(i);
                            cohortBtnProp.GetArrayElementAtIndex(i).objectReferenceValue = teamBtnsList[i];
                        }
                    }
                }

                cSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(cohortPanel);
                Debug.Log("UI Fixes V7: CohortManagerPanel refs updated.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("ApplyUIFixes V7 Exception: " + ex);
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
