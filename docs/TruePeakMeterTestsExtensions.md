# TruePeakMeterTestsExtensions
The `TruePeakMeterTestsExtensions` class provides a set of static methods for creating and testing `TruePeakMeter` instances, allowing for the verification of true peak and sample peak values within specified ranges. These extensions facilitate the development of loudness metering tests, ensuring accurate measurements and comparisons.

## API
* `CreateMeter`: Creates a new `TruePeakMeter` instance.
* `AssertTruePeakInRange(double truePeak, double min, double max)`: Verifies that the true peak value falls within the specified range. Throws an exception if the value is outside the range.
* `AssertSamplePeakInRange(double samplePeak, double min, double max)`: Verifies that the sample peak value falls within the specified range. Throws an exception if the value is outside the range.
* `AssertTruePeakExceedsSamplePeakBy(double truePeak, double samplePeak, double minDifference)`: Verifies that the true peak value exceeds the sample peak value by at least the specified minimum difference. Throws an exception if the difference is less than the minimum.
* `Reset(TruePeakMeter meter)`: Resets the specified `TruePeakMeter` instance.
* `GetTruePeakString(TruePeakMeter meter)`: Returns a string representation of the true peak value for the specified `TruePeakMeter` instance.
* `GetSamplePeakString(TruePeakMeter meter)`: Returns a string representation of the sample peak value for the specified `TruePeakMeter` instance.
* `GetTruePeakLinear(TruePeakMeter meter)`: Returns the true peak value in linear units for the specified `TruePeakMeter` instance.

## Usage
```csharp
// Example 1: Creating a TruePeakMeter instance and verifying its true peak value
TruePeakMeter meter = TruePeakMeterTestsExtensions.CreateMeter();
// ... process audio data using the meter ...
double truePeak = meter.GetTruePeakLinear();
TruePeakMeterTestsExtensions.AssertTruePeakInRange(truePeak, -1.0, 0.0);

// Example 2: Comparing true peak and sample peak values
TruePeakMeter meter2 = TruePeakMeterTestsExtensions.CreateMeter();
// ... process audio data using the meter ...
double truePeak2 = meter2.GetTruePeakLinear();
double samplePeak2 = meter2.GetSamplePeakLinear();
TruePeakMeterTestsExtensions.AssertTruePeakExceedsSamplePeakBy(truePeak2, samplePeak2, 0.1);
```

## Notes
When using the `AssertTruePeakInRange` and `AssertSamplePeakInRange` methods, ensure that the specified range is valid and non-empty, as invalid ranges may cause exceptions. The `Reset` method should be used to reset the `TruePeakMeter` instance between tests to prevent interference. The `GetTruePeakString` and `GetSamplePeakString` methods are useful for logging or displaying peak values, but may not be suitable for precise numerical comparisons due to potential rounding errors. The `GetTruePeakLinear` method returns the true peak value in linear units, which may require additional processing or scaling depending on the specific application. The `TruePeakMeterTestsExtensions` class is designed to be thread-safe, but it is still important to ensure that `TruePeakMeter` instances are properly synchronized when accessed from multiple threads.
