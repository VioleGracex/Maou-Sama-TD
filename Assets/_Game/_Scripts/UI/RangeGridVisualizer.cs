using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    public class RangeGridVisualizer : MonoBehaviour
    {
        [SerializeField] private Color _selfColor = new Color(0f, 0.4f, 1f, 1f); // Blue
        [SerializeField] private Color _rangeColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gray/Transparent

        private Image[] _tiles;
        private const int GRID_SIZE = 7;
        private const int CENTER_INDEX = 24;

        private void Awake()
        {
            _tiles = new Image[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                _tiles[i] = transform.GetChild(i).GetComponent<Image>();
            }
        }

        public void Visualize(AttackPattern pattern, float range)
        {
            if (_tiles == null || _tiles.Length != 49) return;

            int rangeInt = Mathf.CeilToInt(range);

            for (int i = 0; i < 49; i++)
            {
                if (i == CENTER_INDEX)
                {
                    _tiles[i].color = _selfColor;
                    continue;
                }

                int x = (i % GRID_SIZE) - (GRID_SIZE / 2);
                int y = (GRID_SIZE / 2) - (i / GRID_SIZE); // Y is usually inverted in UI layouts (top-down), so center is 0,0

                bool inRange = IsInPattern(x, y, pattern, rangeInt);
                _tiles[i].color = inRange ? _rangeColor : _emptyColor;
            }
        }

        private bool IsInPattern(int x, int y, AttackPattern pattern, int range)
        {
            if (pattern == AttackPattern.Vertical)
            {
                return x == 0 && Mathf.Abs(y) <= range;
            }
            if (pattern == AttackPattern.Horizontal)
            {
                return y == 0 && Mathf.Abs(x) <= range;
            }
            if (pattern == AttackPattern.Cross)
            {
                return (x == 0 && Mathf.Abs(y) <= range) || (y == 0 && Mathf.Abs(x) <= range);
            }
            if (pattern == AttackPattern.Diagonal)
            {
                return Mathf.Abs(x) == Mathf.Abs(y) && Mathf.Abs(x) <= range;
            }
            if (pattern == AttackPattern.All)
            {
                return Mathf.Abs(x) <= range && Mathf.Abs(y) <= range;
            }
            return false;
        }
    }
}
