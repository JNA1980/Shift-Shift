using System;
using System.IO;

namespace ShiftShift.Core
{
    /// <summary>
    /// Generates a metronome as a 16-bit mono WAV (playbook 1.3: level00's audio). Beat 0 is at the very start of
    /// the file (offsetMs = 0). Accented click on the first beat of each bar. Used by an editor menu item; the
    /// MemoryStream here is in-memory only, not file I/O.
    /// </summary>
    public static class ClickTrack
    {
        public const double ClickSeconds = 0.03;

        public static byte[] GenerateWav(double bpm, int beats, int beatsPerBar = 4, int sampleRate = 44100)
        {
            if (bpm <= 0) throw new ArgumentException("bpm must be > 0");
            if (beats <= 0) throw new ArgumentException("beats must be > 0");

            double beatSeconds = 60.0 / bpm;
            int totalSamples = (int)Math.Ceiling(beats * beatSeconds * sampleRate);
            var pcm = new short[totalSamples];
            int clickSamples = (int)(ClickSeconds * sampleRate);

            for (int b = 0; b < beats; b++)
            {
                bool accent = b % beatsPerBar == 0;
                double freq = accent ? 1500.0 : 1000.0;
                double amp = accent ? 0.9 : 0.6;
                int start = (int)Math.Round(b * beatSeconds * sampleRate);
                for (int i = 0; i < clickSamples && start + i < totalSamples; i++)
                {
                    double t = (double)i / sampleRate;
                    double env = Math.Exp(-t * 120.0); // fast decay: sharp, short click
                    pcm[start + i] = (short)(Math.Sin(2 * Math.PI * freq * t) * env * amp * short.MaxValue);
                }
            }

            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataBytes = totalSamples * 2;
                w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                w.Write(36 + dataBytes);
                w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                w.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                w.Write(16);                // fmt chunk size
                w.Write((short)1);          // PCM
                w.Write((short)1);          // mono
                w.Write(sampleRate);
                w.Write(sampleRate * 2);    // byte rate
                w.Write((short)2);          // block align
                w.Write((short)16);         // bits per sample
                w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                w.Write(dataBytes);
                foreach (short s in pcm) w.Write(s);
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
