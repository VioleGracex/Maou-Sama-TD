using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.UI
{
    public class ExitGameAction : MonoBehaviour
    {
        private void Start()
        {
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(QuitGame);
            }
        }

        public void QuitGame()
        {
            Debug.Log("[ExitGameAction] Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
