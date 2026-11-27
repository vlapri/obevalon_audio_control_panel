// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Hotkeys;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Alt+A", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x41)]
    [InlineData("win+shift+m", HotkeyModifiers.Windows | HotkeyModifiers.Shift, 0x4D)]
    [InlineData(" Control + F5 ", HotkeyModifiers.Control, 0x74)]
    [InlineData("Alt+Num0", HotkeyModifiers.Alt, 0x60)]
    [InlineData("F13", HotkeyModifiers.None, 0x7C)]
    [InlineData("Ctrl+Alt+Space", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x20)]
    public void Parse_ReadsValidGestures(string text, HotkeyModifiers modifiers, int key)
    {
        Assert.Equal(new HotkeyGesture(modifiers, key), HotkeyGesture.Parse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ctrl+Alt")]
    [InlineData("A")]
    [InlineData("Shift+A")]
    [InlineData("F5")]
    [InlineData("Ctrl+Hyper+A")]
    [InlineData("Ctrl+A+B")]
    public void Parse_RejectsInvalidGestures(string? text)
    {
        Assert.Null(HotkeyGesture.Parse(text));
    }

    [Fact]
    public void ToString_UsesFixedModifierOrder()
    {
        var gesture = new HotkeyGesture(HotkeyModifiers.Windows | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control, 0x4D);

        Assert.Equal("Ctrl+Alt+Shift+Win+M", gesture.ToString());
    }

    [Theory]
    [InlineData("Ctrl+Alt+A")]
    [InlineData("Win+F12")]
    [InlineData("Ctrl+Shift+OemPlus")]
    [InlineData("Alt+MediaPlayPause")]
    public void ParseAndFormat_RoundTrip(string text)
    {
        Assert.Equal(text, HotkeyGesture.Parse(text)!.ToString());
    }

    [Fact]
    public void IsValid_RequiresSupportedKeyAndModifier()
    {
        Assert.True(new HotkeyGesture(HotkeyModifiers.Control, 0x41).IsValid);
        Assert.False(new HotkeyGesture(HotkeyModifiers.Shift, 0x41).IsValid);
        Assert.False(new HotkeyGesture(HotkeyModifiers.Control, 0x10).IsValid);
        Assert.True(new HotkeyGesture(HotkeyModifiers.None, 0x87).IsValid);
    }
}
