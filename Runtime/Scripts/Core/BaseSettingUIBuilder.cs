using UnityEngine;
using UnityEngine.UIElements;

public class SettingUIBuilder : MonoBehaviour
{
    [SerializeField, NotEmptyOnPlay]
    private DashboardOverlay _dashboardOverlay = default!;
    
    [SerializeField, NotEmptyOnPlay]
    private BaseOverlay _mainOverlay = default!;
    
    [SerializeField, NotEmptyOnPlay]
    private MainPrefsManager _prefsManager = default!;
    
    private MainPrefsV1 _pref => _prefsManager.CurrentPrefs;

    private void Awake()
    {
        _dashboardOverlay.AddUIBuilder(OnBuildUI);
    }

    private void OnBuildUI(UIDocument document)
    {
        var root = document.rootVisualElement;
        root.Q<Button>("resetLeftButton").RegisterCallback<ClickEvent>(evt =>
            _mainOverlay.ResetTransform(OverlayAnchor.LeftHand));
        root.Q<Button>("resetHeadButton").RegisterCallback<ClickEvent>(evt =>
            _mainOverlay.ResetTransform(OverlayAnchor.HMD));
        root.Q<Button>("resetRightButton").RegisterCallback<ClickEvent>(evt =>
            _mainOverlay.ResetTransform(OverlayAnchor.RightHand));
    }
}
