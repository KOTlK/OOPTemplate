using UnityEngine;
using Reflex.Attributes;
using System.Runtime.CompilerServices;

[RequireComponent(typeof(RectTransform))]
public class UIElement : MonoBehaviour {
    [ReadOnly] public string          AssetAddress;
    [ReadOnly] public uint            Id;
               public EntityFlags     Flags;
    [DontSave] public bool            AutoBake;
    [DontSave] public UIEntityManager Em;
               public RectTransform   Transform;
    [Inject]   public Canvas          Canvas;

    public Vector3 Position {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.position = value;
    }

    public Quaternion Rotation {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.rotation;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.rotation = value;
    }

    public Vector3 Scale {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.localScale;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.localScale = value;
    }

    public Matrix4x4 ObjectToWorld {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.localToWorldMatrix;
    }

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
    private void Inject(UIEntityManager em) {
        if (AutoBake) {
            em.BakeEntity(this);
        }
    }

    public virtual void OnBaking(){ }
    public virtual void OnCreate(){ }
    public virtual void UpdateEntity(){ }
    public virtual void Destroy() { }
    public virtual void OnBecameDynamic() { }
    public virtual void OnBecameStatic() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyThisEntity() {
        Em.DestroyEntity(Id);
    }

    public virtual void Hide() {
        gameObject.SetActive(false);
    }

    public virtual void Show() {
        gameObject.SetActive(true);
    }
}