using UnityEngine;
using UnityEditor;

public class DeleteLegacyBriefingPanelObjects
{
    [MenuItem("Tools/Delete Legacy Briefing Panel Objects")]
    public static void Clean()
    {
        string path = "Assets/_Game/Prefabs/UI/campaign/BriefingPanel.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        
        Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
        if (visualRoot != null)
        {
            Transform icon = visualRoot.Find("BriefingIcon");
            if (icon != null) Object.DestroyImmediate(icon.gameObject);

            Transform box = visualRoot.Find("BriefingBox");
            if (box != null) Object.DestroyImmediate(box.gameObject);

            Transform reward = visualRoot.Find("Reward");
            if (reward != null) Object.DestroyImmediate(reward.gameObject);
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("Legacy Briefing Panel Objects Deleted!");
    }
}
