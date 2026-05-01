using UnityEngine;

namespace MaouSamaTD.Managers
{
    public class UltimateCutInManager : MonoBehaviour
    {
        public static UltimateCutInManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void PlayCutIn(string unitName, string unitTitle, string skillName, 
            Color bannerColor, Color titleBgColor, Color titleTextColor, 
            Color skillBgColor, Color skillTextColor, Color nameTextColor, Sprite portrait, System.Action onComplete = null)
        {
            if (MaouSamaTD.UI.UltimateCutInUI.Instance != null)
            {
                StartCoroutine(MaouSamaTD.UI.UltimateCutInUI.Instance.PlayAnimation(unitName, unitTitle, skillName, bannerColor, titleBgColor, titleTextColor, skillBgColor, skillTextColor, nameTextColor, portrait));
            }
        }
    }
}
