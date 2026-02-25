using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.UI
{
    public class RangePatternUI : MonoBehaviour
    {
        [Header("Grid Setup")]
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private Image[] _tileImages; // Should be 25 images for a 5x5 grid
        
        [Header("Colors")]
        [SerializeField] private Color _unitColor = Color.yellow;
        [SerializeField] private Color _rangeColor = Color.cyan;
        [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        private const int GridSize = 5;
        private const int CenterIndex = 12; // (2, 2) in a 0-indexed 5x5 grid

        public void SetPattern(AttackPattern pattern, int range)
        {
            if (_tileImages == null || _tileImages.Length < 25) return;

            // Reset all
            for (int i = 0; i < _tileImages.Length; i++)
            {
                _tileImages[i].color = _emptyColor;
            }

            // Set Center (Unit)
            _tileImages[CenterIndex].color = _unitColor;

            // Calculate active tiles based on pattern and range
            for (int r = 1; r <= range; r++)
            {
                switch (pattern)
                {
                    case AttackPattern.Vertical:
                        SetTile(0, r, _rangeColor);
                        SetTile(0, -r, _rangeColor);
                        break;
                    case AttackPattern.Horizontal:
                        SetTile(r, 0, _rangeColor);
                        SetTile(-r, 0, _rangeColor);
                        break;
                    case AttackPattern.Diagonal:
                        SetTile(r, r, _rangeColor);
                        SetTile(-r, -r, _rangeColor);
                        SetTile(r, -r, _rangeColor);
                        SetTile(-r, r, _rangeColor);
                        break;
                    case AttackPattern.Cross:
                        SetTile(0, r, _rangeColor);
                        SetTile(0, -r, _rangeColor);
                        SetTile(r, 0, _rangeColor);
                        SetTile(-r, 0, _rangeColor);
                        break;
                    case AttackPattern.All:
                        for (int x = -r; x <= r; x++)
                        {
                            for (int y = -r; y <= r; y++)
                            {
                                if (x == 0 && y == 0) continue;
                                SetTile(x, y, _rangeColor);
                            }
                        }
                        break;
                }
            }
        }

        private void SetTile(int offsetX, int offsetY, Color color)
        {
            int centerX = 2;
            int centerY = 2;
            
            int x = centerX + offsetX;
            int y = centerY - offsetY; // UI grid usually grows top-to-bottom

            if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
            {
                int index = y * GridSize + x;
                if (index >= 0 && index < _tileImages.Length)
                {
                    _tileImages[index].color = color;
                }
            }
        }
    }
}
