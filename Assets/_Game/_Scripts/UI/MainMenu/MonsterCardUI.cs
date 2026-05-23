using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI.MainMenu
{
    public class MonsterCardUI : MonoBehaviour
    {
        [SerializeField] private Image _chibiImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _rankBadgeText;
        [SerializeField] private TextMeshProUGUI _moveBadgeText;
        [SerializeField] private TextMeshProUGUI _statsText;

        public void Setup(MaouSamaTD.Units.EnemyData enemy)
        {
            if (enemy == null) return;

            // Set Image
            if (_chibiImage != null)
            {
                _chibiImage.sprite = enemy.EnemySprite != null ? enemy.EnemySprite : enemy.FullBodyArt;
                _chibiImage.preserveAspect = true;
            }

            // Set Name
            if (_nameText != null)
            {
                _nameText.text = enemy.EnemyName;
            }

            // Set Rank Badge
            if (_rankBadgeText != null)
            {
                Image bgImage = _rankBadgeText.GetComponentInParent<Image>();
                Outline borderOutline = _rankBadgeText.GetComponentInParent<Outline>();
                
                string badgeText = "NORMAL";
                Color textColor = new Color(0.8f, 0.8f, 0.8f, 1f); // #CCCCCC
                Color bgColor = new Color(0.73f, 0.73f, 0.73f, 0.1f); // rgba(187, 187, 187, 0.1)
                Color borderColor = new Color(0.73f, 0.73f, 0.73f, 0.2f); // rgba(187, 187, 187, 0.2)

                if (enemy.IsBoss || enemy.Rank == MaouSamaTD.Units.EnemyRank.Boss)
                {
                    badgeText = "BOSS";
                    textColor = new Color(1f, 0.3f, 0.3f, 1f); // #FF4C4C
                    bgColor = new Color(1f, 0.3f, 0.3f, 0.15f); // rgba(255, 76, 76, 0.15)
                    borderColor = new Color(1f, 0.3f, 0.3f, 0.35f); // rgba(255, 76, 76, 0.35)
                }
                else if (enemy.Rank == MaouSamaTD.Units.EnemyRank.Elite)
                {
                    badgeText = "ELITE";
                    textColor = new Color(0.85f, 0.35f, 1f, 1f); // #D959FF
                    bgColor = new Color(0.85f, 0.35f, 1f, 0.15f); // rgba(217, 89, 255, 0.15)
                    borderColor = new Color(0.85f, 0.35f, 1f, 0.3f); // rgba(217, 89, 255, 0.3)
                }

                _rankBadgeText.text = badgeText;
                _rankBadgeText.color = textColor;

                if (bgImage != null)
                {
                    bgImage.color = bgColor;
                }
                if (borderOutline != null)
                {
                    borderOutline.effectColor = borderColor;
                    borderOutline.effectDistance = new Vector2(1f, 1f);
                }
            }

            // Set Movement Badge
            if (_moveBadgeText != null)
            {
                Image bgImage = _moveBadgeText.GetComponentInParent<Image>();
                Outline borderOutline = _moveBadgeText.GetComponentInParent<Outline>();

                string badgeText = "GROUND";
                Color textColor = new Color(0f, 1f, 0.8f, 1f); // #00FFCC
                Color bgColor = new Color(0f, 1f, 0.8f, 0.1f); // rgba(0, 255, 204, 0.1)
                Color borderColor = new Color(0f, 1f, 0.8f, 0.2f); // rgba(0, 255, 204, 0.2)

                if (enemy.MovementType == MaouSamaTD.Units.EnemyMovementType.Flying)
                {
                    badgeText = "FLYING";
                    textColor = new Color(0.1f, 0.8f, 1f, 1f); // #19CCFF
                    bgColor = new Color(0.1f, 0.8f, 1f, 0.1f); // rgba(25, 204, 255, 0.1)
                    borderColor = new Color(0.1f, 0.8f, 1f, 0.2f); // rgba(25, 204, 255, 0.2)
                }
                else if (enemy.MovementType == MaouSamaTD.Units.EnemyMovementType.Mixed)
                {
                    badgeText = "HIGH GROUND";
                    textColor = new Color(1f, 0.6f, 0.2f, 1f); // #FF9933
                    bgColor = new Color(1f, 0.6f, 0.2f, 0.1f); // rgba(255, 153, 51, 0.1)
                    borderColor = new Color(1f, 0.6f, 0.2f, 0.25f); // rgba(255, 153, 51, 0.25)
                }
                else if (enemy.CollisionType == MaouSamaTD.Units.EnemyCollisionType.IgnoreUnits || enemy.EvasionType == MaouSamaTD.Units.EnemyEvasionType.BypassBlockers)
                {
                    badgeText = "PHASING";
                    textColor = new Color(1f, 0.2f, 0.6f, 1f); // #FF3399
                    bgColor = new Color(1f, 0.2f, 0.6f, 0.1f);
                    borderColor = new Color(1f, 0.2f, 0.6f, 0.2f);
                }

                _moveBadgeText.text = badgeText;
                _moveBadgeText.color = textColor;

                if (bgImage != null)
                {
                    bgImage.color = bgColor;
                }
                if (borderOutline != null)
                {
                    borderOutline.effectColor = borderColor;
                    borderOutline.effectDistance = new Vector2(1f, 1f);
                }
            }

            // Set Stats
            if (_statsText != null)
            {
                _statsText.text = $"<color=#888888>HP: <color=white>{enemy.MaxHp}</color>  |  Speed: <color=white>{enemy.MoveSpeed:F1}</color>  |  Power: <color=white>{enemy.AttackPower:F1}</color></color>";
            }
        }
    }
}
