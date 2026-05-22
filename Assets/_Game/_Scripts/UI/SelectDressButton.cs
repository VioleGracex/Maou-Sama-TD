using UnityEngine;
using UnityEngine.UI;

public class SelectDressButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite newSprite;

    private Button _button;

    private void Start()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(ChangeSprite);
        }
    }

    private void ChangeSprite()
    {
        if (targetRenderer != null && newSprite != null)
        {
            targetRenderer.sprite = newSprite;
        }
        else
        {
            Debug.LogWarning("SelectDressButton: Target Renderer or New Sprite is missing.");
        }
    }

    // Public method in case they want to call it from Inspector events
    public void Click()
    {
        ChangeSprite();
    }
}