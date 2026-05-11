using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MaouSamaTD.Units;
using MaouSamaTD.Editor;


namespace MaouSamaTD.EditorTools
{
    public class BalancingEditorWindow : EditorWindow
    {
        private enum Tab { Vassals, Enemies, Analytics }
        private Tab _currentTab = Tab.Vassals;

        // Paths
        private const string CSV_RELATIVE_PATH = "Assets/_Game/docs~/Math_and_Balance/Balancing_PowerGrid.csv";
        private const string VASSAL_DATA_ROOT = "Assets/_Game/Data/Units/Vassals";
        private const string ENEMY_DATA_ROOT = "Assets/_Game/Data/Units/Enemies";

        // Cached Data Lists
        private List<UnitData> _vassalAssets = new List<UnitData>();
        private List<EnemyData> _enemyAssets = new List<EnemyData>();
        private List<Dictionary<string, string>> _csvRows = new List<Dictionary<string, string>>();
        private HashSet<string> _csvHeaders = new HashSet<string>();

        // Sorting State
        private string _vassalSortColumn = "Name";
        private bool _vassalSortAscending = true;
        private string _enemySortColumn = "Name";
        private bool _enemySortAscending = true;

        // Custom Settings
        private bool _showAvatars = true;

        // Analytics Graph settings
        private bool _showGraphSideList = true;
        private bool _showVassalsOnGraph = true;
        private bool _showEnemiesOnGraph = true;
        
        public enum GraphLayout { Single, GridByClass }
        private GraphLayout _graphLayout = GraphLayout.Single;
        
        public enum MetricY { Power, HP, Attack }
        private MetricY _metricY = MetricY.Power;

        public enum MetricX { CostReward, Speed, Range }
        private MetricX _metricX = MetricX.CostReward;

        private Dictionary<string, bool> _graphUnitVisibility = new Dictionary<string, bool>();
        private Vector2 _graphFilterScroll;

        // Scroll States
        private Vector2 _vassalScroll;
        private Vector2 _enemyScroll;
        private Vector2 _analyticsScroll;

        // Selection / Edit States
        private UnitData _selectedVassal;
        private EnemyData _selectedEnemy;
        private string _searchText = "";

        // Stylings
        private GUIStyle _mismatchStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;

        [MenuItem("Maou-TD/Stat Balancing Studio", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<BalancingEditorWindow>("Stat Balancing Studio");
            window.minSize = new Vector2(950, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadAllData();
        }

        private void LoadAllData()
        {
            // 1. Load Vassals
            _vassalAssets.Clear();
            string[] vassalGuids = AssetDatabase.FindAssets("t:UnitData", new[] { VASSAL_DATA_ROOT });
            foreach (var guid in vassalGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitData asset = AssetDatabase.LoadAssetAtPath<UnitData>(path);
                if (asset != null) _vassalAssets.Add(asset);
            }
            SortVassals();

            // 2. Load Enemies
            _enemyAssets.Clear();
            string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyData", new[] { ENEMY_DATA_ROOT });
            foreach (var guid in enemyGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (asset != null) _enemyAssets.Add(asset);
            }
            SortEnemies();
            
            InitializeGraphVisibility();

            // 3. Load CSV
            _csvRows.Clear();
            _csvHeaders.Clear();
            string fullCsvPath = Path.Combine(Directory.GetCurrentDirectory(), CSV_RELATIVE_PATH);
            if (File.Exists(fullCsvPath))
            {
                try
                {
                    using (var reader = new StreamReader(fullCsvPath))
                    {
                        string headerLine = reader.ReadLine();
                        if (headerLine != null)
                        {
                            string[] headers = ParseCsvLine(headerLine);
                            foreach (var h in headers) _csvHeaders.Add(h);

                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (string.IsNullOrEmpty(line)) continue;
                                string[] values = ParseCsvLine(line);
                                var row = new Dictionary<string, string>();
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    if (i < values.Length)
                                        row[headers[i]] = values[i];
                                    else
                                        row[headers[i]] = "";
                                }
                                _csvRows.Add(row);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Balancing Window] Failed to read CSV: {ex.Message}");
                }
            }
        }

        private string[] ParseCsvLine(string line)
        {
            // Simple split, handles potential quotes
            List<string> parts = new List<string>();
            bool inQuotes = false;
            System.Text.StringBuilder current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            parts.Add(current.ToString().Trim());
            return parts.ToArray();
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Window Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(35));
            GUILayout.Label("⚔️ MAOU-SAMA TD BALANCING STUDIO ⚔️", _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 Reload All Assets", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                LoadAllData();
            }
            EditorGUILayout.EndHorizontal();

            // Tab Buttons
            EditorGUILayout.BeginHorizontal();
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = _currentTab == Tab.Vassals ? new Color(0.4f, 0.7f, 1f, 1f) : defaultColor;
            if (GUILayout.Button("🛡️ Vassals Spreadsheet", _buttonStyle, GUILayout.Height(30))) _currentTab = Tab.Vassals;

            GUI.backgroundColor = _currentTab == Tab.Enemies ? new Color(1f, 0.4f, 0.4f, 1f) : defaultColor;
            if (GUILayout.Button("👹 Enemies Balancing", _buttonStyle, GUILayout.Height(30))) _currentTab = Tab.Enemies;

            GUI.backgroundColor = _currentTab == Tab.Analytics ? new Color(0.4f, 1f, 0.6f, 1f) : defaultColor;
            if (GUILayout.Button("📊 Balancing Analytics", _buttonStyle, GUILayout.Height(30))) _currentTab = Tab.Analytics;
            GUI.backgroundColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            // Search Bar & Options
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔍 Filter Name / Class:", GUILayout.Width(150));
            _searchText = EditorGUILayout.TextField(_searchText);
            if (!string.IsNullOrEmpty(_searchText) && GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _searchText = "";
            }
            GUILayout.Space(25);
            _showAvatars = GUILayout.Toggle(_showAvatars, _showAvatars ? "🖼️ Avatars On" : "🖼️ Avatars Off", "Button", GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            // Body Area
            switch (_currentTab)
            {
                case Tab.Vassals:
                    DrawVassalsGrid();
                    break;
                case Tab.Enemies:
                    DrawEnemiesGrid();
                    break;
                case Tab.Analytics:
                    DrawAnalyticsTab();
                    break;
            }
        }

        private void InitializeStyles()
        {
            if (_mismatchStyle == null)
            {
                _mismatchStyle = new GUIStyle(EditorStyles.label);
                _mismatchStyle.normal.textColor = new Color(1f, 0.65f, 0f, 1f); // Vibrant orange
                _mismatchStyle.fontStyle = FontStyle.Bold;

                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.alignment = TextAnchor.MiddleCenter;

                _titleStyle = new GUIStyle(EditorStyles.boldLabel);
                _titleStyle.fontSize = 12;
                _titleStyle.alignment = TextAnchor.MiddleLeft;

                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.fontStyle = FontStyle.Bold;
                _buttonStyle.fontSize = 11;
            }
        }

        private void DrawVassalsGrid()
        {
            // Filter first to get counts
            List<UnitData> filteredVassals = new List<UnitData>();
            foreach (var vassal in _vassalAssets)
            {
                if (!string.IsNullOrEmpty(_searchText))
                {
                    if (!vassal.UnitName.ToLower().Contains(_searchText.ToLower()) &&
                        !vassal.Class.ToString().ToLower().Contains(_searchText.ToLower()))
                        continue;
                }
                filteredVassals.Add(vassal);
            }

            EditorGUILayout.BeginHorizontal();

            // Left Side: Large Grid Scroll View
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 250));
            
            // Total entry count display
            EditorGUILayout.LabelField($"📊 Showing {filteredVassals.Count} of {_vassalAssets.Count} Vassals", EditorStyles.boldLabel);

            _vassalScroll = EditorGUILayout.BeginScrollView(_vassalScroll, EditorStyles.helpBox);

            // Table Header Row
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("#", GUILayout.Width(25));
            if (_showAvatars)
            {
                GUILayout.Label("Img", EditorStyles.miniBoldLabel, GUILayout.Width(30));
                DrawHeaderButton("Name", "Name", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 100);
            }
            else
            {
                DrawHeaderButton("Name", "Name", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 130);
            }
            DrawHeaderButton("Class", "Class", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 90);
            DrawHeaderButton("Rarity", "Rarity", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 60);
            DrawHeaderButton("Cost", "Cost", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 40);
            DrawHeaderButton("Max HP", "HP", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 50);
            DrawHeaderButton("Attack", "ATK", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 50);
            DrawHeaderButton("Defense", "DEF", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 50);
            DrawHeaderButton("Range", "Range", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 45);
            DrawHeaderButton("Block", "Block", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 45);
            DrawHeaderButton("Interval", "Interval", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 50);
            DrawHeaderButton("Redeploy", "Redeploy", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 60);
            DrawHeaderButton("Flying", "Flying", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 45);
            DrawHeaderButton("Damage", "DMG Type", ref _vassalSortColumn, ref _vassalSortAscending, SortVassals, 65);
            GUILayout.Label("Sync", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < filteredVassals.Count; i++)
            {
                var vassal = filteredVassals[i];

                // Cross-reference CSV
                var csvRow = _csvRows.Find(r => r.ContainsKey("Name") && r["Name"] == vassal.UnitName);
                bool hasMismatch = false;
                if (csvRow != null)
                {
                    if (float.TryParse(csvRow.GetValueOrDefault("Final HP", "0"), out float csvHp) && Mathf.RoundToInt(csvHp) != Mathf.RoundToInt(vassal.MaxHp)) hasMismatch = true;
                    if (float.TryParse(csvRow.GetValueOrDefault("Final ATK", "0"), out float csvAtk) && Mathf.RoundToInt(csvAtk) != Mathf.RoundToInt(vassal.AttackPower)) hasMismatch = true;
                    if (float.TryParse(csvRow.GetValueOrDefault("Final DEF", "0"), out float csvDef) && Mathf.RoundToInt(csvDef) != Mathf.RoundToInt(vassal.Defense)) hasMismatch = true;
                }

                Color rowBg = _selectedVassal == vassal ? new Color(0.2f, 0.4f, 0.6f, 0.4f) : Color.clear;
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(rowRect, rowBg);

                if (hasMismatch)
                {
                    // Draw a soft orange background outline for mistyped rows
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3, rowRect.height), new Color(1f, 0.5f, 0f, 0.8f));
                }

                // Click selection
                if (GUI.Button(new Rect(rowRect.x, rowRect.y, rowRect.width - 100, rowRect.height), "", GUIStyle.none))
                {
                    _selectedVassal = vassal;
                    GUI.FocusControl(null);
                }

                // Inline Displays
                GUILayout.Label($"{i + 1}.", EditorStyles.label, GUILayout.Width(25));
                
                if (_showAvatars)
                {
                    Sprite avatar = vassal.BaseSkin.Avatar;
                    if (avatar == null && vassal.Skins != null && vassal.Skins.Count > 0) avatar = vassal.Skins[0].Avatar;
                    
                    Rect avatarRect = GUILayoutUtility.GetRect(30, 20, GUILayout.Width(30), GUILayout.Height(20));
                    Rect centeredRect = new Rect(avatarRect.x + 5, avatarRect.y, 20, 20);
                    if (avatar != null && avatar.texture != null)
                    {
                        GUI.DrawTexture(centeredRect, avatar.texture, ScaleMode.ScaleToFit);
                        GUI.Label(centeredRect, new GUIContent("", vassal.UnitName));
                    }
                    else
                    {
                        GUI.Box(centeredRect, new GUIContent("?", vassal.UnitName));
                    }
                    GUILayout.Label(vassal.UnitName, hasMismatch ? _mismatchStyle : EditorStyles.label, GUILayout.Width(100));
                }
                else
                {
                    GUILayout.Label(vassal.UnitName, hasMismatch ? _mismatchStyle : EditorStyles.label, GUILayout.Width(130));
                }

                GUILayout.Label(vassal.Class.ToString(), GUILayout.Width(90));
                GUILayout.Label(vassal.Rarity.GetShortName(), GUILayout.Width(60));

                // Edit stat properties inline
                EditorGUI.BeginChangeCheck();
                int cost = EditorGUILayout.IntField(vassal.DeploymentCost, GUILayout.Width(40));
                float hp = EditorGUILayout.FloatField(vassal.MaxHp, GUILayout.Width(50));
                float atk = EditorGUILayout.FloatField(vassal.AttackPower, GUILayout.Width(50));
                float def = EditorGUILayout.FloatField(vassal.Defense, GUILayout.Width(50));
                float range = EditorGUILayout.FloatField(vassal.Range, GUILayout.Width(45));
                int block = EditorGUILayout.IntField(vassal.BlockCount, GUILayout.Width(45));
                float interval = EditorGUILayout.FloatField(vassal.AttackInterval, GUILayout.Width(50));
                float redeploy = EditorGUILayout.FloatField(vassal.RespawnTime, GUILayout.Width(60));
                bool flying = EditorGUILayout.Toggle(vassal.CanAttackFlying, GUILayout.Width(45));
                DamageType dmgType = (DamageType)EditorGUILayout.EnumPopup(vassal.DamageType, GUILayout.Width(65));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vassal, "Inline Stat Modification");
                    vassal.DeploymentCost = cost;
                    vassal.MaxHp = hp;
                    vassal.AttackPower = atk;
                    vassal.Defense = def;
                    vassal.Range = range;
                    vassal.BlockCount = block;
                    vassal.AttackInterval = interval;
                    vassal.RespawnTime = redeploy;
                    vassal.CanAttackFlying = flying;
                    vassal.DamageType = dmgType;
                    EditorUtility.SetDirty(vassal);
                }

                // Sync Row Buttons
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                if (GUILayout.Button("⬇️ Live", EditorStyles.miniButtonLeft, GUILayout.Width(48)))
                {
                    SyncVassalToCsv(vassal, csvRow);
                }
                if (GUILayout.Button("⬆️ CSV", EditorStyles.miniButtonRight, GUILayout.Width(48)))
                {
                    SyncCsvToVassal(vassal, csvRow);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right Side: Sidebar Editor for selected Vassal
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(240));
            if (_selectedVassal != null)
            {
                EditorGUILayout.LabelField($"🛡️ Selected: {_selectedVassal.UnitName}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // Detailed edit properties
                EditorGUI.BeginChangeCheck();
                _selectedVassal.UnitTitle = EditorGUILayout.TextField("Title", _selectedVassal.UnitTitle);
                _selectedVassal.Rarity = (UnitRarity)EditorGUILayout.EnumPopup("Rarity", _selectedVassal.Rarity);
                _selectedVassal.Class = (UnitClass)EditorGUILayout.EnumPopup("Class", _selectedVassal.Class);
                _selectedVassal.AttackType = (AttackType)EditorGUILayout.EnumPopup("Attack Type", _selectedVassal.AttackType);
                _selectedVassal.AttackPattern = (AttackPattern)EditorGUILayout.EnumPopup("Attack Pattern", _selectedVassal.AttackPattern);
                _selectedVassal.RespawnTime = EditorGUILayout.FloatField("Respawn Time", _selectedVassal.RespawnTime);

                EditorGUILayout.Space();
                float totalPower = _selectedVassal.MaxHp + _selectedVassal.AttackPower * 5 + _selectedVassal.Defense * 10 + _selectedVassal.Range * 50;
                EditorGUILayout.LabelField($"Calculated Power: {totalPower:F0}", EditorStyles.boldLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_selectedVassal);
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("🌐 Open in Unit Browser", GUILayout.Height(30)))
                {
                    MaouUnitBrowser.OpenAndSelect(_selectedVassal);
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Ping Asset File", GUILayout.Height(25)))
                {
                    EditorGUIUtility.PingObject(_selectedVassal);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a vassal row to view detailed attributes & metadata.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Bulk actions footer
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (GUILayout.Button("🔄 Sync ALL (Live -> CSV Spreadsheet)", GUILayout.Height(25)))
            {
                SyncAllVassalsToCsv();
            }
            if (GUILayout.Button("📥 Sync ALL (CSV Spreadsheet -> Live Assets)", GUILayout.Height(25)))
            {
                SyncAllCsvToVassals();
            }
            if (GUILayout.Button("💾 Write CSV Out to File", GUILayout.Height(25)))
            {
                SaveCsvFile();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEnemiesGrid()
        {
            // Filter first to get counts
            List<EnemyData> filteredEnemies = new List<EnemyData>();
            foreach (var enemy in _enemyAssets)
            {
                if (!string.IsNullOrEmpty(_searchText))
                {
                    if (!enemy.EnemyName.ToLower().Contains(_searchText.ToLower()) &&
                        !enemy.DamageType.ToString().ToLower().Contains(_searchText.ToLower()))
                        continue;
                }
                filteredEnemies.Add(enemy);
            }

            EditorGUILayout.BeginHorizontal();

            // Left Side: Scroll View
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 250));
            
            // Total entry count display
            EditorGUILayout.LabelField($"📊 Showing {filteredEnemies.Count} of {_enemyAssets.Count} Enemies", EditorStyles.boldLabel);

            _enemyScroll = EditorGUILayout.BeginScrollView(_enemyScroll, EditorStyles.helpBox);

            // Header Row
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("#", GUILayout.Width(25));
            if (_showAvatars)
            {
                GUILayout.Label("Img", EditorStyles.miniBoldLabel, GUILayout.Width(30));
                DrawHeaderButton("Name", "Name", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 100);
            }
            else
            {
                DrawHeaderButton("Name", "Name", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 130);
            }
            DrawHeaderButton("Hp", "MaxHP", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 50);
            DrawHeaderButton("Speed", "Speed", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 50);
            DrawHeaderButton("ATK", "Attack", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 50);
            DrawHeaderButton("Interval", "Interval", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 55);
            DrawHeaderButton("Range", "Range", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 50);
            DrawHeaderButton("Exit Dmg", "ExitDmg", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 55);
            DrawHeaderButton("Flying", "Flying", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 50);
            DrawHeaderButton("Priority", "Priority", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 90);
            DrawHeaderButton("Dmg Type", "Damage", ref _enemySortColumn, ref _enemySortAscending, SortEnemies, 70);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < filteredEnemies.Count; i++)
            {
                var enemy = filteredEnemies[i];

                Color rowBg = _selectedEnemy == enemy ? new Color(0.6f, 0.2f, 0.2f, 0.4f) : Color.clear;
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(rowRect, rowBg);

                // Click selection
                if (GUI.Button(rowRect, "", GUIStyle.none))
                {
                    _selectedEnemy = enemy;
                    GUI.FocusControl(null);
                }

                GUILayout.Label($"{i + 1}.", EditorStyles.label, GUILayout.Width(25));

                if (_showAvatars)
                {
                    Sprite avatar = enemy.EnemySprite;
                    Rect avatarRect = GUILayoutUtility.GetRect(30, 20, GUILayout.Width(30), GUILayout.Height(20));
                    Rect centeredRect = new Rect(avatarRect.x + 5, avatarRect.y, 20, 20);
                    if (avatar != null && avatar.texture != null)
                    {
                        GUI.DrawTexture(centeredRect, avatar.texture, ScaleMode.ScaleToFit);
                        GUI.Label(centeredRect, new GUIContent("", enemy.EnemyName));
                    }
                    else
                    {
                        GUI.Box(centeredRect, new GUIContent("?", enemy.EnemyName));
                    }
                    GUILayout.Label(enemy.EnemyName, EditorStyles.label, GUILayout.Width(100));
                }
                else
                {
                    GUILayout.Label(enemy.EnemyName, EditorStyles.label, GUILayout.Width(130));
                }

                EditorGUI.BeginChangeCheck();
                float hp = EditorGUILayout.FloatField(enemy.MaxHp, GUILayout.Width(50));
                float speed = EditorGUILayout.FloatField(enemy.MoveSpeed, GUILayout.Width(50));
                float atk = EditorGUILayout.FloatField(enemy.AttackPower, GUILayout.Width(50));
                float interval = EditorGUILayout.FloatField(enemy.AttackInterval, GUILayout.Width(55));
                float range = EditorGUILayout.FloatField(enemy.AttackRange, GUILayout.Width(50));
                float exitDmg = EditorGUILayout.FloatField(enemy.ExitDamage, GUILayout.Width(55));
                bool isFlying = enemy.MovementType == EnemyMovementType.Flying;
                bool toggleFlying = EditorGUILayout.Toggle(isFlying, GUILayout.Width(50));
                EnemyTargetingPriority priority = (EnemyTargetingPriority)EditorGUILayout.EnumPopup(enemy.TargetingPriority, GUILayout.Width(90));
                DamageType dmgType = (DamageType)EditorGUILayout.EnumPopup(enemy.DamageType, GUILayout.Width(70));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(enemy, "Enemy Stat Modification");
                    enemy.MaxHp = hp;
                    enemy.MoveSpeed = speed;
                    enemy.AttackPower = atk;
                    enemy.AttackInterval = interval;
                    enemy.AttackRange = range;
                    enemy.ExitDamage = exitDmg;
                    enemy.MovementType = toggleFlying ? EnemyMovementType.Flying : EnemyMovementType.Ground;
                    enemy.TargetingPriority = priority;
                    enemy.DamageType = dmgType;
                    EditorUtility.SetDirty(enemy);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right Side Sidebar
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(240));
            if (_selectedEnemy != null)
            {
                EditorGUILayout.LabelField($"👹 Selected: {_selectedEnemy.EnemyName}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                _selectedEnemy.IsBoss = EditorGUILayout.Toggle("Is Boss Unit", _selectedEnemy.IsBoss);
                _selectedEnemy.EvasionType = (EnemyEvasionType)EditorGUILayout.EnumPopup("Evasion Type", _selectedEnemy.EvasionType);
                _selectedEnemy.PhasingCharges = EditorGUILayout.IntField("Phasing Charges", _selectedEnemy.PhasingCharges);
                _selectedEnemy.CurrencyReward = EditorGUILayout.IntField("Reward", _selectedEnemy.CurrencyReward);
                _selectedEnemy.HpBarYOffset = EditorGUILayout.FloatField("HP Bar Offset Y", _selectedEnemy.HpBarYOffset);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_selectedEnemy);
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("🌐 Open in Enemy Browser", GUILayout.Height(30)))
                {
                    MaouEnemyBrowser.OpenAndSelect(_selectedEnemy);
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Ping Enemy Asset File", GUILayout.Height(25)))
                {
                    EditorGUIUtility.PingObject(_selectedEnemy);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select an enemy row to view full attributes.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTriangle(Vector2 center, float size, Color color)
        {
            Vector3[] points = new Vector3[]
            {
                new Vector3(center.x, center.y - size, 0),
                new Vector3(center.x - size, center.y + size, 0),
                new Vector3(center.x + size, center.y + size, 0)
            };
            Handles.color = color;
            Handles.DrawAAConvexPolygon(points);
        }

        private void DrawBossIndicator(Vector2 center, float size, Color color)
        {
            // Draw a background golden star/diamond
            Vector3[] diamondPoints = new Vector3[]
            {
                new Vector3(center.x, center.y - size * 1.5f, 0),
                new Vector3(center.x - size * 1.5f, center.y, 0),
                new Vector3(center.x, center.y + size * 1.5f, 0),
                new Vector3(center.x + size * 1.5f, center.y, 0)
            };
            Handles.color = new Color(1f, 0.85f, 0f, 1f); // Rich gold
            Handles.DrawAAConvexPolygon(diamondPoints);
            
            // Draw the main triangle inside
            DrawTriangle(center, size, color);
        }

        private float CalculateMaxMetricX(List<UnitData> vassals, List<EnemyData> enemies)
        {
            float maxVal = 10f; // baseline
            if (_metricX == MetricX.CostReward)
            {
                foreach (var v in vassals) maxVal = Mathf.Max(maxVal, v.DeploymentCost);
                foreach (var e in enemies) maxVal = Mathf.Max(maxVal, e.CurrencyReward);
                return Mathf.Ceil(maxVal / 10f) * 10f; // round up to nearest 10
            }
            else if (_metricX == MetricX.Speed)
            {
                foreach (var v in vassals) maxVal = Mathf.Max(maxVal, v.AttackInterval);
                foreach (var e in enemies) maxVal = Mathf.Max(maxVal, e.MoveSpeed);
                return Mathf.Ceil(maxVal / 1f) * 1f; // round up to nearest 1
            }
            else // Range
            {
                foreach (var v in vassals) maxVal = Mathf.Max(maxVal, v.Range);
                foreach (var e in enemies) maxVal = Mathf.Max(maxVal, e.AttackRange);
                return Mathf.Ceil(maxVal / 2f) * 2f; // round up to nearest 2
            }
        }

        private float CalculateMaxMetricY(List<UnitData> vassals, List<EnemyData> enemies)
        {
            float maxVal = 100f; // baseline
            if (_metricY == MetricY.Power)
            {
                foreach (var v in vassals)
                {
                    float power = v.MaxHp + v.AttackPower * 5 + v.Defense * 10 + v.Range * 50;
                    maxVal = Mathf.Max(maxVal, power);
                }
                foreach (var e in enemies)
                {
                    float power = e.MaxHp + e.AttackPower * 5 + e.AttackRange * 50 + e.MoveSpeed * 100;
                    maxVal = Mathf.Max(maxVal, power);
                }
                return Mathf.Ceil(maxVal / 500f) * 500f; // round up to nearest 500
            }
            else if (_metricY == MetricY.HP)
            {
                foreach (var v in vassals) maxVal = Mathf.Max(maxVal, v.MaxHp);
                foreach (var e in enemies) maxVal = Mathf.Max(maxVal, e.MaxHp);
                return Mathf.Ceil(maxVal / 100f) * 100f; // round up to nearest 100
            }
            else // Attack
            {
                foreach (var v in vassals) maxVal = Mathf.Max(maxVal, v.AttackPower);
                foreach (var e in enemies) maxVal = Mathf.Max(maxVal, e.AttackPower);
                return Mathf.Ceil(maxVal / 20f) * 20f; // round up to nearest 20
            }
        }

        private bool IsEnemyInClassQuadrant(EnemyData enemy, UnitClass targetClass)
        {
            switch (targetClass)
            {
                case UnitClass.Bastion:
                    return enemy.IsBoss || enemy.MaxHp >= 150f;
                case UnitClass.Vanguard:
                    return !enemy.IsBoss && enemy.MaxHp < 150f && enemy.MoveSpeed >= 1.5f && enemy.MovementType == EnemyMovementType.Ground;
                case UnitClass.Ranger:
                    return enemy.MovementType == EnemyMovementType.Flying || enemy.AttackRange > 1.5f;
                case UnitClass.Warlock:
                    return enemy.EvasionType != EnemyEvasionType.None || enemy.PhasingCharges > 0 || enemy.DamageType == DamageType.Magic;
                default:
                    return false;
            }
        }

        private void SetAllGraphVisibility(bool state)
        {
            List<string> keys = new List<string>(_graphUnitVisibility.Keys);
            foreach (var key in keys)
            {
                _graphUnitVisibility[key] = state;
            }
        }

        private void InitializeGraphVisibility()
        {
            foreach (var v in _vassalAssets)
            {
                string key = "V_" + v.UnitName;
                if (!_graphUnitVisibility.ContainsKey(key))
                    _graphUnitVisibility[key] = true;
            }
            foreach (var e in _enemyAssets)
            {
                string key = "E_" + e.EnemyName;
                if (!_graphUnitVisibility.ContainsKey(key))
                    _graphUnitVisibility[key] = true;
            }
        }

        private void DrawSingleGraph(Rect graphRect, string title, UnitClass? filterClass)
        {
            // Sleek plot background
            EditorGUI.DrawRect(graphRect, new Color(0.12f, 0.12f, 0.15f, 1.0f));

            // Grid outline & Title
            Handles.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            Handles.DrawLine(new Vector2(graphRect.x + 40, graphRect.y + 25), new Vector2(graphRect.x + 40, graphRect.y + graphRect.height - 30));
            Handles.DrawLine(new Vector2(graphRect.x + 40, graphRect.y + graphRect.height - 30), new Vector2(graphRect.x + graphRect.width - 20, graphRect.y + graphRect.height - 30));

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontSize = 11;
            GUI.Label(new Rect(graphRect.x + 45, graphRect.y + 5, graphRect.width - 50, 18), title, titleStyle);

            // Get dynamic bounds
            float maxX = CalculateMaxMetricX(_vassalAssets, _enemyAssets);
            float maxY = CalculateMaxMetricY(_vassalAssets, _enemyAssets);

            GUIStyle tickStyle = new GUIStyle(EditorStyles.miniLabel);
            tickStyle.normal.textColor = Color.gray;

            // X-Axis tick line drawing
            int xSteps = 5;
            for (int i = 0; i <= xSteps; i++)
            {
                float pct = i / (float)xSteps;
                float val = pct * maxX;
                float xPos = graphRect.x + 40 + pct * (graphRect.width - 70);
                Handles.color = new Color(0.3f, 0.3f, 0.35f, 0.3f);
                Handles.DrawLine(new Vector2(xPos, graphRect.y + 25), new Vector2(xPos, graphRect.y + graphRect.height - 30));
                
                Handles.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                Handles.DrawLine(new Vector2(xPos, graphRect.y + graphRect.height - 30), new Vector2(xPos, graphRect.y + graphRect.height - 25));
                
                string labelText = _metricX == MetricX.CostReward ? val.ToString("F0") : val.ToString("F1");
                GUI.Label(new Rect(xPos - 12, graphRect.y + graphRect.height - 23, 40, 16), labelText, tickStyle);
            }

            // Y-Axis tick line drawing
            int ySteps = 4;
            for (int i = 0; i <= ySteps; i++)
            {
                float pct = i / (float)ySteps;
                float val = pct * maxY;
                float yPos = graphRect.y + graphRect.height - 30 - pct * (graphRect.height - 55);
                Handles.color = new Color(0.3f, 0.3f, 0.35f, 0.3f);
                Handles.DrawLine(new Vector2(graphRect.x + 40, yPos), new Vector2(graphRect.x + graphRect.width - 20, yPos));
                
                Handles.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                Handles.DrawLine(new Vector2(graphRect.x + 35, yPos), new Vector2(graphRect.x + 40, yPos));
                
                string labelText = val.ToString("F0");
                GUI.Label(new Rect(graphRect.x + 5, yPos - 8, 32, 16), labelText, tickStyle);
            }

            // Draw axis labels
            string labelX = _metricX switch {
                MetricX.CostReward => "Cost / Reward (Gold)",
                MetricX.Speed => "Speed (Interval / MoveSpeed)",
                _ => "Attack Range"
            };
            string labelY = _metricY switch {
                MetricY.Power => "Calculated Power",
                MetricY.HP => "Base Max HP",
                _ => "Attack Power (ATK)"
            };

            GUI.Label(new Rect(graphRect.x + graphRect.width * 0.5f - 60, graphRect.y + graphRect.height - 11, 150, 12), labelX, tickStyle);

            // Draw Target/Balance line if Metric is Power vs CostReward
            if (_metricX == MetricX.CostReward && _metricY == MetricY.Power)
            {
                // Draw target balance reference line
                Handles.color = new Color(0.1f, 0.7f, 0.3f, 0.3f); // dash green balance
                float startX = graphRect.x + 40;
                float startY = graphRect.y + graphRect.height - 30;
                
                // Vassal baseline: Power = Cost * 40
                float endX_30 = graphRect.x + 40 + Mathf.Clamp01(30f / maxX) * (graphRect.width - 70);
                float endY_1200 = graphRect.y + graphRect.height - 30 - Mathf.Clamp01(1200f / maxY) * (graphRect.height - 55);
                Handles.DrawLine(new Vector2(startX, startY), new Vector2(endX_30, endY_1200));
            }

            // Draw Vassal plot points
            if (_showVassalsOnGraph)
            {
                foreach (var vassal in _vassalAssets)
                {
                    if (filterClass.HasValue && vassal.Class != filterClass.Value) continue;
                    if (!_graphUnitVisibility.GetValueOrDefault("V_" + vassal.UnitName, true)) continue;

                    float valY = _metricY switch {
                        MetricY.Power => vassal.MaxHp + vassal.AttackPower * 5 + vassal.Defense * 10 + vassal.Range * 50,
                        MetricY.HP => vassal.MaxHp,
                        _ => vassal.AttackPower
                    };
                    float valX = _metricX switch {
                        MetricX.CostReward => vassal.DeploymentCost,
                        MetricX.Speed => vassal.AttackInterval,
                        _ => vassal.Range
                    };

                    float xPct = Mathf.Clamp01(valX / maxX);
                    float yPct = Mathf.Clamp01(valY / maxY);

                    float xPos = graphRect.x + 40 + xPct * (graphRect.width - 70);
                    float yPos = graphRect.y + graphRect.height - 30 - yPct * (graphRect.height - 55);

                    Color dotColor = vassal.Class switch
                    {
                        UnitClass.Bastion => new Color(0.4f, 0.6f, 1.0f, 1f), // Blue
                        UnitClass.Vanguard => new Color(1.0f, 0.4f, 0.4f, 1f), // Red
                        UnitClass.Ranger => new Color(1.0f, 0.8f, 0.2f, 1f), // Gold
                        UnitClass.Warlock => new Color(0.8f, 0.4f, 1.0f, 1f), // Purple
                        _ => Color.cyan
                    };

                    // Draw solid disc
                    Handles.color = dotColor;
                    Handles.DrawSolidDisc(new Vector3(xPos, yPos, 0), Vector3.forward, 5f);
                    
                    // Draw clean white outline
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(new Vector3(xPos, yPos, 0), Vector3.forward, 5f);

                    // Check hover
                    Rect dotRect = new Rect(xPos - 5, yPos - 5, 10, 10);
                    if (dotRect.Contains(Event.current.mousePosition))
                    {
                        GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox);
                        tooltipStyle.normal.textColor = Color.white;
                        tooltipStyle.fontSize = 10;
                        string tipStr = $"{vassal.UnitName} ({labelY}:{valY:F0}, {labelX}:{valX:F1})";
                        Vector2 tipSize = tooltipStyle.CalcSize(new GUIContent(tipStr));
                        EditorGUI.DrawRect(new Rect(Event.current.mousePosition.x + 12, Event.current.mousePosition.y - 12, tipSize.x + 6, tipSize.y + 4), new Color(0, 0, 0, 0.9f));
                        GUI.Label(new Rect(Event.current.mousePosition.x + 15, Event.current.mousePosition.y - 10, tipSize.x, tipSize.y), tipStr, tooltipStyle);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            _selectedVassal = vassal;
                            _currentTab = Tab.Vassals;
                            Repaint();
                        }
                    }
                }
            }

            // Draw Enemy plot points
            if (_showEnemiesOnGraph)
            {
                foreach (var enemy in _enemyAssets)
                {
                    if (filterClass.HasValue && !IsEnemyInClassQuadrant(enemy, filterClass.Value)) continue;
                    if (!_graphUnitVisibility.GetValueOrDefault("E_" + enemy.EnemyName, true)) continue;

                    float valY = _metricY switch {
                        MetricY.Power => enemy.MaxHp + enemy.AttackPower * 5 + enemy.AttackRange * 50 + enemy.MoveSpeed * 100,
                        MetricY.HP => enemy.MaxHp,
                        _ => enemy.AttackPower
                    };
                    float valX = _metricX switch {
                        MetricX.CostReward => enemy.CurrencyReward,
                        MetricX.Speed => enemy.MoveSpeed,
                        _ => enemy.AttackRange
                    };

                    float xPct = Mathf.Clamp01(valX / maxX);
                    float yPct = Mathf.Clamp01(valY / maxY);

                    float xPos = graphRect.x + 40 + xPct * (graphRect.width - 70);
                    float yPos = graphRect.y + graphRect.height - 30 - yPct * (graphRect.height - 55);

                    Color triangleColor = enemy.DamageType switch {
                        DamageType.Magic => new Color(0.85f, 0.3f, 0.9f, 1f), // Glowing magic magenta
                        DamageType.Ranged => new Color(0.1f, 0.85f, 0.85f, 1f), // Ranged/piercing teal
                        _ => new Color(1f, 0.4f, 0.1f, 1f) // Physical / True crimson orange
                    };

                    if (enemy.IsBoss)
                    {
                        DrawBossIndicator(new Vector2(xPos, yPos), 6f, triangleColor);
                    }
                    else
                    {
                        DrawTriangle(new Vector2(xPos, yPos), 5f, triangleColor);
                    }

                    // Check hover
                    Rect dotRect = new Rect(xPos - 5, yPos - 5, 10, 10);
                    if (dotRect.Contains(Event.current.mousePosition))
                    {
                        GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox);
                        tooltipStyle.normal.textColor = Color.white;
                        tooltipStyle.fontSize = 10;
                        string isBossText = enemy.IsBoss ? " [BOSS]" : "";
                        string tipStr = $"{enemy.EnemyName}{isBossText} ({labelY}:{valY:F0}, {labelX}:{valX:F1})";
                        Vector2 tipSize = tooltipStyle.CalcSize(new GUIContent(tipStr));
                        EditorGUI.DrawRect(new Rect(Event.current.mousePosition.x + 12, Event.current.mousePosition.y - 12, tipSize.x + 6, tipSize.y + 4), new Color(0, 0, 0, 0.9f));
                        GUI.Label(new Rect(Event.current.mousePosition.x + 15, Event.current.mousePosition.y - 10, tipSize.x, tipSize.y), tipStr, tooltipStyle);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            _selectedEnemy = enemy;
                            _currentTab = Tab.Enemies;
                            Repaint();
                        }
                    }
                }
            }
        }

        private void DrawAnalyticsTab()
        {
            // Initialize graph visibility first
            InitializeGraphVisibility();

            EditorGUILayout.BeginHorizontal();

            // Left Area: Plotting Grid and Toolbar Configurations
            float graphSectionWidth = _showGraphSideList ? position.width - 240 : position.width - 30;
            EditorGUILayout.BeginVertical(GUILayout.Width(graphSectionWidth));

            // Controls Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // X Axis Selector
            EditorGUILayout.LabelField("X-Axis:", GUILayout.Width(45));
            _metricX = (MetricX)EditorGUILayout.EnumPopup(_metricX, GUILayout.Width(100));

            GUILayout.Space(10);

            // Y Axis Selector
            EditorGUILayout.LabelField("Y-Axis:", GUILayout.Width(45));
            _metricY = (MetricY)EditorGUILayout.EnumPopup(_metricY, GUILayout.Width(80));

            GUILayout.Space(10);

            // Layout Selector
            EditorGUILayout.LabelField("Layout:", GUILayout.Width(50));
            _graphLayout = (GraphLayout)EditorGUILayout.EnumPopup(_graphLayout, GUILayout.Width(110));

            GUILayout.Space(15);
            _showVassalsOnGraph = EditorGUILayout.ToggleLeft("🛡️ Vassals", _showVassalsOnGraph, GUILayout.Width(80));
            _showEnemiesOnGraph = EditorGUILayout.ToggleLeft("👹 Enemies", _showEnemiesOnGraph, GUILayout.Width(80));

            GUILayout.FlexibleSpace();
            _showGraphSideList = EditorGUILayout.ToggleLeft("📋 Show Sidebar", _showGraphSideList, GUILayout.Width(110));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_graphLayout == GraphLayout.Single)
            {
                Rect graphRect = GUILayoutUtility.GetRect(graphSectionWidth - 10, 400);
                DrawSingleGraph(graphRect, "All Unit Comparison", null);
            }
            else
            {
                // 2x2 Grid Layout
                float singleGraphWidth = (graphSectionWidth - 15) / 2f;
                float singleGraphHeight = 220f;

                EditorGUILayout.BeginVertical();
                
                // Row 1
                EditorGUILayout.BeginHorizontal();
                Rect rect1 = GUILayoutUtility.GetRect(singleGraphWidth, singleGraphHeight);
                DrawSingleGraph(rect1, "🛡️ Bastion Class (Heavy Defenders / Bosses)", UnitClass.Bastion);
                GUILayout.Space(5);
                Rect rect2 = GUILayoutUtility.GetRect(singleGraphWidth, singleGraphHeight);
                DrawSingleGraph(rect2, "⚔️ Vanguard Class (Fighters / Fast Runners)", UnitClass.Vanguard);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                // Row 2
                EditorGUILayout.BeginHorizontal();
                Rect rect3 = GUILayoutUtility.GetRect(singleGraphWidth, singleGraphHeight);
                DrawSingleGraph(rect3, "🏹 Ranger Class (Shooters / Ranged / Flyers)", UnitClass.Ranger);
                GUILayout.Space(5);
                Rect rect4 = GUILayoutUtility.GetRect(singleGraphWidth, singleGraphHeight);
                DrawSingleGraph(rect4, "🔮 Warlock Class (Mages / Magic / Special)", UnitClass.Warlock);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            // Legend labels
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Legend:", EditorStyles.boldLabel, GUILayout.Width(60));
            
            // Circles (Vassals)
            GUIStyle circleStyle = new GUIStyle(EditorStyles.miniLabel);
            circleStyle.normal.textColor = Color.white;
            circleStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("● Bastion", circleStyle, GUILayout.Width(70));
            GUILayout.Label("● Vanguard", circleStyle, GUILayout.Width(80));
            GUILayout.Label("● Ranger", circleStyle, GUILayout.Width(70));
            GUILayout.Label("● Warlock", circleStyle, GUILayout.Width(70));

            GUILayout.Space(20);
            // Triangles (Enemies)
            GUIStyle triStyle = new GUIStyle(circleStyle);
            GUILayout.Label("▲ Enemy (Ground/Pierce)", triStyle, GUILayout.Width(150));
            GUILayout.Label("⧓ BOSS Enemy", triStyle, GUILayout.Width(110));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Right Area Sidebar Checklist
            if (_showGraphSideList)
            {
                GUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200));
                EditorGUILayout.LabelField("👁️ Plot Visibility", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("All On", EditorStyles.miniButtonLeft))
                {
                    SetAllGraphVisibility(true);
                }
                if (GUILayout.Button("All Off", EditorStyles.miniButtonRight))
                {
                    SetAllGraphVisibility(false);
                }
                EditorGUILayout.EndHorizontal();

                _graphFilterScroll = EditorGUILayout.BeginScrollView(_graphFilterScroll);
                
                EditorGUILayout.LabelField("🛡️ Vassals", EditorStyles.boldLabel);
                foreach (var v in _vassalAssets)
                {
                    string key = "V_" + v.UnitName;
                    _graphUnitVisibility[key] = EditorGUILayout.Toggle(v.UnitName, _graphUnitVisibility.GetValueOrDefault(key, true));
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("👹 Enemies", EditorStyles.boldLabel);
                foreach (var e in _enemyAssets)
                {
                    string key = "E_" + e.EnemyName;
                    _graphUnitVisibility[key] = EditorGUILayout.Toggle(e.EnemyName, _graphUnitVisibility.GetValueOrDefault(key, true));
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SyncVassalToCsv(UnitData vassal, Dictionary<string, string> csvRow)
        {
            if (csvRow == null)
            {
                csvRow = new Dictionary<string, string>();
                csvRow["Name"] = vassal.UnitName;
                csvRow["File"] = $"{vassal.name}.md";
                _csvRows.Add(csvRow);
            }

            csvRow["Class"] = vassal.Class.ToString();
            csvRow["Rarity"] = vassal.Rarity.GetShortName();
            csvRow["Final HP"] = vassal.MaxHp.ToString("F0");
            csvRow["Final ATK"] = vassal.AttackPower.ToString("F0");
            csvRow["Final DEF"] = vassal.Defense.ToString("F0");
            csvRow["Range"] = vassal.Range.ToString("F1");
            csvRow["AttackInterval"] = vassal.AttackInterval.ToString("F2");
            csvRow["DeploymentCost"] = vassal.DeploymentCost.ToString();
            csvRow["BlockCount"] = vassal.BlockCount.ToString();
            csvRow["RespawnTime"] = vassal.RespawnTime.ToString("F1");
            csvRow["CanAttackFlying"] = vassal.CanAttackFlying ? "True" : "False";
            csvRow["DamageType"] = vassal.DamageType.ToString();
            csvRow["AttackType"] = vassal.AttackType.ToString();
            csvRow["AttackPattern"] = vassal.AttackPattern.ToString();

            float totalPower = vassal.MaxHp + vassal.AttackPower * 5 + vassal.Defense * 10 + vassal.Range * 50;
            csvRow["Total Power"] = totalPower.ToString("F0");

            Debug.Log($"[Balancing Studio] Synced {vassal.UnitName} stats to in-memory CSV rows.");
        }

        private void SyncCsvToVassal(UnitData vassal, Dictionary<string, string> csvRow)
        {
            if (csvRow == null) return;

            Undo.RecordObject(vassal, "Sync from CSV");

            if (float.TryParse(csvRow.GetValueOrDefault("Final HP", "0"), out float hp)) vassal.MaxHp = hp;
            if (float.TryParse(csvRow.GetValueOrDefault("Final ATK", "0"), out float atk)) vassal.AttackPower = atk;
            if (float.TryParse(csvRow.GetValueOrDefault("Final DEF", "0"), out float def)) vassal.Defense = def;
            if (float.TryParse(csvRow.GetValueOrDefault("Range", "0"), out float r)) vassal.Range = r;
            if (float.TryParse(csvRow.GetValueOrDefault("AttackInterval", "0"), out float interval)) vassal.AttackInterval = interval;
            if (int.TryParse(csvRow.GetValueOrDefault("DeploymentCost", "0"), out int cost)) vassal.DeploymentCost = cost;
            if (int.TryParse(csvRow.GetValueOrDefault("BlockCount", "0"), out int block)) vassal.BlockCount = block;
            if (float.TryParse(csvRow.GetValueOrDefault("RespawnTime", "0"), out float resp)) vassal.RespawnTime = resp;
            if (csvRow.ContainsKey("CanAttackFlying")) vassal.CanAttackFlying = csvRow["CanAttackFlying"] == "True";

            if (csvRow.ContainsKey("DamageType") && System.Enum.TryParse(csvRow["DamageType"], out DamageType dt)) vassal.DamageType = dt;
            if (csvRow.ContainsKey("AttackType") && System.Enum.TryParse(csvRow["AttackType"], out AttackType at)) vassal.AttackType = at;
            if (csvRow.ContainsKey("AttackPattern") && System.Enum.TryParse(csvRow["AttackPattern"], out AttackPattern ap)) vassal.AttackPattern = ap;

            // Parse Class and map aliases/custom string categories
            if (csvRow.ContainsKey("Class"))
            {
                string classStr = csvRow["Class"].Trim().ToLower();
                UnitClass parsedClass = UnitClass.Bastion;
                switch (classStr)
                {
                    case "bastion":
                    case "tank":
                        parsedClass = UnitClass.Bastion;
                        break;
                    case "vanguard":
                    case "berserker":
                        parsedClass = UnitClass.Vanguard;
                        break;
                    case "executioner":
                    case "strikers":
                        parsedClass = UnitClass.Executioner;
                        break;
                    case "ranger":
                    case "marksman":
                        parsedClass = UnitClass.Ranger;
                        break;
                    case "warlock":
                    case "mage":
                        parsedClass = UnitClass.Warlock;
                        break;
                    case "sage":
                    case "blood":
                    case "blood sage":
                        parsedClass = UnitClass.Sage;
                        break;
                    case "architect":
                        parsedClass = UnitClass.Architect;
                        break;
                    case "necromancer":
                    case "summoner":
                        parsedClass = UnitClass.Necromancer;
                        break;
                    case "support":
                    case "tactician":
                    case "scout":
                        parsedClass = UnitClass.Support;
                        break;
                    case "gunner":
                        parsedClass = UnitClass.Gunner;
                        break;
                    case "assassin":
                        parsedClass = UnitClass.Assassin;
                        break;
                    case "overlord":
                        parsedClass = UnitClass.Overlord;
                        break;
                    default:
                        if (System.Enum.TryParse(csvRow["Class"], true, out UnitClass uc))
                        {
                            parsedClass = uc;
                        }
                        break;
                }
                vassal.Class = parsedClass;
            }

            // Parse Rarity
            if (csvRow.ContainsKey("Rarity"))
            {
                string rarityStr = csvRow["Rarity"].Trim().ToUpper();
                UnitRarity parsedRarity = UnitRarity.Common;
                switch (rarityStr)
                {
                    case "C":
                    case "COMMON":
                    case "NORMAL":
                    case "N":
                        parsedRarity = UnitRarity.Common;
                        break;
                    case "UC":
                    case "UNCOMMON":
                        parsedRarity = UnitRarity.Uncommon;
                        break;
                    case "R":
                    case "RARE":
                        parsedRarity = UnitRarity.Rare;
                        break;
                    case "SR":
                    case "ELITE":
                        parsedRarity = UnitRarity.Elite;
                        break;
                    case "SSR":
                    case "MASTER":
                        parsedRarity = UnitRarity.Master;
                        break;
                    case "UR":
                    case "LEGENDARY":
                        parsedRarity = UnitRarity.Legendary;
                        break;
                    default:
                        if (System.Enum.TryParse(csvRow["Rarity"], true, out UnitRarity ur))
                        {
                            parsedRarity = ur;
                        }
                        break;
                }
                vassal.Rarity = parsedRarity;
            }

            EditorUtility.SetDirty(vassal);
            Debug.Log($"[Balancing Studio] Overwrote {vassal.UnitName} asset with CSV values (including Class and Rarity).");
        }

        private void SortVassals()
        {
            if (_vassalAssets == null || _vassalAssets.Count == 0) return;

            _vassalAssets.Sort((a, b) =>
            {
                int comparison = 0;
                switch (_vassalSortColumn)
                {
                    case "Name":
                        comparison = string.Compare(a.UnitName, b.UnitName, System.StringComparison.OrdinalIgnoreCase);
                        break;
                    case "Class":
                        comparison = a.Class.CompareTo(b.Class);
                        break;
                    case "Rarity":
                        comparison = a.Rarity.CompareTo(b.Rarity);
                        break;
                    case "HP":
                        comparison = a.MaxHp.CompareTo(b.MaxHp);
                        break;
                    case "ATK":
                        comparison = a.AttackPower.CompareTo(b.AttackPower);
                        break;
                    case "DEF":
                        comparison = a.Defense.CompareTo(b.Defense);
                        break;
                    case "Range":
                        comparison = a.Range.CompareTo(b.Range);
                        break;
                    case "Interval":
                        comparison = a.AttackInterval.CompareTo(b.AttackInterval);
                        break;
                    case "Cost":
                        comparison = a.DeploymentCost.CompareTo(b.DeploymentCost);
                        break;
                    case "Block":
                        comparison = a.BlockCount.CompareTo(b.BlockCount);
                        break;
                    case "Redeploy":
                        comparison = a.RespawnTime.CompareTo(b.RespawnTime);
                        break;
                    case "Flying":
                        comparison = a.CanAttackFlying.CompareTo(b.CanAttackFlying);
                        break;
                    case "DMG Type":
                        comparison = a.DamageType.CompareTo(b.DamageType);
                        break;
                    default:
                        comparison = 0;
                        break;
                }
                return _vassalSortAscending ? comparison : -comparison;
            });
        }

        private void SortEnemies()
        {
            if (_enemyAssets == null || _enemyAssets.Count == 0) return;

            _enemyAssets.Sort((a, b) =>
            {
                int comparison = 0;
                switch (_enemySortColumn)
                {
                    case "Name":
                        comparison = string.Compare(a.EnemyName, b.EnemyName, System.StringComparison.OrdinalIgnoreCase);
                        break;
                    case "HP":
                        comparison = a.MaxHp.CompareTo(b.MaxHp);
                        break;
                    case "Speed":
                        comparison = a.MoveSpeed.CompareTo(b.MoveSpeed);
                        break;
                    case "ATK":
                        comparison = a.AttackPower.CompareTo(b.AttackPower);
                        break;
                    case "Interval":
                        comparison = a.AttackInterval.CompareTo(b.AttackInterval);
                        break;
                    case "Range":
                        comparison = a.AttackRange.CompareTo(b.AttackRange);
                        break;
                    case "ExitDmg":
                        comparison = a.ExitDamage.CompareTo(b.ExitDamage);
                        break;
                    case "Flying":
                        bool aFlying = a.MovementType == EnemyMovementType.Flying;
                        bool bFlying = b.MovementType == EnemyMovementType.Flying;
                        comparison = aFlying.CompareTo(bFlying);
                        break;
                    case "Priority":
                        comparison = a.TargetingPriority.CompareTo(b.TargetingPriority);
                        break;
                    case "DMG Type":
                        comparison = a.DamageType.CompareTo(b.DamageType);
                        break;
                    default:
                        comparison = 0;
                        break;
                }
                return _enemySortAscending ? comparison : -comparison;
            });
        }

        private void DrawHeaderButton(string displayName, string fieldName, ref string sortColumn, ref bool sortAscending, System.Action sortAction, float width)
        {
            string labelText = displayName;
            if (sortColumn == fieldName)
            {
                labelText += sortAscending ? " ▲" : " ▼";
            }

            GUIStyle headerButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            headerButtonStyle.fontStyle = FontStyle.Bold;
            headerButtonStyle.alignment = TextAnchor.MiddleLeft;

            if (GUILayout.Button(labelText, headerButtonStyle, GUILayout.Width(width)))
            {
                if (sortColumn == fieldName)
                {
                    sortAscending = !sortAscending;
                }
                else
                {
                    sortColumn = fieldName;
                    sortAscending = true;
                }
                sortAction?.Invoke();
            }
        }

        private void SyncAllVassalsToCsv()
        {
            foreach (var vassal in _vassalAssets)
            {
                var csvRow = _csvRows.Find(r => r.ContainsKey("Name") && r["Name"] == vassal.UnitName);
                SyncVassalToCsv(vassal, csvRow);
            }
            SaveCsvFile();
        }

        private void SaveCsvFile()
        {
            string fullCsvPath = Path.Combine(Directory.GetCurrentDirectory(), CSV_RELATIVE_PATH);
            try
            {
                using (var writer = new StreamWriter(fullCsvPath, false, System.Text.Encoding.UTF8))
                {
                    List<string> headers = new List<string> {
                        "File", "Name", "Rarity", "Class", "Base HP", "Base ATK", "Base DEF", "Range",
                        "Final HP", "Final ATK", "Final DEF", "Total Power", "AttackInterval",
                        "DeploymentCost", "BlockCount", "RespawnTime", "CanAttackFlying",
                        "DamageType", "AttackType", "AttackPattern"
                    };

                    writer.WriteLine(string.Join(",", headers));

                    foreach (var row in _csvRows)
                    {
                        List<string> values = new List<string>();
                        foreach (var header in headers)
                        {
                            string val = row.GetValueOrDefault(header, "");
                            // escape quotes/commas if needed
                            if (val.Contains(",") || val.Contains("\""))
                            {
                                val = "\"" + val.Replace("\"", "\"\"") + "\"";
                            }
                            values.Add(val);
                        }
                        writer.WriteLine(string.Join(",", values));
                    }
                }
                Debug.Log($"[Balancing Studio] Successfully saved changes back to CSV file: {fullCsvPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Balancing Studio] Failed to save CSV: {ex.Message}");
            }
        }

        private void SyncAllCsvToVassals()
        {
            int count = 0;
            foreach (var vassal in _vassalAssets)
            {
                if (vassal == null) continue;

                Dictionary<string, string> csvRow = null;

                // 1. Hardcoded special cases from unit_mapping.json / custom asset naming
                if (vassal.name == "Char_Magma_UnitData")
                {
                    csvRow = _csvRows.Find(r => r.GetValueOrDefault("File", "").Contains("lava_bender"));
                }
                else if (vassal.name == "Char_Thrax_UnitData")
                {
                    csvRow = _csvRows.Find(r => r.GetValueOrDefault("File", "").Contains("rune_scarred_gladiator"));
                }
                else if (vassal.name == "Char_Kaelen_Cursed_Blademaster_UnitData")
                {
                    csvRow = _csvRows.Find(r => r.GetValueOrDefault("File", "").Contains("kaelia_cursed_blademaster") || r.GetValueOrDefault("File", "").Contains("kaelen_cursed_blademaster"));
                }
                else if (vassal.name == "Char_Vespera_Succubus_Envoy_UnitData")
                {
                    // No exact row in CSV since Vespera is an art-only asset, keep her as is
                    csvRow = null;
                }
                else if (vassal.name == "Char_Zephyria_Cloud_Scout_UnitData")
                {
                    // No exact row in CSV since Zephyria is an art-only asset, keep her as is
                    csvRow = null;
                }

                // 2. Exact match on whole Name
                if (csvRow == null)
                {
                    csvRow = _csvRows.Find(r => r.ContainsKey("Name") && r["Name"].Trim().Equals(vassal.UnitName.Trim(), System.StringComparison.OrdinalIgnoreCase));
                }

                // 3. Match on name before first separator (comma, space, bracket) - maps titled characters like 'Kaldor, Drakmora Infantry'
                if (csvRow == null)
                {
                    csvRow = _csvRows.Find(r => {
                        if (!r.ContainsKey("Name")) return false;
                        string csvName = r["Name"].Trim();
                        string[] parts = csvName.Split(new char[] { ',', '(', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && parts[0].Trim().Equals(vassal.UnitName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                            return true;
                        return false;
                    });
                }

                // 4. Match asset name substring inside the CSV File column as fallback
                if (csvRow == null)
                {
                    string cleanAssetName = vassal.name.Replace("Char_", "").Replace("_UnitData", "").Replace("_", "").ToLower();
                    csvRow = _csvRows.Find(r => r.ContainsKey("File") && r["File"].Replace("_", "").ToLower().Contains(cleanAssetName));
                }

                if (csvRow != null)
                {
                    SyncCsvToVassal(vassal, csvRow);
                    count++;
                }
                else
                {
                    Debug.LogWarning($"[Balancing Studio] Skip syncing '{vassal.name}' (UnitName: '{vassal.UnitName}') - no matching CSV row found.");
                }
            }
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[Balancing Studio] Synced {count} vassals from CSV to ScriptableObjects successfully.");
        }
    }
}
