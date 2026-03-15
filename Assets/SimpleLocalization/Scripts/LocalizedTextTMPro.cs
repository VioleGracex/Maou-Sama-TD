using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Assets.SimpleLocalization.Scripts
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextTMPro : MonoBehaviour
    {
        public string LocalizationKey;

        public void Start()
        {
            Localize();
            LocalizationManager.OnLocalizationChanged += Localize;
        }

        public void OnDestroy()
        {
            LocalizationManager.OnLocalizationChanged -= Localize;
        }

        private void Localize()
        {
            GetComponent<TextMeshProUGUI>().text = LocalizationManager.Localize(LocalizationKey);
        }
    }
}
