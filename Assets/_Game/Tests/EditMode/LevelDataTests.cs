using System;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class LevelDataTests
    {
        private static LevelData Valid() => new LevelData
        {
            name = "test",
            bpm = 100,
            audioClip = "click_100",
            offsetMs = 0,
            palette = "dawn",
            lengthBeats = 32,
            events = new[]
            {
                new LevelEvent { beat = 8, lane = 0, type = "bar" },
                new LevelEvent { beat = 12, lane = 2, type = "bar" },
            },
        };

        [Test]
        public void ValidLevel_HasNoErrors()
        {
            CollectionAssert.IsEmpty(Valid().Validate());
        }

        [Test]
        public void UnknownType_IsReported()
        {
            var l = Valid();
            l.events[1].type = "spike";
            StringAssert.Contains("unknown type 'spike'", string.Join("|", l.Validate()));
        }

        [Test]
        public void LaneOutOfRange_IsReported()
        {
            var l = Valid();
            l.events[0].lane = 3;
            StringAssert.Contains("lane 3", string.Join("|", l.Validate()));
        }

        [Test]
        public void UnsortedEvents_AreReported()
        {
            var l = Valid();
            l.events[0].beat = 20;
            StringAssert.Contains("sorted", string.Join("|", l.Validate()));
        }

        [Test]
        public void SameBeatAndLane_IsReported()
        {
            var l = Valid();
            l.events[1].beat = 8;
            l.events[1].lane = 0;
            StringAssert.Contains("same beat and lane", string.Join("|", l.Validate()));
        }

        [Test]
        public void EventPastEnd_IsReported()
        {
            var l = Valid();
            l.events[1].beat = 40;
            StringAssert.Contains("outside", string.Join("|", l.Validate()));
        }

        [Test]
        public void LaneMapping_ZeroToTwo_BecomesMinusOneToOne()
        {
            Assert.AreEqual(-1, new LevelEvent { lane = 0 }.RunnerLane);
            Assert.AreEqual(1, new LevelEvent { lane = 2 }.RunnerLane);
        }

        [Test]
        public void TypeMapping_MatchesRules()
        {
            Assert.IsTrue(ObstacleTypes.TryGet("bar", out var bar));
            Assert.AreEqual(ObstacleKind.Block, bar.Kind);
            Assert.IsFalse(bar.AllLanes);
            Assert.IsTrue(ObstacleTypes.TryGet("ghost", out var ghost));
            Assert.IsTrue(ghost.AllLanes, "ghost = barrier that needs phase");
            Assert.AreEqual(ObstacleKind.Barrier, ghost.Kind);
            Assert.IsTrue(ObstacleTypes.TryGet("low", out var low));
            Assert.IsTrue(low.AllLanes, "low bars span all lanes: slide is the only answer");
            Assert.IsTrue(ObstacleTypes.TryGet("wall", out var wall));
            Assert.AreEqual(ObstacleKind.Wall, wall.Kind);
            Assert.IsFalse(ObstacleTypes.TryGet(null, out _));
        }
    }

    public class ClickTrackTests
    {
        private static short Sample(byte[] wav, int index) => BitConverter.ToInt16(wav, 44 + index * 2);

        [Test]
        public void Header_IsPcm16Mono_WithCorrectLength()
        {
            byte[] wav = ClickTrack.GenerateWav(100, 4, 4, 1000);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "mono");
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), "16-bit");
            int samples = (int)Math.Ceiling(4 * 0.6 * 1000); // 4 beats at 100 BPM
            Assert.AreEqual(samples * 2, BitConverter.ToInt32(wav, 40));
            Assert.AreEqual(44 + samples * 2, wav.Length);
        }

        [Test]
        public void Clicks_StartExactlyOnEachBeat_SilenceBetween()
        {
            const int rate = 10000;
            byte[] wav = ClickTrack.GenerateWav(100, 4, 4, rate); // beat = 0.6 s = 6000 samples
            for (int b = 0; b < 4; b++)
            {
                int start = b * 6000;
                Assert.AreEqual(0, Sample(wav, start), "sine starts at 0 on the beat");
                Assert.AreNotEqual(0, Sample(wav, start + 2), $"click sounding just after beat {b}");
                Assert.AreEqual(0, Sample(wav, start + 1000), $"silence mid-beat {b}");
            }
        }
    }
}
