using UnityEngine;

public class CmdOverlayUIBuilder : MonoBehaviour
{
    [SerializeField, NotEmptyOnPlay]
    private BaseOverlay _baseOverlay = default!;

    private CmdOverlayImpl _cmdOverlayImpl=default!;

    private void Awake()
    {
        _cmdOverlayImpl = new(_baseOverlay);
        _baseOverlay.AddUIBuilder(_cmdOverlayImpl.OnBuildUI);
    }

    private void Update() => _cmdOverlayImpl.Update();

    private void Oestroy()
    {
        _cmdOverlayImpl.Dispose();
    }
}
