using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureCompressionBrowser : EditorWindow
{
    private DefaultAsset selectedFolder;
    private List<TextureInfo> textureInfos = new List<TextureInfo>();
    private List<TextureInfo> filteredInfos = new List<TextureInfo>();
    private Vector2 scrollPosition;
    private bool isGridView = true;
    private float gridItemWidth = 150f;
    private float gridItemHeight = 220f;
    private string searchFilter = "";
    private TextureImporterCompression bulkCompression = TextureImporterCompression.Uncompressed;
    private int lastSelectedIndex = -1;

    private Texture2D selectionTexture;
    private GUIStyle selectedBoxStyle;

    // Async loading variables
    private bool isLoading = false;
    private bool isScanning = false;
    private bool cancelRequested = false;
    private string[] pendingGuids;
    private int currentLoadIndex = 0;
    private const int LOAD_BATCH_SIZE = 10;

    private string[] pendingScanGuids;
    private int currentScanIndex = 0;
    private const int SCAN_BATCH_SIZE = 5;

    // Sorting & Pagination
    private enum SortType { NameAscending, NameDescending, SizeAscending, SizeDescending }
    private SortType currentSort = SortType.NameAscending;
    private int currentPage = 0;
    private int itemsPerPage = 100;
    private readonly int[] itemsPerPageOptions = { 50, 100, 200, 500 };

    private enum UsageFilter { All, Used, Unused }
    private UsageFilter currentUsageFilter = UsageFilter.All;

    private class TextureInfo
    {
        public string path;
        public string guid;
        public string name;
        public long fileSize;
        public int width;
        public int height;
        public TextureImporterCompression compression;
        public bool isSelected;
        public int useCount = -1; // -1 means Not Scanned
        public List<string> usedInScenes = new List<string>();

        public Texture Thumbnail
        {
            get
            {
                Texture preview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                return preview != null ? preview : AssetDatabase.GetCachedIcon(path);
            }
        }

        public string GetReadableFileSize()
        {
            return FormatBytes(fileSize);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:F2} {units[unitIndex]}";
    }

    [MenuItem("Tools/Texture Compression Browser")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextureCompressionBrowser>("Texture Browser");
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        selectionTexture = new Texture2D(1, 1);
        selectionTexture.SetPixel(0, 0, new Color(0.24f, 0.48f, 0.9f, 0.4f));
        selectionTexture.Apply();

        EditorApplication.update += ProcessLoading;
        EditorApplication.update += ProcessScanning;
    }

    private void OnDisable()
    {
        EditorApplication.update -= ProcessLoading;
        EditorApplication.update -= ProcessScanning;
        isLoading = false;
        isScanning = false;
    }

    private void OnGUI()
    {
        if (selectedBoxStyle == null)
        {
            selectedBoxStyle = new GUIStyle(GUI.skin.box);
            selectedBoxStyle.normal.background = selectionTexture;
        }

        DrawToolbar();

        if (isLoading || isScanning)
        {
            DrawLoadingState();
            return;
        }

        if (selectedFolder == null)
        {
            DrawEmptyState();
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (isGridView)
        {
            DrawGridView();
        }
        else
        {
            DrawListView();
        }

        EditorGUILayout.EndScrollView();

        DrawFooter();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        EditorGUI.BeginDisabledGroup(isLoading || isScanning);
        EditorGUI.BeginChangeCheck();
        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField(GUIContent.none, selectedFolder, typeof(DefaultAsset), false, GUILayout.Width(150));
        if (EditorGUI.EndChangeCheck())
        {
            StartLoadingTextures();
        }
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            StartLoadingTextures();
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
        {
            foreach (var info in filteredInfos) info.isSelected = true;
        }
        if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton))
        {
            foreach (var info in filteredInfos) info.isSelected = false;
        }

        GUILayout.Space(10);
        
        EditorGUI.BeginChangeCheck();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFiltering();
        }
        
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (GUILayout.Button("", "ToolbarSeachCancelButton"))
            {
                searchFilter = "";
                ApplyFiltering();
                GUI.FocusControl(null);
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Usage:", EditorStyles.miniLabel, GUILayout.Width(40));
        EditorGUI.BeginChangeCheck();
        currentUsageFilter = (UsageFilter)EditorGUILayout.EnumPopup(currentUsageFilter, EditorStyles.toolbarDropDown, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFiltering();
        }

        if (GUILayout.Button("Scan Usage", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            ScanProjectForUsage();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField("Bulk:", EditorStyles.miniLabel, GUILayout.Width(35));
        bulkCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(bulkCompression, EditorStyles.toolbarDropDown, GUILayout.Width(100));
        
        int selectedCount = filteredInfos.Count(i => i.isSelected);
        string applyLabel = selectedCount > 0 ? $"Apply ({selectedCount})" : "Apply All Filtered";
        if (GUILayout.Button(applyLabel, EditorStyles.toolbarButton))
        {
            BulkApplyCompression();
        }

        GUILayout.Space(10);

        if (GUILayout.Button(isGridView ? "List View" : "Grid View", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            isGridView = !isGridView;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // Second Toolbar Row for Sorting
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginDisabledGroup(isLoading || isScanning);
        
        EditorGUILayout.LabelField("Sort By:", EditorStyles.miniLabel, GUILayout.Width(45));
        EditorGUI.BeginChangeCheck();
        currentSort = (SortType)EditorGUILayout.EnumPopup(currentSort, EditorStyles.toolbarDropDown, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            ApplySorting();
        }

        GUILayout.FlexibleSpace();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawLoadingState()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        
        float progress = 0;
        string label = "";

        if (isLoading)
        {
            progress = pendingGuids != null && pendingGuids.Length > 0 ? (float)currentLoadIndex / pendingGuids.Length : 0f;
            label = $"Loading Textures... {currentLoadIndex} / {pendingGuids?.Length ?? 0}";
        }
        else if (isScanning)
        {
            progress = pendingScanGuids != null && pendingScanGuids.Length > 0 ? (float)currentScanIndex / pendingScanGuids.Length : 0f;
            label = $"Scanning Usage... {currentScanIndex} / {pendingScanGuids?.Length ?? 0}";
        }

        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        EditorGUI.ProgressBar(rect, progress, label);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Cancel"))
        {
            cancelRequested = true;
        }
        
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawEmptyState()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        EditorGUILayout.HelpBox("Select a folder to start browsing textures.", MessageType.Info);
        if (GUILayout.Button("Select Folder", GUILayout.Height(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = path.Replace(Application.dataPath, "Assets");
                selectedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
                StartLoadingTextures();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        long totalSize = filteredInfos.Sum(i => i.fileSize);
        int selectedCount = filteredInfos.Count(i => i.isSelected);
        
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredInfos.Count / itemsPerPage));
        
        // Page Navigation
        EditorGUI.BeginDisabledGroup(currentPage == 0);
        if (GUILayout.Button("◄ Prev", GUILayout.Width(60)))
        {
            currentPage--;
            scrollPosition = Vector2.zero;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField($"Page {currentPage + 1} of {totalPages}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));

        EditorGUI.BeginDisabledGroup(currentPage >= totalPages - 1);
        if (GUILayout.Button("Next ►", GUILayout.Width(60)))
        {
            currentPage++;
            scrollPosition = Vector2.zero;
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Items per page:", EditorStyles.miniLabel, GUILayout.Width(90));
        
        // Items per page dropdown
        int prevItemsPerPage = itemsPerPage;
        string[] optionsStr = itemsPerPageOptions.Select(x => x.ToString()).ToArray();
        int selectedIndex = System.Array.IndexOf(itemsPerPageOptions, itemsPerPage);
        if (selectedIndex < 0) selectedIndex = 1;
        
        selectedIndex = EditorGUILayout.Popup(selectedIndex, optionsStr, GUILayout.Width(60));
        itemsPerPage = itemsPerPageOptions[selectedIndex];
        
        if (itemsPerPage != prevItemsPerPage)
        {
            currentPage = 0; // Reset page on limit change
            scrollPosition = Vector2.zero;
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField($"Showing {filteredInfos.Count} of {textureInfos.Count} textures | Selected: {selectedCount}", EditorStyles.miniLabel, GUILayout.Width(250));
        EditorGUILayout.LabelField($"Total Size: {FormatBytes(totalSize)}", EditorStyles.miniBoldLabel, GUILayout.Width(120));
        
        EditorGUILayout.EndHorizontal();
    }

    private void StartLoadingTextures()
    {
        textureInfos.Clear();
        filteredInfos.Clear();
        lastSelectedIndex = -1;
        currentPage = 0;
        
        currentUsageFilter = UsageFilter.All; // Reset usage filter on new load

        if (selectedFolder == null) return;

        string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        pendingGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        currentLoadIndex = 0;
        cancelRequested = false;
        isLoading = true;
    }

    private void ProcessLoading()
    {
        if (!isLoading) return;

        if (cancelRequested)
        {
            isLoading = false;
            pendingGuids = null;
            ApplyFiltering();
            Repaint();
            return;
        }

        int count = 0;
        while (currentLoadIndex < pendingGuids.Length && count < LOAD_BATCH_SIZE)
        {
            string guid = pendingGuids[currentLoadIndex];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(Application.dataPath, "..", path));
                
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int w = 0, h = 0;
                if (tex != null)
                {
                    w = tex.width;
                    h = tex.height;
                    Resources.UnloadAsset(tex); // Free RAM immediately
                }

                textureInfos.Add(new TextureInfo
                {
                    path = path,
                    guid = guid,
                    name = Path.GetFileName(path),
                    fileSize = fileInfo.Length,
                    width = w,
                    height = h,
                    compression = importer.textureCompression,
                    isSelected = false
                });
            }

            currentLoadIndex++;
            count++;
        }

        Repaint();

        if (currentLoadIndex >= pendingGuids.Length)
        {
            isLoading = false;
            pendingGuids = null;
            ApplyFiltering();
            Repaint();
        }
    }

    private void ScanProjectForUsage()
    {
        if (textureInfos.Count == 0 || isScanning) return;

        // Reset usage info
        foreach (var info in textureInfos)
        {
            info.useCount = 0;
            info.usedInScenes.Clear();
        }

        // Find all potential containers: Scenes, Prefabs, ScriptableObjects, and Materials
        pendingScanGuids = AssetDatabase.FindAssets("t:Scene t:Prefab t:ScriptableObject t:Material");
        currentScanIndex = 0;
        cancelRequested = false;
        isScanning = true;
    }

    private void ProcessScanning()
    {
        if (!isScanning) return;

        if (cancelRequested)
        {
            isScanning = false;
            pendingScanGuids = null;
            ApplyFiltering();
            Repaint();
            return;
        }

        int count = 0;
        while (currentScanIndex < pendingScanGuids.Length && count < SCAN_BATCH_SIZE)
        {
            string path = AssetDatabase.GUIDToAssetPath(pendingScanGuids[currentScanIndex]);
            string fullPath = Path.Combine(Application.dataPath, "..", path);
            
            if (File.Exists(fullPath))
            {
                string content = File.ReadAllText(fullPath);
                
                foreach (var info in textureInfos)
                {
                    if (content.Contains(info.guid))
                    {
                        info.useCount++;
                        if (path.EndsWith(".unity"))
                        {
                            string sceneName = Path.GetFileNameWithoutExtension(path);
                            if (!info.usedInScenes.Contains(sceneName)) info.usedInScenes.Add(sceneName);
                        }
                    }
                }
            }

            currentScanIndex++;
            count++;
        }

        Repaint();

        if (currentScanIndex >= pendingScanGuids.Length)
        {
            isScanning = false;
            pendingScanGuids = null;
            ApplyFiltering();
            Repaint();
        }
    }

    private void ApplyFiltering()
    {
        var tempInfos = textureInfos.AsEnumerable();

        // Search Filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            string search = searchFilter.ToLower();
            tempInfos = tempInfos.Where(i => i.name.ToLower().Contains(search));
        }

        // Usage Filter
        switch (currentUsageFilter)
        {
            case UsageFilter.Used:
                tempInfos = tempInfos.Where(i => i.useCount > 0);
                break;
            case UsageFilter.Unused:
                tempInfos = tempInfos.Where(i => i.useCount == 0);
                break;
        }

        filteredInfos = tempInfos.ToList();
        ApplySorting();
        currentPage = 0; 
    }

    private void ApplySorting()
    {
        switch (currentSort)
        {
            case SortType.NameAscending:
                filteredInfos.Sort((a, b) => a.name.CompareTo(b.name));
                break;
            case SortType.NameDescending:
                filteredInfos.Sort((a, b) => b.name.CompareTo(a.name));
                break;
            case SortType.SizeAscending:
                filteredInfos.Sort((a, b) => a.fileSize.CompareTo(b.fileSize));
                break;
            case SortType.SizeDescending:
                filteredInfos.Sort((a, b) => b.fileSize.CompareTo(a.fileSize));
                break;
        }
        lastSelectedIndex = -1;
    }

    private void BulkApplyCompression()
    {
        var targets = filteredInfos.Where(i => i.isSelected).ToList();
        if (targets.Count == 0) targets = filteredInfos; 

        if (targets.Count == 0) return;
        
        if (!EditorUtility.DisplayDialog("Bulk Apply Compression", $"Apply {bulkCompression} to {targets.Count} textures?", "Yes", "No"))
        {
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < targets.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Applying Compression", $"Processing {targets[i].name}...", (float)i / targets.Count);
                ApplyCompression(targets[i], bulkCompression);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
    }

    private void DrawGridView()
    {
        float windowWidth = position.width;
        int columns = Mathf.Max(1, Mathf.FloorToInt(windowWidth / gridItemWidth));
        
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredInfos.Count);
        
        EditorGUILayout.BeginVertical();
        for (int i = startIndex; i < endIndex; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < columns; j++)
            {
                int index = i + j;
                if (index < endIndex)
                {
                    DrawGridItem(filteredInfos[index], index);
                }
                else
                {
                    GUILayout.Space(gridItemWidth);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGridItem(TextureInfo info, int globalIndex)
    {
        GUIStyle style = info.isSelected ? selectedBoxStyle : GUI.skin.box;
        Rect itemRect = EditorGUILayout.BeginVertical(style, GUILayout.Width(gridItemWidth), GUILayout.Height(gridItemHeight + 20));
        
        // Thumbnail - High Quality
        Rect texRect = GUILayoutUtility.GetRect(gridItemWidth - 10, 120);
        Texture thumbnail = info.Thumbnail;
        if (thumbnail != null)
        {
            GUI.DrawTexture(texRect, thumbnail, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.LabelField(texRect, "Loading...", EditorStyles.centeredGreyMiniLabel);
        }

        if (info.isSelected)
        {
            Rect checkRect = new Rect(texRect.x + 2, texRect.y + 2, 18, 18);
            GUI.Box(checkRect, "✔", EditorStyles.miniButton);
        }

        EditorGUILayout.LabelField(info.name, EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{info.width}x{info.height}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        
        // Usage Indicator
        if (info.useCount >= 0)
        {
            using (new GUIColorScope(info.useCount > 0 ? Color.green : new Color(1f, 0.4f, 0.4f))) // Softer red
            {
                string usageStr = info.useCount > 0 ? $"Used ({info.useCount})" : "Unused";
                EditorGUILayout.LabelField(usageStr, EditorStyles.miniBoldLabel, GUILayout.Width(70));
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(info.GetReadableFileSize(), EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        TextureImporterCompression newCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(info.compression);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyCompression(info, newCompression);
        }

        if (GUILayout.Button("Select Asset", EditorStyles.miniButton))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(info.path);
        }

        EditorGUILayout.EndVertical();
        HandleSelection(info, globalIndex, itemRect);
    }

    private void DrawListView()
    {
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredInfos.Count);

        EditorGUILayout.BeginVertical();
        for (int i = startIndex; i < endIndex; i++)
        {
            TextureInfo info = filteredInfos[i];
            GUIStyle style = info.isSelected ? selectedBoxStyle : GUI.skin.box;

            Rect itemRect = EditorGUILayout.BeginHorizontal(style, GUILayout.Height(50));
            
            // Thumbnail
            Rect rect = GUILayoutUtility.GetRect(45, 45);
            Texture thumbnail = info.Thumbnail;
            if (thumbnail != null)
            {
                GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
            }

            if (info.isSelected)
            {
                EditorGUI.LabelField(new Rect(rect.x - 2, rect.y - 2, 15, 15), "✔");
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(info.name, EditorStyles.boldLabel);
            if (info.usedInScenes.Count > 0)
            {
                EditorGUILayout.LabelField("Used in: " + string.Join(", ", info.usedInScenes), EditorStyles.miniLabel);
            }
            else if (info.useCount == 0)
            {
                using (new GUIColorScope(new Color(1f, 0.4f, 0.4f)))
                {
                    EditorGUILayout.LabelField("Unused / Not Scanned", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField($"{info.width}x{info.height}", GUILayout.Width(80));
            EditorGUILayout.LabelField(info.GetReadableFileSize(), GUILayout.Width(80));

            // Usage Count Column
            string countStr = info.useCount >= 0 ? info.useCount.ToString() : "?";
            EditorGUILayout.LabelField($"Uses: {countStr}", GUILayout.Width(60));

            EditorGUI.BeginChangeCheck();
            TextureImporterCompression newCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(info.compression, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyCompression(info, newCompression);
            }

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(info.path);
            }

            EditorGUILayout.EndHorizontal();
            HandleSelection(info, i, itemRect);
        }
        EditorGUILayout.EndVertical();
    }

    private struct GUIColorScope : System.IDisposable
    {
        private readonly Color oldColor;
        public GUIColorScope(Color newColor) { oldColor = GUI.color; GUI.color = newColor; }
        public void Dispose() { GUI.color = oldColor; }
    }

    private void HandleSelection(TextureInfo info, int index, Rect rect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            if (e.button == 0) // Left click
            {
                if (e.shift && lastSelectedIndex != -1)
                {
                    int start = Mathf.Min(lastSelectedIndex, index);
                    int end = Mathf.Max(lastSelectedIndex, index);
                    
                    if (!e.control && !e.command)
                    {
                        foreach (var other in textureInfos) other.isSelected = false;
                    }

                    for (int i = 0; i < filteredInfos.Count; i++)
                    {
                        if (i >= start && i <= end) filteredInfos[i].isSelected = true;
                    }
                }
                else if (e.control || e.command)
                {
                    info.isSelected = !info.isSelected;
                    lastSelectedIndex = index;
                }
                else
                {
                    foreach (var other in textureInfos) other.isSelected = false;
                    info.isSelected = true;
                    lastSelectedIndex = index;
                }
                e.Use();
                Repaint();
            }
        }
    }

    private void ApplyCompression(TextureInfo info, TextureImporterCompression newCompression)
    {
        TextureImporter importer = AssetImporter.GetAtPath(info.path) as TextureImporter;
        if (importer != null)
        {
            importer.textureCompression = newCompression;
            importer.SaveAndReimport();
            info.compression = newCompression;
            FileInfo fileInfo = new FileInfo(Path.Combine(Application.dataPath, "..", info.path));
            info.fileSize = fileInfo.Length;
        }
    }
}
