using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class ChartTests
    {
        private static LevelData Level(params LevelEvent[] events) => new LevelData
        {
            name = "t",
            bpm = 100,
            audioClip = "click_100",
            offsetMs = 0,
            palette = "dawn",
            lengthBeats = 32,
            events = events,
        };

        private static LevelEvent E(double beat, int lane, string type) => new LevelEvent { beat = beat, lane = lane, type = type };

        [Test]
        public void Snap_QuarterAndEighth()
        {
            Assert.AreEqual(8.0, Chart.Snap(8.3, 1));
            Assert.AreEqual(9.0, Chart.Snap(8.5, 1));
            Assert.AreEqual(8.5, Chart.Snap(8.3, 2));
            Assert.AreEqual(8.0, Chart.Snap(8.2, 2));
        }

        [Test]
        public void Place_KeepsEventsSorted()
        {
            var c = new Chart(Level(E(12, 0, "bar")));
            c.Place(8, 2, "bar");
            Assert.AreEqual(8.0, c.Events[0].beat);
            Assert.AreEqual(12.0, c.Events[1].beat);
        }

        [Test]
        public void Place_SameCell_Replaces()
        {
            var c = new Chart(Level(E(8, 0, "bar")));
            c.Place(8, 0, "wide");
            Assert.AreEqual(1, c.Events.Count);
            Assert.AreEqual("wide", c.Events[0].type);
        }

        [Test]
        public void Place_AllLaneType_ReplacesWholeBeat()
        {
            var c = new Chart(Level(E(8, 0, "bar"), E(8, 2, "bar")));
            c.Place(8, 0, "low");
            Assert.AreEqual(1, c.Events.Count);
            Assert.AreEqual("low", c.Events[0].type);
            Assert.AreEqual(1, c.Events[0].lane, "all-lane events are stored in the middle lane");
            Assert.AreSame(c.Events[0], c.At(8, 2), "an all-lane event covers every lane");
        }

        [Test]
        public void Place_Gate_KeepsOpenLane_AndCoversTheBeat()
        {
            var c = new Chart(Level(E(8, 2, "bar")));
            LevelEvent g = c.Place(8, 0, "gate");
            Assert.AreEqual(1, c.Events.Count, "gate replaces the bar on its beat");
            Assert.AreEqual(0, g.lane, "clicked lane = the opening");
            Assert.AreSame(g, c.At(8, 2), "clicking a blocked lane of the gate selects the gate");
        }

        [Test]
        public void Place_BarrierWithCores_KeepsItsLane_AndFillsTheBeat()
        {
            var c = new Chart(Level(E(8, 2, "bar")));
            LevelEvent g = c.Place(8, 0, "ghostbar");
            Assert.AreEqual(1, c.Events.Count, "the barrier replaces the bar on its beat");
            Assert.AreEqual(0, g.lane, "clicked lane = the core's lane");
            Assert.AreSame(g, c.At(8, 2), "the barrier covers every lane of its beat");
            LevelEvent w = c.Place(12, 2, "ghostwide");
            Assert.AreEqual(2, w.lane, "clicked lane = the free lane");
            CollectionAssert.IsEmpty(c.Check(true).Errors);
        }

        [Test]
        public void Place_ClampsToLevel()
        {
            var c = new Chart(Level());
            LevelEvent e = c.Place(40, 5, "bar");
            Assert.AreEqual(32.0, e.beat);
            Assert.AreEqual(2, e.lane);
        }

        [Test]
        public void Remove_And_Move()
        {
            var c = new Chart(Level(E(8, 0, "bar"), E(12, 2, "bar")));
            Assert.IsTrue(c.Remove(8, 0));
            Assert.IsFalse(c.Remove(8, 0));
            c.Move(c.Events[0], 16, 1);
            Assert.AreEqual(16.0, c.Events[0].beat);
            Assert.AreEqual(1, c.Events[0].lane);
            Assert.AreEqual(1, c.Events.Count);
        }

        [Test]
        public void Counts_PerType()
        {
            var c = new Chart(Level(E(8, 0, "bar"), E(12, 2, "bar"), E(16, 1, "low")));
            var n = c.Counts();
            Assert.AreEqual(2, n["bar"]);
            Assert.AreEqual(1, n["low"]);
            Assert.AreEqual(0, n["wall"]);
        }

        [Test]
        public void LengthSeconds()
        {
            Assert.AreEqual(19.2, new Chart(Level()).LengthSeconds, 1e-9); // 32 beats at 100 BPM
        }

        [Test]
        public void Check_WarnsInFirstEightBeats()
        {
            var r = new Chart(Level(E(4, 0, "bar"))).Check(false);
            CollectionAssert.IsEmpty(r.Errors);
            StringAssert.Contains("first 8 beats", string.Join("|", r.Warnings));
        }

        [Test]
        public void Check_StrictSpacing_WarnsDifferentTypesUnderOneBeat()
        {
            var c = new Chart(Level(E(8, 0, "bar"), E(8.5, 1, "low")));
            Assert.IsEmpty(c.Check(false).Warnings);
            StringAssert.Contains("less than 1 beat", string.Join("|", c.Check(true).Warnings));
            var ok = new Chart(Level(E(8, 0, "bar"), E(9, 1, "low")));
            Assert.IsEmpty(ok.Check(true).Warnings, "exactly 1 beat apart is fine");
        }

        [Test]
        public void Check_AllLaneOverlap_IsError()
        {
            var c = new Chart(Level(E(8, 0, "bar"), E(8, 1, "low")));
            StringAssert.Contains("overlap", string.Join("|", c.Check(false).Errors));
        }

        [Test]
        public void ToJson_IsReadableSortedAndInvariant()
        {
            var c = new Chart(Level(E(12, 2, "wide"), E(8.5, 0, "bar")));
            c.Level.name = "Say \"hi\"";
            string expected =
                "{\n" +
                "  \"name\": \"Say \\\"hi\\\"\",\n" +
                "  \"bpm\": 100.0,\n" +
                "  \"audioClip\": \"click_100\",\n" +
                "  \"offsetMs\": 0.0,\n" +
                "  \"palette\": \"dawn\",\n" +
                "  \"lengthBeats\": 32.0,\n" +
                "  \"events\": [\n" +
                "    {\n      \"beat\": 8.5,\n      \"lane\": 0,\n      \"type\": \"bar\"\n    },\n" +
                "    {\n      \"beat\": 12.0,\n      \"lane\": 2,\n      \"type\": \"wide\"\n    }\n" +
                "  ]\n" +
                "}\n";
            Assert.AreEqual(expected, c.ToJson());
        }

        [Test]
        public void ToJson_NoEvents()
        {
            StringAssert.EndsWith("\"events\": []\n}\n", new Chart(Level()).ToJson());
        }
    }
}
