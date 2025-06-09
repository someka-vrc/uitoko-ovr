using Valve.VR;
using UnityEngine.UIElements;
using System.Collections.Generic;

internal class EventReport
{
    internal bool IsPrimary { get; private set; }
    internal EventReport(bool isPrimary) => IsPrimary = isPrimary;

    internal bool FocusLeaved => !FocusLeave.Equals(default(VREvent_t));
    internal VREvent_t FocusLeave;
    internal bool MouseDowned => !MouseDown.Equals(default(VREvent_t));
    internal VREvent_t MouseDown;
    internal bool MouseMoved => !MouseMove.Equals(default(VREvent_t));
    internal VREvent_t MouseMove;
    internal bool MouseUpped => !MouseUp.Equals(default(VREvent_t));
    internal VREvent_t MouseUp;
    internal bool Scrolled => !Scroll.Equals(default(VREvent_t));
    internal VREvent_t Scroll;
    internal bool ButtonPressed => !ButtonPress.Equals(default(VREvent_t));
    internal VREvent_t ButtonPress;
    internal bool ButtonUnpressed => !ButtonUnpress.Equals(default(VREvent_t));
    internal VREvent_t ButtonUnpress;

    internal bool IsInside { get; private set; }
    internal bool HasChanged { get; private set; }

    internal HashSet<VisualElement> PrvHover = new();
    internal HashSet<VisualElement> NewHover = new();
    internal void Reset()
    {
        FocusLeave = default;
        MouseDown = default;
        MouseMove = default;
        MouseUp = default;
        Scroll = default;
        ButtonPress = default;
        ButtonUnpress = default;
    }

    internal void PostCollect()
    {
        HasChanged = FocusLeaved || MouseDowned || MouseMoved || MouseUpped || Scrolled || ButtonPressed || ButtonUnpressed;

        if (MouseMoved) IsInside = true;
        else if (FocusLeaved) IsInside = false;
    }

    internal void ShiftHoverHistory()
    {
        PrvHover = new(NewHover);
        NewHover.Clear();
    }
}