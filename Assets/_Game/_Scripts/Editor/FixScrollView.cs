using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixScrollView
{
    [MenuItem("Tools/MaouSamaTD/Fix Skill Container Scroll View")]
    public static void Execute()
    {
        // Find the container
        GameObject container = GameObject.Find("SkillButtons_Container");
        if (container == null)
        {
            Debug.LogError("SkillButtons_Container not found in the current scene.");
            return;
        }

        Transform middleArea = container.transform.parent;
        
        // Create ScrollView
        GameObject scrollView = new GameObject("SkillButtons_ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollView.transform.SetParent(middleArea, false);
        scrollView.transform.SetSiblingIndex(container.transform.GetSiblingIndex());
        
        RectTransform svRect = scrollView.GetComponent<RectTransform>();
        RectTransform oldRect = container.GetComponent<RectTransform>();
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
        oldRect.anchorMin = new Vector2(0, 1);
        oldRect.anchorMax = new Vector2(1, 1);
        oldRect.pivot = new Vector2(0.5f, 1);
        oldRect.anchoredPosition = Vector2.zero;
        oldRect.sizeDelta = new Vector2(0, oldRect.sizeDelta.y);

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
        sr.scrollSensitivity = 10f;
        
        // Update SkillPanelUI component reference if possible
        var skillPanel = Object.FindObjectOfType<MaouSamaTD.UI.Skills.SkillPanelUI>();
        if (skillPanel != null)
        {
            var so = new UnityEditor.SerializedObject(skillPanel);
            so.Update();
            var prop = so.FindProperty("_buttonContainer");
            if (prop != null)
            {
                prop.objectReferenceValue = oldRect;
                so.ApplyModifiedProperties();
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(container.scene);
        Debug.Log("Successfully wrapped SkillButtons_Container in a ScrollView!");
    }
}
