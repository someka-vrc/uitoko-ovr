using UnityEngine;
using Valve.VR;
using UnityEngine.UIElements;
using System.Collections.Generic;
using R3;
using System;

internal class VROverlayInputBridge : IDisposable
{
    ///<summary> MouseMoveイベントで受け付けたデバイスのカーソルインデックス </summary>
    // private uint _laserCursorIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    ///<summary> デバイスIDがマウスムーブ中しか取れないので辞書化する </summary>
    private readonly Dictionary<uint, uint> _cursorDeviceMap = new()
    {
        { 0, OpenVR.k_unTrackedDeviceIndexInvalid },
        { 1, OpenVR.k_unTrackedDeviceIndexInvalid },
    };
    private DragControl _dragCon = new();
    private ClickControl _clickCon;
    private EventReport _pri = new(true);
    private EventReport _sub = new(false);
    private EventReport[] _hands;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="multiClickThreshSec">ダブルクリックの間隔=シングルクリック判定にかかる時間</param>
    internal VROverlayInputBridge(ReactiveProperty<double> multiClickThreshSec)
    {
        _clickCon = new(multiClickThreshSec);
        _hands = new[] { _pri, _sub };
    }

    /// <summary>
    /// フォーカスされたオーバーレイ上のマウスイベントをUIDocumentへ転送する。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>PointerDown, PointerMove, PointerUp, PointerEnter, PointerLeave, Click, Wheel が対象。</item>
    /// <item>Click は UIDocument 側が自動で判断できないため、PointerDown から PointerUp までが0.5秒未満であればClickとして送出する。</item>
    /// <item>UIDocument の PointerMoveEvent では、evt.button == 0 ならドラッグである。</item>
    /// <item>cursorIndex は認識順等によって不定。</item>
    /// </list>
    /// </remarks>

    internal void Update(UIDocument document, RenderTexture renderTexture, ulong overlayHandle)
    {
        _sub.Reset();
        _pri.Reset();
        foreach (var vrEvent in OpenVRUtil.Overlay.PollOverlayEvents(overlayHandle))
        {
            if (vrEvent.eventType == (uint)EVREventType.VREvent_FocusLeave)
            {
                EventReport report = IsPrimaryCursor(vrEvent.data.overlay.cursorIndex) ? _pri : _sub;
                report.FocusLeave = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_MouseButtonDown)
            {
                EventReport report = IsPrimaryCursor(vrEvent.data.mouse.cursorIndex) ? _pri : _sub;
                report.MouseDown = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_MouseMove)
            {
                EventReport report = IsPrimaryDevice(vrEvent.trackedDeviceIndex) ? _pri : _sub;
                // デバイスを記録
                _cursorDeviceMap[vrEvent.data.mouse.cursorIndex] = vrEvent.trackedDeviceIndex;
                report.MouseMove = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_MouseButtonUp)
            {
                EventReport report = IsPrimaryCursor(vrEvent.data.mouse.cursorIndex) ? _pri : _sub;
                report.MouseUp = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ScrollSmooth)
            {
                // mouse ではなく scroll から cursorIndex を取得できる
                EventReport report = IsPrimaryCursor(vrEvent.data.scroll.cursorIndex) ? _pri : _sub;
                report.Scroll = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonPress)
            {
                EventReport report = IsPrimaryDevice(vrEvent.trackedDeviceIndex) ? _pri : _sub;
                report.ButtonPress = vrEvent;
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonUnpress)
            {
                EventReport report = IsPrimaryDevice(vrEvent.trackedDeviceIndex) ? _pri : _sub;
                report.ButtonUnpress = vrEvent;
            }
        }

        _pri.PostCollect();
        _sub.PostCollect();
        if (_pri.HasChanged || _sub.HasChanged) HandleEvents(document, renderTexture);
    }

    /// <summary>
    /// 両手のホバー状態をリセットする。
    /// オーバーレイが消えたり瞬間移動したときに呼ぶこと。呼ばないと MouseEnter/MouseLeave が正常動作しなくなる。
    /// </summary>
    /// <param name="document"></param>
    /// <param name="rt"></param>
    internal void ResetHover(UIDocument document, RenderTexture rt)
    {
        VisualElement root = document.rootVisualElement;
        VREventDispatcher.SendMouseLeaveAll(root, rt, _pri, _sub);
        _pri.PrvHover.Clear();
        _pri.NewHover.Clear();
        _sub.PrvHover.Clear();
        _sub.NewHover.Clear();
    }

    private void HandleEvents(UIDocument document, RenderTexture rt)
    {
        VisualElement root = document.rootVisualElement;

        // ドラッグ中 対象の手のドラッグ継続かドラッグ終了
        if (_dragCon.IsMouseDown)
        {
            // ドラッグ中の手のみ
            bool isPrimary = IsPrimaryCursor(_dragCon.Cursor);
            var rep = isPrimary ? _pri : _sub;
            // マウスムーブ
            if (rep.MouseMoved)
            {
                // マウスドラッグイベント
                var pickAll = VREventDispatcher.SendMouseMove(root, rt, rep, true);
                // エンターリーブ用データ
                foreach (var item in pickAll) rep.NewHover.Add(item);

                // ドラッグ期限延長
                _dragCon.MouseDown(_dragCon.Target);
            }
            // マウスアップ
            if (rep.MouseUpped)
            {
                uint deviceId = GetDeviceId();
                var position = VREventDispatcher.GetMousePosition(rep.MouseUp, rt);
                var picked = root.panel.Pick(position);
                // マウスアップイベント
                _dragCon.MouseUpAction = () => VREventDispatcher.SendMouseUp(root, rt, rep, picked);
                _dragCon.MouseUp();

                // クリックイベント
                _clickCon.ClickAction = clickCount => VREventDispatcher.SendClick(root, rt, rep, picked, clickCount);
                _clickCon.MouseUp(picked);
            }
            // フォーカスアウトイベント
            if (rep.FocusLeaved)
            {
                _dragCon.MouseUp();
            }
        }
        // フォーカス完全喪失
        else if (!_sub.IsInside && !_pri.IsInside)
        {
            VREventDispatcher.SendFocusOut(root);
        }
        // ドラッグ中でない
        else
        {
            // マウスダウンイベント
            foreach (var hand in _hands)
            {
                // 同時のときは片手のみ採用し、もう片方は確認しない
                if (hand.MouseDowned && !VREventDispatcher.SendMouseDown(root, rt, hand, _dragCon, _clickCon))
                    break;
            }

            // マウスムーブイベント
            foreach (var hand in _hands)
            {
                if (hand.MouseMoved)
                {
                    var pickAll = VREventDispatcher.SendMouseMove(root, rt, hand, false);
                    foreach (var item in pickAll) hand.NewHover.Add(item);
                }
            }
        }

        // スクロールイベント
        foreach (var hand in _hands)
        {
            if (hand.Scrolled)
                VREventDispatcher.SendScroll(root, rt, hand);
        }

        // ボタンイベント

        foreach (var hand in _hands)
        {
            if (hand.ButtonPressed)
                VREventDispatcher.SendButtonPress(root, rt, hand);
        }

        // マウスエンター/リーブイベント
        if (_pri.MouseMoved || _sub.MouseMoved)
        {
            VREventDispatcher.SendMouseEnterLeave(root, rt, _pri, _sub);
            if (_pri.MouseMoved) _pri.ShiftHoverHistory();
            if (_sub.MouseMoved) _sub.ShiftHoverHistory();
        }
    }

    private bool IsPrimaryCursor(uint cursorIndex) => OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand) == _cursorDeviceMap[cursorIndex];
    private bool IsPrimaryDevice(uint deviceId) => OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand) == deviceId;
    private uint GetDeviceId() => _cursorDeviceMap.TryGetValue(_dragCon.Cursor, out var did)
                                        ? did : OpenVR.k_unTrackedDeviceIndexInvalid;

    public void Dispose()
    {
        _dragCon.Dispose();
    }
}

