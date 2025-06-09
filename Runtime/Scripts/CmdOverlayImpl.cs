using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Linq;
using System.Threading;
using UnityEngine.UIElements;

internal class CmdOverlayImpl : IDisposable
{
    private readonly BaseOverlay _baseOverlay;

    private CancellationTokenSource _moduleLoaderCts;
    private ReactiveProperty<ModuleInfo[]> _modules;

    internal CmdOverlayImpl(BaseOverlay baseOverlay)
    {
        _baseOverlay = baseOverlay;
        _moduleLoaderCts = new();
        _modules = new(new ModuleInfo[] { });
        _modules.Subscribe(OnModuleLoaded);
    }

    ///<summary> <see cref="BaseOverlay.AddUIBuilder"/> </summary>
    internal void OnBuildUI(UIDocument document)
    {
        // TODO BaseOverlayのメイン部分のUXML/USS切り分け

        // 起動時モジュール読込
        ReloadModules().Forget();
    }

    internal void Update()
    {

    }

    private async UniTask ReloadModules()
    {
        var results = await ModuleLoader.Load(_moduleLoaderCts.Token);
        _modules.Value = results.Where(r => r.Success).Select(r => r.ModuleInfo!).ToArray();
    }

    private void OnModuleLoaded(ModuleInfo[] obj)
    {
        // TODO オーバーレイにボタンを追加する
    }

    public void Dispose()
    {

        _moduleLoaderCts.Dispose();
    }
}