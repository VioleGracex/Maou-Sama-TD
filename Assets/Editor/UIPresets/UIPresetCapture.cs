using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIPresetManager
{
    /// <summary>
    /// Handles capturing UI active-state snapshots from the current scene
    /// and applying them back safely (with undo + null-reference protection).
    ///
    /// Scope: every GameObject that is a descendant of any Canvas in the scene,
    /// including the Canvas roots themselves.
    ///
    /// Performance: uses batch GlobalObjectId APIs so N objects = 1 round-trip,
    /// not N round-trips.  Also pre-builds a Canvas-root set to avoid per-object
    /// parent-chain walks.
    /// </summary>
    public static class UIPresetCapture
    {
        // ─────────────────────────────────────────────────────────────────────
        //  EDITOR-MODE GUARD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns true when it is safe to run capture/apply operations.</summary>
        public static bool IsEditorSafe()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[UIPresetManager] Operations are disabled during Play Mode.");
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CAPTURE  (batched — single GlobalObjectId round-trip)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Walks every Canvas in the active scene and records the activeSelf state
        /// of every Canvas + all of its descendants.
        /// Uses the batch <c>GlobalObjectId.GetGlobalObjectIdsSlow</c> API for speed.
        /// </summary>
        public static UIPreset CaptureCurrentScene(string presetName)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            var preset = new UIPreset
            {
                name      = presetName,
                scenePath = activeScene.path,
                sceneName = activeScene.name,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entries   = new List<UIObjectState>()
            };

            // ── 1. Collect only UI objects (Canvas descendants) ──────────────
            var uiObjects = CollectUIObjects(activeScene);
            if (uiObjects.Count == 0) return preset;

            // ── 2. Build hierarchy paths (cheap — no asset DB calls) ─────────
            var paths = new string[uiObjects.Count];
            for (int i = 0; i < uiObjects.Count; i++)
                paths[i] = GetHierarchyPath(uiObjects[i]);

            // ── 3. Batch-fetch ALL GlobalObjectIds in one call ───────────────
            var goids = new GlobalObjectId[uiObjects.Count];
            GlobalObjectId.GetGlobalObjectIdsSlow(uiObjects.ToArray(), goids);

            // ── 4. Build entries ─────────────────────────────────────────────
            for (int i = 0; i < uiObjects.Count; i++)
            {
                preset.entries.Add(new UIObjectState
                {
                    guid          = goids[i].ToString(),
                    hierarchyPath = paths[i],
                    isActive      = uiObjects[i].activeSelf
                });
            }

            return preset;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  APPLY  (batched — single GlobalObjectId round-trip)
        // ─────────────────────────────────────────────────────────────────────

        public class ApplyResult
        {
            public int Applied;
            public int Skipped;   // objects not found (deleted or moved)
            public int Unchanged; // already at the desired state
        }

        /// <summary>
        /// Applies a preset to the current scene.
        /// Records an Undo group so the entire apply is reverted with one Ctrl+Z.
        /// Returns an <see cref="ApplyResult"/> summarising what happened.
        /// </summary>
        public static ApplyResult ApplyPreset(UIPreset preset)
        {
            var result = new ApplyResult();

            // Register one undo group for the whole operation
            Undo.SetCurrentGroupName($"Apply UI Preset '{preset.name}'");
            int undoGroup = Undo.GetCurrentGroup();

            Scene activeScene = SceneManager.GetActiveScene();

            // ── 1. Batch-parse all GUIDs ─────────────────────────────────────
            var entries    = preset.entries;
            var gids       = new GlobalObjectId[entries.Count];
            var validGid   = new bool[entries.Count];

            for (int i = 0; i < entries.Count; i++)
                validGid[i] = !string.IsNullOrEmpty(entries[i].guid) &&
                              GlobalObjectId.TryParse(entries[i].guid, out gids[i]);

            // ── 2. Path lookup (fallback) ─────────────────────────────────────
            var pathLookup = BuildPathLookup(activeScene);

            // ── 3. Apply states ───────────────────────────────────────────────
            for (int i = 0; i < entries.Count; i++)
            {
                UIObjectState entry = entries[i];
                GameObject go = null;

                // Strategy 1: GlobalObjectId singular resolve (batch version unavailable in this Unity build)
                if (validGid[i])
                {
                    try
                    {
                        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gids[i]);
                        if (obj is GameObject resolved && resolved != null)
                            go = resolved;
                    }
                    catch { /* ignore, fall through to path lookup */ }
                }

                // Strategy 2: hierarchy path fallback
                if (go == null && pathLookup.TryGetValue(entry.hierarchyPath, out GameObject found))
                    go = found;

                if (go == null)
                {
                    result.Skipped++;
                    continue;
                }

                if (go.activeSelf == entry.isActive)
                {
                    result.Unchanged++;
                    continue;
                }

                Undo.RecordObject(go, $"UI Preset: {entry.hierarchyPath}");
                go.SetActive(entry.isActive);
                result.Applied++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SCENE TRAVERSAL HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all GameObjects that are under a Canvas root (including the Canvas itself),
        /// without walking parent chains per-object.  Uses a Canvas root set for O(1) checks.
        /// </summary>
        private static List<GameObject> CollectUIObjects(Scene scene)
        {
            var result = new List<GameObject>(256);

            // Find all Canvas roots first — GetComponentsInChildren is fast (native)
            foreach (var root in scene.GetRootGameObjects())
            {
                // GetComponentsInChildren includes inactive objects when true
                var canvases = root.GetComponentsInChildren<Canvas>(includeInactive: true);
                foreach (var canvas in canvases)
                {
                    // Collect the canvas GO and all its children
                    CollectRecursive(canvas.gameObject, result);
                }
            }

            // Deduplicate (a nested canvas would be collected by its parent canvas too)
            var seen    = new HashSet<int>(result.Count);
            var deduped = new List<GameObject>(result.Count);
            foreach (var go in result)
            {
                if (seen.Add(go.GetInstanceID()))
                    deduped.Add(go);
            }
            return deduped;
        }

        private static void CollectRecursive(GameObject go, List<GameObject> result)
        {
            result.Add(go);
            foreach (Transform child in go.transform)
                CollectRecursive(child.gameObject, result);
        }

        /// <summary>Builds a hierarchy-path → GameObject map for all scene objects.</summary>
        private static Dictionary<string, GameObject> BuildPathLookup(Scene scene)
        {
            var lookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var roots  = scene.GetRootGameObjects();

            foreach (var root in roots)
                BuildPathLookupRecursive(root, root.name, lookup);

            return lookup;
        }

        private static void BuildPathLookupRecursive(GameObject go, string path, Dictionary<string, GameObject> lookup)
        {
            if (!lookup.ContainsKey(path))
                lookup[path] = go;

            foreach (Transform child in go.transform)
                BuildPathLookupRecursive(child.gameObject, path + "/" + child.name, lookup);
        }

        /// <summary>
        /// Returns the full hierarchy path of a GameObject, e.g. "Canvas/MainPanel/VassalInspector".
        /// Iterative (no recursion) to avoid stack overflow on deep hierarchies.
        /// </summary>
        public static string GetHierarchyPath(GameObject go)
        {
            var parts = new System.Text.StringBuilder(64);
            Transform t = go.transform;

            while (t != null)
            {
                if (parts.Length > 0) parts.Insert(0, '/');
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return parts.ToString();
        }
    }
}
