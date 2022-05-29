using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace LocalAudioLinkTakeover
{
    // basically a copy & paste version of NAudio's WaveInProvider but with a configurable buffer size
    internal class ConfigurableWaveInProvider : IWaveProvider
    {
        private readonly IWaveIn waveIn;
        private readonly BufferedWaveProvider bufferedWaveProvider;

        /// <summary>
        /// Creates a new ConfigurableWaveInProvider
        /// n.b. Should make sure the WaveFormat is set correctly on IWaveIn before calling
        /// </summary>
        /// <param name="waveIn">The source of wave data</param>
        public ConfigurableWaveInProvider(IWaveIn waveIn)
        {
            this.waveIn = waveIn;
            waveIn.DataAvailable += OnDataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(WaveFormat);
        }

        public int BufferLength {
            get
            {
                return bufferedWaveProvider.BufferLength;
            }
            set
            {
                bufferedWaveProvider.BufferLength = value;
            }
        }

        public TimeSpan BufferDuration
        {
            get
            {
                return bufferedWaveProvider.BufferDuration;
            }
            set
            {
                bufferedWaveProvider.BufferDuration = value;
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        /// <summary>
        /// Reads data from the WaveInProvider
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            return bufferedWaveProvider.Read(buffer, offset, count);
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveIn.WaveFormat;
    }
}
