using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Loudness;

namespace SampleProvider
{
    /// <summary>
    /// Extension methods that perform loudness analysis on an <see cref="ISampleProvider"/>.
    /// </summary>
    public static class SampleProviderLoudnessExtensions
    {
        private const int DefaultBufferSize = 8192;

        /// <summary>
        /// Analyzes the entire <paramref name="source"/> and returns a <see cref="LoudnessMeter"/>
        /// that contains the accumulated loudness data.
        /// </summary>
        /// <param name="source">The audio source to analyse.</param>
        /// <param name="cancellationToken">
        /// Optional token that can be used to cancel the operation. The method checks the token
        /// after each buffer read and throws <see cref="OperationCanceledException"/> if cancellation
        /// is requested.
        /// </param>
        /// <param name="progress">
        /// Optional progress reporter that receives a value in the range <c>0.0 … 1.0</c> representing
        /// the fraction of the source that has been processed. Progress is reported only when the
        /// <paramref name="source"/> is a <see cref="WaveStream"/> (which provides <c>Length</c> and
        /// <c>Position</c> information). If the source does not expose length information, the
        /// progress argument is ignored.
        /// </param>
        /// <returns>A <see cref="LoudnessMeter"/> containing the accumulated analysis data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled via <paramref name="cancellationToken"/>.</exception>
        public static LoudnessMeter Analyze(
            this ISampleProvider source,
            CancellationToken cancellationToken = default,
            IProgress<double>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var meter = new LoudnessMeter();
            var buffer = new float[DefaultBufferSize];
            long totalLength = 0;
            long processed = 0;

            // If the source is a WaveStream we can report progress based on its Length.
            if (source is WaveStream ws)
            {
                totalLength = ws.Length;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = source.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                // Feed the read samples into the meter.
                meter.AddSamples(buffer.AsSpan(0, read));

                // Update progress if possible.
                if (totalLength > 0 && progress is not null)
                {
                    // WaveStream.Position is updated by the underlying Read call.
                    processed = ws.Position;
                    double fraction = (double)processed / totalLength;
                    progress.Report(fraction);
                }
            }

            return meter;
        }

        /// <summary>
        /// Asynchronously analyses the entire <paramref name="source"/> on a background thread,
        /// returning a <see cref="LoudnessMeter"/> when the operation completes.
        /// </summary>
        /// <param name="source">The audio source to analyse.</param>
        /// <param name="cancellationToken">
        /// Optional token that can be used to cancel the operation. The token is observed
        /// by the underlying synchronous <see cref="Analyze(ISampleProvider, CancellationToken, IProgress{double}?)"/>
        /// call.
        /// </param>
        /// <param name="progress">
        /// Optional progress reporter that receives a value in the range <c>0.0 … 1.0</c> representing
        /// the fraction of the source that has been processed. See <see cref="Analyze(ISampleProvider, CancellationToken, IProgress{double}?)"/>
        /// for details.
        /// </param>
        /// <returns>A task that resolves to a <see cref="LoudnessMeter"/> containing the accumulated analysis data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled via <paramref name="cancellationToken"/>.</exception>
        public static Task<LoudnessMeter> AnalyzeAsync(
            this ISampleProvider source,
            CancellationToken cancellationToken = default,
            IProgress<double>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            // Run the synchronous implementation on a thread‑pool thread.
            return Task.Run(() => source.Analyze(cancellationToken, progress), cancellationToken);
        }
    }
}
