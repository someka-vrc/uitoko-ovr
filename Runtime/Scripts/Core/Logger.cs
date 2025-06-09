using Microsoft.Extensions.Logging;
using ZLogger.Unity;
using ZLogger;
using System;
using System.Collections.Generic;

internal static class Logger
{
    private static ILoggerFactory LOGGER_FACTORY;
    static Logger()
    {
#if UNITY_EDITOR // プレイモード
#elif DEBUG // ビルド
#else // リリース
#endif
        LOGGER_FACTORY = LoggerFactory.Create(logging =>
        {
            logging
                .SetMinimumLevel(LogLevel.Trace)
#if UNITY_EDITOR // プレイモード
                .AddZLoggerUnityDebug(options =>
                {
                    options.UsePlainTextFormatter(formatter =>
                    {
                        formatter.SetPrefixFormatter($"[{0:short}]{1}: ", (in MessageTemplate template, in LogInfo info) => template.Format(info.LogLevel, GetLogCategory(info.Category)));
                        formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                    });
                })
#endif
                .AddZLoggerRollingFile(options =>
                {
                    // File name determined by parameters to be rotated
                    options.FilePathSelector = (timestamp, sequenceNumber) => $"logs/{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";
                    // Limit of size if you want to rotate by file size. (KB)
                    options.RollingSizeKB = 1024;
                    options.UsePlainTextFormatter(formatter =>
                    {
                        formatter.SetPrefixFormatter($"{0}[{1:short}]{2}: ", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel, info.Category));
                        formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                    });
                });
        });
    }

    internal static ILogger<T> Create<T>() => LOGGER_FACTORY.CreateLogger<T>();

    private static Dictionary<LogCategory, LogCategory> LOG_CATEGORIES = new();
    private static LogCategory GetLogCategory(LogCategory orig)
    {
        if (LOG_CATEGORIES.TryGetValue(orig, out var cat))
        {
            return cat;
        }
        else
        {
            cat = new LogCategory(TruncateCenter(orig.Name, 11, "..."));
            LOG_CATEGORIES[orig] = cat;
            return cat;
        }
    }
    /// <summary>
    /// 文字列を指定の文字数に収まるように中心を削り、代替文字を挿入して返します。
    /// 文字数の偶奇数に関わらず、右側の文字を優先して削ります。
    /// </summary>
    /// <param name="input">元の文字列。</param>
    /// <param name="maxLength">目標の文字数。</param>
    /// <param name="ellipsis">挿入する代替文字（例: "..."）。</param>
    /// <returns>整形された文字列。</returns>
    /// <example>TruncateCenter("Internationalization", 10, "...") -> "Inte...ion"</example>
    private static string TruncateCenter(string input, int maxLength, string ellipsis)
    {
        // 高速化のためのnull/emptyチェック - ReferenceEquals使用で参照比較
        if (input.Length == 0)
        {
            return input;
        }

        // 負数チェック - branchless比較で高速化
        if ((uint)maxLength > int.MaxValue) // uint変換でネガティブを大きな値に変換
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLengthは0以上である必要があります。");
        }

        var inputLength = input.Length;

        // 早期リターン - 切り詰めが不要な場合
        if (inputLength <= maxLength)
        {
            return input;
        }

        // ellipsisのnull/emptyチェックを最適化
        if (ReferenceEquals(ellipsis, null) || ellipsis.Length == 0)
        {
            // AsSpan().Slice()を使用してSubstringより高速な部分文字列取得
            return maxLength == 0 ? string.Empty : input.AsSpan(0, maxLength).ToString();
        }

        var ellipsisLength = ellipsis.Length;

        // 代替文字が目標長より長い場合の最適化
        if (maxLength <= ellipsisLength)
        {
            return maxLength == ellipsisLength ? ellipsis : ellipsis.AsSpan(0, maxLength).ToString();
        }

        // 残りの文字数計算 - 代替文字を除いた部分
        var remainingLength = maxLength - ellipsisLength;

        // ビット演算を使った高速な除算 - remainingLength >> 1 は remainingLength / 2 と同等
        var leftLength = remainingLength >> 1;

        // 右側の長さ - ビット演算で奇数判定も兼ねる
        var rightLength = remainingLength - leftLength;

        // string.Create使用でStringBuilderより高速化 - 一度のメモリ割り当てで完了
        return string.Create(maxLength, (input, leftLength, ellipsis, rightLength, inputLength),
            static (span, state) =>
            {
                var (sourceInput, leftLen, sourceEllipsis, rightLen, sourceInputLength) = state;

                // ReadOnlySpanを使用して高速コピー
                sourceInput.AsSpan(0, leftLen).CopyTo(span);

                var offset = leftLen;
                sourceEllipsis.AsSpan().CopyTo(span.Slice(offset));

                offset += sourceEllipsis.Length;
                sourceInput.AsSpan(sourceInputLength - rightLen, rightLen).CopyTo(span.Slice(offset));
            });
    }

    // 使用例とテスト
    /*
    Console.WriteLine(TruncateCenter("Internationalization", 10, "...")); // "Inte...ion"
    Console.WriteLine(TruncateCenter("Hello", 10, "..."));                // "Hello"
    Console.WriteLine(TruncateCenter("A", 1, "..."));                     // "A"
    Console.WriteLine(TruncateCenter("AB", 1, "..."));                    // "."
    Console.WriteLine(TruncateCenter("LongString", 5, ".."));             // "L..ng"
    */
}