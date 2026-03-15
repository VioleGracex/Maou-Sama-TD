using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.Gacha
{
    public class SoulIntentPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private List<Button> _soulButtons;
        [SerializeField] private Button _btnDismiss;

        public event System.Action<UnitData> OnSoulChosen;

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void SelectSoul(UnitData data)
        {
            OnSoulChosen?.Invoke(data);
            Close();
        }
    }
}
