using System;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class TrackPathTests
    {
        private static TrackEvent T(double beat, string piece) => new TrackEvent { beat = beat, piece = piece };
        private static TrackPath Path(params TrackEvent[] t) => new TrackPath(t, 5, -60, 400);

        [Test]
        public void NoPieces_IsStraightAlongZ()
        {
            TrackPath p = Path();
            foreach (double s in new[] { -60.0, 0, 12.3, 400 })
            {
                TrackPose q = p.PoseAt(s);
                Assert.AreEqual(0, q.X, 1e-9);
                Assert.AreEqual(0, q.Y, 1e-9);
                Assert.AreEqual(s, q.Z, 1e-9);
                Assert.AreEqual(0, q.Yaw, 1e-9);
            }
        }

        [Test]
        public void Bend_TurnsByItsAngle_AndStaysTurned()
        {
            TrackPath p = Path(T(8, "bendRight"));
            Assert.AreEqual(0, p.PoseAt(40).Yaw, 1e-9, "straight before the piece");
            Assert.AreEqual(10 * Math.PI / 180, p.PoseAt(60).Yaw, 1e-9, "half the turn at the middle");
            Assert.AreEqual(20 * Math.PI / 180, p.PoseAt(80).Yaw, 1e-9);
            Assert.AreEqual(20 * Math.PI / 180, p.PoseAt(200).Yaw, 1e-9);
            Assert.Greater(p.PoseAt(200).X, 10, "a right bend moves the road to +X");
        }

        [Test]
        public void LeftThenRight_CancelOut()
        {
            TrackPath p = Path(T(8, "bendLeft"), T(16, "bendRight"));
            Assert.AreEqual(0, p.PoseAt(120).Yaw, 1e-9);
            Assert.Less(p.PoseAt(120).X, -5);
        }

        [Test]
        public void SBend_ShiftsSideways_SameHeadingAfter()
        {
            TrackPath p = Path(T(8, "sLeft"));
            Assert.AreEqual(0, p.PoseAt(80).Yaw, 1e-9);
            Assert.Less(p.PoseAt(80).X, -2);
            Assert.AreEqual(p.PoseAt(80).X, p.PoseAt(150).X, 1e-9, "straight again afterwards");
        }

        [Test]
        public void Rise_Drop_Crest_Dip_Heights()
        {
            Assert.AreEqual(4, Path(T(8, "rise")).PoseAt(80).Y, 1e-9);
            Assert.AreEqual(2, Path(T(8, "rise")).PoseAt(60).Y, 1e-9);
            Assert.AreEqual(-4, Path(T(8, "drop")).PoseAt(300).Y, 1e-9);
            Assert.AreEqual(2.5, Path(T(8, "crest")).PoseAt(60).Y, 1e-9);
            Assert.AreEqual(0, Path(T(8, "crest")).PoseAt(80).Y, 1e-9);
            Assert.AreEqual(-2.5, Path(T(8, "dip")).PoseAt(60).Y, 1e-9);
        }

        [Test]
        public void Slopes_AreFlatWherePiecesJoin_AndGentle()
        {
            TrackPath p = Path(T(8, "rise"), T(16, "crest"));
            Assert.AreEqual(0, p.PoseAt(40).Pitch, 1e-9);
            Assert.AreEqual(0, p.PoseAt(80).Pitch, 1e-6);
            Assert.AreEqual(0, p.PoseAt(120).Pitch, 1e-6);
            for (double s = 0; s < 130; s += 0.5)
                Assert.Less(Math.Abs(p.PoseAt(s).Pitch), 12 * Math.PI / 180, $"slope at {s} m");
        }

        [Test]
        public void GroundDistance_MatchesS_SoTimingIsUnchanged()
        {
            // Walk the path in small steps: the ground-plan length between two s values equals their difference.
            TrackPath p = Path(T(8, "bendLeft"), T(16, "sRight"), T(24, "rise"));
            double length = 0;
            TrackPose prev = p.PoseAt(0);
            for (double s = 0.25; s <= 200; s += 0.25)
            {
                TrackPose q = p.PoseAt(s);
                length += Math.Sqrt((q.X - prev.X) * (q.X - prev.X) + (q.Z - prev.Z) * (q.Z - prev.Z));
                prev = q;
            }
            Assert.AreEqual(200, length, 0.01);
        }

        [Test]
        public void PastTheEnd_ContinuesStraightOnTheEndHeading()
        {
            var p = new TrackPath(new[] { T(0, "bendRight") }, 5, 0, 40);
            TrackPose a = p.PoseAt(40), b = p.PoseAt(50);
            Assert.AreEqual(a.Yaw, b.Yaw, 1e-9);
            Assert.AreEqual(10, Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z)), 0.01);
        }

        [Test]
        public void Spans_FindThePieceAtADistance()
        {
            TrackPath p = Path(T(8, "tunnel"));
            Assert.IsFalse(p.TryGetSpan(39, out _));
            Assert.IsTrue(p.TryGetSpan(40, out TrackPath.Span span));
            Assert.AreEqual(TrackPieces.Dressing.Tunnel, span.Spec.Dressing);
            Assert.AreEqual(80, span.End, 1e-9);
            Assert.IsFalse(p.TryGetSpan(80, out _));
        }

        [Test]
        public void Kit_HasTwelvePieces()
        {
            int n = 0;
            foreach (string _ in TrackPieces.Names) n++;
            Assert.AreEqual(12, n);
        }

        [Test]
        public void Validate_UnknownPiece_Overlap_OutOfRange()
        {
            StringAssert.Contains("unknown piece", string.Join("|", TrackPieces.Validate(new[] { T(8, "loop") }, 64)));
            StringAssert.Contains("before the previous piece ends",
                string.Join("|", TrackPieces.Validate(new[] { T(8, "rise"), T(12, "drop") }, 64)));
            StringAssert.Contains("outside", string.Join("|", TrackPieces.Validate(new[] { T(80, "rise") }, 64)));
            CollectionAssert.IsEmpty(TrackPieces.Validate(new[] { T(8, "rise"), T(16, "drop") }, 64));
            CollectionAssert.IsEmpty(TrackPieces.Validate(null, 64));
        }

        [Test]
        public void Level_Validate_IncludesTrack()
        {
            var level = new LevelData
            {
                name = "t", bpm = 100, audioClip = "c", lengthBeats = 32, palette = "dawn",
                events = new LevelEvent[0], track = new[] { T(8, "nope") },
            };
            StringAssert.Contains("unknown piece", string.Join("|", level.Validate()));
        }

        [Test]
        public void Chart_KeepsTrackOnSave_AndWarns()
        {
            var level = new LevelData
            {
                name = "t", bpm = 100, audioClip = "c", lengthBeats = 96, palette = "dawn",
                events = new[] { new LevelEvent { beat = 10, lane = 1, type = "gap" } },
                track = new[] { T(8, "rise"), T(16, "bendLeft") },
            };
            var c = new Chart(level);
            StringAssert.Contains("\"track\": [\n    { \"beat\": 8.0, \"piece\": \"rise\" },\n    { \"beat\": 16.0, \"piece\": \"bendLeft\" }\n  ]\n}\n", c.ToJson());
            Assert.AreEqual(2, c.ToLevel().track.Length);
            StringAssert.Contains("sloped piece", string.Join("|", c.Check(false).Warnings));

            var turns = new System.Collections.Generic.List<TrackEvent>();
            for (int i = 0; i < 7; i++) turns.Add(T(8 + i * 8, "bendRight"));
            level.track = turns.ToArray();
            StringAssert.Contains("cross itself", string.Join("|", new Chart(level).Check(false).Warnings));
        }
    }
}
