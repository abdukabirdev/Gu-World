// Hand-written stubs of the parts of UGUI the game uses, for the compile check only
// (Tools/V2/check.sh unity). Not part of the game: Unity has the real package.
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour
    {
        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void Start() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnRectTransformDimensionsChange() { }
        protected virtual void OnTransformParentChanged() { }
        protected virtual void OnDidApplyAnimationProperties() { }
        protected virtual void OnCanvasGroupChanged() { }
        public virtual bool IsActive() => true;
        public bool IsDestroyed() => false;
    }

    public abstract class BaseEventData
    {
        public bool used => false;
        public void Use() { }
    }

    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left, Right, Middle }
        public InputButton button;
        public Vector2 position;
        public Vector2 delta;
        public Vector2 pressPosition;
        public Vector2 scrollDelta;
        public int clickCount;
        public bool dragging;
        public GameObject pointerEnter;
        public GameObject pointerPress;
        public GameObject pointerDrag;
        public Camera pressEventCamera;
        public Camera enterEventCamera;
        public PointerEventData(EventSystem system) { }
    }

    public class AxisEventData : BaseEventData { }

    public interface IEventSystemHandler { }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IDropHandler : IEventSystemHandler { void OnDrop(PointerEventData eventData); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData eventData); }
    public interface ISubmitHandler : IEventSystemHandler { void OnSubmit(BaseEventData eventData); }

    public class RaycastResult
    {
        public GameObject gameObject;
    }

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public GameObject currentSelectedGameObject => null;
        public void SetSelectedGameObject(GameObject selected) { }
        public bool IsPointerOverGameObject() => false;
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults) { }
    }

    public abstract class BaseInputModule : UIBehaviour { }
    public abstract class BaseRaycaster : UIBehaviour { }
}

namespace UnityEngine.UI
{
    public interface ILayoutElement
    {
        void CalculateLayoutInputHorizontal();
        void CalculateLayoutInputVertical();
        float minWidth { get; }
        float preferredWidth { get; }
        float flexibleWidth { get; }
        float minHeight { get; }
        float preferredHeight { get; }
        float flexibleHeight { get; }
        int layoutPriority { get; }
    }

    public interface IMeshModifier
    {
        void ModifyMesh(Mesh mesh);
        void ModifyMesh(VertexHelper verts);
    }

    public class VertexHelper : IDisposable
    {
        public int currentVertCount => 0;
        public int currentIndexCount => 0;
        public void Clear() { }
        public void PopulateUIVertex(ref UIVertex vertex, int i) { }
        public void SetUIVertex(UIVertex vertex, int i) { }
        public void AddVert(Vector3 position, Color32 color, Vector2 uv0) { }
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0) { }
        public void AddVert(UIVertex v) { }
        public void AddTriangle(int idx0, int idx1, int idx2) { }
        public void AddUIVertexQuad(UIVertex[] verts) { }
        public void AddUIVertexStream(List<UIVertex> verts, List<int> indices) { }
        public void AddUIVertexTriangleStream(List<UIVertex> verts) { }
        public void GetUIVertexStream(List<UIVertex> stream) { }
        public void FillMesh(Mesh mesh) { }
        public void Dispose() { }
    }

    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
        public virtual Material material { get; set; }
        public virtual Material materialForRendering => null;
        public virtual Material defaultMaterial => null;
        public virtual Texture mainTexture => null;
        public RectTransform rectTransform => null;
        public Canvas canvas => null;
        public CanvasRenderer canvasRenderer => null;
        public int depth => 0;
        public virtual void SetAllDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void SetMaterialDirty() { }
        public virtual void SetLayoutDirty() { }
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
        protected virtual void UpdateGeometry() { }
        public virtual void SetNativeSize() { }
        public Rect GetPixelAdjustedRect() => default;
    }

    public abstract class MaskableGraphic : Graphic
    {
        public bool maskable { get; set; }
    }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public Sprite sprite { get; set; }
        public Sprite overrideSprite { get; set; }
        public Type type { get; set; }
        public bool preserveAspect { get; set; }
        public bool fillCenter { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
        public bool fillClockwise { get; set; }
        public int fillOrigin { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public float alphaHitTestMinimumThreshold { get; set; }
        public bool useSpriteMesh { get; set; }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; }
    }

    public class Text : MaskableGraphic, ILayoutElement
    {
        public Font font { get; set; }
        public virtual string text { get; set; }
        public bool supportRichText { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public int fontSize { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
        public FontStyle fontStyle { get; set; }
        public float pixelsPerUnit => 1;
        public TextGenerator cachedTextGenerator => null;
        public TextGenerator cachedTextGeneratorForLayout => null;
        public TextGenerationSettings GetGenerationSettings(Vector2 extents) => default;
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth => 0;
        public virtual float preferredWidth => 0;
        public virtual float flexibleWidth => 0;
        public virtual float minHeight => 0;
        public virtual float preferredHeight => 0;
        public virtual float flexibleHeight => 0;
        public virtual int layoutPriority => 0;
    }

    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        protected Graphic graphic => null;
        public virtual void ModifyMesh(Mesh mesh) { }
        public abstract void ModifyMesh(VertexHelper vh);
    }

    public class Shadow : BaseMeshEffect
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
        public bool useGraphicAlpha { get; set; }
        public override void ModifyMesh(VertexHelper vh) { }
    }

    public class Outline : Shadow { }

    public class Mask : UIBehaviour
    {
        public bool showMaskGraphic { get; set; }
    }

    public class RectMask2D : UIBehaviour
    {
        public Vector4 padding { get; set; }
        public Vector2Int softness { get; set; }
    }

    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float scaleFactor { get; set; }
        public float referencePixelsPerUnit { get; set; }
        public float dynamicPixelsPerUnit { get; set; }
    }

    public class GraphicRaycaster : BaseRaycaster
    {
        public bool ignoreReversedGraphics { get; set; }
    }

    public class LayoutElement : UIBehaviour, ILayoutElement
    {
        public virtual bool ignoreLayout { get; set; }
        public virtual float minWidth { get; set; }
        public virtual float minHeight { get; set; }
        public virtual float preferredWidth { get; set; }
        public virtual float preferredHeight { get; set; }
        public virtual float flexibleWidth { get; set; }
        public virtual float flexibleHeight { get; set; }
        public virtual int layoutPriority { get; set; }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
    }

    public abstract class LayoutGroup : UIBehaviour, ILayoutElement
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
        public virtual float minWidth => 0;
        public virtual float preferredWidth => 0;
        public virtual float flexibleWidth => 0;
        public virtual float minHeight => 0;
        public virtual float preferredHeight => 0;
        public virtual float flexibleHeight => 0;
        public virtual int layoutPriority => 0;
        public virtual void CalculateLayoutInputHorizontal() { }
        public abstract void CalculateLayoutInputVertical();
        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();
    }

    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childScaleWidth { get; set; }
        public bool childScaleHeight { get; set; }
        public bool reverseArrangement { get; set; }
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight }
        public enum Axis { Horizontal, Vertical }
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Corner startCorner { get; set; }
        public Axis startAxis { get; set; }
        public Vector2 cellSize { get; set; }
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; }
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }

    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot) { }
        public static void MarkLayoutForRebuild(RectTransform rect) { }
    }

    public static class LayoutUtility
    {
        public static float GetPreferredWidth(RectTransform rect) => 0;
        public static float GetPreferredHeight(RectTransform rect) => 0;
        public static float GetMinWidth(RectTransform rect) => 0;
        public static float GetMinHeight(RectTransform rect) => 0;
    }

    [Serializable]
    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
        public static ColorBlock defaultColorBlock => default;
    }

    public struct Navigation
    {
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get; set; }
    }

    public class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable { get; set; }
        public Transition transition { get; set; }
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
        public Image image { get; set; }
        public Navigation navigation { get; set; }
        public virtual void Select() { }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
    }

    public class Toggle : Selectable
    {
        public class ToggleEvent : UnityEvent<bool> { }
        public bool isOn { get; set; }
        public Graphic graphic { get; set; }
        public ToggleEvent onValueChanged = new ToggleEvent();
        public void SetIsOnWithoutNotify(bool value) { }
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public bool wholeNumbers { get; set; }
        public virtual float value { get; set; }
        public float normalizedValue { get; set; }
        public SliderEvent onValueChanged { get; set; } = new SliderEvent();
        public virtual void SetValueWithoutNotify(float input) { }
    }

    public class Scrollbar : Selectable
    {
        public float value { get; set; }
        public float size { get; set; }
    }

    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float elasticity { get; set; }
        public bool inertia { get; set; }
        public float decelerationRate { get; set; }
        public float scrollSensitivity { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public Scrollbar horizontalScrollbar { get; set; }
        public Vector2 normalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public Vector2 velocity { get; set; }
        public ScrollRectEvent onValueChanged { get; set; } = new ScrollRectEvent();
        public void StopMovement() { }
    }

    public class InputField : Selectable
    {
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom }
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public class SubmitEvent : UnityEvent<string> { }
        public class OnChangeEvent : UnityEvent<string> { }
        public class EndEditEvent : UnityEvent<string> { }
        public string text { get; set; }
        public Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public ContentType contentType { get; set; }
        public LineType lineType { get; set; }
        public Color caretColor { get; set; }
        public bool customCaretColor { get; set; }
        public Color selectionColor { get; set; }
        public float caretBlinkRate { get; set; }
        public int caretWidth { get; set; }
        public int caretPosition { get; set; }
        public bool isFocused => false;
        public OnChangeEvent onValueChanged { get; set; } = new OnChangeEvent();
        public EndEditEvent onEndEdit { get; set; } = new EndEditEvent();
        public SubmitEvent onSubmit { get; set; } = new SubmitEvent();
        public void ActivateInputField() { }
        public void DeactivateInputField() { }
        public void SetTextWithoutNotify(string input) { }
        public void MoveTextEnd(bool shift) { }
    }
}
