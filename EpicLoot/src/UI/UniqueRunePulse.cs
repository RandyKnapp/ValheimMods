using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>Makes a unique item's rune marker breathe: a slow swell in brightness.</summary>
public class UniqueRunePulse : MonoBehaviour
{
    private const float AlphaFloor = 0.7f;
    private const float Speed = 0.5f;

    private Image _image;
    private Color _base;

    /// <summary>Colours the marker and starts or stops its breathing.</summary>
    public static void Apply(Image marker, Color color, bool pulse)
    {
        UniqueRunePulse existing = marker.GetComponent<UniqueRunePulse>();
        if (!pulse)
        {
            if (existing != null)
            {
                existing.enabled = false;
            }

            marker.color = color;
            return;
        }

        if (existing == null)
        {
            existing = marker.gameObject.AddComponent<UniqueRunePulse>();
        }

        existing._image = marker;
        existing._base = color;
        existing.enabled = true;
    }

    private void LateUpdate()
    {
        if (_image == null)
        {
            return;
        }

        float swell = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Speed * 2f * Mathf.PI);
        Color color = _base;
        color.a *= AlphaFloor + (1f - AlphaFloor) * swell;
        _image.color = color;
    }

    private void OnDisable()
    {
        if (_image != null)
        {
            _image.color = _base;
        }
    }
}
