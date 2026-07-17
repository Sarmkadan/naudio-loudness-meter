using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace NAudio.Loudness.Tests
{
    /// <summary>
    /// Validation helpers for <see cref="LoudnessMeterTests"/>.
    /// Validates that all expected test methods exist and are properly defined in the <see cref="LoudnessMeterTests"/> class.
    /// </summary>
    public static class LoudnessMeterTestsValidation
    {
        private static readonly IReadOnlyList<string> ExpectedTestMethods = new List<string>
        {
            nameof(LoudnessMeterTests.FullScaleSine_Mono_IsMinus3Lufs),
            nameof(LoudnessMeterTests.FullScaleSine_Stereo_IsAboutZeroLufs),
            nameof(LoudnessMeterTests.HalvingAmplitude_DropsSixLu),
            nameof(LoudnessMeterTests.SineTunedToMinus23_MeasuresMinus23),
            nameof(LoudnessMeterTests.Silence_IntegratedIsNegativeInfinity),
            nameof(LoudnessMeterTests.AbsoluteGate_IgnoresSilentSection),
            nameof(LoudnessMeterTests.MomentaryAndShortTerm_AgreeOnSteadyTone),
            nameof(LoudnessMeterTests.NotEnoughAudio_WindowsReturnNegativeInfinity),
            nameof(LoudnessMeterTests.SampleRateIndependent_WithinTolerance)
        };

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
            var type = typeof(LoudnessMeterTests);

            foreach (var methodName in ExpectedTestMethods)
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

                if (method == null)
                {
                    problems.Add($"Test method '{methodName}' is missing from {type.Name}");
                }
                else if (method.ReturnType != typeof(void))
                {
                    problems.Add($"Test method '{methodName}' has incorrect return type. Expected: void, Actual: {method.ReturnType.Name}");
                }
                else if (!method.IsPublic)
                {
                    problems.Add($"Test method '{methodName}' is not public");
                }
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