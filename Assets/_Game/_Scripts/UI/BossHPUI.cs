using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    public class BossHPUI : MonoBehaviour
    {
        [SerializeField] private GameObject _container;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _hpValueText;

        private EnemyUnit _activeBoss;

        private void Start()
        {
            if (_container != null) _container.SetActive(false);
        }

        private void Update()
        {
            if (_activeBoss == null || _activeBoss.IsDead)
            {
                FindActiveBoss();
            }

            UpdateUI();
        }

        private void FindActiveBoss()
        {
            var enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e.EnemyData != null && e.EnemyData.IsBoss && !e.IsDead)
                {
                    _activeBoss = e;
                    if (_container != null) _container.SetActive(true);
                    if (_bossNameText != null) _bossNameText.text = e.EnemyData.EnemyName.ToUpper();
                    return;
                }
            }

            if (_container != null) _container.SetActive(false);
        }

        private void UpdateUI()
        {
            if (_activeBoss == null || _activeBoss.IsDead)
            {
                if (_container != null && _container.activeSelf) _container.SetActive(false);
                return;
            }

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = _activeBoss.CurrentHp / _activeBoss.MaxHp;
            }

            if (_hpValueText != null)
            {
                _hpValueText.text = $"{Mathf.CeilToInt(_activeBoss.CurrentHp)} / {Mathf.CeilToInt(_activeBoss.MaxHp)}";
            }
        }
    }
}
