using UnityEngine;
using Reflex.Attributes;

[RequireComponent(typeof(RectTransform))]
public class UIElement : Entity {
             public RectTransform  Transform;
    [Inject] public Canvas         Canvas;

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

    [Inject]
    protected override void Inject(EntityManager em) {
        // Do not inject default entity manager.
    }

    [Inject]
    private void Inject(UIEntityManager em) {
        Debug.Log("Inject into ui element");
        if (AutoBake) {
            em.BakeEntity(this);
        }
    }
}