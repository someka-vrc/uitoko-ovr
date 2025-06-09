using Valve.VR;
using UnityEngine.UIElements;
using R3;
using System;
using System.Linq;
/// <summary>
/// ドラッグ制御
/// MouseLeave や 領域外での MouseUp の判定を逃した場合のため、ドラッグを一定時間後に失効させる。
/// </summary>
internal class DragControl : IDisposable
{
    ///<summary> ドラッグ完了時のアクション </summary>
    internal Action? MouseUpAction { private get; set; }

    private const double DRAG_EXPIRE_SEC = 1.5;
    internal VisualElement? Target => _target;
    private VisualElement? _target;
    private DisposableBag _disposableBag = new();
    private ReactiveProperty<bool> _isMouseDown = new(false);
    private Subject<Unit> _mDownStream = new();
    private uint _cursor = OpenVR.k_unTrackedDeviceIndexInvalid;

    internal DragControl()
    {
        _isMouseDown.AddTo(ref _disposableBag);
        _mDownStream
            .Do(_ => _isMouseDown.Value = true)
            .Select(_ => Observable.Timer(TimeSpan.FromSeconds(DRAG_EXPIRE_SEC))) // タイマー
            .Switch() // タイマー更新（直前のをキャンセル）
            .Subscribe(_ => _isMouseDown.Value = false) // タイムアウトでOFF
            .AddTo(ref _disposableBag);
    }

    internal bool IsMouseDown => _isMouseDown.Value;

    ///<summary> ドラッグ開始時、ドラッグ継続時に呼ぶこと。 </summary>
    internal void MouseDown(VisualElement? target)
    {
        _target = target;
        _mDownStream.OnNext(Unit.Default); // トリガーする
    }

    ///<summary> ドラッグ完了時に呼ぶこと。 アクションを即時実行する。 </summary>
    internal void MouseUp()
    {
        _isMouseDown.Value = false;
        _target = null;
        MouseUpAction?.Invoke();
        MouseUpAction = null;
    }
    
    internal void SetCursor(VREvent_t vrEvent) => _cursor = vrEvent.data.mouse.cursorIndex;
    internal bool IsSameCursor(VREvent_t vrEvent) => _cursor == vrEvent.data.mouse.cursorIndex;
    internal uint Cursor => _cursor;

    public void Dispose()
    {
        _disposableBag.Dispose();
    }
}

