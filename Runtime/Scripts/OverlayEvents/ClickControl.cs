using UnityEngine.UIElements;
using R3;
using System;
using System.Linq;
/// <summary>
/// クリック制御。クリックイベントをバッファしてマルチクリックを識別する。
/// </summary>
internal class ClickControl : IDisposable
{
    ///<summary> クリック確定時に実行するアクション </summary>
    internal Action<int>? ClickAction { private get; set; }

    ///<summary> 長押しによる失効までの時間＝クリックからイベント送出までの時間 </summary>
    private const double CLICK_THRESH_SEC = 0.2;
    private DisposableBag _disposableBag = new();
    private ReactiveProperty<VisualElement?> _target = new(null);
    private Subject<VisualElement> _mDownStream = new();
    private Subject<VisualElement> _mUpStream = new();
    private Subject<Unit> _clickStream = new();

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="multiClickThreshSec">ダブルクリックの間隔=シングルクリック判定にかかる時間</param>
    internal ClickControl(ReadOnlyReactiveProperty<double> multiClickThreshSec)
    {
        _target.AddTo(ref _disposableBag);
        _mDownStream
            .Where(elm => elm != null)
            .Do(elm => _target.Value = elm)
            .Select(_ => Observable.Timer(TimeSpan.FromSeconds(CLICK_THRESH_SEC))) // 0.5秒後にタイマー
            .Switch() // タイマー更新（直前のをキャンセル）
            .Subscribe(_ => _target.Value = null) // タイムアウトでOFF
            .AddTo(ref _disposableBag);
        _mUpStream
            .Where(elm => elm != null && _target.Value == elm) // 押下時と同じ非nullの要素の場合
            .Subscribe(elm => _clickStream.OnNext(Unit.Default)) // クリックトリガー
            .AddTo(ref _disposableBag);
        _clickStream
            .Chunk(_clickStream.Debounce(TimeSpan.FromSeconds(multiClickThreshSec.CurrentValue)))
            .Subscribe(clicks =>
            {
                ClickAction?.Invoke(clicks.Length);
                ClickAction = null;
            })
            .AddTo(ref _disposableBag);
    }

    ///<summary> マウス押下時に呼ぶこと。 </summary>
    internal void MouseDown(VisualElement target) => _mDownStream.OnNext(target);

    ///<summary> マウスを離した時に呼ぶこと。 マルチクリック発生を待ち、なければアクションを実行する。</summary>
    internal void MouseUp(VisualElement target) => _mUpStream.OnNext(target);

    public void Dispose()
    {
        _disposableBag.Dispose();
    }
}

