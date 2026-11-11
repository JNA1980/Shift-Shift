using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class TempoMapTests
    {
        private static TempoMap Rising() => new TempoMap(130, new[] { new TempoChange { beat = 96, bpm = 150 } });

        [Test]
        public void OneTempo_IsBeatTimesSixtyOverBpm()
        {
            var t = new TempoMap(120);
            Assert.AreEqual(4.0, t.TimeAtBeat(8), 1e-12);
            Assert.AreEqual(8.0, t.BeatAtTime(4), 1e-12);
            Assert.AreEqual(-2.0, t.TimeAtBeat(-4), 1e-12, "count-in beats are negative, at the base tempo");
            Assert.AreEqual(-4.0, t.BeatAtTime(-2), 1e-12);
            Assert.IsFalse(t.HasChanges);
        }

        [Test]
        public void TempoChange_TimeIsContinuous_AndBeatsGetShorter()
        {
            TempoMap t = Rising();
            double atChange = 96 * 60.0 / 130;
            Assert.AreEqual(atChange, t.TimeAtBeat(96), 1e-9);
            Assert.AreEqual(atChange + 0.4, t.TimeAtBeat(97), 1e-9, "one beat at 150 BPM = 0.4 s");
            Assert.AreEqual(atChange - 60.0 / 130, t.TimeAtBeat(95), 1e-9);
            Assert.AreEqual(130, t.BpmAtBeat(95.9));
            Assert.AreEqual(150, t.BpmAtBeat(96));
            Assert.AreEqual(130, t.BpmAtBeat(-8));
        }

        [Test]
        public void BeatAtTime_UndoesTimeAtBeat()
        {
            TempoMap t = Rising();
            foreach (double beat in new[] { -8.0, 0, 12.5, 95.99, 96, 96.01, 150, 208 })
                Assert.AreEqual(beat, t.BeatAtTime(t.TimeAtBeat(beat)), 1e-9, $"beat {beat}");
        }

        [Test]
        public void SeveralChanges_AnyOrderInTheList()
        {
            var t = new TempoMap(100, new[] { new TempoChange { beat = 20, bpm = 200 }, new TempoChange { beat = 10, bpm = 50 } });
            Assert.AreEqual(6.0, t.TimeAtBeat(10), 1e-9);          // 10 beats at 100
            Assert.AreEqual(18.0, t.TimeAtBeat(20), 1e-9);         // + 10 beats at 50
            Assert.AreEqual(18.3, t.TimeAtBeat(21), 1e-9);         // + 1 beat at 200
        }

        [Test]
        public void Validate_Rules()
        {
            CollectionAssert.IsEmpty(TempoMap.Validate(null, 100));
            CollectionAssert.IsEmpty(TempoMap.Validate(new[] { new TempoChange { beat = 96, bpm = 150 } }, 208));
            StringAssert.Contains("inside the level", string.Join("|", TempoMap.Validate(new[] { new TempoChange { beat = 0, bpm = 150 } }, 208)));
            StringAssert.Contains("outside 40-300", string.Join("|", TempoMap.Validate(new[] { new TempoChange { beat = 8, bpm = 10 } }, 208)));
            StringAssert.Contains("in order", string.Join("|", TempoMap.Validate(
                new[] { new TempoChange { beat = 50, bpm = 150 }, new TempoChange { beat = 20, bpm = 150 } }, 208)));
        }

        [Test]
        public void Click_FollowsTheTempoMap()
        {
            // 4 beats at 120 then 4 at 240, 1 kHz sample rate: 2.0 s + 1.0 s = 3000 samples (+ 44-byte header).
            var t = new TempoMap(120, new[] { new TempoChange { beat = 4, bpm = 240 } });
            byte[] wav = ClickTrack.GenerateWav(t, 8, 4, 1000);
            Assert.AreEqual(44 + 3000 * 2, wav.Length);
        }

        [Test]
        public void Chart_KeepsTempoAndPaletteChangesOnSave()
        {
            var level = new LevelData
            {
                name = "t", bpm = 130, audioClip = "c", lengthBeats = 208, palette = "dawn", events = new LevelEvent[0],
                tempo = new[] { new TempoChange { beat = 96, bpm = 150 } },
                paletteChanges = new[] { new PaletteChange { beat = 32, palette = "overcast" } },
                track = new[] { new TrackEvent { beat = 8, piece = "rise" } },
            };
            var c = new Chart(level);
            StringAssert.Contains(
                "\"events\": [],\n  \"tempo\": [\n    { \"beat\": 96.0, \"bpm\": 150.0 }\n  ],\n  \"paletteChanges\": [\n" +
                "    { \"beat\": 32.0, \"palette\": \"overcast\" }\n  ],\n  \"track\": [", c.ToJson());
            Assert.AreEqual(1, c.ToLevel().tempo.Length);
            Assert.AreEqual(96 * 60.0 / 130 + 112 * 0.4, c.LengthSeconds, 1e-9);
            CollectionAssert.IsEmpty(c.Check(false).Errors);
        }

        [Test]
        public void Level_Validate_ChecksPaletteChanges()
        {
            var level = new LevelData
            {
                name = "t", bpm = 130, audioClip = "c", lengthBeats = 64, palette = "dawn", events = new LevelEvent[0],
                paletteChanges = new[] { new PaletteChange { beat = 80, palette = "" } },
            };
            string errors = string.Join("|", level.Validate());
            StringAssert.Contains("palette is missing", errors);
            StringAssert.Contains("outside", errors);
        }
    }
}
