using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Microsoft.Extensions.Logging;
using ZLogger;
using System.Text;
using System.Text.RegularExpressions;
using Valve.Newtonsoft.Json;

internal class ModuleLoader
{
    private static ILogger<ModuleLoader> LOG = Logger.Create<ModuleLoader>();

    internal static async UniTask<ModuleInfoLoadResult[]> Load(CancellationToken ct)
    {
        string[] rootDirs = new[]
        {
            Path.Combine(Application.persistentDataPath, "modules"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules"),
        };
        var moduleFolders = rootDirs
            .SelectMany(root => Directory.Exists(root) ? Directory.GetDirectories(root) : Array.Empty<string>())
            .Distinct()
            .ToArray();

        // 同時実行数制御
        var semaphore = new SemaphoreSlim(4);

        var tasks = moduleFolders.Select(async moduleFolder =>
        {

            await semaphore.WaitAsync(); // セマフォのトークンを取得
            try
            {
                var result = await LoadModule(moduleFolder, ct);
                await WriteReport(result, ct);
                return result;
            }
            finally
            {
                semaphore.Release(); // セマフォのトークンを解放
            }
        }).ToArray();

        return await UniTask.WhenAll(tasks);
    }

    private static async UniTask WriteReport(ModuleInfoLoadResult item, CancellationToken ct)
    {
        StringBuilder sb = new();
        sb.AppendLine("=========== CmdOverlay ModuleLoader report ===========");
        sb.Append("Date: ").AppendLine(DateTime.Now.ToString());
        sb.Append("package.json: ").AppendLine(string.IsNullOrEmpty(item.JsonPath) ? "なし" : "あり");
        sb.Append("結果: ").AppendLine(item.Success ? "成功" : "失敗");
        if (!item.Success) sb.AppendLine("エラーメッセージ").AppendLine(item.ErrorMessage);
        sb.AppendLine("------------------------------------------------------");
        sb.AppendLine("解釈されたモジュール情報: ");
        sb.AppendLine(item.ResolvedJson);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(item.FolderPath, "module_loader_report.txt"), sb.ToString(), ct);
        }
        catch (System.Exception)
        {
            try
            {
                await File.WriteAllTextAsync(Path.Combine(item.FolderPath, $"module_loader_report-{DateTime.Now:yyyyMMdd-HHmmss}.txt"), sb.ToString(), ct);
            }
            catch (System.Exception e)
            {
                LOG.ZLogError($"Writing ModuleLoader report file failure", e);
            }
        }
    }

    internal static async UniTask<ModuleInfoLoadResult> LoadModule(string folder, CancellationToken ct)
    {
        ModuleInfoValidator validator = new(folder);
        var files = Directory.GetFiles(folder);
        string jsonPath = Path.Combine(folder, "package.json");

        StringBuilder errMsgs = new();
        try
        {
            ModuleInfo? moduleInfo;
            // 定義あり
            if (File.Exists(jsonPath))
            {
                string text = await File.ReadAllTextAsync(jsonPath, ct);
                moduleInfo = JsonConvert.DeserializeObject<ModuleInfo>(text);
                if (moduleInfo == null)
                {
                    errMsgs.AppendLine("JSONの読込に失敗しました。書式エラーがないか確認してください。");
                    moduleInfo = new();
                }
                else
                {
                    var result = validator.Validate(moduleInfo);
                    if (!result.IsValid)
                    {
                        errMsgs.AppendLine("バリデーションエラーが発生しました:");
                        foreach (var error in result.Errors)
                        {
                            errMsgs.Append("  ");
                            errMsgs.AppendLine(error.ErrorMessage);
                        }
                    }
                }
            }
            // 定義なし
            else
            {
                Regex extPattern = new("\\.(bat|cmd|exe)$", RegexOptions.IgnoreCase);
                var executables = Directory.EnumerateFiles(folder).Where(f => extPattern.IsMatch(f)).ToList();
                if (executables.Count == 0)
                {
                    errMsgs.AppendLine($"{folder} に1個の実行可能ファイル(bat, cmd, exe) または package.json が必要です。");
                }
                else if (executables.Count > 1)
                {
                    errMsgs.AppendLine($"{folder} に複数の実行可能ファイルが存在し対象を判断できません。削除するか、package.jsonで指定してください。");
                }

                Regex imgPattern = new Regex("\\.(png|jpe?g)$", RegexOptions.IgnoreCase);
                var images = Directory.EnumerateFiles(folder).Where(f => imgPattern.IsMatch(f)).ToList();

                moduleInfo = new();
                moduleInfo.Name = Path.GetFileName(folder);
                moduleInfo.ModuleDefenition.CommandModule.FileName = executables[0];
                if (images.Count > 0) moduleInfo.ModuleDefenition.Icon = images[0];
            }
            return new()
            {
                FolderPath = folder,
                JsonPath = jsonPath,
                ModuleInfo = moduleInfo,
                Success = errMsgs.Length == 0,
                ErrorMessage = errMsgs.ToString(),
                ResolvedJson = moduleInfo == null ? ""
                    : JsonConvert.SerializeObject(moduleInfo, Formatting.Indented)
            };
        }
        catch (Exception e)
        {
            errMsgs.AppendLine($"{jsonPath} の読込中にエラーが発生しました。");
            errMsgs.AppendLine(e.Message);
            LOG.ZLogError($"{errMsgs}", e);
            return new() { FolderPath = folder, ErrorMessage = errMsgs.ToString() };
        }
    }
}

internal class ModuleInfoLoadResult
{
    internal string FolderPath { get; set; } = "";
    internal string? JsonPath { get; set; }
    internal ModuleInfo? ModuleInfo { get; set; }
    internal bool Success { get; set; }
    internal string ErrorMessage { get; set; } = "";
    internal string ResolvedJson { get; set; } = "";

}