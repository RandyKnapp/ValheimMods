using System;
using EpicLoot.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Drag-to-move for the trader panels (the adventure merchant and Hildir's tempering panel).
///
/// The panel only moves while the drag key (<see cref="ELConfig.TraderPanelDragKey"/>, Left Alt by
/// default) is held. The panel itself is not a drag target at all, so a press on its background or one
/// of its buttons can never nudge it, and moving the mouse a little while clicking a button no longer
/// turns the click into a drag. While the key is down, a transparent surface covers the whole panel,
/// lists and buttons included, and takes the drag. The four-way arrow handle in the top-right corner
/// drags the panel at any time, which is all that is left when the key is set to None.
/// </summary>
internal class TraderPanelDrag : MonoBehaviour
{
    private const float HandleSize = 26f;
    private const float HandleInset = 10f;
    private static readonly Color HandleColor = new Color(1f, 0.92f, 0.78f, 0.75f);
    private static readonly Color HandleActiveColor = new Color(1f, 0.75f, 0.35f, 1f);

    private static readonly Vector3[] Corners = new Vector3[4];
    private static Sprite _moveIcon;

    private RectTransform _panel;
    private RectTransform _handle;
    private Image _handleImage;
    private GameObject _modifierSurface;
    private Action<Vector2> _onMoved;

    private bool _handleHovered;
    private bool _dragging;
    private PointerEventData.InputButton _dragButton;
    private Vector2 _dragOffset;

    /// <summary>
    /// Makes the panel movable. <paramref name="onMoved"/> receives its anchoredPosition when a drag
    /// ends, to be saved; the panel is already there.
    /// </summary>
    public static void Attach(RectTransform panel, Action<Vector2> onMoved)
    {
        if (panel.GetComponent<TraderPanelDrag>() != null)
        {
            return;
        }

        TraderPanelDrag drag = panel.gameObject.AddComponent<TraderPanelDrag>();
        drag._panel = panel;
        drag._onMoved = onMoved;
        drag._modifierSurface = CreateModifierSurface(drag);
        drag._handle = CreateHandle(drag);
        drag._handleImage = drag._handle.GetComponent<Image>();
    }

    private void Update()
    {
        // Read every frame, so a change in Quick Configure applies at once. None is never held.
        bool modifierHeld = ZInput.GetKey(ELConfig.TraderPanelDragKey.Value, false);

        // Kept up until a drag started with the key ends, even if the key is let go partway through.
        bool surfaceActive = modifierHeld || _dragging;
        if (_modifierSurface.activeSelf != surfaceActive)
        {
            _modifierSurface.SetActive(surfaceActive);
            if (surfaceActive)
            {
                // Over everything the panel holds, including dialogs opened since, with the handle on top.
                _modifierSurface.transform.SetAsLastSibling();
                _handle.SetAsLastSibling();
            }
        }

        _handleImage.color = _handleHovered || _dragging || modifierHeld ? HandleActiveColor : HandleColor;
    }

    private void OnDisable()
    {
        // Closed mid-drag (the store shut, the player walked away): keep where it got to.
        if (_dragging)
        {
            EndDrag();
        }

        _handleHovered = false;
        if (_modifierSurface != null)
        {
            _modifierSurface.SetActive(false);
        }
    }

    private void BeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        RectTransform parent = (RectTransform)_panel.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            _dragOffset = _panel.anchoredPosition - localPoint;
            _dragButton = eventData.button;
            _dragging = true;
        }
    }

    private void Drag(PointerEventData eventData)
    {
        if (!_dragging || eventData.button != _dragButton)
        {
            return;
        }

        RectTransform parent = (RectTransform)_panel.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            _panel.anchoredPosition = localPoint + _dragOffset;
            KeepHandleOnScreen(eventData.pressEventCamera);
        }
    }

    private void EndDrag()
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _onMoved?.Invoke(_panel.anchoredPosition);
    }

    // The handle is the one part that always drags, so it must never leave the screen.
    private void KeepHandleOnScreen(Camera eventCamera)
    {
        _handle.GetWorldCorners(Corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, Corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, Corners[2]);

        Vector2 shift = Vector2.zero;
        if (min.x < 0f)
        {
            shift.x = -min.x;
        }
        else if (max.x > Screen.width)
        {
            shift.x = Screen.width - max.x;
        }

        if (min.y < 0f)
        {
            shift.y = -min.y;
        }
        else if (max.y > Screen.height)
        {
            shift.y = Screen.height - max.y;
        }

        if (shift == Vector2.zero)
        {
            return;
        }

        RectTransform parent = (RectTransform)_panel.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, min, eventCamera, out Vector2 from) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, min + shift, eventCamera, out Vector2 to))
        {
            _panel.anchoredPosition += to - from;
        }
    }

    private static GameObject CreateModifierSurface(TraderPanelDrag owner)
    {
        RectTransform rect = CreateChild("ModifierDragSurface", owner._panel);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // A hit area only: clear, but still a raycast target.
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        rect.gameObject.AddComponent<DragSurface>().Owner = owner;
        rect.gameObject.SetActive(false);
        return rect.gameObject;
    }

    private static RectTransform CreateHandle(TraderPanelDrag owner)
    {
        RectTransform rect = CreateChild("MoveHandle", owner._panel);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-HandleInset, -HandleInset);
        rect.sizeDelta = new Vector2(HandleSize, HandleSize);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = MoveIcon;
        image.preserveAspect = true;
        image.color = HandleColor;
        image.raycastTarget = true;

        DragSurface surface = rect.gameObject.AddComponent<DragSurface>();
        surface.Owner = owner;
        surface.IsHandle = true;
        return rect;
    }

    private static RectTransform CreateChild(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private class DragSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public TraderPanelDrag Owner;
        public bool IsHandle;

        public void OnBeginDrag(PointerEventData eventData) => Owner.BeginDrag(eventData);

        public void OnDrag(PointerEventData eventData) => Owner.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == Owner._dragButton)
            {
                Owner.EndDrag();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsHandle)
            {
                Owner._handleHovered = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsHandle)
            {
                Owner._handleHovered = false;
            }
        }
    }

    // A four-way "move" arrow, drawn once at runtime rather than shipped in the asset bundle: white so
    // the Image color tints it, with a dark outline so it reads on any panel background.
    private static Sprite MoveIcon
    {
        get
        {
            if (_moveIcon == null)
            {
                _moveIcon = BuildMoveIcon();
            }

            return _moveIcon;
        }
    }

    // In icon units, [-1, 1] across the texture: one arm pointing right, rotated for the other three.
    private static readonly Vector2[] ArrowHead = { new(0.52f, -0.27f), new(0.9f, 0f), new(0.52f, 0.27f) };
    private static readonly Vector2[] ArrowShaft = { new(0f, -0.08f), new(0.56f, -0.08f), new(0.56f, 0.08f), new(0f, 0.08f) };
    private const float IconOutline = 0.08f;
    private const float IconOutlineAlpha = 0.7f;

    private static Sprite BuildMoveIcon()
    {
        const int size = 64;
        float texel = 2f / size;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) * texel - 1f, (y + 0.5f) * texel - 1f);
                float distance = MoveIconDistance(point);

                // Anti-aliased over one texel: a white fill composited over a translucent black outline.
                float fill = Mathf.Clamp01(0.5f - distance / texel);
                float outline = Mathf.Clamp01(0.5f - (distance - IconOutline) / texel) * IconOutlineAlpha;
                float alpha = fill + outline * (1f - fill);
                float shade = alpha > 0f ? fill / alpha : 0f;
                pixels[y * size + x] = new Color(shade, shade, shade, alpha);
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "EpicLoot_MoveIcon",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Distance to the nearest arm, negative inside.
    private static float MoveIconDistance(Vector2 point)
    {
        float best = float.MaxValue;
        for (int arm = 0; arm < 4; arm++)
        {
            best = Mathf.Min(best, ConvexDistance(point, ArrowHead));
            best = Mathf.Min(best, ConvexDistance(point, ArrowShaft));
            point = new Vector2(-point.y, point.x);
        }

        return best;
    }

    // Signed distance to a counter-clockwise convex polygon. Inside it is the (negative) distance past
    // the nearest edge line. Outside it is the true distance to the outline: the edge-line distance
    // alone would give the outline mitred spikes at the heads' sharp base corners, bridging the arms.
    private static float ConvexDistance(Vector2 point, Vector2[] polygon)
    {
        float inside = float.MinValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];
            Vector2 normal = new Vector2(b.y - a.y, a.x - b.x).normalized;
            inside = Mathf.Max(inside, Vector2.Dot(point - a, normal));
        }

        if (inside <= 0f)
        {
            return inside;
        }

        float outside = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 edge = polygon[(i + 1) % polygon.Length] - a;
            float along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            outside = Mathf.Min(outside, Vector2.Distance(point, a + along * edge));
        }

        return outside;
    }
}
