# 実行順序

- Awake
  - please フィールドや実装クラスの初期化
  - オーバーレイ `.AddUIBuilder(OnBuildUI);`
  - please イベント登録
  - please その他前準備
  - ここでは OpenVR は起動してない
- Start
  - `MonoBehaviourValidator`
  - OpenVRLifeCycle.Start() -> `OpenVRUtil.Sys.InitOpenVR()`, `_onInitVR?.Invoke();`
  - `_onInitVR` 外のここでは OpenVR の起動は保証されない
- Update
  - please 毎フレームの実装
  - `VROverlayInputBridge.Update()` -> `VREventDispatcher.SendXxxx(event)` -> `(VisualElement).SendEvent(event)`
- OnDestroy
  - `_onShutdownVR?.Invoke();`
  - `OpenVRUtil.Sys.ShutdownOpenVR();`