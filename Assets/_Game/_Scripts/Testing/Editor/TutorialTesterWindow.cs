using UnityEditor;
using UnityEngine;
using MaouSamaTD.Managers;
using MaouSamaTD.Testing;
using System.Collections.Generic;

namespace MaouSamaTD.Editor.Testing
{
    public class TutorialTesterWindow : EditorWindow
    {
        private List<string> _logs = new List<string>();
        private Vector2 _scrollPos;
        private bool _isAutoPlaying;

        [MenuItem("Maou-TD/Tutorial Tester")]
        public static void ShowWindow()
        {
            GetWindow<TutorialTesterWindow>("Tutorial Tester");
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            EditorApplication.update -= Repaint;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (logString.StartsWith("[AutoPlayer]") || logString.StartsWith("[tutorial]"))
            {
                _logs.Add($"[{System.DateTime.Now:HH:mm:ss}] {logString}");
                if (_logs.Count > 50) _logs.RemoveAt(0);
                _scrollPos.y = float.MaxValue;
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Tutorial Tester Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the tester.", MessageType.Info);
                if (GUILayout.Button("Play Game"))
                {
                    EditorApplication.isPlaying = true;
                }
                return;
            }

            var autoPlayer = GameObject.FindFirstObjectByType<TutorialAutoPlayer>();
            var tutorialManager = GameObject.FindFirstObjectByType<TutorialManager>();

            if (autoPlayer == null)
            {
                EditorGUILayout.HelpBox("TutorialAutoPlayer not found in scene.", MessageType.Warning);
                if (GUILayout.Button("Setup AutoPlayer"))
                {
                    var go = new GameObject("TutorialAutoPlayer");
                    go.AddComponent<TutorialAutoPlayer>();
                }
                return;
            }

            // Status Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            if (tutorialManager != null && tutorialManager.IsInTutorial)
            {
                EditorGUILayout.LabelField($"Step Index: {tutorialManager.GetCurrentStepIndex()}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Step Name: {tutorialManager.GetCurrentStepName()}", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField($"Action Key: {tutorialManager.GetCurrentStepActionKey()}", EditorStyles.miniLabel);
                
                if (GUILayout.Button("Verify Targets"))
                {
                    CheckTutorialTargets(tutorialManager);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No active tutorial.");
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            
            // Auto Player Controls
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto Player", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (autoPlayer == null)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("SPAWN AUTO-PLAYER", GUILayout.Height(30)))
                {
                    var go = new GameObject("TutorialAutoPlayer");
                    go.AddComponent<TutorialAutoPlayer>();
                }
                GUI.backgroundColor = Color.white;
            }
            else if (!autoPlayer.IsAutoPlaying)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("START AUTO-PLAY", GUILayout.Height(30)))
                {
                    autoPlayer.IsAutoPlaying = true;
                    autoPlayer.StartAutoPlay();
                }
            }
            else
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("STOP AUTO-PLAY", GUILayout.Height(30)))
                {
                    autoPlayer.IsAutoPlaying = false;
                    autoPlayer.StopAllCoroutines();
                }
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Force Skip", GUILayout.Height(30)))
            {
                tutorialManager?.OnActionTriggered(tutorialManager.GetCurrentStepActionKey());
            }
            EditorGUILayout.EndHorizontal();
            
            // Status Only for Time Scale
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(80));
            EditorGUILayout.LabelField(Time.timeScale.ToString("F2"), EditorStyles.boldLabel);
            if (GUILayout.Button("Resume (TS=1)", GUILayout.Width(100))) Time.timeScale = 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Logs Section
            EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.Height(300));
            foreach (var log in _logs)
            {
                EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Logs"))
            {
                _logs.Clear();
            }
        }

        private void CheckTutorialTargets(TutorialManager manager)
        {
            var step = manager.GetCurrentStep();
            if (step == null) return;

            Debug.Log($"[Tester] Checking targets for step: {step.StepName}");
            if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
            {
                var go = GameObject.Find(step.TargetUI.Name);
                Debug.Log($"[Tester] Main Target '{step.TargetUI.Name}': {(go != null ? "FOUND" : "NOT FOUND")}");
            }

            if (step.AdditionalTargetUI != null)
            {
                foreach (var t in step.AdditionalTargetUI)
                {
                    var go = GameObject.Find(t.Name);
                    Debug.Log($"[Tester] Add'l Target '{t.Name}': {(go != null ? "FOUND" : "NOT FOUND")}");
                }
            }
        }
    }
}
