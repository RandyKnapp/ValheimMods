using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>A solid border in the unique colour laid over the rim of the cell's rarity background.</summary>
public class UniqueItemBorder : UniqueCellEffect
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        BuildRoundedRect(rect.width * 0.5f, rect.height * 0.5f, RimRadius);
        Vector2 center = rect.center;

        foreach (Sample sample in Samples)
        {
            vh.AddVert(center + sample.Point, Tint, Vector2.zero);
            vh.AddVert(center + sample.Point + sample.Inward * RimWidth, Tint, Vector2.zero);
        }

        AddRingTriangles(vh, 2);
    }
}
