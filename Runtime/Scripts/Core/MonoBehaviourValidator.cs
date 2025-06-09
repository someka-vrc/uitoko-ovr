using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using System.Reflection;
using ZLogger;
using UnityEditor;
using UnityEditorInternal;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
internal class ValidateOnPlayAttribute : Attribute
{
    internal ValidateOnPlayAttribute() { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
internal class NotEmptyOnPlayAttribute : ValidateOnPlayAttribute
{
    internal NotEmptyOnPlayAttribute() { }
}
public class MonoBehaviourValidator : MonoBehaviour
{
    [SerializeField]
    private List<AssemblyDefinitionAsset> _assemblyDefinitionAsset = new();

    private static ILogger<MonoBehaviourValidator> LOG = Logger.Create<MonoBehaviourValidator>();
    private const BindingFlags BINDING_ATTR = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private void Start()
    {

        var mbs = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .SelectMany(ro => ro.GetComponentsInChildren<MonoBehaviour>())
            .Where(mb => _assemblyDefinitionAsset.Any(asmDef => IsClassPartOfAsmDef(mb.GetType(), asmDef.name)))
            .SelectMany(mb => GetMembers(mb))
            .ToList();
        LOG.ZLogDebug($"{nameof(MonoBehaviourValidator)} start. Found {mbs.Count} fields or properties.");
        var errors = mbs
            .SelectMany(mb => Validate(mb))
            .ToList();
        foreach (var error in errors)
        {
            LOG.ZLogError($"{error.message}", error.mb);
        }
        if (errors.Count > 0)
        {
            LOG.ZLogError($"Validation failed with {errors.Count} errors. See log for details.");
            // アプリケーションを終了
#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // エディタでのみプレイモードを終了
#else
            Application.Quit(); // ビルドされたアプリケーションでは終了
#endif
        }
    }

    private static IEnumerable<(MonoBehaviour Mb, MemberInfo Member, object? Value)> GetMembers(MonoBehaviour mb)
    {
        foreach (var m in mb.GetType().GetFields(BINDING_ATTR)
            .Where(f => f.CustomAttributes.Any(ca => typeof(ValidateOnPlayAttribute).IsAssignableFrom(ca.AttributeType))))
        {
            object? val = null;
            (MonoBehaviour Mb, MemberInfo Member, object? Value) ret = default;
            try
            {
                val = m.GetValue(mb);
                ret = (Mb: mb, Member: m, Value: val);
            }
            catch (Exception ex)
            {
                LOG.ZLogError($"Failed to get value for field {m.Name} in {GetPath(mb)}: {ex.Message}");
                ret = (Mb: mb, Member: m, Value: GetDefaultValueForValueType(m.FieldType));
            }
            yield return ret;
        }

        foreach (var m in mb.GetType().GetProperties(BINDING_ATTR)
            .Where(f => f.CustomAttributes.Any(ca => typeof(ValidateOnPlayAttribute).IsAssignableFrom(ca.AttributeType))))
        {
            object? val = null;
            (MonoBehaviour Mb, MemberInfo Member, object? Value) ret = default;
            try
            {
                val = m.GetValue(mb);
                ret = (Mb: mb, Member: m, Value: val);
            }
            catch (Exception ex)
            {
                LOG.ZLogError($"Failed to get value for property {m.Name} in {GetPath(mb)}: {ex.Message}");
                ret = (Mb: mb, Member: m, Value: GetDefaultValueForValueType(m.PropertyType));
            }
            yield return ret;
        }
    }

    private static IEnumerable<(MonoBehaviour mb, string message)> Validate((MonoBehaviour Mb, MemberInfo Info, object? Value) data)
    {
        if (data.Info.GetCustomAttribute<NotEmptyOnPlayAttribute>() != null)
        {
            if (data.Value == null ||
                (data.Value is string str && string.IsNullOrEmpty(str)) ||
                (data.Value is UnityEngine.Object obj && (obj == null || !obj)))
            {
                yield return (data.Mb, $"{GetPath(data.Mb)}.{data.Info.Name} is null or empty.");
            }
        }

    }
    public static object? GetDefaultValueForValueType(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        else
        {
            // 参照型の場合は null
            return null;
        }
    }

    private static bool HasAttribute(System.Reflection.MemberInfo member, Type attributeType) => member.GetCustomAttributes(attributeType, false).Length > 0;
    private static string GetPath(Transform current) => current.parent == null ? "/" + current.name : GetPath(current.parent) + "/" + current.name;
    private static string GetPath(Component component) => GetPath(component.transform) + "/" + component.GetType().ToString();

    /// <summary>
    /// 指定されたTypeのクラスが、指定されたasmdef (アセンブリ名) に属しているかを判断します。
    /// </summary>
    /// <param name="classType">チェック対象のクラスのType</param>
    /// <param name="targetAsmDefName">対象のasmdefファイルのアセンブリ名 (例: "MyProject.Core")</param>
    /// <returns>クラスが対象のasmdefに属していればtrue、そうでなければfalse</returns>
    public static bool IsClassPartOfAsmDef(Type classType, string targetAsmDefName) => classType?.Assembly?.GetName()?.Name?.ToLower() == targetAsmDefName?.ToLower();
}