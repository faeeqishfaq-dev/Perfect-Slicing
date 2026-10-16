using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class ComboMeterUI : MonoBehaviour
    {
        [Header("Meter Segments")]
        [SerializeField] [Tooltip("The vertical rectangular segment fill images from bottom to top")]
        private List<Image> segmentFills = new List<Image>();

        [Header("Color Progression")]
        [SerializeField] private Color[] multiplierColors = new Color[]
        {
            new Color(0.2f, 0.85f, 0.3f), // x1 Green
            new Color(0.95f, 0.85f, 0.1f), // x2 Yellow
            new Color(1.0f, 0.55f, 0.1f),  // x3 Orange
            new Color(0.3f, 0.7f, 1.0f),  // x4 Blue
            new Color(0.85f, 0.25f, 1.0f) // x5 Purple
        };

        private void Start()
        {
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.OnComboUpdated -= HandleComboUpdated;
                ComboManager.Instance.OnComboUpdated += HandleComboUpdated;
                UpdateMeter(ComboManager.Instance.CurrentMultiplier, ComboManager.Instance.MeterProgress);
            }
            else
            {
                UpdateMeter(1, 0f);
            }
        }

        private void OnEnable()
        {
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.OnComboUpdated -= HandleComboUpdated;
                ComboManager.Instance.OnComboUpdated += HandleComboUpdated;
                UpdateMeter(ComboManager.Instance.CurrentMultiplier, ComboManager.Instance.MeterProgress);
            }
        }

        private void OnDisable()
        {
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.OnComboUpdated -= HandleComboUpdated;
            }
        }

        private void HandleComboUpdated(int multiplier, float meterFill01)
        {
            UpdateMeter(multiplier, meterFill01);
        }

        public void UpdateMeter(int multiplier, float meterFill01)
        {
            if (segmentFills == null || segmentFills.Count == 0) return;

            int colorIndex = Mathf.Clamp(multiplier - 1, 0, multiplierColors.Length - 1);
            Color activeColor = multiplierColors[colorIndex];

            int totalSegments = segmentFills.Count;
            float filledSegmentsFloat = meterFill01 * totalSegments;

            for (int i = 0; i < totalSegments; i++)
            {
                Image seg = segmentFills[i];
                if (seg == null) continue;

                if (i < Mathf.FloorToInt(filledSegmentsFloat))
                {
                    seg.color = activeColor;
                    seg.fillAmount = 1f;
                }
                else if (i == Mathf.FloorToInt(filledSegmentsFloat))
                {
                    float partial = filledSegmentsFloat - i;
                    seg.color = activeColor;
                    seg.fillAmount = partial;
                }
                else
                {
                    seg.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0.15f);
                    seg.fillAmount = 1f;
                }
            }
        }
    }
}
