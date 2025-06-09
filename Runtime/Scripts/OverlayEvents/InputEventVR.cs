using UnityEngine;
using Valve.VR;
using UnityEngine.UIElements;
internal sealed class InputEventVR
{
    internal static IPointerEvent SystemEvent(bool isPrimary, EventType type, Vector2 mousePosition = default, uint button = 0, int clickCount = 0, Vector2 delta = default)
    {
        PointerEventVR ret = new();
        ret.pointerType = UnityEngine.UIElements.PointerType.mouse;
        ret.pointerId = PointerId.mousePointerId;

        ret.isPrimary = isPrimary;
        int btn = (int)button;
        ret.button = btn;
        switch (type)
        {
            case EventType.MouseDown:
            case EventType.MouseDrag:
                PointerEventVR.PressButton(ret.pointerId, btn);
                ret.button = btn;
                break;
            case EventType.MouseUp:
                PointerEventVR.ReleaseButton(ret.pointerId, btn);
                ret.button = btn;
                break;
            default:
                ret.button = -1;
                break;
        }

        ret.pressedButtons = PointerEventVR.GetPressedButtons(ret.pointerId);
        ret.position = mousePosition;
        ret.localPosition = mousePosition;
        ret.deltaPosition = delta;
        ret.clickCount = clickCount;

        return ret;

    }
}

internal enum VREventMouseButton
{
    Trigger = EVRMouseButton.Left,
    Stick = EVRMouseButton.Right,
    Grip = EVRMouseButton.Middle,
}

internal class PointerEventVR : IPointerEvent
{
    public int pointerId { get; set; } = 0;
    public string pointerType { get; set; } = UnityEngine.UIElements.PointerType.unknown;
    public bool isPrimary { get; set; } = false;
    public int button { get; set; } = -1;
    public int pressedButtons { get; set; } = 0;
    public Vector3 position { get; set; } = Vector3.zero;
    public Vector3 localPosition { get; set; } = Vector3.zero;
    public Vector3 deltaPosition { get; set; } = Vector3.zero;
    public float deltaTime { get; set; } = 0f;
    public int clickCount { get; set; } = 0;
    public float pressure { get; set; } = 0f;
    public float tangentialPressure { get; set; } = 0f;
    public float altitudeAngle { get; set; } = 0f;
    public float azimuthAngle { get; set; } = 0f;
    public float twist { get; set; } = 0f;
    public Vector2 tilt { get; set; } = new Vector2(0f, 0f);
    public PenStatus penStatus { get; set; } = PenStatus.None;
    public Vector2 radius { get; set; } = Vector2.zero;
    public Vector2 radiusVariance { get; set; } = Vector2.zero;
    public EventModifiers modifiers { get; set; } = EventModifiers.None;
    public bool shiftKey { get; set; } = false;
    public bool ctrlKey { get; set; } = false;
    public bool commandKey { get; set; } = false;
    public bool altKey { get; set; } = false;
    public bool actionKey { get; set; } = false;

    private static int[] PRESSED_BUTTONS = new int[PointerId.maxPointers];
    internal static void PressButton(int pointerId, int buttonId)
    {
        Debug.Assert(buttonId >= 0);
        Debug.Assert(buttonId < 32);
        PRESSED_BUTTONS[pointerId] |= 1 << buttonId;
    }

    public static void ReleaseButton(int pointerId, int buttonId)
    {
        Debug.Assert(buttonId >= 0);
        Debug.Assert(buttonId < 32);
        PRESSED_BUTTONS[pointerId] &= ~(1 << buttonId);
    }

    public static void ReleaseAllButtons(int pointerId)
    {
        PRESSED_BUTTONS[pointerId] = 0;
    }
    public static int GetPressedButtons(int pointerId)
    {
        return PRESSED_BUTTONS[pointerId];
    }
}