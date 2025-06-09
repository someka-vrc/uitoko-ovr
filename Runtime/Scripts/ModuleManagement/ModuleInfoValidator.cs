using FluentValidation;
using System.Text.RegularExpressions;
using System.IO;

internal class ModuleInfoValidator : AbstractValidator<ModuleInfo>
{
    private const string MSG_MUST = "{PropertyName} は必須です。";
    private const string MSG_MAXLEN = "{PropertyName} は {MaxLength} 文字以下で指定してください。";
    private const string MSG_COL_FMT = "{PropertyName} は \"#FFFFFF\" 形式、または SUCCESS, WARN, ERROR のいずれかを指定してください。";
    private const string MSG_IMG_EXT = "{PropertyName} には .png か .jpg を指定してください。";
    private const string MSG_NOFILE = "{PropertyName} というファイルが存在しません。";

    private string _folderPath;
    internal ModuleInfoValidator(string folderPath)
    {
        _folderPath = folderPath;

        RuleFor(mi => mi.Name)
            .NotEmpty().WithMessage(MSG_MUST)
            .MaximumLength(16).WithMessage(MSG_MAXLEN);
        RuleFor(mi => mi.ModuleDefenition)
            .SetValidator(new ModuleDefinitionValidator("ModuleDefinition", folderPath));
        RuleForEach(mi => mi.ModuleDefenitions)
            .SetValidator(new ModuleDefinitionValidator("ModuleDefinitions", folderPath));
    }

    private class ModuleDefinitionValidator : AbstractValidator<ModuleDefinition>
    {
        public ModuleDefinitionValidator(string jsonPath, string folderPath)
        {
            RuleFor(mi => mi.Icon)
                .Must(s => !string.IsNullOrEmpty(s) || Regex.IsMatch(s.ToLower(), ".+\\.(png|jpe?g)$",RegexOptions.IgnoreCase))
                .WithName($"{jsonPath}.Icon")
                .WithMessage(MSG_IMG_EXT)
                .Must(s => File.Exists(Path.Combine(folderPath, s)))
                .WithName($"{jsonPath}.Icon")
                .WithMessage(MSG_NOFILE);

            RuleFor(mi => mi.CommandModule)
                .SetValidator(new CommandModuleValidator($"{jsonPath}.CommandModule"));
        }
    }

    private class CommandModuleValidator : AbstractValidator<CommandModule>
    {
        public CommandModuleValidator(string jsonPath)
        {
            RuleFor(mi => mi.FileName)
                .NotEmpty()
                .WithName($"{jsonPath}.FileName")
                .WithMessage(MSG_MUST);
            RuleForEach(mi => mi.CommandParameters)
                .SetValidator(new CommandParameterValidator($"{jsonPath}.CommandParameters"));
            RuleForEach(mi => mi.ExitCode)
                .SetValidator(new ExitCodeValidator($"{jsonPath}.ExitCode"));
        }
    }

    private class CommandParameterValidator : AbstractValidator<CommandParameter>
    {
        public CommandParameterValidator(string jsonPath)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithName($"{jsonPath}.Name")
                .WithMessage(MSG_MUST)
                .MaximumLength(16).WithMessage(MSG_MAXLEN);
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithName($"{jsonPath}.Description")
                .MaximumLength(100).WithMessage(MSG_MAXLEN);
        }
    }

    private class ExitCodeValidator : AbstractValidator<ExitCode>
    {
        public ExitCodeValidator(string jsonPath)
        {
            RuleFor(p => p.Color)
                .NotEmpty()
                .WithName($"{jsonPath}.Color")
                .MaximumLength(16).WithMessage(MSG_MAXLEN)
                .Matches(c => "^(#[0-9A-F]{6}|SUCCESS|WARN|ERROR)$")
                .WithName($"{jsonPath}.Color")
                .WithMessage(MSG_COL_FMT);
        }
    }
}
