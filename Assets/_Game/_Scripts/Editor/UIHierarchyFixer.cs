using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace MaouSamaTD.Editor
{
    public static class UIHierarchyFixer
    {
        private const string GOLD_BTN_PATH = "Assets/_Game/Art/UI/Buttons/UI_Btn_GoldBorder.png";
        private const string FILTER_TAB_PATH = "Assets/_Game/Art/UI/Buttons/UI_Btn_Filter_Normal.png";
        private const string PREFAB_PATH = "Assets/_Game/Prefabs/UI/UnitInspector_FullScreen_UI.prefab";

        [MenuItem("MAOU TD/Finalize & Save Prefab")]
        public static void FinalizeAndSave()
        {
            GameObject root = GameObject.Find("UnitInspector_FullScreen_UI");
            if (root == null)
            {
                Debug.LogError("Could not find 'UnitInspector_FullScreen_UI' in the scene.");
                return;
            }

            // Ensure Correct Path
            string dir = "Assets/_Game/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "_Game/Prefabs/UI"));
                AssetDatabase.Refresh();
            }

            // 1. Unpack & Clean Break
            if (PrefabUtility.IsPartOfAnyPrefab(root))
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // 2. Refresh Visuals just in case
            ApplyVisuals(root.transform);

            // 3. Save as Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PREFAB_PATH, InteractionMode.AutomatedAction);
            Debug.Log($"Successfully saved Unit Inspector Prefab to {PREFAB_PATH}");
        }

        private static void ApplyVisuals(Transform root)
        {
            Sprite goldBtn = AssetDatabase.LoadAssetAtPath<Sprite>(GOLD_BTN_PATH);
            Sprite filterTab = AssetDatabase.LoadAssetAtPath<Sprite>(FILTER_TAB_PATH);

            Transform charPanel = root.Find("Character_Panel");
            Transform detPanel = root.Find("Details_Panel");

            if (charPanel != null && charPanel is RectTransform rtChar)
            {
                rtChar.anchorMin = new Vector2(0, 0);
                rtChar.anchorMax = new Vector2(0.4f, 1);
                rtChar.offsetMin = rtChar.offsetMax = Vector2.zero;
            }
            if (detPanel != null && detPanel is RectTransform rtDet)
            {
                rtDet.anchorMin = new Vector2(0.4f, 0);
                rtDet.anchorMax = new Vector2(1, 1);
                rtDet.offsetMin = rtDet.offsetMax = Vector2.zero;
                
                Image img = rtDet.GetComponent<Image>();
                if (img != null) img.color = new Color(0.16f, 0.16f, 0.16f, 0.86f);

                Transform btnLevel = rtDet.Find("LevelUp_Button");
                if (btnLevel != null && btnLevel.GetComponent<Image>() != null) btnLevel.GetComponent<Image>().sprite = goldBtn;
                
                Transform btnPromote = rtDet.Find("Promote_Button");
                if (btnPromote != null && btnPromote.GetComponent<Image>() != null) btnPromote.GetComponent<Image>().sprite = goldBtn;

                Transform tabs = rtDet.Find("Tabs_Root");
                if (tabs != null)
                {
                    foreach (Transform tab in tabs)
                    {
                        Image tabImg = tab.GetComponent<Image>();
                        if (tabImg != null) tabImg.sprite = filterTab;
                    }
                }
            }
        }
    }
}
