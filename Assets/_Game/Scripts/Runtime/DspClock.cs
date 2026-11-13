using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Stand-in song clock until the Conductor exists (day 12): song time = AudioSettings.dspTime since creation,
    /// at a fixed BPM. Rhythm timing never uses Time.time (project rules).
    ///
    /// Not yet verified (see Docs/LOG.md): in WebGL, dspTime may not advance until the browser unlocks audio
    /// (first click or key press), and it advances in audio-buffer steps rather than smoothly.
    /// </summary>
    public sealed class DspClock : IClock
    {
        private readonly double _start;
        private readonly double _beatsPerSecond;

        public DspClock(double bpm)
        {
            _start = AudioSettings.dspTime;
            _beatsPerSecond = bpm / 60.0;
        }

        public double SongTime => AudioSettings.dspTime - _start;

        public double BeatAt(double songTime) => songTime * _beatsPerSecond;

        public double SongTimeAtBeat(double beat) => beat / _beatsPerSecond;
    }
}
