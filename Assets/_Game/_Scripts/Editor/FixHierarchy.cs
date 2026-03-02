using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixHierarchy : EditorWindow
{
    [MenuItem("Tools/Fix Mission Readiness")]
    public static void Fix()
    {
        GameObject missionPanel = null;
        GameObject canvas = null;

        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == "MissionReadinessPanel" && t.gameObject.hideFlags == HideFlags.None)
            {
                missionPanel = t.gameObject;
            }
            if (t.name == "Canvas" && t.gameObject.hideFlags == HideFlags.None)
            {
                canvas = t.gameObject;
            }
        }

        if (missionPanel != null && canvas != null)
        {
            missionPanel.transform.SetParent(canvas.transform, false);
            EditorUtility.SetDirty(missionPanel);
            EditorSceneManager.MarkSceneDirty(missionPanel.scene);
            Debug.Log("MissionReadinessPanel successfully reparented to Canvas.");
        }
        else
        {
            Debug.LogError("Failed to reparent. Missing objects.");
        }
    }
}
