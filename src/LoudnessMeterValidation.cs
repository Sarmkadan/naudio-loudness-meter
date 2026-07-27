using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.LoudnessMeter
{
    public class LoudnessMeterValidation
    {
        public LoudnessMeterValidation(float sampleRate, int channelCount, IProvider provider)
        {
            if (sampleRate < 0)
            {
                throw new ArgumentException("Sample rate must be greater than zero.", nameof(sampleRate));
            }
            if (channelCount <= 0)
            {
                throw new ArgumentException("Channel count must be greater than zero.", nameof(channelCount));
            }
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            // Centralize input validation
            if (sampleRate < 44100 || sampleRate > 192000)
            {
                throw new ArgumentException("Unsupported sample rate. Supported range is 44.1 kHz to 192 kHz.", nameof(sampleRate));
            }
            if (channelCount != 1 && channelCount != 2)
            {
                throw new ArgumentException("Unsupported channel layout. Only mono and stereo are supported.", nameof(channelCount));
            }
        }
    }
