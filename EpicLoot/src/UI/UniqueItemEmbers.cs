using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Embers drifting up a unique item's inventory cell: a handful of soft motes in the unique colour that
/// rise, sway and fade, each restarting from a new spot when its life runs out.
/// </summary>
public class UniqueItemEmbers : UniqueCellEffect
{
    private const int MoteCount = 14;
    private const float Margin = 5f;
    private const float MinLife = 2.2f;
    private const float MaxLife = 3.8f;
    private const float MinSize = 1.3f;
    private const float MaxSize = 2.8f;
    private const float Sway = 3f;
    private const float Rise = 0.55f;
    private const float Brightness = 0.35f;
    private const float Alpha = 0.9f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;
        Vector2 center = rect.center;
        float time = Time.unscaledTime;

        Color core = Color.Lerp(Tint, Color.white, Brightness);
        Color rim = core;
        rim.a = 0f;

        for (int i = 0; i < MoteCount; i++)
        {
            float life = Mathf.Lerp(MinLife, MaxLife, Rand(i, 1));
            float clock = time + Rand(i, 2) * life;
            int cycle = Mathf.FloorToInt(clock / life);
            float age = clock / life - cycle;

            float x = Mathf.Lerp(-halfWidth + Margin, halfWidth - Margin, Rand(i, 3 + cycle * 7))
                + Sway * Mathf.Sin(age * 2f * Mathf.PI * (0.6f + Rand(i, 4)) + Rand(i, 5) * 6f);
            float travel = rect.height * Rise;
            float startY = -halfHeight + Margin + Rand(i, 6 + cycle * 7) * (rect.height - 2f * Margin - travel);
            float y = startY + age * travel;

            float fade = Mathf.Sin(age * Mathf.PI);
            float size = Mathf.Lerp(MinSize, MaxSize, Rand(i, 7)) * (1f - 0.3f * age);

            core.a = Tint.a * Alpha * fade;
            Vector2 p = center + new Vector2(x, y);
            int v = vh.currentVertCount;
            vh.AddVert(p, core, Vector2.zero);
            vh.AddVert(p + new Vector2(0f, size), rim, Vector2.zero);
            vh.AddVert(p + new Vector2(size, 0f), rim, Vector2.zero);
            vh.AddVert(p + new Vector2(0f, -size), rim, Vector2.zero);
            vh.AddVert(p + new Vector2(-size, 0f), rim, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
            vh.AddTriangle(v, v + 3, v + 4);
            vh.AddTriangle(v, v + 4, v + 1);
        }
    }

    private static float Rand(int mote, int salt)
    {
        uint h = (uint)(mote * 73856093) ^ (uint)(salt * 19349663) ^ 0x9E3779B9u;
        h ^= h >> 13;
        h *= 0x5bd1e995u;
        h ^= h >> 15;
        return (h & 0xFFFFFFu) / 16777216f;
    }
}
