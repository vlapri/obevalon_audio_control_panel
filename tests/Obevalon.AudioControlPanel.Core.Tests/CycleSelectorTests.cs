// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Logic;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class CycleSelectorTests
{
    private static OutputEntry Entry(
        string id,
        bool inCycle = true,
        bool isDefault = false,
        bool hidden = false,
        AudioDeviceState? state = AudioDeviceState.Active) =>
        new(id, id, null, null, inCycle, hidden, state, isDefault);

    [Fact]
    public void Next_GoesToFollowingOutputAndWrapsAround()
    {
        OutputEntry[] entries = [Entry("a", isDefault: true), Entry("b"), Entry("c")];
        Assert.Equal("b", CycleSelector.Next(entries));

        entries = [Entry("a"), Entry("b"), Entry("c", isDefault: true)];
        Assert.Equal("a", CycleSelector.Next(entries));
    }

    [Fact]
    public void Previous_GoesBackAndWrapsAround()
    {
        OutputEntry[] entries = [Entry("a", isDefault: true), Entry("b"), Entry("c")];

        Assert.Equal("c", CycleSelector.Previous(entries));
    }

    [Fact]
    public void OnlyMarkedOutputs_TakePart()
    {
        OutputEntry[] entries = [Entry("jack", isDefault: true), Entry("bt", inCycle: false), Entry("hdmi")];

        Assert.Equal("hdmi", CycleSelector.Next(entries));
        Assert.Equal(["jack", "hdmi"], CycleSelector.GetCandidates(entries).Select(e => e.Id));
    }

    [Fact]
    public void UnavailableAndHiddenOutputs_AreSkipped()
    {
        OutputEntry[] entries =
        [
            Entry("a", isDefault: true),
            Entry("unplugged", state: AudioDeviceState.Unplugged),
            Entry("missing", state: null),
            Entry("hidden", hidden: true),
            Entry("b"),
        ];

        Assert.Equal("b", CycleSelector.Next(entries));
    }

    [Fact]
    public void CurrentNotInCycle_GoesToFirstCandidate()
    {
        OutputEntry[] entries = [Entry("x", inCycle: false, isDefault: true), Entry("a"), Entry("b")];

        Assert.Equal("a", CycleSelector.Next(entries));
        Assert.Equal("b", CycleSelector.Previous(entries));
    }

    [Fact]
    public void NoneMarked_AllVisibleAvailableOutputsTakePart()
    {
        OutputEntry[] entries =
        [
            Entry("a", inCycle: false, isDefault: true),
            Entry("b", inCycle: false, hidden: true),
            Entry("c", inCycle: false),
        ];

        Assert.Equal("c", CycleSelector.Next(entries));
    }

    [Fact]
    public void FewerThanTwoCandidates_ReturnsNull()
    {
        Assert.Null(CycleSelector.Next([]));
        Assert.Null(CycleSelector.Next([Entry("a")]));
        Assert.Null(CycleSelector.Next([Entry("a", isDefault: true), Entry("b", state: AudioDeviceState.Unplugged)]));

        // Marked outputs exist but only one is available: no fallback to unmarked ones.
        Assert.Null(CycleSelector.Next([Entry("a", isDefault: true), Entry("b", state: AudioDeviceState.Disabled), Entry("c", inCycle: false)]));
    }
}
