using System;
using R3;

internal class DelayedOffToggle : IDisposable
{
    private readonly ReactiveProperty<bool> _state = new ReactiveProperty<bool>(false);
    private readonly Subject<bool> _trigger = new Subject<bool>();
    private IDisposable? _offDelayDisposable;

    internal ReadOnlyReactiveProperty<bool> State => _state;

    /// <summary>
    /// 遅延オフトグル
    /// </summary>
    /// <param name="offDelaySeconds">オフになるまでの遅延時間（秒）</param>
    /// <remarks>
    /// 動作シーケンス(offDelaySeconds = 1.0f)
    /// t=0.0s: SetTrigger(false) → t=1.0s にfalseを出力予定
    /// t=0.5s: SetTrigger(true)  → 即座にtrueを出力、状態がtrueに
    /// t=2.0s: SetTrigger(false) → t=3.0s にfalseを出力予定  
    /// t=2.5s: SetTrigger(false) → t=3.0s にfalseを出力予定  
    /// t=3.1s: 何も来ない        → falseが出力され、状態がfalseに
    public DelayedOffToggle(float offDelaySeconds = 1.0f)
    {
        _trigger
            .Subscribe(trigger =>
            {
                if (trigger)
                {
                    // オンにしろ → 即時オン & オフ遅延キャンセル
                    _offDelayDisposable?.Dispose();
                    _offDelayDisposable = null;
                    _state.Value = true;
                }
                else
                {
                    // オフにしろ → 1秒待ってからオフ
                    if (_offDelayDisposable == null)
                    {
                        _offDelayDisposable = Observable
                            .Timer(TimeSpan.FromSeconds(offDelaySeconds))
                            .Subscribe(_ =>
                            {
                                _state.Value = false;
                                _offDelayDisposable = null;
                            });
                    }
                }
            });
    }

    /// <summary>
    /// 外部からオン・オフを指示
    /// </summary>
    public void SetTrigger(bool value)
    {
        _trigger.OnNext(value);
    }

    public void Dispose()
    {
        _offDelayDisposable?.Dispose();
        _state?.Dispose();
        _trigger?.Dispose();
    }
}
