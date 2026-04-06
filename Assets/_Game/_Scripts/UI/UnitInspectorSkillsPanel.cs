using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles skill slot iconography in the unit inspector.
    /// </summary>
    public class UnitInspectorSkillsPanel : MonoBehaviour
    {
        [Header("Skill Slots")]
        [SerializeField] private Image[] _skillSlots;

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            RefreshSkillSlot(0, u.PassiveSkill);
            RefreshSkillSlot(1, u.ActiveSkill);
            RefreshSkillSlot(2, u.UltimateSkill);
        }

        private void RefreshSkillSlot(int index, MaouSamaTD.Skills.UnitSkillData data)
        {
            if (_skillSlots == null || index < 0 || index >= _skillSlots.Length) return;
            
            bool hasSkill = data != null;
            _skillSlots[index].gameObject.SetActive(true); // Keep slot on, just toggle icon
            if (hasSkill)
            {
                _skillSlots[index].sprite = data.Icon;
                _skillSlots[index].color = Color.white;
            }
            else
            {
                _skillSlots[index].sprite = null;
                _skillSlots[index].color = new Color(0,0,0,0);
            }
        }
    }
}
