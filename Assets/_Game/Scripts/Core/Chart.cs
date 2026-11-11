using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ShiftShift.Core
{
    /// <summary>
    /// Editable chart for the ChartEditor window (playbook 2.1): place/move/remove events on a beat grid, the editor's
    /// checks, counts, and a writer that saves human-readable JSON sorted by beat. Engine-free so it is tested
    /// outside Unity; the window only draws and handles the mouse.
    /// </summary>
    public sealed class Chart
    {
        public LevelData Level { get; }
        public List<LevelEvent> Events { get; }

        public Chart(LevelData level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Events = new List<LevelEvent>(level.events ?? Array.Empty<LevelEvent>());
            Sort();
        }

        public double LengthSeconds => Level.bpm > 0 ? new TempoMap(Level.bpm, Level.tempo).TimeAtBeat(Level.lengthBeats) : 0;

        /// <summary>Nearest grid position: subdivision 1 = quarter notes (whole beats), 2 = eighths.</summary>
        public static double Snap(double beat, int subdivision)
        {
            if (subdivision < 1) subdivision = 1;
            return Math.Round(beat * subdivision, MidpointRounding.AwayFromZero) / subdivision;
        }

        public static bool IsAllLanes(string type) => ObstacleTypes.TryGet(type, out var s) && s.AllLanes;

        /// <summary>Occupies its whole beat: all-lane types, opening types (gate, wide bar) and barriers with ghost blocks.</summary>
        public static bool SpansBeat(string type) =>
            ObstacleTypes.TryGet(type, out var s) && (s.AllLanes || s.LaneIsOpening || s.GhostBlocks != ObstacleTypes.GhostBlocks.None);

        /// <summary>The event drawn in this cell (an all-lane event covers every lane of its beat), or null.</summary>
        public LevelEvent At(double beat, int lane)
        {
            foreach (LevelEvent e in Events)
                if (Same(e.beat, beat) && (e.lane == lane || SpansBeat(e.type))) return e;
            return null;
        }

        /// <summary>Places an event, replacing whatever it would overlap on that beat. Clamped to the level.</summary>
        public LevelEvent Place(double beat, int lane, string type)
        {
            if (!ObstacleTypes.TryGet(type, out _)) throw new ArgumentException($"unknown type '{type}'");
            beat = Math.Max(0, Math.Min(Level.lengthBeats, beat));
            lane = Math.Max(0, Math.Min(2, lane));
            bool spans = SpansBeat(type);
            Events.RemoveAll(e => Same(e.beat, beat) && (spans || SpansBeat(e.type) || e.lane == lane));
            // All-lane types are stored in the middle lane; opening types keep the clicked lane as the open lane.
            var added = new LevelEvent { beat = beat, lane = IsAllLanes(type) ? 1 : lane, type = type };
            Events.Add(added);
            Sort();
            return added;
        }

        public bool Remove(double beat, int lane)
        {
            LevelEvent e = At(beat, lane);
            return e != null && Events.Remove(e);
        }

        /// <summary>Moves an event to a new cell (replacing whatever is there).</summary>
        public LevelEvent Move(LevelEvent e, double beat, int lane)
        {
            Events.Remove(e);
            return Place(beat, lane, e.type);
        }

        public Dictionary<string, int> Counts()
        {
            var counts = new Dictionary<string, int>();
            foreach (string t in ObstacleTypes.Names) counts[t] = 0;
            foreach (LevelEvent e in Events)
                if (counts.ContainsKey(e.type)) counts[e.type]++;
            return counts;
        }

        public sealed class Report
        {
            public readonly List<string> Errors = new List<string>();   // save is refused
            public readonly List<string> Warnings = new List<string>(); // save allowed
        }

        /// <summary>
        /// The level's own validation (errors) plus the editor checks from playbook 2.1 (warnings):
        /// events in the first 8 beats; with <paramref name="strictSpacing"/> (levels 1-4), two different types less
        /// than 1 beat apart.
        /// </summary>
        public Report Check(bool strictSpacing)
        {
            var r = new Report();
            LevelData copy = ToLevel();
            r.Errors.AddRange(copy.Validate());

            // An all-lane obstacle cannot share a beat with anything else.
            for (int i = 0; i < Events.Count; i++)
            for (int j = i + 1; j < Events.Count; j++)
            {
                LevelEvent a = Events[i], b = Events[j];
                if (Same(a.beat, b.beat) && (SpansBeat(a.type) || SpansBeat(b.type)) && a.lane != b.lane)
                    r.Errors.Add($"beat {Fmt(a.beat)}: '{a.type}' and '{b.type}' overlap (one spans the whole beat)");
            }

            foreach (LevelEvent e in Events)
                if (e.beat < 8) r.Warnings.Add($"beat {Fmt(e.beat)}: '{e.type}' is in the first 8 beats");

            // Track pieces (playbook 2.3).
            if (Level.track != null)
            {
                double turn = 0, maxTurn = 0;
                foreach (TrackEvent t in Level.track)
                {
                    if (!TrackPieces.TryGet(t.piece, out TrackPieces.Spec spec)) continue;
                    if (!spec.TurnReturns) turn += spec.TurnDegrees;
                    maxTurn = Math.Max(maxTurn, Math.Abs(turn));
                    if (spec.RiseMetres == 0) continue;
                    foreach (LevelEvent e in Events)
                        if (e.type == "gap" && e.beat >= t.beat - 1 && e.beat < t.beat + spec.LengthBeats)
                            r.Warnings.Add($"beat {Fmt(e.beat)}: gap on a sloped piece ('{t.piece}'): the hole may not sit flush");
                }
                if (maxTurn > 120)
                    r.Warnings.Add($"track turns {Fmt(maxTurn)} degrees one way: it may cross itself; mix left and right bends");
            }

            if (strictSpacing)
                for (int i = 0; i + 1 < Events.Count; i++)
                {
                    LevelEvent a = Events[i], b = Events[i + 1];
                    if (a.type != b.type && b.beat - a.beat < 1.0 - 1e-9)
                        r.Warnings.Add($"beats {Fmt(a.beat)}-{Fmt(b.beat)}: '{a.type}' and '{b.type}' less than 1 beat apart");
                }
            return r;
        }

        /// <summary>The level with the current events, sorted.</summary>
        public LevelData ToLevel() => new LevelData
        {
            name = Level.name,
            bpm = Level.bpm,
            audioClip = Level.audioClip,
            offsetMs = Level.offsetMs,
            palette = Level.palette,
            lengthBeats = Level.lengthBeats,
            events = Events.ToArray(),
            track = Level.track, // edited by hand in the JSON for now; kept as loaded
            tempo = Level.tempo,
            paletteChanges = Level.paletteChanges,
        };

        /// <summary>Human-readable JSON, two-space indent, events sorted by beat then lane, invariant number format.</summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"name\": ").Append(Str(Level.name)).Append(",\n");
            sb.Append("  \"bpm\": ").Append(Fmt(Level.bpm)).Append(",\n");
            sb.Append("  \"audioClip\": ").Append(Str(Level.audioClip)).Append(",\n");
            sb.Append("  \"offsetMs\": ").Append(Fmt(Level.offsetMs)).Append(",\n");
            sb.Append("  \"palette\": ").Append(Str(Level.palette)).Append(",\n");
            sb.Append("  \"lengthBeats\": ").Append(Fmt(Level.lengthBeats)).Append(",\n");
            sb.Append("  \"events\": [");
            for (int i = 0; i < Events.Count; i++)
            {
                LevelEvent e = Events[i];
                sb.Append(i == 0 ? "\n" : ",\n");
                sb.Append("    {\n");
                sb.Append("      \"beat\": ").Append(Fmt(e.beat)).Append(",\n");
                sb.Append("      \"lane\": ").Append(e.lane.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("      \"type\": ").Append(Str(e.type)).Append('\n');
                sb.Append("    }");
            }
            bool hasTrack = Level.track != null && Level.track.Length > 0;
            bool hasTempo = Level.tempo != null && Level.tempo.Length > 0;
            bool hasPalettes = Level.paletteChanges != null && Level.paletteChanges.Length > 0;
            sb.Append(Events.Count > 0 ? "\n  ]" : "]").Append(hasTrack || hasTempo || hasPalettes ? ",\n" : "\n");
            if (hasTempo)
            {
                sb.Append("  \"tempo\": [");
                for (int i = 0; i < Level.tempo.Length; i++)
                    sb.Append(i == 0 ? "\n" : ",\n").Append("    { \"beat\": ").Append(Fmt(Level.tempo[i].beat))
                        .Append(", \"bpm\": ").Append(Fmt(Level.tempo[i].bpm)).Append(" }");
                sb.Append("\n  ]").Append(hasPalettes || hasTrack ? ",\n" : "\n");
            }
            if (hasPalettes)
            {
                sb.Append("  \"paletteChanges\": [");
                for (int i = 0; i < Level.paletteChanges.Length; i++)
                    sb.Append(i == 0 ? "\n" : ",\n").Append("    { \"beat\": ").Append(Fmt(Level.paletteChanges[i].beat))
                        .Append(", \"palette\": ").Append(Str(Level.paletteChanges[i].palette)).Append(" }");
                sb.Append("\n  ]").Append(hasTrack ? ",\n" : "\n");
            }
            if (hasTrack)
            {
                // One line per piece: short, and easy to edit by hand.
                sb.Append("  \"track\": [");
                for (int i = 0; i < Level.track.Length; i++)
                {
                    TrackEvent t = Level.track[i];
                    sb.Append(i == 0 ? "\n" : ",\n");
                    sb.Append("    { \"beat\": ").Append(Fmt(t.beat)).Append(", \"piece\": ").Append(Str(t.piece)).Append(" }");
                }
                sb.Append("\n  ]\n");
            }
            sb.Append("}\n");
            return sb.ToString();
        }

        private void Sort() => Events.Sort((a, b) => a.beat != b.beat ? a.beat.CompareTo(b.beat) : a.lane.CompareTo(b.lane));

        private static bool Same(double a, double b) => Math.Abs(a - b) < 1e-6;

        public static string Fmt(double v) => v.ToString("0.0#####", CultureInfo.InvariantCulture);

        private static string Str(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
