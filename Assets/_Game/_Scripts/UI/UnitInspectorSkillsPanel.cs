using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles skill slot iconography in the unit inspector and interactive popups.
    /// </summary>
    public class UnitInspectorSkillsPanel : MonoBehaviour
    {
        [Header("Skill Slots")]
        [SerializeField] private Image[] _skillSlots;

        private void Awake()
        {
            // Auto-bind skill slots if unassigned
            if (_skillSlots == null || _skillSlots.Length == 0)
            {
                var rootUI = GetComponentInParent<UnitInspectorFullScreenUI>();
                Transform searchRoot = rootUI != null ? rootUI.transform : this.transform.root;

                System.Collections.Generic.List<Image> slots = new System.Collections.Generic.List<Image>();
                var images = searchRoot.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    string n = img.name.ToLower();
                    if (n.Contains("skill") || n.Contains("slot") || n.Contains("passive") || n.Contains("active"))
                    {
                        slots.Add(img);
                    }
                }
                if (slots.Count > 0) _skillSlots = slots.ToArray();
            }
        }

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            RefreshSkillSlot(0, u.PassiveSkill);
            RefreshSkillSlot(1, u.ActiveSkill);
            RefreshSkillSlot(2, u.UltimateSkill);
        }

        private void RefreshSkillSlot(int index, MaouSamaTD.Skills.UnitSkillData data)
        {
            if (_skillSlots == null || index < 0 || index >= _skillSlots.Length || _skillSlots[index] == null) return;
            
            bool hasSkill = data != null;
            _skillSlots[index].gameObject.SetActive(true); // Keep slot on, just toggle icon

            // Dynamically add a button component to allow clicking
            var btn = _skillSlots[index].gameObject.GetComponent<Button>();
            if (btn == null) btn = _skillSlots[index].gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();

            if (hasSkill)
            {
                _skillSlots[index].sprite = data.Icon;
                _skillSlots[index].color = Color.white;
                btn.onClick.AddListener(() => ShowSkillPopup(data));
            }
            else
            {
                _skillSlots[index].sprite = null;
                // Keep the box visible as #303030 solid dark grey block per user requirement
                _skillSlots[index].color = new Color(0.189f, 0.189f, 0.189f, 1f);
            }
        }

        private void ShowSkillPopup(MaouSamaTD.Skills.UnitSkillData data)
        {
            if (data == null) return;

            // 1. Create full screen dark blur overlay
            var overlayGo = new GameObject("SkillPopupOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(this.transform.root, false); // Attach to Root Canvas
            
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var overlayImage = overlayGo.AddComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.02f, 0.04f, 0.8f);

            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() => Destroy(overlayGo));

            // 2. Centered Dialogue Box
            var dialogGo = new GameObject("PopupDialog", typeof(RectTransform));
            dialogGo.transform.SetParent(overlayGo.transform, false);
            
            var dialogRect = dialogGo.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(500, 320);

            var dialogImage = dialogGo.AddComponent<Image>();
            dialogImage.color = new Color(0.1f, 0.1f, 0.14f, 0.95f);

            var outline = dialogGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);

            // Add smooth fade in using CanvasGroup
            var cg = overlayGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(FadeInCanvasGroup(cg));

            // 3. Layout: Icon, Title, Description, Footer
            // Skill Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(dialogGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.08f, 0.72f);
            iconRect.anchorMax = new Vector2(0.24f, 0.92f);
            iconRect.sizeDelta = Vector2.zero;

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = data.Icon;
            iconImg.preserveAspect = true;

            var iconOutline = iconGo.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            iconOutline.effectDistance = new Vector2(1, 1);

            // Skill Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(dialogGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.28f, 0.72f);
            titleRect.anchorMax = new Vector2(0.92f, 0.92f);
            titleRect.sizeDelta = Vector2.zero;

            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = data.SkillName?.ToUpper();
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.95f, 0.95f, 1f);
            titleText.alignment = TextAlignmentOptions.Left;

            // Skill Description Box
            var descGo = new GameObject("Description", typeof(RectTransform));
            descGo.transform.SetParent(dialogGo.transform, false);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.08f, 0.22f);
            descRect.anchorMax = new Vector2(0.92f, 0.65f);
            descRect.sizeDelta = Vector2.zero;

            var descText = descGo.AddComponent<TextMeshProUGUI>();
            descText.text = data.GetFormattedDescription();
            descText.fontSize = 15;
            descText.color = new Color(0.8f, 0.8f, 0.85f);
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.enableWordWrapping = true;

            // Close hint footer
            var footerGo = new GameObject("Footer", typeof(RectTransform));
            footerGo.transform.SetParent(dialogGo.transform, false);
            var footerRect = footerGo.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.05f, 0.04f);
            footerRect.anchorMax = new Vector2(0.95f, 0.14f);
            footerRect.sizeDelta = Vector2.zero;

            var footerText = footerGo.AddComponent<TextMeshProUGUI>();
            footerText.text = "— Tap anywhere to close —";
            footerText.fontSize = 12;
            footerText.fontStyle = FontStyles.Italic;
            footerText.color = new Color(0.5f, 0.5f, 0.6f, 0.8f);
            footerText.alignment = TextAlignmentOptions.Center;
        }

        private System.Collections.IEnumerator FadeInCanvasGroup(CanvasGroup cg)
        {
            float duration = 0.15f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }
}
