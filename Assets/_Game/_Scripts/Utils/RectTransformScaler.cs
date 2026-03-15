using UnityEngine;
using NaughtyAttributes;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class RectTransformScaler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("When enabled, resizing this RectTransform will automatically scale all children positions and sizes.")]
    [SerializeField] private bool _autoScaleChildren = false;
    
    [Header("Manual Scaling")]
    [Tooltip("The factor to multiply current size and child offsets by when clicking Apply.")]
    [SerializeField, Range(0.1f, 3f)] private float _manualScaleFactor = 0.9f;

    private RectTransform _rectTransform;
    private Vector2 _lastSize;
    private static bool _globalIsScaling = false;

    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
        _lastSize = _rectTransform.rect.size;
    }

    private void Update()
    {
        // Safeguard: Only auto-scale in the Editor unless explicitly forced
        #if !UNITY_EDITOR
        if (!_autoScaleChildren) return; 
        #endif
        
        if (_globalIsScaling || !_autoScaleChildren || _rectTransform == null) return;

        Vector2 currentSize = _rectTransform.rect.size;
        
        // Only scale if size actually changed significantly and isn't zero
        // Use a small epsilon to avoid floating point inaccuracy loops
        if (Vector2.Distance(currentSize, _lastSize) > 0.001f && _lastSize.x > 0.1f && _lastSize.y > 0.1f)
        {
            _globalIsScaling = true;
            try 
            {
                float ratioX = currentSize.x / _lastSize.x;
                float ratioY = currentSize.y / _lastSize.y;

                ScaleHierarchy(ratioX, ratioY);
            }
            finally 
            {
                _globalIsScaling = false;
            }
            
            _lastSize = currentSize;
        }
        else
        {
            _lastSize = currentSize;
        }
    }

    [Button("Apply Manual Scale Factor")]
    private void ApplyManualScale()
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        
#if UNITY_EDITOR
        // Record entire hierarchy for reliable undo
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Manual Scale RectTransform");
#endif

        // Scale self size
        _rectTransform.sizeDelta *= _manualScaleFactor;
        
        // Scale all children
        ScaleHierarchy(_manualScaleFactor, _manualScaleFactor);
        
        // Update last size to sync
        _lastSize = _rectTransform.rect.size;
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"<color=green>RectTransformScaler:</color> Applied <b>{_manualScaleFactor}x</b> scale to <b>{name}</b> and its children.");
    }

    [Button("Scale Down 10% (0.9x)")]
    private void QuickScaleDown()
    {
        _manualScaleFactor = 0.9f;
        ApplyManualScale();
    }

    [Button("Scale Up 10% (1.1x)")]
    private void QuickScaleUp()
    {
        _manualScaleFactor = 1.1f;
        ApplyManualScale();
    }

    private void ScaleHierarchy(float ratioX, float ratioY)
    {
        // Scale all descendants recursively
        RectTransform[] allChildren = GetComponentsInChildren<RectTransform>(true);
        
        foreach (RectTransform child in allChildren)
        {
            if (child == _rectTransform) continue;

            // 1. Scale anchored position relative to pivot
            Vector3 anchoredPos = child.anchoredPosition3D;
            anchoredPos.x *= ratioX;
            anchoredPos.y *= ratioY;
            child.anchoredPosition3D = anchoredPos;

            // 2. Scale sizeDelta
            Vector2 sizeDelta = child.sizeDelta;
            sizeDelta.x *= ratioX;
            sizeDelta.y *= ratioY;
            child.sizeDelta = sizeDelta;

            // 3. Scale Text Font Sizes (TMPro & Legacy)
            float fontRatio = (ratioX + ratioY) / 2f;
            if (child.GetComponent<TMPro.TMP_Text>() is TMPro.TMP_Text tmp)
            {
                tmp.fontSize *= fontRatio;
                if (tmp.enableAutoSizing)
                {
                    tmp.fontSizeMin *= fontRatio;
                    tmp.fontSizeMax *= fontRatio;
                }
            }
            else if (child.GetComponent<UnityEngine.UI.Text>() is UnityEngine.UI.Text legacyText)
            {
                legacyText.fontSize = Mathf.RoundToInt(legacyText.fontSize * fontRatio);
            }

            // 4. Scale Layout Groups
            if (child.GetComponent<UnityEngine.UI.LayoutGroup>() is UnityEngine.UI.LayoutGroup lg)
            {
                RectOffset padding = lg.padding;
                padding.left = Mathf.RoundToInt(padding.left * ratioX);
                padding.right = Mathf.RoundToInt(padding.right * ratioX);
                padding.top = Mathf.RoundToInt(padding.top * ratioY);
                padding.bottom = Mathf.RoundToInt(padding.bottom * ratioY);
                lg.padding = padding;

                if (lg is UnityEngine.UI.HorizontalOrVerticalLayoutGroup hvlg)
                {
                    hvlg.spacing *= ((ratioX + ratioY) / 2f);
                }
                else if (lg is UnityEngine.UI.GridLayoutGroup glg)
                {
                    Vector2 cellSize = glg.cellSize;
                    cellSize.x *= ratioX;
                    cellSize.y *= ratioY;
                    glg.cellSize = cellSize;
                    
                    Vector2 spacing = glg.spacing;
                    spacing.x *= ratioX;
                    spacing.y *= ratioY;
                    glg.spacing = spacing;
                }
            }

            // 5. Scale Layout Elements
            if (child.GetComponent<UnityEngine.UI.LayoutElement>() is UnityEngine.UI.LayoutElement le)
            {
                if (le.preferredWidth > 0) le.preferredWidth *= ratioX;
                if (le.preferredHeight > 0) le.preferredHeight *= ratioY;
                if (le.minWidth > 0) le.minWidth *= ratioX;
                if (le.minHeight > 0) le.minHeight *= ratioY;
            }
        }
    }
}

