using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using DG.Tweening;
using MaouSamaTD.Levels;


namespace MaouSamaTD.UI.MainMenu
{
    [System.Serializable]
    public class CampaignSidebarController
    {
        [SerializeField] private GameObject _sidebarRoot;
        [SerializeField] private Transform _sidebarContentContainer;
        [SerializeField] private SidebarLevelItem _sidebarItemPrefab;
        [SerializeField] private Sprite _arrowLeftSprite;
        [SerializeField] private Sprite _arrowRightSprite;

        private string _sidebarFilter = "ALL";
        private CampaignPage _page;

        public GameObject SidebarRoot => _sidebarRoot;
        public Transform SidebarContentContainer => _sidebarContentContainer;
        public string SidebarFilter => _sidebarFilter;

        public void Initialize(
            CampaignPage page,
            GameObject sidebarRoot,
            Transform sidebarContentContainer,
            SidebarLevelItem sidebarItemPrefab,
            Sprite arrowLeftSprite,
            Sprite arrowRightSprite)
        {
            _page = page;
            _sidebarRoot = sidebarRoot;
            _sidebarContentContainer = sidebarContentContainer;
            _sidebarItemPrefab = sidebarItemPrefab;
            _arrowLeftSprite = arrowLeftSprite;
            _arrowRightSprite = arrowRightSprite;
            EnsureLeftSidebarExists();
        }

        public void EnsureLeftSidebarExists()
        {
            Transform targetParent = _page.VisualRoot != null ? _page.VisualRoot.transform : _page.transform;

            if (_sidebarRoot == null)
            {
                var existing = targetParent.Find("LeftSideber");
                if (existing == null) existing = targetParent.Find("LeftSidebar");
                if (existing == null) existing = _page.transform.Find("LeftSideber");
                if (existing == null) existing = _page.transform.Find("LeftSidebar");
                if (existing != null)
                {
                    _sidebarRoot = existing.gameObject;
                }
            }

            if (_sidebarRoot != null)
            {
                var sidebarRect = _sidebarRoot.GetComponent<RectTransform>();
                if (sidebarRect != null)
                {
                    sidebarRect.anchorMin = new Vector2(0f, 0f);
                    sidebarRect.anchorMax = new Vector2(0f, 1f);
                    sidebarRect.pivot = new Vector2(0f, 0.5f);
                    sidebarRect.offsetMin = new Vector2(0f, 115f);
                    sidebarRect.offsetMax = new Vector2(300f, -115f);
                }

                if (_sidebarContentContainer == null)
                {
                    _sidebarContentContainer = _sidebarRoot.transform.Find("ScrollView/Viewport/Content");
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.transform.Find("Viewport/Content");
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.GetComponentInChildren<VerticalLayoutGroup>()?.transform;
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.transform;
                }

                var filtersTrans = _sidebarRoot.transform.Find("FiltersContainer");
                if (filtersTrans == null) filtersTrans = _sidebarRoot.transform.Find("TabsContainer");
                if (filtersTrans != null)
                {
                    var filtersRect = filtersTrans.GetComponent<RectTransform>();
                    if (filtersRect != null)
                    {
                        filtersRect.anchoredPosition = new Vector2(filtersRect.anchoredPosition.x, -90f);
                    }
                }
                var scrollTrans = _sidebarRoot.transform.Find("ScrollView");
                if (scrollTrans != null)
                {
                    var scrollRect = scrollTrans.GetComponent<RectTransform>();
                    if (scrollRect != null)
                    {
                        scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, -140f);
                    }
                }

                var buttons = _sidebarRoot.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.gameObject.name.ToUpper();
                    if (btnName.Contains("ALL"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("ALL"));
                    }
                    else if (btnName.Contains("UNLOCKED"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("UNLOCKED"));
                    }
                    else if (btnName.Contains("CLEARED") || btnName.Contains("COMPLETE"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("CLEARED"));
                    }
                }
            }
            else
            {
                // Dynamic fallback creation
                GameObject sidebarGo = new GameObject("LeftSidebar", typeof(RectTransform), typeof(Image));
                sidebarGo.transform.SetParent(targetParent, false);
                var rect = sidebarGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(0f, 115f);
                rect.offsetMax = new Vector2(300f, -115f);

                var img = sidebarGo.GetComponent<Image>();
                img.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

                var outline = sidebarGo.AddComponent<Outline>();
                outline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.5f);
                outline.effectDistance = new Vector2(2f, 2f);

                _sidebarRoot = sidebarGo;

                GameObject tabsGo = new GameObject("FiltersContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                tabsGo.transform.SetParent(sidebarGo.transform, false);
                var tabsRect = tabsGo.GetComponent<RectTransform>();
                tabsRect.anchorMin = new Vector2(0f, 1f);
                tabsRect.anchorMax = new Vector2(1f, 1f);
                tabsRect.pivot = new Vector2(0.5f, 1f);
                tabsRect.anchoredPosition = new Vector2(0f, -90f);
                tabsRect.sizeDelta = new Vector2(-20f, 35f);

                var tabsLayout = tabsGo.GetComponent<HorizontalLayoutGroup>();
                tabsLayout.spacing = 5f;
                tabsLayout.childControlWidth = true;
                tabsLayout.childControlHeight = true;
                tabsLayout.childForceExpandWidth = true;
                tabsLayout.childForceExpandHeight = true;

                CreateFilterButton(tabsGo.transform, "ALL");
                CreateFilterButton(tabsGo.transform, "UNLOCKED");
                CreateFilterButton(tabsGo.transform, "CLEARED");

                GameObject scrollViewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
                scrollViewGo.transform.SetParent(sidebarGo.transform, false);
                var scrollRect = scrollViewGo.GetComponent<RectTransform>();
                scrollRect.anchorMin = new Vector2(0f, 0f);
                scrollRect.anchorMax = new Vector2(1f, 1f);
                scrollRect.pivot = new Vector2(0.5f, 0.5f);
                scrollRect.offsetMin = new Vector2(10f, 10f);
                scrollRect.offsetMax = new Vector2(-10f, -140f);

                var sr = scrollViewGo.GetComponent<ScrollRect>();
                sr.horizontal = false;
                sr.vertical = true;

                GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewportGo.transform.SetParent(scrollViewGo.transform, false);
                var viewRect = viewportGo.GetComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.1f);
                viewportGo.GetComponent<Mask>().showMaskGraphic = false;

                sr.viewport = viewRect;

                GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentRect = contentGo.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 0f);

                var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 8f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset(5, 5, 5, 5);

                var csf = contentGo.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                sr.content = contentRect;
                _sidebarContentContainer = contentRect;
            }

            if (_sidebarRoot != null)
            {
                var rect = _sidebarRoot.GetComponent<RectTransform>();
                var toggleTrans = _sidebarRoot.transform.Find("HamburgerToggle");
                Button toggleBtn = null;
                TextMeshProUGUI tText = null;
                Image toggleImg = null;

                if (toggleTrans == null)
                {
                    GameObject toggleGo = new GameObject("HamburgerToggle", typeof(RectTransform), typeof(Image), typeof(Button));
                    toggleGo.transform.SetParent(_sidebarRoot.transform, false);
                    var toggleRect = toggleGo.GetComponent<RectTransform>();
                    toggleRect.anchorMin = new Vector2(1f, 0.5f);
                    toggleRect.anchorMax = new Vector2(1f, 0.5f);
                    toggleRect.pivot = new Vector2(0f, 0.5f);
                    toggleRect.anchoredPosition = new Vector2(5f, 0f);
                    toggleRect.sizeDelta = new Vector2(40f, 40f);

                    toggleImg = toggleGo.GetComponent<Image>();
                    toggleImg.color = Color.white;
                    
                    var toggleOutline = toggleGo.AddComponent<Outline>();
                    toggleOutline.effectColor = new Color(0f, 0.8f, 1f, 0.6f);
                    toggleOutline.effectDistance = new Vector2(1f, 1f);

                    GameObject toggleTextGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    toggleTextGo.transform.SetParent(toggleGo.transform, false);
                    var tTextRect = toggleTextGo.GetComponent<RectTransform>();
                    tTextRect.anchorMin = Vector2.zero;
                    tTextRect.anchorMax = Vector2.one;
                    tTextRect.sizeDelta = Vector2.zero;
                    tText = toggleTextGo.GetComponent<TextMeshProUGUI>();
                    tText.text = "◀";
                    tText.alignment = TextAlignmentOptions.Center;
                    tText.fontSize = 20f;
                    tText.color = new Color(0f, 0.8f, 1f, 1f);

                    toggleBtn = toggleGo.GetComponent<Button>();
                }
                else
                {
                    toggleBtn = toggleTrans.GetComponent<Button>();
                    toggleImg = toggleTrans.GetComponent<Image>();
                    tText = toggleTrans.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    if (tText == null) tText = toggleTrans.GetComponentInChildren<TextMeshProUGUI>();
                }

                if (_arrowLeftSprite == null)
                {
#if UNITY_EDITOR
                    _arrowLeftSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/256x256/ic_arrow_left.png");
#endif
                }
                if (_arrowRightSprite == null)
                {
#if UNITY_EDITOR
                    _arrowRightSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/256x256/ic_arrow_right.png");
#endif
                }

                if (toggleBtn != null)
                {
                    toggleBtn.onClick.RemoveAllListeners();
                    bool isExpanded = true;
                    rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                    
                    if (tText != null) tText.gameObject.SetActive(false);
                    
                    if (toggleImg != null && _arrowLeftSprite != null)
                    {
                        toggleImg.sprite = _arrowLeftSprite;
                        toggleImg.color = Color.white;
                        toggleImg.transform.localScale = Vector3.one;
                    }

                    toggleBtn.onClick.AddListener(() => {
                        isExpanded = !isExpanded;
                        float targetX = isExpanded ? 0f : -300f;
                        
                        if (toggleImg != null)
                        {
                            toggleImg.transform.localScale = new Vector3(isExpanded ? 1f : -1f, 1f, 1f);
                        }
                        
                        rect.DOAnchorPosX(targetX, 0.35f).SetEase(Ease.OutQuad).SetUpdate(true);
                    });
                }
            }
        }

        private void CreateFilterButton(Transform parent, string filterType)
        {
            GameObject btnGo = new GameObject($"FilterBtn_{filterType}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            var img = btnGo.GetComponent<Image>();
            img.color = _sidebarFilter == filterType ? new Color(0.92f, 0.3f, 0.29f, 0.9f) : new Color(0.08f, 0.1f, 0.14f, 0.85f);

            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = _sidebarFilter == filterType ? new Color(0.97f, 0.79f, 0.14f, 0.8f) : new Color(0.97f, 0.79f, 0.14f, 0.15f);
            outline.effectDistance = new Vector2(1f, 1f);

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var tm = txtGo.GetComponent<TextMeshProUGUI>();
            tm.text = filterType;
            tm.fontSize = 11f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = _sidebarFilter == filterType ? Color.white : new Color(0.7f, 0.8f, 0.9f, 0.9f);

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                SetSidebarFilter(filterType);
            });
        }

        public void SetSidebarFilter(string filterType)
        {
            _sidebarFilter = filterType;
            
            if (_sidebarRoot != null)
            {
                var buttons = _sidebarRoot.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.gameObject.name.ToUpper();
                    bool isMatched = false;
                    if (btnName.Contains("ALL") && filterType == "ALL") isMatched = true;
                    else if (btnName.Contains("UNLOCKED") && filterType == "UNLOCKED") isMatched = true;
                    else if ((btnName.Contains("CLEARED") || btnName.Contains("COMPLETE")) && filterType == "CLEARED") isMatched = true;

                    var img = btn.GetComponent<Image>();
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (img != null)
                    {
                        img.color = isMatched ? new Color(0.92f, 0.3f, 0.29f, 0.95f) : new Color(0.08f, 0.1f, 0.14f, 0.9f);
                        var outline = btn.GetComponent<Outline>();
                        if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
                        outline.effectColor = isMatched ? new Color(0.97f, 0.79f, 0.14f, 0.8f) : new Color(0.97f, 0.79f, 0.14f, 0.15f);
                        outline.effectDistance = new Vector2(1.5f, 1.5f);
                    }
                    if (txt != null)
                    {
                        txt.color = isMatched ? Color.white : new Color(0.7f, 0.8f, 0.9f, 0.7f);
                    }
                }
            }

            _page.Refresh();
        }

        public void RefreshLeftSidebar(
            List<LevelData> allLevels,
            List<LevelData> mainStoryLevels,
            List<LevelData> resourceDungeons,
            List<LevelData> riteDungeons,
            List<LevelData> vassalDungeons,
            bool showMainStory,
            bool showResourceDungeons,
            bool showSpecialDungeons,
            MaouSamaTD.Managers.SaveManager saveManager)
        {
            EnsureLeftSidebarExists();

            if (_sidebarContentContainer == null) return;

            // Clear old sidebar items
            List<Transform> childrenToDestroy = new List<Transform>();
            foreach (Transform child in _sidebarContentContainer)
            {
                childrenToDestroy.Add(child);
            }
            foreach (var child in childrenToDestroy)
            {
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            if (!Application.isPlaying) return;
            if (allLevels == null) return;

            // Populate all levels in the active toggled categories
            List<(LevelData level, int originalIndex, List<LevelData> originalList)> targetList = new List<(LevelData, int, List<LevelData>)>();
            
            if (showMainStory)
            {
                for (int i = 0; i < mainStoryLevels.Count; i++)
                {
                    targetList.Add((mainStoryLevels[i], i, mainStoryLevels));
                }
            }
            if (showResourceDungeons)
            {
                for (int i = 0; i < resourceDungeons.Count; i++)
                {
                    targetList.Add((resourceDungeons[i], i, resourceDungeons));
                }
            }
            if (showSpecialDungeons)
            {
                List<LevelData> specialList = new List<LevelData>(riteDungeons);
                specialList.AddRange(vassalDungeons);
                for (int i = 0; i < specialList.Count; i++)
                {
                    targetList.Add((specialList[i], i, specialList));
                }
            }

            for (int i = 0; i < targetList.Count; i++)
            {
                var entry = targetList[i];
                var level = entry.level;
                if (level == null) continue;

                bool isPlaced = level.CampaignMapPosition != Vector2.zero && level.CampaignMapPosition != new Vector2(1024f, 571f);
                bool isUnlocked = _page.IsLevelUnlocked(level, entry.originalIndex, entry.originalList);
                bool isCompleted = saveManager != null && saveManager.IsLevelCompleted(level.LevelID);

                // Sidebar filtering check
                if (_sidebarFilter == "UNLOCKED" && !isUnlocked) continue;
                if (_sidebarFilter == "CLEARED" && !isCompleted) continue;

                // Click action
                Action clickAction = () => {
                    if (isPlaced)
                    {
                        _page.CenterScrollOnPosition(level.CampaignMapPosition);
                        var mapBtn = _page.SpawnedButtons.Find(b => b != null && b.LevelDataForCallback == level);
                        if (mapBtn != null)
                        {
                            _page.OnLevelClickedPublic(level, isUnlocked);
                        }
                    }
                };

                if (_sidebarItemPrefab != null)
                {
                    SidebarLevelItem item = UnityEngine.Object.Instantiate(_sidebarItemPrefab, _sidebarContentContainer);
                    item.Setup(level, isUnlocked, isPlaced, isCompleted, clickAction);
                }
                else
                {
                    // Fallback procedural layout
                    GameObject itemGo = new GameObject($"SidebarItem_{level.LevelID}", typeof(Image), typeof(Button));
                    itemGo.transform.SetParent(_sidebarContentContainer, false);

                    var itemRect = itemGo.GetComponent<RectTransform>();
                    itemRect.sizeDelta = new Vector2(0f, 65f);

                    var itemImg = itemGo.GetComponent<Image>();
                    itemImg.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);

                    GameObject accentGo = new GameObject("Accent", typeof(Image));
                    accentGo.transform.SetParent(itemGo.transform, false);
                    var accentRect = accentGo.GetComponent<RectTransform>();
                    accentRect.sizeDelta = new Vector2(4f, 0f);
                    accentRect.anchorMin = new Vector2(0f, 0f);
                    accentRect.anchorMax = new Vector2(0f, 1f);
                    accentRect.pivot = new Vector2(0f, 0.5f);
                    accentRect.anchoredPosition = Vector2.zero;
                    accentGo.GetComponent<Image>().color = isPlaced ? _page.GetCategoryColorPublic(level.Category) : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    GameObject nameGo = new GameObject("Text", typeof(TextMeshProUGUI));
                    nameGo.transform.SetParent(itemGo.transform, false);
                    var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                    nameTmp.text = $"{LevelButton.FormatLevelID(level.LevelID)} {level.LevelName}";
                    nameTmp.fontSize = 13;
                    nameTmp.alignment = TextAlignmentOptions.TopLeft;
                    nameTmp.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);

                    var nameRect = nameGo.GetComponent<RectTransform>();
                    nameRect.anchorMin = new Vector2(0f, 0f);
                    nameRect.anchorMax = new Vector2(1f, 1f);
                    nameRect.pivot = new Vector2(0.5f, 0.5f);
                    nameRect.offsetMin = new Vector2(15f, 20f);
                    nameRect.offsetMax = new Vector2(-40f, -5f);

                    GameObject starHolderGo = new GameObject("Sidebar_StarHolder", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    starHolderGo.transform.SetParent(itemGo.transform, false);
                    var starRect = starHolderGo.GetComponent<RectTransform>();
                    starRect.anchorMin = new Vector2(0f, 0f);
                    starRect.anchorMax = new Vector2(1f, 0f);
                    starRect.pivot = new Vector2(0.5f, 0f);
                    starRect.anchoredPosition = new Vector2(15f, 4f);
                    starRect.sizeDelta = new Vector2(-55f, 10f);

                    var hlg = starHolderGo.GetComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 2f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;

                    if (isUnlocked)
                    {
                        int starsCount = 0;
                        if (saveManager != null && saveManager.CurrentData != null)
                        {
                            if (saveManager.CurrentData.LevelStars != null)
                            {
                                var starData = saveManager.CurrentData.LevelStars.Find(s => s.LevelID == level.LevelID);
                                if (starData.LevelID != null)
                                {
                                    starsCount = starData.Stars;
                                }
                            }
                            
                            if (starsCount == 0 && saveManager.CurrentData.CompletedLevels != null && saveManager.CurrentData.CompletedLevels.Contains(level.LevelID))
                            {
                                starsCount = 3;
                            }
                        }

                        for (int sIndex = 0; sIndex < 3; sIndex++)
                        {
                            GameObject starGo = new GameObject($"Star_{sIndex}", typeof(RectTransform), typeof(Image));
                            starGo.transform.SetParent(starHolderGo.transform, false);
                            var img = starGo.GetComponent<Image>();
                            var sRect = starGo.GetComponent<RectTransform>();
                            sRect.sizeDelta = new Vector2(10f, 10f);

#if UNITY_EDITOR
                            var fullSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Full.png");
                            var emptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Empty.png");
                            img.sprite = (sIndex < starsCount) ? fullSprite : emptySprite;
#endif
                        }
                    }

                    GameObject statusGo = new GameObject("StatusIcon", typeof(TextMeshProUGUI));
                    statusGo.transform.SetParent(itemGo.transform, false);
                    var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();

                    string statusText = "";
                    if (isCompleted)
                    {
                        statusText = "<color=#E6B800>[OK]</color>";
                    }
                    else if (!isUnlocked)
                    {
                        statusText = "<color=#777777>[L]</color>";
                    }
                    else
                    {
                        statusText = isPlaced ? "<color=#00CCFF>></color>" : "";
                    }

                    statusTmp.text = statusText;
                    statusTmp.fontSize = 16;
                    statusTmp.alignment = TextAlignmentOptions.Center;

                    var statusRect = statusGo.GetComponent<RectTransform>();
                    statusRect.sizeDelta = new Vector2(30f, 30f);
                    statusRect.anchorMin = new Vector2(1f, 0.5f);
                    statusRect.anchorMax = new Vector2(1f, 0.5f);
                    statusRect.pivot = new Vector2(1f, 0.5f);
                    statusRect.anchoredPosition = new Vector2(-5f, 0f);

                    var btn = itemGo.GetComponent<Button>();
                    btn.onClick.AddListener(() => clickAction());
                }
            }
        }
    }
}
