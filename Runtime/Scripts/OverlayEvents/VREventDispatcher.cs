using UnityEngine;
using Valve.VR;
using UnityEngine.UIElements;
using System.Collections.Generic;
using R3;
using System.Linq;
using Microsoft.Extensions.Logging;
using ZLogger;

internal class VREventDispatcher
{
    private static ILogger<VREventDispatcher> LOG = Logger.Create<VREventDispatcher>();

    internal static Vector2 GetMousePosition(VREvent_t vrEvent, RenderTexture renderTexture) =>
        vrEvent.data.mouse.x < 0 || vrEvent.data.mouse.x > renderTexture.width || vrEvent.data.mouse.y < 0 || vrEvent.data.mouse.y > renderTexture.height
            ? Vector2.zero
            : new(vrEvent.data.mouse.x, vrEvent.data.mouse.y);

    internal static bool SendMouseDown(VisualElement root, RenderTexture rt, EventReport rep,
        DragControl dragCon, ClickControl clickCon)
    {
        bool done = false;
        VREvent_t vrEvent = rep.MouseDown;
        var position = GetMousePosition(vrEvent, rt);
        // 領域外は通さない
        if (position != Vector2.zero)
        {
            VisualElement picked = root.panel.Pick(position) ?? root;
            var sysEvent = InputEventVR.SystemEvent(rep.IsPrimary, EventType.MouseDown, position, vrEvent.data.mouse.button);
            using var evt = PointerDownEvent.GetPooled(sysEvent);
            evt.target = picked;

            LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.isPrimary, evt.button, evt.position, evt.target }}");
            picked.SendEvent(evt);

            dragCon.SetCursor(vrEvent);
            dragCon.MouseDown(picked);
            clickCon.MouseDown(picked);

            done = true;
        }

        return done;
    }

    internal static List<VisualElement> SendMouseMove(VisualElement root, RenderTexture rt, EventReport rep,
        bool isDrag)
    {
        VREvent_t vrEvent = rep.MouseMove;
        var position = GetMousePosition(vrEvent, rt);
        List<VisualElement> pickAll = new();
        var picked = root.panel.PickAll(position, pickAll);
        EventType type = isDrag ? EventType.MouseDrag : EventType.MouseMove;
        var sysEvent = InputEventVR.SystemEvent(rep.IsPrimary, type, position);
        using var mme = PointerMoveEvent.GetPooled(sysEvent);
        mme.target = picked;

        root.SendEvent(mme);

        return pickAll;
    }

    internal static void SendMouseEnterLeave(VisualElement root, RenderTexture rt,
        EventReport pri, EventReport sub)
    {
        var newHoverElems = new HashSet<VisualElement>(sub.NewHover.Concat(pri.NewHover));
        var prvHoverElems = new HashSet<VisualElement>(sub.PrvHover.Concat(pri.PrvHover));

        // 新しいホバー - 古いホバー = 退出要素
        foreach (var enter in newHoverElems.Except(prvHoverElems))
        {
            bool isPrimary = pri.NewHover.Contains(enter); // priの新しいホバーに進入要素があるならpri
            var rep = isPrimary ? pri : sub;
            var position = GetMousePosition(rep.MouseMove, rt);
            var sysEvent = InputEventVR.SystemEvent(isPrimary, EventType.MouseMove, position);
            using var evt = PointerEnterEvent.GetPooled(sysEvent);
            evt.target = enter;

            LOG.ZLogTrace($"{new { type = evt.GetType().Name, evt.isPrimary, evt.position, evt.target }}");
            enter.SendEvent(evt);
        }
        // 古いホバー - 新しいホバー = 退出要素
        foreach (var leave in prvHoverElems.Except(newHoverElems))
        {
            bool isPrimary = pri.PrvHover.Contains(leave); // priの古いホバーに退出要素があるならpri
            var rep = isPrimary ? pri : sub;
            var position = GetMousePosition(rep.MouseMove, rt);
            var sysEvent = InputEventVR.SystemEvent(isPrimary, EventType.MouseMove, position);
            using var evt = PointerLeaveEvent.GetPooled(sysEvent);
            evt.target = leave;

            LOG.ZLogTrace($"{new { type = evt.GetType().Name, evt.isPrimary, evt.position, evt.target }}");
            leave.SendEvent(evt);
        }
    }

    internal static void SendMouseLeaveAll(VisualElement root, RenderTexture rt,
        EventReport pri, EventReport sub)
    {
        foreach (var hand in new[] { pri, sub })
        {
            foreach (var leave in hand.PrvHover)
            {
                bool isPrimary = hand.PrvHover.Contains(leave);
                var position = GetMousePosition(hand.MouseMove, rt);
                var sysEvent = InputEventVR.SystemEvent(isPrimary, EventType.MouseMove, position);
                using var evt = PointerLeaveEvent.GetPooled(sysEvent);
                evt.target = leave;

                LOG.ZLogTrace($"{new { type = evt.GetType().Name, evt.isPrimary, evt.position, evt.target }}");
                leave.SendEvent(evt);
            }
        }
    }

    internal static void SendMouseUp(VisualElement root, RenderTexture rt, EventReport rep,
        VisualElement picked)
    {
        VREvent_t vrEvent = rep.MouseUp;
        var position = GetMousePosition(vrEvent, rt);
        var sysEvent = InputEventVR.SystemEvent(rep.IsPrimary, EventType.MouseUp, position, vrEvent.data.mouse.button);
        using var evt = PointerUpEvent.GetPooled(sysEvent);
        evt.target = picked;

        LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.isPrimary, evt.button, evt.position, evt.target }}");
        picked.SendEvent(evt);
    }

    internal static void SendClick(VisualElement root, RenderTexture rt, EventReport rep,
        VisualElement picked, int clickCount)
    {
        VREvent_t vrEvent = rep.MouseUp;
        var position = GetMousePosition(vrEvent, rt);
        var sysEvent = InputEventVR.SystemEvent(rep.IsPrimary, EventType.MouseUp, position, vrEvent.data.mouse.button, clickCount);
        using var evt = ClickEvent.GetPooled(sysEvent);
        evt.target = picked;

        LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.isPrimary, evt.button, evt.clickCount, evt.position, evt.target }}");
        picked.SendEvent(evt);
    }

    internal static void SendScroll(VisualElement root, RenderTexture rt, EventReport rep)
    {
        VREvent_t vrEvent = rep.Scroll;
        var position = GetMousePosition(vrEvent, rt);
        var picked = root.panel.Pick(position) ?? root;
        Event sysEvent = new()
        {
            type = EventType.ScrollWheel,
            mousePosition = position,
            delta = new Vector2(vrEvent.data.scroll.xdelta, vrEvent.data.scroll.ydelta)
        };
        using var evt = WheelEvent.GetPooled(sysEvent);
        evt.target = picked;

        LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.button, evt.delta, evt.target }}");
        picked.SendEvent(evt);
    }

    internal static void SendFocusOut(VisualElement root)
    {
        using var evt = FocusOutEvent.GetPooled();

        LOG.ZLogDebug($"{new { type = evt.GetType().Name }}");
        root.SendEvent(evt);
    }

    /// <summary>
    /// コントローラーボタン押下イベント。
    /// </summary>
    /// <remarks>
    /// KeyCode の割り当ては以下の通り。ボタン名は meta quest3 のコントローラーの場合。
    /// ()内は<see cref="VREvent_t"/>.data.controller.button の値。
    /// 他にもボタンが存在する場合は KeyCode.Joystick*Button{button の値} に対応付けられるようになっている(19まで)
    /// <list type="bullet">
    /// <item>A(1): <see cref="KeyCode.Joystick1Button1"/></item>
    /// <item>B(2): <see cref="KeyCode.Joystick1Button2"/></item>
    /// <item>X(1): <see cref="KeyCode.Joystick2Button1"/></item>
    /// <item>Y(2): <see cref="KeyCode.Joystick2Button2"/></item>
    /// </list>
    /// </remarks>
    /// <param name="root"></param>
    /// <param name="rt"></param>
    /// <param name="rep"></param>
    internal static void SendButtonPress(VisualElement root, RenderTexture rt, EventReport rep)
    {
        VREvent_t vrEvent = rep.ButtonPress;
        var position = GetMousePosition(vrEvent, rt);
        var picked = root.panel.Pick(position) ?? root;
        Event sysEvent = new()
        {
            type = EventType.KeyDown,
            mousePosition = position,
            keyCode = rep.IsPrimary
                ? (KeyCode)((int)KeyCode.Joystick1Button0 + vrEvent.data.controller.button)
                : (KeyCode)((int)KeyCode.Joystick2Button0 + vrEvent.data.controller.button)
        };
        using var evt = KeyDownEvent.GetPooled(sysEvent);
        evt.target = picked;

        LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.keyCode, evt.target }}");
        root.SendEvent(evt);
    }

    internal static void SendButtonUnpress(VisualElement root, RenderTexture rt, EventReport rep)
    {
        VREvent_t vrEvent = rep.ButtonUnpress;
        var position = GetMousePosition(vrEvent, rt);
        var picked = root.panel.Pick(position) ?? root;
        Event sysEvent = new()
        {
            type = EventType.KeyUp,
            mousePosition = position,
            keyCode = rep.IsPrimary
                ? (KeyCode)((int)KeyCode.Joystick1Button0 + vrEvent.data.controller.button)
                : (KeyCode)((int)KeyCode.Joystick2Button0 + vrEvent.data.controller.button)
        };
        using var evt = KeyUpEvent.GetPooled(sysEvent);
        evt.target = picked;

        LOG.ZLogDebug($"{new { type = evt.GetType().Name, evt.keyCode, evt.target }}");
        root.SendEvent(evt);
    }
}