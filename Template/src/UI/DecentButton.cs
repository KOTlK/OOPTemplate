using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public delegate void ButtonClick();

public enum ButtonClickType {
    OnDown    = 0,
    OnRelease = 1
}

public class DecentButton : UIElement,
                            IPointerDownHandler,
                            IPointerUpHandler,
                            IPointerEnterHandler,
                            IPointerExitHandler {
    public ButtonClick OnClick = delegate{};

    public ButtonClickType ClickType;
    [Header("Background Settings")]
    public Sprite          Background;
    public Color           Color;

    [Header("Text Settings")]
    public string          Text;
    public Color           TextColor;
    public TMP_FontAsset   TextFont;
    public float           TextSize;

    [Space(10)]
    [ReadOnly] public bool Clicked;
    [ReadOnly] public bool PointerInsideBounds;

    [Space(10)]
    [SerializeField] private Image    _backgroundImage;
    [SerializeField] private TMP_Text _text;

    private void OnValidate() {
        if(_backgroundImage != null)  {
            _backgroundImage.sprite = Background;
            _backgroundImage.color  = Color;
        }

        if(_text != null) {
            _text.text      = Text;
            _text.color     = TextColor;
            _text.font      = TextFont;
            _text.fontSize  = TextSize;
        }
    }

    public override void UpdateLate(float dt) {
        if(Clicked) {
            Clicked = false;
        }
    }

    public virtual void OnPointerDown(PointerEventData data) {
        if(ClickType == ButtonClickType.OnDown) {
            Clicked = true;
            OnClick();
        }
    }

    public virtual void OnPointerUp(PointerEventData data) {
        if(ClickType == ButtonClickType.OnRelease && PointerInsideBounds) {
            Clicked = true;
            OnClick();
        }
    }

    public virtual void OnPointerEnter(PointerEventData data) {
        PointerInsideBounds = true;
    }

    public virtual void OnPointerExit(PointerEventData data) {
        PointerInsideBounds = false;
    }
}