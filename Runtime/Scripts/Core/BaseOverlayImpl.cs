using UnityEngine;
using Valve.VR;
using System;
using static SteamVR_Utils;
using UnityEngine.UIElements;
using Microsoft.Extensions.Logging;
using ZLogger;
using R3;

internal sealed class BaseOverlayImpl : IDisposable
{
    private ILogger<BaseOverlayImpl> _log = Logger.Create<BaseOverlayImpl>();

    private RenderTexture _renderTexture;
    private UIDocument _uiDocument;
    private OverlayBasicPrefsV1 _pref;
    private ulong _overlayHandle = OpenVR.k_ulOverlayHandleInvalid;
    private VROverlayInputBridge _inputBridge;
    private readonly GrabState _grabState;
    private DelayedOffToggle _isHover;

    internal BaseOverlayImpl(BaseOverlay mainOverlay, string overlayKey, string overlayName, RenderTexture renderTexture, UIDocument uIDocument, OverlayBasicPrefsV1 overlayBasicV1)
    {
        _renderTexture = renderTexture;
        _uiDocument = uIDocument;
        _pref = overlayBasicV1;

        _overlayHandle = OpenVRUtil.Overlay.CreateOverlay(overlayKey, overlayName);
        OpenVRUtil.Overlay.FlipOverlayVertical(_overlayHandle);
        OpenVRUtil.Overlay.SetOverlayMouseScale(_overlayHandle, _renderTexture.width, _renderTexture.height);
        OpenVRUtil.Overlay.ShowOverlay(_overlayHandle);
        OpenVRUtil.Overlay.SendVRSmoothScrollEvents(_overlayHandle, true);
        OpenVRUtil.Overlay.MultiCursor(_overlayHandle, true);

        BuildFooterUI();
#if UNITY_EDITOR
        _uiDocument.rootVisualElement.AddToClassList("test");
#endif
        _inputBridge = new VROverlayInputBridge(_pref.MultiClickThreshSec);
        _grabState = new();
        _isHover = new();
        _isHover.State.Subscribe(isHover =>
        {
            OpenVRUtil.Overlay.SetOverlayInputMouse(_overlayHandle, isHover);
            OpenVRUtil.Overlay.MakeOverlaysInteractiveIfVisible(_overlayHandle, isHover);
            uint device = AnchorToDevice(_pref.Anchor.Value);
            _log.ZLogTrace($"Hover state {isHover}");
        });
    }

    internal void Update()
    {
        if (!OpenVRUtil.Sys.IsOpenVRAvailable()) return;

        if (AnchorToDevice(_pref.Anchor.Value) == OpenVR.k_unTrackedDeviceIndexInvalid) return;


        OpenVRUtil.Overlay.SetOverlaySize(_overlayHandle, _pref.Size.Value);
        OpenVRUtil.Overlay.SetOverlayTransformRelative(_overlayHandle, AnchorToDevice(_pref.Anchor.Value), GetCurrentRelative());
        OpenVRUtil.Overlay.SetOverlayRenderTexture(_overlayHandle, _renderTexture);

        CheckHover();
        _inputBridge.Update(_uiDocument, _renderTexture, _overlayHandle);
    }

    ///<summary> <see cref="OpenVRLifeCycle._onShutdownVR"/> </summary>
    public void Dispose()
    {
        OpenVRUtil.Overlay.SetOverlayInputMouse(_overlayHandle, false);
        OpenVRUtil.Overlay.DestroyOverlay(_overlayHandle);
    }

    private uint AnchorToDevice(OverlayAnchor anchor)
    {
        return anchor switch
        {
            OverlayAnchor.LeftHand => OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand),
            OverlayAnchor.RightHand => OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand),
            OverlayAnchor.HMD => OpenVR.k_unTrackedDeviceIndex_Hmd,
            _ => OpenVR.k_unTrackedDeviceIndexInvalid
        };
    }

    internal void ResetTransform(OverlayAnchor anchor)
    {
        _grabState.Reset(); // 掴み状態をリセット
        _pref.Size.Value = 0.17f; // サイズをデフォルトに戻す
        _pref.RelativePos[(int)anchor].Value = OverlayBasicPrefsConst.DEFAULT_POSITIONS[(int)anchor]; // 相対位置をデフォルトに戻す
    }

    private RigidTransform GetCurrentRelative()
    {
        return _pref.RelativePos[(int)_pref.Anchor.Value].Value;
    }


    /// <summary>
    /// レイキャストでコントローラーが指しているかを判定し、ホバー状態を更新する。
    /// 指している間だけマウス入力モードにする。
    /// </summary>
    private void CheckHover()
    {
        if (_grabState.IsHolding)
        {
            _isHover.SetTrigger(true);
            return;
        }

        uint leftHandIndex = AnchorToDevice(OverlayAnchor.LeftHand);
        uint rightHandIndex = AnchorToDevice(OverlayAnchor.RightHand);
        var position = Vector2.zero;
        // 左手についているときは左手レイキャストしない
        if (_pref.Anchor.Value != OverlayAnchor.LeftHand && leftHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            position = OpenVRUtil.Overlay.GetOverlayIntersectionForController(_overlayHandle, leftHandIndex, _renderTexture);
        }
        // 頭についていて左が範囲内、または右手についているときは右手レイキャストしない
        if (position == Vector2.zero && _pref.Anchor.Value != OverlayAnchor.RightHand && rightHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            position = OpenVRUtil.Overlay.GetOverlayIntersectionForController(_overlayHandle, rightHandIndex, _renderTexture);
        }

        bool isHover = position != Vector2.zero || OpenVRUtil.Overlay.IsHoverTargetOverlay(_overlayHandle);
        _isHover.SetTrigger(isHover);
    }

    /// <summary>
    /// 奥へ動かす 全倒しで0.3くらい
    /// </summary>
    /// <param name="farDelta"></param>
    internal void MoveForward(float farDelta)
    {
        if (!_grabState.IsHolding) return;
        if (farDelta == 0) return;

        // オーバーレイの前方向を取得し、farDelta分だけ前方向に移動
        var anchorTransform = OpenVRUtil.Sys.GetControllerTransform(AnchorToDevice(_pref.Anchor.Value));
        Vector3 movementInAnchor = anchorTransform.rot * (Vector3.down * farDelta);
        var newLocalPos = GetCurrentRelative().pos + movementInAnchor;

        // 新しい相対位置をセット（回転はそのまま）
        _pref.RelativePos[(int)_pref.Anchor.Value].Value = new RigidTransform(newLocalPos, GetCurrentRelative().rot);
    }

    /// <summary>
    /// サイズを大きくする 全倒しで0.3くらい
    /// </summary>
    /// <param name="scaleDelta"></param>
    internal void ScaleUp(float scaleDelta)
    {
        if (!_grabState.IsHolding) return;
        if (scaleDelta == 0) return;

        // scaleDeltaが0でない場合は、オーバーレイのサイズを変更
        float size = _pref.Size.Value;
        size = Math.Clamp(size + scaleDelta, 0.01f, 1f);
        OpenVRUtil.Overlay.SetOverlaySize(_overlayHandle, size);
        _pref.Size.Value = size; // サイズを保存
    }

    /// <summary>
    /// オーバーレイの絶対位置回転を保ったまま、別のアンカーに移動する。
    /// </summary>
    /// <param name="to"></param>
    internal void Grab(OverlayAnchor holder)
    {
        if (_grabState.IsHolding) return;

        _grabState.IsHolding = true;
        _grabState.Holder = holder;
        _grabState.OriginalAnchor = _pref.Anchor.Value;
        _grabState.HolderBackup = _pref.RelativePos[(int)holder].Value;

        // アンカーデバイスの位置と回転を取得
        RigidTransform fromTransfrom = OpenVRUtil.Sys.GetControllerTransform(AnchorToDevice(_pref.Anchor.Value));
        RigidTransform toTransform = OpenVRUtil.Sys.GetControllerTransform(AnchorToDevice(holder));

        // オーバーレイのワールド空間での位置と回転
        RigidTransform overlayWorld = fromTransfrom * GetCurrentRelative();

        // 新しいデバイスとの相対位置を計算
        RigidTransform newTransform = toTransform.GetInverse() * overlayWorld;
        _pref.Anchor.Value = holder;
        _pref.RelativePos[(int)holder].Value = newTransform;
    }

    internal void Ungrab()
    {
        if (!_grabState.IsHolding) return;

        var grabbed = _grabState.Clone();
        _grabState.IsHolding = false; // Grab を呼ぶためfalseにする
        Grab(_grabState.OriginalAnchor);
        _pref.RelativePos[(int)grabbed.Holder].Value = grabbed.HolderBackup;
        _grabState.Reset();
    }
    private sealed class GrabState
    {
        ///<summary> 掴み中かどうか。掴み中は他のデバイスが掴もうとしても無視しなくてはならない </summary>
        internal bool IsHolding;
        ///<summary> 掴んでいるデバイス </summary>
        internal OverlayAnchor Holder;
        ///<summary> もともとのデバイス </summary>
        internal OverlayAnchor OriginalAnchor;
        ///<summary> 掴んだ側の位置関係バックアップ </summary>
        internal RigidTransform HolderBackup;

        internal void Reset()
        {
            IsHolding = false;
            Holder = OverlayAnchor.LeftHand;
            OriginalAnchor = OverlayAnchor.LeftHand;
            HolderBackup = new RigidTransform();
        }

        internal GrabState Clone()
        {
            return new GrabState
            {
                IsHolding = IsHolding,
                Holder = Holder,
                OriginalAnchor = OriginalAnchor,
                HolderBackup = new(HolderBackup.pos, HolderBackup.rot)
            };

        }
    }
    private void BuildFooterUI()
    {
        var root = _uiDocument.rootVisualElement;

        HandleDockingButton("dockingLeftButton", OverlayAnchor.LeftHand, root);
        HandleDockingButton("dockingHeadButton", OverlayAnchor.HMD, root);
        HandleDockingButton("dockingRightButton", OverlayAnchor.RightHand, root);
        var grabButton = root.Q<VisualElement>("grabButton");
        root.RegisterCallback<FocusOutEvent>(evt =>
        {
            _log.ZLogDebug($"{new { type = evt.GetType().Name }}");
            Ungrab();
            if (grabButton.HasPointerCapture(0)) grabButton.ReleasePointer(0);
        });
        grabButton.RegisterCallback<PointerDownEvent>(evt =>
        {
            _log.ZLogDebug($"{new { type = evt.GetType().Name, evt.position, evt.isPrimary, evt.currentTarget, evt.button, evt.clickCount }}");
            if (evt.button == 1)
            {
                Grab(evt.isPrimary ? OverlayAnchor.RightHand : OverlayAnchor.LeftHand);
                grabButton.CapturePointer(0);
            }
        }, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerUpEvent>(evt =>
        {
            _log.ZLogDebug($"{new { type = evt.GetType().Name, evt.position, evt.isPrimary, evt.currentTarget, evt.button, evt.clickCount }}");
            if (evt.button == 1)
            {
                Ungrab();
                if (grabButton.HasPointerCapture(0))
                {
                    grabButton.ReleasePointer(0);
                }
            }
        });
        root.RegisterCallback<WheelEvent>(evt =>
        {
            // x:+左へ, y:+上へ スティック全倒しで0.3くらい
            // 8分割した円で上下・左右のみ反応、斜めは無視
            float angle = Mathf.Atan2(evt.delta.y, evt.delta.x);
            const float cut = 2 * Mathf.PI / 16;

            if (Mathf.Abs(evt.delta.x) > 0.1f || Mathf.Abs(evt.delta.y) > 0.1f)
            {
                bool r = angle is >= (-1 * cut) and <= (1 * cut);
                bool l = angle is >= (7 * cut) or <= (-7 * cut);
                bool u = angle is >= (3 * cut) and <= (5 * cut);
                bool d = angle is >= (-5 * cut) and <= (-3 * cut);

                if (r || l)
                {
                    ScaleUp(-evt.delta.x * 0.01f); // 左右反転
                }
                else if (u || d)
                {
                    MoveForward(evt.delta.y * 0.01f);
                }
                // それ以外(斜め)は不感
            }
        });
    }

    private void HandleDockingButton(string buttonId, OverlayAnchor anchor, VisualElement root)
    {
        root.Q<Button>(buttonId).RegisterCallback<ClickEvent>(evt =>
        {
            _log.ZLogDebug($"{new { type = evt.GetType().Name, evt.position, evt.isPrimary, evt.currentTarget, evt.button, evt.clickCount }}");

            if (evt.clickCount == 1)
            {
                _pref.Anchor.Value = anchor;
                // ホバー状態を削除
                _inputBridge.ResetHover(_uiDocument, _renderTexture);
            }
            else if (evt.clickCount > 1)
            {
                ResetTransform(anchor);
            }
        });
    }
}
