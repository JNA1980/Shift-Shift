using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    /// <summary>
    /// One level chart (Assets/_Game/Levels/levelNN.json), playbook 1.3. Parsed with Unity's JsonUtility at runtime
    /// (works in WebGL, no System.IO); plain fields so the format stays human-readable and diffable. See Docs/LEVELS.md.
    /// </summary>
    [Serializable]
    public sealed class LevelData
    {
        public string name;
        public double bpm;
        /// <summary>Resources path of the music, without extension (e.g. "click_100" for Audio/Resources/click_100.wav).</summary>
        public string audioClip;
        /// <summary>Where beat 0 sits in the audio file, in milliseconds.</summary>
        public double offsetMs;
        public string palette;
        public double lengthBeats;
        public LevelEvent[] events;
        /// <summary>Optional track pieces (playbook 2.3). Missing or empty = a straight track.</summary>
        public TrackEvent[] track;

        /// <summary>Problems that would make the level unplayable. Empty list = valid.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(name)) errors.Add("name is missing");
            if (bpm < 40 || bpm > 300) errors.Add($"bpm {bpm} is outside 40-300");
            if (lengthBeats <= 0) errors.Add("lengthBeats must be > 0");
            if (offsetMs < 0) errors.Add("offsetMs must be >= 0");
            if (events == null) { errors.Add("events is missing"); return errors; }

            var seen = new HashSet<(double, int)>();
            double previous = double.NegativeInfinity;
            for (int i = 0; i < events.Length; i++)
            {
                LevelEvent e = events[i];
                string at = $"event {i} (beat {e.beat})";
                if (e.beat < 0 || e.beat > lengthBeats) errors.Add($"{at}: beat outside 0-{lengthBeats}");
                if (e.beat < previous) errors.Add($"{at}: events must be sorted by beat");
                previous = e.beat;
                if (e.lane < 0 || e.lane > 2) errors.Add($"{at}: lane {e.lane} must be 0, 1 or 2");
                if (!ObstacleTypes.TryGet(e.type, out _)) errors.Add($"{at}: unknown type '{e.type}'");
                if (!seen.Add((e.beat, e.lane))) errors.Add($"{at}: two events on the same beat and lane");
            }
            errors.AddRange(TrackPieces.Validate(track, lengthBeats));
            return errors;
        }
    }

    [Serializable]
    public sealed class LevelEvent
    {
        public double beat;
        /// <summary>0 left, 1 middle, 2 right (ignored for all-lane types).</summary>
        public int lane;
        /// <summary>
        /// bar | wide | low | gate | gap | hurdle | ghost | ghostbar | ghostwide | wall.
        /// For wide, gate and ghostwide, lane = the free lane. For ghostbar, lane = the lane with the solid core.
        /// </summary>
        public string type;

        /// <summary>Runner lane (-1..1) for this event.</summary>
        public int RunnerLane => lane - 1;
    }

    /// <summary>How a chart "type" maps to obstacle rules. One place to change if the meaning of a type changes.</summary>
    public static class ObstacleTypes
    {
        /// <summary>Solid cores inside a phase barrier (the ghost layer, level 4): they only hit while phased.</summary>
        public enum Cores { None, EventLane, OtherLanes }

        public readonly struct Spec
        {
            public readonly ObstacleKind Kind;
            public readonly ObstacleLayer Layer;
            public readonly bool AllLanes;
            /// <summary>Blocks every lane except the event's lane (gate opening, wide bar's free lane).</summary>
            public readonly bool LaneIsOpening;
            /// <summary>
            /// A barrier across all lanes with ghost-layer cores: in the event's lane, or in the other two.
            /// The player must phase AND not be in a core's lane.
            /// </summary>
            public readonly Cores Cores;

            public Spec(ObstacleKind kind, ObstacleLayer layer, bool allLanes, bool laneIsOpening = false,
                Cores cores = Cores.None)
            {
                Cores = cores;
                Kind = kind;
                Layer = layer;
                AllLanes = allLanes;
                LaneIsOpening = laneIsOpening;
            }
        }

        private static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            { "bar", new Spec(ObstacleKind.Block, ObstacleLayer.Solid, false) }, // change lane
            { "wide", new Spec(ObstacleKind.Block, ObstacleLayer.Solid, false, true) }, // two lanes blocked: be in the free lane
            { "low", new Spec(ObstacleKind.Low, ObstacleLayer.Solid, true) },    // slide (all lanes: no dodging it, John day 13)
            { "gate", new Spec(ObstacleKind.Gate, ObstacleLayer.Solid, false, true) }, // sprint through the one open lane
            { "gap", new Spec(ObstacleKind.Gap, ObstacleLayer.Solid, true) },    // jump
            { "hurdle", new Spec(ObstacleKind.Hurdle, ObstacleLayer.Solid, true) }, // short wall: jump
            // "ghost": a barrier across all lanes that only phase (Both hold) gets through (playbook 1.4: ghost -> phase held).
            { "ghost", new Spec(ObstacleKind.Barrier, ObstacleLayer.Solid, true) },
            // Two overlapping layers (plan, level 4). The barrier is spawned across all lanes; "allLanes" is false
            // here only so the chart keeps the event's lane.
            { "ghostbar", new Spec(ObstacleKind.Barrier, ObstacleLayer.Solid, false, false, Cores.EventLane) },   // phase, but not in this lane
            { "ghostwide", new Spec(ObstacleKind.Barrier, ObstacleLayer.Solid, false, false, Cores.OtherLanes) }, // phase in this lane
            { "wall", new Spec(ObstacleKind.Wall, ObstacleLayer.Solid, true) },  // charge
        };

        public static IEnumerable<string> Names => Specs.Keys;

        public static bool TryGet(string type, out Spec spec)
        {
            spec = default;
            return type != null && Specs.TryGetValue(type, out spec);
        }
    }
}
