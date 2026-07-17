using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NAudio.Loudness.Tests
{
    /// <summary>
    /// Validation helpers for <see cref="LoudnessMeterTests"/>.
    /// </summary>
    public static class LoudnessMeterTestsValidation
    {
        /// <summary>
        /// Validates the given <paramref name="value"/> and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The <see cref="LoudnessMeterTests"/> instance to validate.</param>
        /// <returns>A list of human-readable problems.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this LoudnessMeterTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Since LoudnessMeterTests only contains methods and no properties, 
            // there's nothing to validate in terms of null/empty strings or out-of-range numbers.
            // However, we can still validate that the methods are not null.

            if (value.FullScaleSine_Mono_IsMinus3Lufs == null)
            {
                problems.Add("FullScaleSine_Mono_IsMinus3Lufs is null");
            }

            if (value.FullScaleSine_Stereo_IsAboutZeroLufs == null)
            {
                problems.Add("FullScaleSine_Stereo_IsAboutZeroLufs is null");
            }

            if (value.HalvingAmplitude_DropsSixLu == null)
            {
                problems.Add("HalvingAmplitude_DropsSixLu is null");
            }

            if (value.SineTunedToMinus23_MeasuresMinus23 == null)
            {
                problems.Add("SineTunedToMinus23_MeasuresMinus23 is null");
            }

            if (value.Silence_IntegratedIsNegativeInfinity == null)
            {
                problems.Add("Silence_IntegratedIsNegativeInfinity is null");
            }

            if (value.AbsoluteGate_IgnoresSilentSection == null)
            {
                problems.Add("AbsoluteGate_IgnoresSilentSection is null");
            }

            if (value.MomentaryAndShortTerm_AgreeOnSteadyTone == null)
            {
                problems.Add("MomentaryAndShortTerm_AgreeOnSteadyTone is null");
            }

            if (value.NotEnoughAudio_WindowsReturnNegativeInfinity == null)
            {
                problems.Add("NotEnoughAudio_WindowsReturnNegativeInfinity is null");
            }

            if (value.SampleRateIndependent_WithinTolerance == null)
            {
                problems.Add("SampleRateIndependent_WithinTolerance is null");
            }

            return new ReadOnlyCollection<string>(problems);
        }

        /// <summary>
        /// Checks if the given <paramref name="value"/> is valid.
        /// </summary>
        /// <param name="value">The <see cref="LoudnessMeterTests"/> instance to check.</param>
        /// <returns>True if the instance is valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this LoudnessMeterTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures the given <paramref name="value"/> is valid.
        /// </summary>
        /// <param name="value">The <see cref="LoudnessMeterTests"/> instance to ensure.</param>
        /// <exception cref="ArgumentException">Thrown if the instance is not valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this LoudnessMeterTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);

            if (problems.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, problems), nameof(value));
            }
        }
    }
}
