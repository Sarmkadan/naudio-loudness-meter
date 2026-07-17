using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NAudio.Loudness;

/// <summary>
/// Validation helpers for <see cref="LoudnessMeter"/>.
/// </summary>
public static class LoudnessMeterValidation
{
    /// <summary>
    /// Returns a read‑only list of validation problems for the supplied <see cref="LoudnessMeter"/>.
    /// </summary>
    /// <param name="value">The meter to validate.</param>
    /// <returns>A read‑only list of human‑readable problem descriptions. The list is empty when the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Validate(this LoudnessMeter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // SampleRate must be a positive integer.
        if (value.SampleRate <= 0)
        {
            problems.Add(string.Format(CultureInfo.InvariantCulture,
                "SampleRate must be greater than zero, but was {0}.", value.SampleRate));
        }

        // No other public members expose state that can be validated.
        return problems;
    }

    /// <summary>
    /// Determines whether the supplied <see cref="LoudnessMeter"/> is valid.
    /// </summary>
    /// <param name="value">The meter to validate.</param>
    /// <returns><c>true</c> if the meter has no validation problems; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static bool IsValid(this LoudnessMeter value) =>
        value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the supplied <see cref="LoudnessMeter"/> is valid.
    /// </summary>
    /// <param name="value">The meter to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the meter has one or more validation problems. The exception message contains the list of problems.</exception>
    public static void EnsureValid(this LoudnessMeter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            var message = string.Join(Environment.NewLine, problems);
            throw new ArgumentException(message, nameof(value));
        }
    }
}
