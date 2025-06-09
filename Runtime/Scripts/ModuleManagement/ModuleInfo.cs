using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;

internal enum ModuleType
{
    Command,
    Folder
}

internal enum ExecMode
{
    Normal,
    Repeat
}

[Serializable]
internal class ModuleInfo
{
    internal string Name { get; set; } = "";
    internal string Author { get; set; } = "";
    internal string Version { get; set; } = "";
    internal string Description { get; set; } = "";
    internal string Homepage { get; set; } = "";
    internal string Repository { get; set; } = "";
    internal ModuleDefinition ModuleDefenition { get; set; } = new();
    internal List<ModuleDefinition> ModuleDefenitions { get; set; } = new();
}

[Serializable]
internal class ModuleDefinition
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal ModuleType ModuleType { get; set; } = ModuleType.Command;

    internal string Icon { get; set; } = "";

    internal CommandModule CommandModule { get; set; } = new();
}

[Serializable]
internal class CommandModule
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal ExecMode ExecMode { get; set; } = ExecMode.Normal;

    internal string FileName { get; set; } = "";
    internal string Arguments { get; set; } = "";
    internal List<CommandParameter> CommandParameters { get; set; } = new();
    internal List<ExitCode> ExitCode { get; set; } = new();
    internal int Timeout { get; set; } = 3;
    internal int RepeatInteraval { get; set; } = 10;
}

[Serializable]
internal class CommandParameter
{
    internal string Name { get; set; } = "";
    internal string Description { get; set; } = "";
    internal bool IsArg { get; set; } = true;
    internal bool IsSwitch { get; set; } = false;
    internal string DefaultValue { get; set; } = "";
}

[Serializable]
internal class ExitCode
{
    internal int Min { get; set; } = int.MinValue;
    internal int Max { get; set; } = int.MaxValue;
    internal string Color { get; set; } = "#00FF00";
}
