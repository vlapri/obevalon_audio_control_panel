// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class SettingsMigratorTests
{
    [Fact]
    public void FileWithoutVersion_IsVersion1()
    {
        Assert.Equal(1, SettingsMigrator.GetVersion(IniDocument.Parse("[General]\nLanguage=cs")));
    }

    [Fact]
    public void Migrate_SetsCurrentVersion()
    {
        var document = IniDocument.Parse("[General]\nLanguage=cs");

        SettingsMigrator.Migrate(document);

        Assert.Equal(AppSettings.CurrentSettingsVersion, SettingsMigrator.GetVersion(document));
        Assert.Equal("cs", document.GetValue("General", "Language"));
    }

    [Fact]
    public void NewerFile_IsReadAsWellAsPossible()
    {
        var settings = SettingsSerializer.Read(IniDocument.Parse("[General]\nSettingsVersion=99\nLanguage=en\nFutureKey=1"));

        Assert.Equal("en", settings.Language);
    }
}
