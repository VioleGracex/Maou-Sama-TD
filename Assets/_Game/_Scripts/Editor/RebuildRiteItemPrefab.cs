using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI;

namespace MaouSamaTD.Editor
{
    public static class RebuildRiteItemPrefab
    {
        [MenuItem("Maou-TD/UI/Rebuild RiteItem Prefab Complete")]
        public static void Rebuild()
        {
            // COMMENTED OUT TO PREVENT BREAKING PREFAB
            /*
            string prefabPath = "Assets/_Game/Prefabs/UI/RiteItem_Prefab.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError("Prefab not found at " + prefabPath);
                return;
            }

            // Create a temporary instance to modify
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            
            // Fix root layout
            var rootCsf = prefab.GetComponent<ContentSizeFitter>();
            if (rootCsf != null)
            {
                rootCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                rootCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            var rootRt = prefab.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(1000f, 150f);

            // Fix TopRow
            Transform topRow = prefab.transform.Find("TopRow");
            if (topRow != null)
            {
                var trHlg = topRow.GetComponent<HorizontalLayoutGroup>();
                if (trHlg != null)
                {
                    trHlg.childControlWidth = true;
                    trHlg.childForceExpandWidth = false;
                }

                // RangeGrid setup
                Transform rangeGridTrans = topRow.Find("RangeGrid");
                if (rangeGridTrans == null && prefab.transform.Find("RangeGrid") != null)
                {
                    rangeGridTrans = prefab.transform.Find("RangeGrid");
                    rangeGridTrans.SetParent(topRow, false);
                }
                
                if (rangeGridTrans != null)
                {
                    rangeGridTrans.SetSiblingIndex(topRow.childCount - 2); // Before expand arrow
                    var rgLe = rangeGridTrans.GetComponent<LayoutElement>();
                    if (rgLe == null) rgLe = rangeGridTrans.gameObject.AddComponent<LayoutElement>();
                    rgLe.ignoreLayout = false;
                    rgLe.minWidth = 100f;
                    rgLe.minHeight = 100f;
                    rgLe.preferredWidth = 100f;
                    rgLe.preferredHeight = 100f;
                    
                    var rgRt = rangeGridTrans.GetComponent<RectTransform>();
                    rgRt.sizeDelta = new Vector2(100f, 100f);
                }

                // Tags setup inside TitleContainer
                Transform titleContainer = topRow.Find("TitleContainer");
                if (titleContainer != null)
                {
                    var tcLe = titleContainer.GetComponent<LayoutElement>();
                    if (tcLe == null) tcLe = titleContainer.gameObject.AddComponent<LayoutElement>();
                    tcLe.flexibleWidth = 1f;

                    Transform tagsTrans = titleContainer.Find("TagsContainer");
                    if (tagsTrans == null)
                    {
                        GameObject tagsGo = new GameObject("TagsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                        tagsGo.transform.SetParent(titleContainer, false);
                        tagsTrans = tagsGo.transform;
                        
                        var tagsHlg = tagsGo.GetComponent<HorizontalLayoutGroup>();
                        tagsHlg.spacing = 5f;
                        tagsHlg.childControlWidth = true;
                        tagsHlg.childControlHeight = true;
                        tagsHlg.childForceExpandWidth = false;
                        tagsHlg.childForceExpandHeight = false;
                    }

                    // Create Tag Prefab template inside the prefab (as a hidden asset, or we can just save it out)
                    // For simplicity, we will create a child called "TagPrefab" inside the prefab and disable it,
                    // so it can be instantiated at runtime.
                    Transform tagPrefabTrans = prefab.transform.Find("TagTemplate");
                    if (tagPrefabTrans == null)
                    {
                        GameObject tagGo = new GameObject("TagTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                        tagGo.transform.SetParent(prefab.transform, false);
                        tagPrefabTrans = tagGo.transform;
                        tagGo.SetActive(false); // Hide it
                        
                        var img = tagGo.GetComponent<Image>();
                        img.color = new Color(0.2f, 0.4f, 0.6f, 1f); // Dark blueish tag background
                        
                        var hlg = tagGo.GetComponent<HorizontalLayoutGroup>();
                        hlg.padding = new RectOffset(8, 8, 4, 4);
                        hlg.childAlignment = TextAnchor.MiddleCenter;
                        
                        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                        textGo.transform.SetParent(tagGo.transform, false);
                        var tmp = textGo.GetComponent<TextMeshProUGUI>();
                        tmp.text = "TAG";
                        tmp.fontSize = 14;
                        tmp.color = Color.white;
                        tmp.alignment = TextAlignmentOptions.Center;
                        
                        var tmpLe = textGo.AddComponent<LayoutElement>();
                        tmpLe.preferredHeight = 20f;
                        
                        var tagLe = tagGo.AddComponent<LayoutElement>();
                        tagLe.preferredHeight = 28f;
                    }

                    // Assign everything to CohortRiteItemUI
                    var ui = prefab.GetComponent<CohortRiteItemUI>();
                    var type = typeof(CohortRiteItemUI);
                    
                    if (rangeGridTrans != null)
                    {
                        type.GetField("_rangeGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .SetValue(ui, rangeGridTrans.GetComponent<RangePatternUI>());
                    }
                    
                    type.GetField("_tagsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .SetValue(ui, tagsTrans.GetComponent<RectTransform>());
                        
                    type.GetField("_tagPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .SetValue(ui, tagPrefabTrans.gameObject);
                }
            }

            // Apply modifications back to the prefab
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            Object.DestroyImmediate(prefab);
            Debug.Log("RiteItem_Prefab has been successfully rebuilt with Tags and RangeGrid!");
            */
        }
    }
}
