using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    /// <summary>One track piece in a level: it starts at <see cref="beat"/> and runs for the piece's length.</summary>
    [Serializable]
    public sealed class TrackEvent
    {
        public double beat;
        /// <summary>A name from <see cref="TrackPieces"/>. Everything not covered by a piece is "straight".</summary>
        public string piece;
    }

    /// <summary>The modular track kit (playbook 2.3): what each piece does to the path, and how it is dressed.</summary>
    public static class TrackPieces
    {
        public enum Dressing { None, Tunnel, Bridge, Pillars }

        public readonly struct Spec
        {
            public readonly double LengthBeats;
            /// <summary>Turn in degrees (positive = right). With <see cref="TurnReturns"/> the heading swings out and comes back (S-bend).</summary>
            public readonly double TurnDegrees;
            public readonly bool TurnReturns;
            /// <summary>Height change in metres. With <see cref="RiseReturns"/> the road goes up (or down) and comes back.</summary>
            public readonly double RiseMetres;
            public readonly bool RiseReturns;
            public readonly Dressing Dressing;

            public Spec(double lengthBeats, double turn = 0, bool turnReturns = false, double rise = 0, bool riseReturns = false,
                Dressing dressing = Dressing.None)
            {
                LengthBeats = lengthBeats;
                TurnDegrees = turn;
                TurnReturns = turnReturns;
                RiseMetres = rise;
                RiseReturns = riseReturns;
                Dressing = dressing;
            }

            public bool IsFlatAndStraight => TurnDegrees == 0 && RiseMetres == 0;
        }

        public const string Straight = "straight";
        public const double PieceBeats = 8;

        private static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            { Straight, new Spec(PieceBeats) },
            { "bendLeft", new Spec(PieceBeats, turn: -20) },
            { "bendRight", new Spec(PieceBeats, turn: 20) },
            { "sLeft", new Spec(PieceBeats, turn: -14, turnReturns: true) },   // shifts the road left, same heading after
            { "sRight", new Spec(PieceBeats, turn: 14, turnReturns: true) },
            { "rise", new Spec(PieceBeats, rise: 4) },
            { "drop", new Spec(PieceBeats, rise: -4) },
            { "crest", new Spec(PieceBeats, rise: 2.5, riseReturns: true) },   // hill: up and back down
            { "dip", new Spec(PieceBeats, rise: -2.5, riseReturns: true) },
            { "tunnel", new Spec(PieceBeats, dressing: Dressing.Tunnel) },
            { "bridge", new Spec(PieceBeats, dressing: Dressing.Bridge) },
            { "pillars", new Spec(PieceBeats, dressing: Dressing.Pillars) },
        };

        public static IEnumerable<string> Names => Specs.Keys;

        public static bool TryGet(string name, out Spec spec)
        {
            spec = default;
            return name != null && Specs.TryGetValue(name, out spec);
        }

        /// <summary>Problems in a level's track list. Empty = valid. Null or empty list = all straight.</summary>
        public static List<string> Validate(TrackEvent[] track, double lengthBeats)
        {
            var errors = new List<string>();
            if (track == null) return errors;
            double previousEnd = double.NegativeInfinity;
            for (int i = 0; i < track.Length; i++)
            {
                TrackEvent t = track[i];
                string at = $"track {i} (beat {t.beat})";
                if (!TryGet(t.piece, out Spec spec)) { errors.Add($"{at}: unknown piece '{t.piece}'"); continue; }
                if (t.beat < 0 || t.beat > lengthBeats) errors.Add($"{at}: beat outside 0-{lengthBeats}");
                if (t.beat < previousEnd - 1e-6) errors.Add($"{at}: '{t.piece}' starts before the previous piece ends (pieces are {spec.LengthBeats} beats long and must be in order)");
                previousEnd = t.beat + spec.LengthBeats;
            }
            return errors;
        }
    }

    /// <summary>A point on the track's centre line. Yaw: 0 = +Z, positive turns right (+X). Angles in radians.</summary>
    public readonly struct TrackPose
    {
        public readonly double X, Y, Z, Yaw, Pitch;

        public TrackPose(double x, double y, double z, double yaw, double pitch)
        {
            X = x; Y = y; Z = z; Yaw = yaw; Pitch = pitch;
        }
    }

    /// <summary>
    /// The track's centre line, from a level's piece list. Distance along the track ("s", metres) is measured on the
    /// ground plan, so s = beat x metresPerBeat everywhere: bends and slopes never change WHEN the runner reaches an
    /// obstacle, only where it is drawn. s = 0 is the origin heading +Z; before it the track is straight.
    /// Turns and slopes ease in and out (no kink where pieces join).
    /// </summary>
    public sealed class TrackPath
    {
        public readonly struct Span
        {
            public readonly double Start, End;
            public readonly string Piece;
            public readonly TrackPieces.Spec Spec;

            public Span(double start, double end, string piece, TrackPieces.Spec spec)
            {
                Start = start; End = end; Piece = piece; Spec = spec;
            }
        }

        public double Start { get; }
        public double End { get; }
        public IReadOnlyList<Span> Spans => _spans;

        private readonly List<Span> _spans = new List<Span>();
        private readonly double _step;
        private readonly double[] _x, _z; // sampled from s = 0 forward

        /// <param name="track">Valid piece list (see <see cref="TrackPieces.Validate"/>); unknown pieces are skipped.</param>
        public TrackPath(TrackEvent[] track, double metresPerBeat, double startMetres, double endMetres, double step = 0.25)
        {
            if (metresPerBeat <= 0) throw new ArgumentOutOfRangeException(nameof(metresPerBeat));
            Start = Math.Min(startMetres, 0);
            End = Math.Max(endMetres, 0);
            _step = step;
            if (track != null)
                foreach (TrackEvent t in track)
                    if (t != null && TrackPieces.TryGet(t.piece, out TrackPieces.Spec spec) && t.beat >= 0)
                        _spans.Add(new Span(t.beat * metresPerBeat, (t.beat + spec.LengthBeats) * metresPerBeat, t.piece, spec));
            _spans.Sort((a, b) => a.Start.CompareTo(b.Start));

            // Ground-plan position: integrate the heading (midpoint rule).
            int n = (int)Math.Ceiling(End / step) + 1;
            _x = new double[n + 1];
            _z = new double[n + 1];
            for (int i = 0; i < n; i++)
            {
                double yaw = YawAt((i + 0.5) * step);
                _x[i + 1] = _x[i] + Math.Sin(yaw) * step;
                _z[i + 1] = _z[i] + Math.Cos(yaw) * step;
            }
        }

        private static double Ease(double t) => t * t * (3 - 2 * t);          // 0..1, flat at both ends
        private static double EaseSlope(double t) => 6 * t * (1 - t);
        private static double Bump(double t) { double s = Math.Sin(Math.PI * t); return s * s; } // 0..1..0
        private static double BumpSlope(double t) => Math.PI * Math.Sin(2 * Math.PI * t);

        public double YawAt(double s)
        {
            double yaw = 0;
            foreach (Span p in _spans)
            {
                if (s <= p.Start) break;
                double t = Math.Min(1, (s - p.Start) / (p.End - p.Start));
                yaw += p.Spec.TurnDegrees * (p.Spec.TurnReturns ? Bump(t) : Ease(t));
            }
            return yaw * Math.PI / 180;
        }

        public double HeightAt(double s)
        {
            double y = 0;
            foreach (Span p in _spans)
            {
                if (s <= p.Start) break;
                double t = Math.Min(1, (s - p.Start) / (p.End - p.Start));
                y += p.Spec.RiseMetres * (p.Spec.RiseReturns ? Bump(t) : Ease(t));
            }
            return y;
        }

        private double SlopeAt(double s)
        {
            double slope = 0;
            foreach (Span p in _spans)
            {
                if (s <= p.Start) break;
                if (s >= p.End) continue;
                double len = p.End - p.Start, t = (s - p.Start) / len;
                slope += p.Spec.RiseMetres * (p.Spec.RiseReturns ? BumpSlope(t) : EaseSlope(t)) / len;
            }
            return slope;
        }

        /// <summary>Pose at distance s. Outside Start..End the track continues straight on its end heading.</summary>
        public TrackPose PoseAt(double s)
        {
            if (s <= 0) return new TrackPose(0, 0, s, 0, 0);
            double max = (_x.Length - 1) * _step;
            double c = Math.Min(s, max);
            int i = Math.Min((int)(c / _step), _x.Length - 2);
            double f = c / _step - i;
            double x = _x[i] + (_x[i + 1] - _x[i]) * f, z = _z[i] + (_z[i + 1] - _z[i]) * f;
            double yaw = YawAt(c);
            if (s > max)
            {
                x += Math.Sin(yaw) * (s - max);
                z += Math.Cos(yaw) * (s - max);
            }
            return new TrackPose(x, HeightAt(c), z, yaw, Math.Atan(SlopeAt(c)));
        }

        /// <summary>The piece covering distance s, or false on plain straight track.</summary>
        public bool TryGetSpan(double s, out Span span)
        {
            foreach (Span p in _spans)
                if (s >= p.Start && s < p.End) { span = p; return true; }
            span = default;
            return false;
        }
    }
}
