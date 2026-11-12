// Hand-written stubs of the parts of the Input System and URP the game uses, for the compile
// check only (Tools/V2/check.sh unity). Not part of the game: Unity has the real packages.
using System;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem
{
    public enum Key
    {
        None, Space, Enter, Tab, Backquote, Quote, Semicolon, Comma, Period, Slash, Backslash, LeftBracket, RightBracket, Minus, Equals,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0,
        LeftShift, RightShift, LeftAlt, RightAlt, LeftCtrl, RightCtrl, LeftMeta, RightMeta, ContextMenu, Escape,
        LeftArrow, RightArrow, UpArrow, DownArrow, Backspace, PageDown, PageUp, Home, End, Insert, Delete, CapsLock, NumLock, PrintScreen, ScrollLock, Pause,
        NumpadEnter, NumpadDivide, NumpadMultiply, NumpadPlus, NumpadMinus, NumpadPeriod, NumpadEquals,
        Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }

    public class InputDevice
    {
        public bool added => true;
    }

    namespace Controls
    {
        public class InputControl<T> where T : struct
        {
            public T ReadValue() => default;
        }

        public class ButtonControl : InputControl<float>
        {
            public bool isPressed => false;
            public bool wasPressedThisFrame => false;
            public bool wasReleasedThisFrame => false;
        }

        public class KeyControl : ButtonControl
        {
            public Key keyCode => Key.None;
        }

        public class AxisControl : InputControl<float> { }

        public class Vector2Control : InputControl<Vector2>
        {
            public AxisControl x => null;
            public AxisControl y => null;
        }

        public class DeltaControl : Vector2Control { }

        public class AnyKeyControl : ButtonControl { }
    }

    public class Keyboard : InputDevice
    {
        public static Keyboard current => null;
        public Controls.KeyControl this[Key key] => null;
        public Controls.AnyKeyControl anyKey => null;
        public Controls.KeyControl spaceKey => null;
        public Controls.KeyControl enterKey => null;
        public Controls.KeyControl escapeKey => null;
        public Controls.KeyControl tabKey => null;
        public Controls.KeyControl leftShiftKey => null;
        public Controls.KeyControl rightShiftKey => null;
        public Controls.KeyControl leftCtrlKey => null;
        public Controls.KeyControl leftAltKey => null;
        public Controls.KeyControl upArrowKey => null;
        public Controls.KeyControl downArrowKey => null;
        public Controls.KeyControl leftArrowKey => null;
        public Controls.KeyControl rightArrowKey => null;
        public Controls.KeyControl backspaceKey => null;
        public Controls.KeyControl numpadEnterKey => null;
        public Controls.KeyControl wKey => null;
        public Controls.KeyControl aKey => null;
        public Controls.KeyControl sKey => null;
        public Controls.KeyControl dKey => null;
        public Controls.KeyControl qKey => null;
        public Controls.KeyControl eKey => null;
        public event Action<char> onTextInput;
    }

    public class Pointer : InputDevice
    {
        public Controls.Vector2Control position => null;
        public Controls.DeltaControl delta => null;
    }

    public class Mouse : Pointer
    {
        public static Mouse current => null;
        public Controls.ButtonControl leftButton => null;
        public Controls.ButtonControl rightButton => null;
        public Controls.ButtonControl middleButton => null;
        public Controls.DeltaControl scroll => null;
        public void WarpCursorPosition(Vector2 position) { }
    }

    public class Gamepad : InputDevice
    {
        public static Gamepad current => null;
    }

    namespace UI
    {
        public class InputSystemUIInputModule : BaseInputModule { }
    }
}

namespace UnityEngine.Rendering
{
    public abstract class VolumeComponent : ScriptableObject
    {
        public bool active;
    }

    public class VolumeParameter<T>
    {
        public bool overrideState;
        public T value;
        public void Override(T x) { }
    }

    public class FloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : FloatParameter { }
    public class ClampedFloatParameter : FloatParameter { }
    public class ColorParameter : VolumeParameter<Color> { }
    public class BoolParameter : VolumeParameter<bool> { }

    public class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent => null;
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
        public bool Has<T>() where T : VolumeComponent => false;
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal;
        public float priority;
        public float weight;
        public VolumeProfile profile { get; set; }
        public VolumeProfile sharedProfile;
    }

    public abstract class RenderPipelineAsset : ScriptableObject { }

    public static class GraphicsSettings
    {
        public static RenderPipelineAsset currentRenderPipeline => null;
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public float renderScale { get; set; }
        public float shadowDistance { get; set; }
        public int msaaSampleCount { get; set; }
        public int shadowCascadeCount { get; set; }
        public bool supportsHDR { get; set; }
        public int maxAdditionalLightsCount { get; set; }
        public bool supportsCameraDepthTexture { get; set; }
        public bool supportsCameraOpaqueTexture { get; set; }
    }

    public static class UniversalRenderPipeline
    {
        public static UniversalRenderPipelineAsset asset => null;
    }

    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
        public bool renderShadows { get; set; }
        public bool requiresDepthTexture { get; set; }
        public bool requiresColorTexture { get; set; }
    }

    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => null;
    }

    public class UniversalAdditionalLightData : MonoBehaviour { }

    public class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new MinFloatParameter();
        public MinFloatParameter intensity = new MinFloatParameter();
        public ClampedFloatParameter scatter = new ClampedFloatParameter();
        public ColorParameter tint = new ColorParameter();
    }

    public class Vignette : VolumeComponent
    {
        public ColorParameter color = new ColorParameter();
        public ClampedFloatParameter intensity = new ClampedFloatParameter();
        public ClampedFloatParameter smoothness = new ClampedFloatParameter();
    }

    public class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new FloatParameter();
        public ClampedFloatParameter contrast = new ClampedFloatParameter();
        public ColorParameter colorFilter = new ColorParameter();
        public ClampedFloatParameter saturation = new ClampedFloatParameter();
    }

    public enum TonemappingMode { None, Neutral, ACES }

    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }

    public class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode = new TonemappingModeParameter();
    }
}
