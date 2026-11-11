using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    /// <summary>A tempo change inside a level: from <see cref="beat"/> on, the song runs at <see cref="bpm"/>.</summary>
    [Serializable]
    public sealed class TempoChange
    {
        public double beat;
        public double bpm;
    }

    /// <summary>A palette change inside a level: on <see cref="beat"/>, the look switches to <see cref="palette"/>.</summary>
    [Serializable]
    public sealed class PaletteChange
    {
        public double beat;
        public string palette;
    }

    /// <summary>
    /// Beats to seconds and back for a level whose tempo can change (level 6: 130 to 150 BPM at the midpoint).
    /// Beat 0 is at 0 s. Before the first change, and for the count-in before beat 0, the base tempo applies.
    /// With no changes this is exactly beat x 60 / bpm, as before.
    /// </summary>
    public sealed class TempoMap
    {
        private readonly double[] _beat;   // start beat of each section; _beat[0] = 0
        private readonly double[] _time;   // start time of each section
        private readonly double[] _bpm;

        public TempoMap(double bpm, TempoChange[] changes = null)
        {
            if (bpm <= 0) throw new ArgumentException("bpm must be > 0");
            var list = new List<TempoChange>();
            if (changes != null)
                foreach (TempoChange c in changes)
                    if (c != null && c.beat > 0 && c.bpm > 0) list.Add(c);
            list.Sort((a, b) => a.beat.CompareTo(b.beat));

            int n = list.Count + 1;
            _beat = new double[n];
            _time = new double[n];
            _bpm = new double[n];
            _bpm[0] = bpm;
            for (int i = 1; i < n; i++)
            {
                _beat[i] = list[i - 1].beat;
                _bpm[i] = list[i - 1].bpm;
                _time[i] = _time[i - 1] + (_beat[i] - _beat[i - 1]) * 60.0 / _bpm[i - 1];
            }
        }

        public double BaseBpm => _bpm[0];
        public bool HasChanges => _bpm.Length > 1;

        private int SectionAtBeat(double beat)
        {
            int i = _beat.Length - 1;
            while (i > 0 && beat < _beat[i]) i--;
            return i;
        }

        public double BpmAtBeat(double beat) => _bpm[SectionAtBeat(beat)];

        public double TimeAtBeat(double beat)
        {
            int i = SectionAtBeat(beat);
            return _time[i] + (beat - _beat[i]) * 60.0 / _bpm[i];
        }

        public double BeatAtTime(double time)
        {
            int i = _time.Length - 1;
            while (i > 0 && time < _time[i]) i--;
            return _beat[i] + (time - _time[i]) * _bpm[i] / 60.0;
        }

        /// <summary>Problems in a level's tempo list. Empty = valid. Null or empty list = one tempo.</summary>
        public static List<string> Validate(TempoChange[] changes, double lengthBeats)
        {
            var errors = new List<string>();
            if (changes == null) return errors;
            double previous = 0;
            for (int i = 0; i < changes.Length; i++)
            {
                TempoChange c = changes[i];
                string at = $"tempo {i} (beat {c.beat})";
                if (c.beat <= 0 || c.beat >= lengthBeats) errors.Add($"{at}: beat must be inside the level, after beat 0");
                if (c.beat <= previous && i > 0) errors.Add($"{at}: tempo changes must be in order");
                if (c.bpm < 40 || c.bpm > 300) errors.Add($"{at}: bpm {c.bpm} is outside 40-300");
                previous = c.beat;
            }
            return errors;
        }
    }
}
