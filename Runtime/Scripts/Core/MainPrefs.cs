using System.Collections.Generic;
using System.Collections.ObjectModel;
using R3;
using UnityEngine;
using Valve.Newtonsoft.Json;
using static SteamVR_Utils;


[System.Serializable]
public class MainPrefsPrefsBase
{
    public int PrefsVersion = 1;
    public MainPrefsV1 V1 = new();
}

[System.Serializable]
public class MainPrefsV1
{
    public OverlayBasicPrefsV1 OverlayBasicV1 = new();
}

public class OverlayBasicPrefsConst
{
    ///<summary> オーバーレイのデフォルトの位置と回転 </summary>
public static readonly ReadOnlyCollection<RigidTransform> DEFAULT_POSITIONS = new List<RigidTransform>()
    {
        new()
        {
            pos = new Vector3(0.05479747f, -0.0469130278f, -0.129450351f),
            rot = new Quaternion(-0.17539753f, 0.635436714f, 0.0542577207f, -0.750007987f)
        },
        new()
        {
            pos = new Vector3(-0.05411893f, -0.0296949f, -0.1451366f),
            rot = new Quaternion(0.268390328f, 0.562502921f, 0.148123369f, 0.767864943f)
        },
        new()
        {
            pos = new Vector3(-0.0953058153f, 0.0564153455f, 0.5348932f),
            rot = new Quaternion(0.0316211767f, -0.0134537583f, -0.00757326838f, 0.999380648f)
        }
    }.AsReadOnly();
}

[System.Serializable]
public class OverlayBasicPrefsV1
{
    ///<summary> ダブルクリックの間隔=シングルクリック判定にかかる時間 </summary>
    public ReactiveProperty<double> MultiClickThreshSec = new(0.5);
    ///<summary> オーバーレイのサイズ </summary>
    public SerializableReactiveProperty<float> Size = new(0.17f);
    ///<summary> オーバーレイのアンカー </summary>
    public SerializableReactiveProperty<OverlayAnchor> Anchor = new(OverlayAnchor.LeftHand);
    ///<summary> オーバーレイのデバイスからの相対的な位置と回転 </summary>
    public SerializableReactiveProperty<RigidTransform> RelativePosLeft = new(OverlayBasicPrefsConst.DEFAULT_POSITIONS[0]);
    public SerializableReactiveProperty<RigidTransform> RelativePosRight = new(OverlayBasicPrefsConst.DEFAULT_POSITIONS[1]);
    public SerializableReactiveProperty<RigidTransform> RelativePosHead = new(OverlayBasicPrefsConst.DEFAULT_POSITIONS[2]);
    [JsonIgnore]
    public List<SerializableReactiveProperty<RigidTransform>> RelativePos =>
        new()
        {
            RelativePosLeft,
            RelativePosRight,
            RelativePosHead
        };
}
