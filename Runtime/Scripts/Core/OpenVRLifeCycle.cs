using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.Extensions.Logging;
using ZLogger;

public class OpenVRLifeCycle : MonoBehaviour
{
    private ILogger<OpenVRLifeCycle> _log = Logger.Create<OpenVRLifeCycle>();

    ///<summary> Start()フェーズで InitOpenVR 後に呼ばれるイベント </summary>
    [SerializeField]
    private UnityEvent _onInitVR = new();

    ///<summary> OnDestroy()フェーズで ShutdownOpenVR 前に呼ばれるイベント </summary>
    [SerializeField]
    private UnityEvent _onShutdownVR = new();

    void Start()
    {
        try
        {
            OpenVRUtil.Sys.InitOpenVR();
        }
        catch (System.Exception e)
        {
            _log.ZLogError($"InitOpenVR failed.", e);
        }
        _onInitVR?.Invoke();
    }

    void OnDestroy()
    {
        _onShutdownVR?.Invoke();
        OpenVRUtil.Sys.ShutdownOpenVR();
    }
}
