using UnityEngine;
using Valve.VR;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using System;

public partial class DashboardOverlay : MonoBehaviour
{

    [SerializeField, NotEmptyOnPlay]
    private UIDocument _uIDocument = default!;

    [SerializeField, NotEmptyOnPlay]
    private string _overlayKey = "";

    [SerializeField, NotEmptyOnPlay]
    private string _overlayName = "";

    [SerializeField, NotEmptyOnPlay]
    [Tooltip("Assets/StreamingAssets/(write here)")]
    private string _iconPath = "dashboard-icon.jpg";

    [SerializeField, NotEmptyOnPlay]
    private MainPrefsManager _prefsManager = default!;

    [NotEmptyOnPlay]
    private RenderTexture _renderTexture => _uIDocument.panelSettings.targetTexture;

    private List<Action<UIDocument>> _uiBuilders = new();
    private ulong _dashboardHandle = OpenVR.k_ulOverlayHandleInvalid;
    private ulong _thumbnailHandle = OpenVR.k_ulOverlayHandleInvalid;
    private VROverlayInputBridge _inputBridge = default!;

    internal void AddUIBuilder(Action<UIDocument> builder) => _uiBuilders.Add(builder);

    ///<summary> <see cref="OpenVRLifeCycle._onInitVR"/> </summary>
    public void OnInitVR()
    {
        OpenVRUtil.Sys.InitOpenVR();

        (_dashboardHandle, _thumbnailHandle) = OpenVRUtil.Overlay.CreateDashboardOverlay(_overlayKey, _overlayName);

        if (!string.IsNullOrEmpty(_iconPath))
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, _iconPath);
            OpenVRUtil.Overlay.SetOverlayFromFile(_thumbnailHandle, filePath);
        }

        OpenVRUtil.Overlay.FlipOverlayVertical(_dashboardHandle);
        OpenVRUtil.Overlay.SetOverlaySize(_dashboardHandle, 2.5f);
        OpenVRUtil.Overlay.SetOverlayMouseScale(_dashboardHandle, _renderTexture.width, _renderTexture.height);

        foreach (var builder in _uiBuilders) builder.Invoke(_uIDocument);
#if UNITY_EDITOR
        _uIDocument.rootVisualElement.AddToClassList("test");
#endif
        _inputBridge = new VROverlayInputBridge(_prefsManager.CurrentPrefs.OverlayBasicV1.MultiClickThreshSec);
    }

    private void Update()
    {
        if (!OpenVRUtil.Overlay.IsOverlayAvailable()) return;

        OpenVRUtil.Overlay.SetOverlayRenderTexture(_dashboardHandle, _renderTexture);
        _inputBridge.Update(_uIDocument, _renderTexture, _dashboardHandle);
    }

    ///<summary> <see cref="OpenVRLifeCycle._onShutdownVR"/> </summary>
    public void OnShutdownVR()
    {
        OpenVRUtil.Overlay.DestroyOverlay(_dashboardHandle);
    }
}
