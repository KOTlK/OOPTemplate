using UnityEngine;

[System.Flags]
public enum UIElementFlags : uint {
    None         = 0,
    Dynamic      = 0x1,
    UpdateLately = 0x2,
}

[RequireComponent(typeof(RectTransform))]
public class UIElement : MonoBehaviour {
    public uint           Id;
    public UIElementFlags Flags;
    public RectTransform  Transform;
    public Canvas         Canvas;

    public Vector3 WorldPosition    => transform.position;

    public Vector2 AnchoredPosition {
        get {
            return Transform.anchoredPosition;
        }
        set {
            Transform.anchoredPosition = value;
        }
    }

    // bottom-left = (0,0)
    // top-right   = (1,1);
    public Vector2 NormalizedCanvasPosition {
        get {
            var canvasRectTransform = (RectTransform)Canvas.transform;
            var width  = canvasRectTransform.rect.width;
            var height = canvasRectTransform.rect.height;
            var canvasPoint = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                Transform.position,
                null,
                out canvasPoint
            );

            canvasPoint.x += width / 2;
            canvasPoint.x /= width;
            canvasPoint.y += height / 2;
            canvasPoint.y /= height;

            return canvasPoint;
        }
        set {
            var pos                 = value;
            var canvasRectTransform = (RectTransform)Canvas.transform;
            var width               = canvasRectTransform.rect.width / 2;
            var height              = canvasRectTransform.rect.height / 2;
            pos.x = (pos.x * (width  - -width))  - width;
            pos.y = (pos.y * (height - -height)) - height;
            AnchoredPosition = pos;
        }
    }

    // Pixel position of the object on canvas
    public Vector2 PixelPosition {
        get {
            var canvasRectTransform = (RectTransform)Canvas.transform;
            var width               = canvasRectTransform.rect.width;
            var height              = canvasRectTransform.rect.height;
            var canvasPoint         = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                Transform.position,
                null,
                out canvasPoint
            );

            canvasPoint.x += width / 2;
            canvasPoint.y += height / 2;

            return canvasPoint;
        }
        set {
            var pos                 = value;
            var canvasRectTransform = (RectTransform)Canvas.transform;
            var width               = canvasRectTransform.rect.width;
            var height              = canvasRectTransform.rect.height;
            pos.x -= width / 2;
            pos.y -= height / 2;
            AnchoredPosition = pos;
        }
    }
    
    public virtual void ResolveDependencies(Container container) {
    }

    public virtual void UpdateElement(float dt) {
    }

    public virtual void UpdateLate(float dt) {
    }

    public virtual void OnElementCreate() {
    }

    public virtual void OnElementDestroy() {
    }
}