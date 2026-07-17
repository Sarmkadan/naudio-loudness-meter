# LoudnessMeterTestsExtensions

A static extension class that provides test‑helper methods for the `LoudnessMeter` type. It enables concise assertions on loudness measurements (integrated, momentary, short‑term, loudness range) and offers utility methods for feeding sample data and computing loudness differences. Designed for unit‑test scenarios where measured values must be compared against expected references with a configurable tolerance.

## API

### `AssertIntegratedLufs`

```csharp
public static void AssertIntegratedLufs(this LoudnessMeter meter, double expected, double tolerance)
```

Asserts that the integrated loudness (in LUFS) measured by the meter is within `tolerance` of `expected`.  
**Parameters:**  
- `meter` – The `LoudnessMeter` instance whose integrated loudness is checked.  
- `expected` – The expected integrated loudness value in LUFS.  
- `tolerance` – The allowed absolute difference between the measured and expected values.  

**Throws:**  
- An assertion exception (e.g., `Xunit.Sdk.AssertException` or `NUnit.Framework.AssertionException`) if the condition is not met.

---

### `AssertMomentaryLufs`

```csharp
public static void AssertMomentaryLufs(this LoudnessMeter meter, double expected, double tolerance)
```

Asserts that the momentary loudness (in LUFS) measured by the meter is within `tolerance` of `expected`.  
**Parameters:**  
- `meter` – The `LoudnessMeter` instance whose momentary loudness is checked.  
- `expected` – The expected momentary loudness value in LUFS.  
- `tolerance` – The allowed absolute difference.  

**Throws:**  
- An assertion exception if the condition is not met.

---

### `AssertShortTermLufs`

```csharp
public static void AssertShortTermLufs(this LoudnessMeter meter, double expected, double tolerance)
```

Asserts that the short‑term loudness (in LUFS) measured by the meter is within `tolerance` of `expected`.  
**Parameters:**  
- `meter` – The `LoudnessMeter` instance whose short‑term loudness is checked.  
- `expected` – The expected short‑term loudness value in LUFS.  
- `tolerance` – The allowed absolute difference.  

**Throws:**  
- An assertion exception if the condition is not met.

---

### `AssertApproximatelyEqual`

```csharp
public static void AssertApproximatelyEqual(this double actual, double expected, double tolerance)
```

Asserts that two `double` values are approximately equal within the given tolerance.  
**Parameters:**  
- `actual` – The measured or computed value.  
- `expected` – The reference value.  
- `tolerance` – The allowed absolute difference.  

**Throws:**  
- An assertion exception if `|actual - expected| > tolerance`.

---

### `WithSamples`

```csharp
public static LoudnessMeter WithSamples(this LoudnessMeter meter, float[] samples)
```

Feeds an array of audio samples into the meter and returns the same meter instance. Enables fluent chaining.  
**Parameters:**  
- `meter` – The `LoudnessMeter` to feed samples into.  
- `samples` – A `float[]` of audio sample values (typically in the range [-1.0, 1.0]).  

**Returns:**  
- The same `LoudnessMeter` instance, after processing the samples.

**Throws:**  
- `ArgumentNullException` if `samples` is `null`.  
- May throw other exceptions if the meter’s internal state is invalid (e.g., sample rate mismatch).

---

### `AssertLoudnessRange`

```csharp
public static void AssertLoudnessRange(this LoudnessMeter meter, double expected, double tolerance)
```

Asserts that the loudness range (LRA, in LU) measured by the meter is within `tolerance` of `expected`.  
**Parameters:**  
- `meter` – The `LoudnessMeter` instance whose loudness range is checked.  
- `expected` – The expected loudness range value in LU.  
- `tolerance` – The allowed absolute difference.  

**Throws:**  
- An assertion exception if the condition is not met.

---

### `LoudnessDifference`

```csharp
public static double LoudnessDifference(this LoudnessMeter meter, double referenceLoudness)
```

Computes the difference between the meter’s integrated loudness and a reference loudness value.  
**Parameters:**  
- `meter` – The `LoudnessMeter` whose integrated loudness is used.  
- `referenceLoudness` – The reference loudness value in LUFS.  

**Returns:**  
- A `double` representing `integratedLoudness - referenceLoudness`.

**Throws:**  
- `InvalidOperationException` if the meter has not processed any samples (integrated loudness is undefined).

---

## Usage

### Example 1: Feeding samples and asserting integrated loudness

```csharp
using NAudio.LoudnessMeter;

[Fact]
public void SineWave_ShouldHaveExpectedIntegratedLoudness()
{
    // Arrange: generate a 1 kHz sine wave at -20 dBFS
    float[] samples = GenerateSineWave(1000, 48000, 1.0, -20.0);
    var meter = new LoudnessMeter(48000);

    // Act: feed samples using the extension
    meter.WithSamples(samples);

    // Assert: integrated loudness should be approximately -20 LUFS
    meter.AssertIntegratedLufs(-20.0, 0.5);
}
```

### Example 2: Using loudness difference and range assertions

```csharp
using NAudio.LoudnessMeter;

[Fact]
public void AudioClip_LoudnessRange_ShouldBeWithinTolerance()
{
    // Arrange
    float[] clip = LoadTestAudio("speech.wav");
    var meter = new LoudnessMeter(44100);

    // Act
    meter.WithSamples(clip);

    // Assert loudness range
    meter.AssertLoudnessRange(8.0, 1.0);

    // Compute difference from a target loudness
    double diff = meter.LoudnessDifference(-23.0);
    AssertApproximatelyEqual(diff, 0.0, 0.5);
}
```

---

## Notes

- All assertion methods throw an exception when the condition fails. The exact exception type depends on the test framework in use (e.g., xUnit, NUnit, MSTest).  
- `WithSamples` modifies the internal state of the `LoudnessMeter`. Calling it multiple times accumulates sample data; there is no automatic reset.  
- The meter must be configured with the correct sample rate before feeding samples. Passing samples with a different sample rate than the meter’s constructor argument may produce incorrect measurements.  
- `LoudnessDifference` requires that at least one block of samples has been processed; otherwise an `InvalidOperationException` is thrown.  
- These extension methods are **not thread‑safe**. They are intended for single‑threaded test execution. Concurrent calls on the same `LoudnessMeter` instance will produce undefined behavior.  
- Edge cases:  
  - An empty `samples` array will not change the meter’s state; subsequent assertions may fail if no data has been processed.  
  - Sample values outside the typical [-1.0, 1.0] range are accepted but may lead to unrealistic loudness readings.  
  - Tolerance values must be non‑negative; negative tolerances will cause assertions to always fail (or throw depending on the framework).
