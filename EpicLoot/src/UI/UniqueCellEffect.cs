using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// A decoration drawn into a unique item's inventory cell as vertex-coloured geometry over the canvas's
/// white texture: no sprite, any tint, redrawn every frame while enabled.
/// </summary>
public abstract class UniqueCellEffect : MaskableGraphic
{
    // The rarity background sprite has a 2px rim with ~5px rounded corners; the effects follow that shape.
    protected const float RimWidth = 2f;
    protected const float RimRadius = 5f;

    private const int SamplesPerEdge = 12;
    private const int SamplesPerCorner = 5;

    protected struct Sample
    {
        public Vector2 Point;
        public Vector2 Inward;
        /// <summary>The fraction of the perimeter walked to reach this sample.</summary>
        public float P;
    }

    protected static readonly List<Sample> Samples = new List<Sample>();

    private Color _tint = Color.white;

    public Color Tint
    {
        get => _tint;
        set
        {
            if (_tint == value)
            {
                return;
            }

            _tint = value;
            SetVerticesDirty();
        }
    }

    public static T Create<T>(Transform parent, string name, float size) where T : UniqueCellEffect
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(T));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);

        T effect = go.GetComponent<T>();
        effect.raycastTarget = false;
        return effect;
    }

    private void Update()
    {
        SetVerticesDirty();
    }

    // Walks a rounded rectangle clockwise from the start of its top edge into Samples: each straight
    // edge, then the quarter arc after it.
    protected static void BuildRoundedRect(float halfWidth, float halfHeight, float radius)
    {
        Samples.Clear();
        radius = Mathf.Min(radius, halfWidth, halfHeight);
        float edgeX = 2f * (halfWidth - radius);
        float edgeY = 2f * (halfHeight - radius);
        float arc = Mathf.PI * 0.5f * radius;
        float total = 2f * edgeX + 2f * edgeY + 4f * arc;
        float walked = 0f;

        Vector2[] corners =
        {
            new Vector2(halfWidth - radius, halfHeight - radius),
            new Vector2(halfWidth - radius, -(halfHeight - radius)),
            new Vector2(-(halfWidth - radius), -(halfHeight - radius)),
            new Vector2(-(halfWidth - radius), halfHeight - radius)
        };
        Vector2[] edgeStarts =
        {
            new Vector2(-(halfWidth - radius), halfHeight),
            new Vector2(halfWidth, halfHeight - radius),
            new Vector2(halfWidth - radius, -halfHeight),
            new Vector2(-halfWidth, -(halfHeight - radius))
        };
        Vector2[] edgeDirs = { Vector2.right, Vector2.down, Vector2.left, Vector2.up };
        Vector2[] inwards = { Vector2.down, Vector2.left, Vector2.up, Vector2.right };
        float[] edgeLengths = { edgeX, edgeY, edgeX, edgeY };

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < SamplesPerEdge; i++)
            {
                float u = (float)i / SamplesPerEdge;
                Samples.Add(new Sample
                {
                    Point = edgeStarts[side] + edgeDirs[side] * (edgeLengths[side] * u),
                    Inward = inwards[side],
                    P = (walked + edgeLengths[side] * u) / total
                });
            }

            walked += edgeLengths[side];

            float from = Mathf.Atan2(-inwards[side].y, -inwards[side].x);
            for (int i = 0; i < SamplesPerCorner; i++)
            {
                float u = (float)i / SamplesPerCorner;
                float angle = from - Mathf.PI * 0.5f * u;
                Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Samples.Add(new Sample
                {
                    Point = corners[side] + outward * radius,
                    Inward = -outward,
                    P = (walked + arc * u) / total
                });
            }

            walked += arc;
        }
    }

    /// <summary>Joins consecutive samples into a closed strip of quads, <paramref name="ringVerts"/> vertices per sample.</summary>
    protected static void AddRingTriangles(VertexHelper vh, int ringVerts)
    {
        int count = Samples.Count;
        for (int i = 0; i < count; i++)
        {
            int a = i * ringVerts;
            int b = ((i + 1) % count) * ringVerts;
            for (int r = 0; r < ringVerts - 1; r++)
            {
                vh.AddTriangle(a + r, b + r, b + r + 1);
                vh.AddTriangle(a + r, b + r + 1, a + r + 1);
            }
        }
    }
}
