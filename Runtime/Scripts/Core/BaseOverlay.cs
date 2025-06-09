using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public partial class BaseOverlay : MonoBehaviour
{
    
    [SerializeField, NotEmptyOnPlay]
    private string _overlayKey = "";

    [SerializeField, NotEmptyOnPlay]
    private string _overlayName = "";

    [SerializeField, NotEmptyOnPlay]
    private UIDocument _uIDocument = default!;

    [SerializeField, NotEmptyOnPlay]
    private MainPrefsManager _prefsManager = default!;

    [NotEmptyOnPlay]
    private RenderTexture _renderTexture => _uIDocument.panelSettings.targetTexture;
    private BaseOverlayImpl _impl = default!;

    private List<Action<UIDocument>> _uiBuilders = new();

    internal void AddUIBuilder(Action<UIDocument> builder) => _uiBuilders.Add(builder);
    
    ///<summary> <see cref="OpenVRLifeCycle._onInitVR"/> </summary>
    internal void OnInitVR()
    {
        foreach (var builder in _uiBuilders) builder.Invoke(_uIDocument);
        _impl = new BaseOverlayImpl(this, _overlayKey, _overlayName, _renderTexture,
        _uIDocument, _prefsManager.CurrentPrefs.OverlayBasicV1);
    }

    private void Update() => _impl.Update();
    ///<summary> <see cref="OpenVRLifeCycle._onShutdownVR"/> </summary>
    internal void OnShutdownVR() => _impl.Dispose();
    ///<summary> 奥へ動かす 全倒しで0.3くらい </summary>
    internal void MoveForward(float farDelta) => _impl.MoveForward(farDelta);
    ///<summary> サイズを大きくする 全倒しで0.3くらい </summary>
    internal void ScaleUp(float scaleDelta) => _impl.ScaleUp(scaleDelta);
    internal void ResetTransform(OverlayAnchor anchor) => _impl.ResetTransform(anchor);
    
    ///<summary> オーバーレイの絶対位置回転を保ったまま、別のアンカーに移動する。 </summary>
    internal void Grab(OverlayAnchor holder) => _impl.Grab(holder);
    ///<summary> Grab を解除する。 </summary>
    internal void Ungrab() => _impl.Ungrab();
}

