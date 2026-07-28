public class LoudnessAnalysisJsonExtensions
    {
        public static void ToJson(LoudnessAnalysis analysis, string path)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            // ... rest of the method remains the same ...
        }

        public static void SaveToFile(LoudnessAnalysis analysis, string path)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            // ... rest of the method remains the same ...
        }

        public static LoudnessAnalysis LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            // ... rest of the method remains the same ...
        }
    }