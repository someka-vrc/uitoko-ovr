using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ZLogger;
using R3;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class MainPrefsManager : MonoBehaviour
{
    private ILogger<MainPrefsManager> _log = Logger.Create<MainPrefsManager>();

    [SerializeField, NotEmptyOnPlay]
    private string _prefFileName = "Prefs-basic.json";

    ///<summary> インスペクタ確認用 </summary>
    [SerializeField, NotEmptyOnPlay]
    private MainPrefsPrefsBase _prefsContainer = new();

    // 外部から現在の設定値にアクセスするためのプロパティ
    // ここで直接 PrefsV1 にアクセスできるようにする
    internal MainPrefsV1 CurrentPrefs => _prefsContainer.V1;

    private JsonSerializerSettings _jsonSettings;
    private string _prefsPath => Path.Combine(Application.persistentDataPath, _prefFileName);

    public MainPrefsManager()
    {
        _jsonSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new ReactiveOnlyValueResolver()
        };
        _jsonSettings.Converters.Add(new Vector3Converter());
        _jsonSettings.Converters.Add(new QuaternionConverter());
    }

        ReactiveProperty<bool> prefChanged = new(false);
    void Awake()
    {
        // データのロード
        LoadPrefs();
        CurrentPrefs.OverlayBasicV1.MultiClickThreshSec.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.MultiClickThreshSec));
        CurrentPrefs.OverlayBasicV1.Size.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.Size));
        CurrentPrefs.OverlayBasicV1.Anchor.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.Anchor));
        CurrentPrefs.OverlayBasicV1.RelativePosLeft.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.RelativePosLeft));
        CurrentPrefs.OverlayBasicV1.RelativePosRight.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.RelativePosRight));
        CurrentPrefs.OverlayBasicV1.RelativePosHead.Subscribe(OnNext(CurrentPrefs.OverlayBasicV1.RelativePosHead));

        prefChanged.Debounce(TimeSpan.FromSeconds(3))
            .Subscribe(pch =>
            {
                if (!pch) return; // 変更がなければ何もしない
                SavePrefs();
                prefChanged.Value = false; // 保存後はフラグをリセット
            });
    }

    private Action<T> OnNext<T>(ReactiveProperty<T> rp)
    {
        return value => prefChanged.Value = true;
    }

    void OnDestroy()
    {
        // スクリプトが無効化されるときに設定を保存
        SavePrefs();
    }

    ///<summary> 環境設定をファイルから読み込む </summary>
    private void LoadPrefs()
    {
        if (File.Exists(_prefsPath))
        {
            string json = File.ReadAllText(_prefsPath);
            try
            {
                // ロード時には新しい PrefsBase オブジェクトを生成し、その中の v1 を使う
                // これにより、古いバージョンのデータから新しいバージョンへの移行なども制御しやすくなる
                _prefsContainer = JsonConvert.DeserializeObject<MainPrefsPrefsBase>(json, _jsonSettings);
                if (_prefsContainer == null || _prefsContainer.PrefsVersion < 1 || _prefsContainer.V1 == null)
                {
                    _log.ZLogWarning($"設定ファイルの内容が不正です。デフォルト設定を生成します。");
                    BackupAndCreatePrefs();
                }
                // ここで prefsVersion をチェックし、必要に応じてデータ移行処理を行う
                // if (_prefsContainer.prefsVersion < 1) { // 移行処理 }
            }
            catch (Exception e)
            {
                _log.ZLogWarning($"設定ファイルのロード中にエラーが発生しました。デフォルト設定を生成します。: {e.Message}");
                BackupAndCreatePrefs();
            }
        }
        else
        {
            _log.ZLogInformation($"設定ファイルが見つかりません。デフォルト設定を生成します。");
            _prefsContainer = new MainPrefsPrefsBase(); // ファイルが存在しない場合はデフォルト設定
            SavePrefs();
        }
    }

    private void BackupAndCreatePrefs()
    {
        // 既存の設定ファイルをバックアップ
        if (File.Exists(_prefsPath))
        {
            string backupPath = _prefsPath + ".bak-" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(_prefsPath, backupPath, true);
            _log.ZLogDebug($"設定ファイルを退避しました: {backupPath}");
        }
        _prefsContainer = new MainPrefsPrefsBase(); // デフォルト設定を使用
        SavePrefs();
    }

    ///<summary> 環境設定をファイルに書き込む </summary>
    public void SavePrefs()
    {
        string json = JsonConvert.SerializeObject(_prefsContainer, Formatting.Indented, _jsonSettings);
        File.WriteAllText(_prefsPath, json);
    }
}