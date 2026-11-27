// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Logic;

/// <summary>Chooses the next output for "next output" cycling (SPEC F1.5).</summary>
public static class CycleSelector
{
    /// <summary>
    /// Outputs that take part in cycling, in the user's order: visible, available and marked "in cycle".
    /// When no output is marked at all, every visible available output takes part.
    /// </summary>
    public static IReadOnlyList<OutputEntry> GetCandidates(IReadOnlyList<OutputEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var anyMarked = entries.Any(e => e.InCycle);
        return entries
            .Where(e => !e.Hidden && e.IsAvailable && (e.InCycle || !anyMarked))
            .ToList();
    }

    /// <summary>
    /// Returns the ID of the output after the current default one, the first candidate when the current
    /// one is not a candidate, or null when fewer than two outputs can take part.
    /// </summary>
    public static string? Next(IReadOnlyList<OutputEntry> entries) => Step(entries, +1);

    /// <summary>Like <see cref="Next"/>, in the opposite direction.</summary>
    public static string? Previous(IReadOnlyList<OutputEntry> entries) => Step(entries, -1);

    private static string? Step(IReadOnlyList<OutputEntry> entries, int direction)
    {
        var candidates = GetCandidates(entries);
        if (candidates.Count < 2)
        {
            return null;
        }

        var current = -1;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].IsDefault)
            {
                current = i;
                break;
            }
        }

        if (current < 0)
        {
            return candidates[direction > 0 ? 0 : candidates.Count - 1].Id;
        }

        var next = (current + direction + candidates.Count) % candidates.Count;
        return candidates[next].Id;
    }
}
