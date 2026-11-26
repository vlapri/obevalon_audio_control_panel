// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows.Data;
using System.Windows.Markup;

namespace Obevalon.AudioControlPanel.Localization;

/// <summary>
/// XAML markup extension for localized text that updates when the language changes:
/// <c>Text="{loc:Loc MainWindow_Title}"</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
