using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class RunnerModelTests
    {
        private RunnerModel _m;

        [SetUp]
        public void SetUp() => _m = new RunnerModel(new RunnerRules { JumpSeconds = 0.5, ChargeTaps = 4 });

        private static ObstacleInfo Obs(ObstacleKind kind, double t, int lane = 0, bool all = false,
            ObstacleLayer layer = ObstacleLayer.Solid) =>
            new ObstacleInfo { Kind = kind, Time = t, Lane = lane, AllLanes = all, Layer = layer };

        // ---------- lanes ----------

        [Test]
        public void StartsInMiddleLane()
        {
            Assert.AreEqual(0, _m.LaneAt(0));
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Block, 1.0, lane: 0)));
        }

        [Test]
        public void TapChangesLane_FromKeyDownTime()
        {
            _m.OnGestureFired(GestureType.TapLeft, 1.0, 1);
            Assert.AreEqual(0, _m.LaneAt(0.99));
            Assert.AreEqual(-1, _m.LaneAt(1.0));
        }

        [Test]
        public void Lanes_ClampAtEdges()
        {
            _m.OnGestureFired(GestureType.TapLeft, 1.0, 1);
            _m.OnGestureFired(GestureType.TapLeft, 1.5, 1);
            Assert.AreEqual(-1, _m.LaneAt(2));
            _m.OnGestureFired(GestureType.TapRight, 2.5, 1);
            Assert.AreEqual(0, _m.LaneAt(3));
        }

        [Test]
        public void LaneChanges_AppliedInKeyDownOrder_EvenIfReportedOutOfOrder()
        {
            // Right tap pressed at 1.0 but reported after a left tap pressed at 1.1.
            _m.OnGestureFired(GestureType.TapLeft, 1.1, 1);
            _m.OnGestureFired(GestureType.TapRight, 1.0, 1);
            Assert.AreEqual(1, _m.LaneAt(1.05));
            Assert.AreEqual(0, _m.LaneAt(1.2));
        }

        [Test]
        public void Block_InOtherLane_Passes()
        {
            _m.OnGestureFired(GestureType.TapRight, 0.5, 1);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Block, 1.0, lane: 0)));
        }

        // ---------- lane change in progress (key-down to key-up): either lane is safe ----------

        [Test]
        public void MovingBackEarly_ReleasedAfterBlock_Passes()
        {
            // John's day-11 run: right at 2.24, back left pressed 2.88 / recognised 3.051, block in the middle at 3.0.
            _m.OnGestureFired(GestureType.TapRight, 2.240, 1, 2.304);
            _m.OnGestureFired(GestureType.TapLeft, 2.880, 1, 3.051);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Block, 3.0, lane: 0)));
        }

        [Test]
        public void MovingBackEarly_ReleasedBeforeBlock_Hits()
        {
            _m.OnGestureFired(GestureType.TapRight, 2.240, 1, 2.304);
            _m.OnGestureFired(GestureType.TapLeft, 2.880, 1, 2.950);
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Block, 3.0, lane: 0)));
        }

        [Test]
        public void DodgingOutLate_PressedBeforeBlock_Passes()
        {
            _m.OnGestureFired(GestureType.TapRight, 2.950, 1, 3.100);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Block, 3.0, lane: 0)));
        }

        [Test]
        public void DodgingOut_PressedAfterBlock_Hits()
        {
            _m.OnGestureFired(GestureType.TapRight, 3.050, 1, 3.150);
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Block, 3.0, lane: 0)));
        }

        [Test]
        public void RecognizedLane_LagsKeyDownLane()
        {
            _m.OnGestureFired(GestureType.TapLeft, 1.0, 1, 1.1);
            Assert.AreEqual(-1, _m.LaneAt(1.05));
            Assert.AreEqual(0, _m.RecognizedLaneAt(1.05));
            Assert.AreEqual(-1, _m.RecognizedLaneAt(1.1));
        }

        // ---------- decision A: holds count from key-down ----------

        [Test]
        public void Slide_CountsFromKeyDown_NotFromRecognition()
        {
            _m.OnHoldStarted(GestureType.HoldLeft, 1.0); // recognised at 1.25, timed 1.0
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Low, 1.1)));
        }

        [Test]
        public void Low_WithoutSlide_Hits()
        {
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Low, 1.0)));
        }

        [Test]
        public void Slide_EndsAtKeyUp()
        {
            _m.OnHoldStarted(GestureType.HoldLeft, 1.0);
            _m.OnHoldEnded(GestureType.HoldLeft, 2.0);
            Assert.IsTrue(_m.IsSliding(1.99));
            Assert.IsFalse(_m.IsSliding(2.0));
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Low, 2.1)));
        }

        [Test]
        public void Gate_NeedsSprint()
        {
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Gate, 1.0, all: true)));
            _m.OnHoldStarted(GestureType.HoldRight, 2.0);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Gate, 2.2, all: true)));
        }

        // ---------- jump ----------

        [Test]
        public void Gap_NeedsAirborne_ForJumpDuration()
        {
            _m.OnGestureFired(GestureType.BothTap, 1.0, 0);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Gap, 1.0, all: true)));
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Gap, 1.5, all: true)));
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Gap, 1.51, all: true)));
        }

        // ---------- phase ----------

        [Test]
        public void Phased_IgnoresSolid_CollidesWithGhost()
        {
            _m.OnHoldStarted(GestureType.BothHold, 1.0);
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Block, 1.2, all: true)));
            Assert.AreEqual(ObstacleOutcome.Hit,
                _m.Resolve(Obs(ObstacleKind.Block, 1.3, lane: 0, layer: ObstacleLayer.Ghost)));
        }

        [Test]
        public void NotPhased_IgnoresGhost()
        {
            Assert.AreEqual(ObstacleOutcome.Pass,
                _m.Resolve(Obs(ObstacleKind.Block, 1.0, lane: 0, layer: ObstacleLayer.Ghost)));
        }

        // ---------- charge ----------

        [Test]
        public void Wall_WithoutCharge_Hits()
        {
            _m.OnGestureFired(GestureType.Alternate, 1.0, 3);
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Wall, 2.0, all: true)));
        }

        [Test]
        public void Wall_WithCharge_Breaks_AndUsesTheCharge()
        {
            _m.OnGestureFired(GestureType.Alternate, 1.0, 4);
            Assert.IsTrue(_m.HasCharge(1.0));
            Assert.AreEqual(ObstacleOutcome.Break, _m.Resolve(Obs(ObstacleKind.Wall, 2.0, all: true)));
            Assert.IsFalse(_m.HasCharge(2.0));
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Wall, 3.0, all: true)));
        }

        [Test]
        public void Charge_EarnedAfterWall_DoesNotCount()
        {
            _m.OnGestureFired(GestureType.Alternate, 2.5, 4);
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Wall, 2.0, all: true)));
        }

        [Test]
        public void LongStreak_EarnsAChargeEveryFourTaps()
        {
            for (int s = 2; s <= 8; s++) _m.OnGestureFired(GestureType.Alternate, s * 0.5, s);
            Assert.AreEqual(ObstacleOutcome.Break, _m.Resolve(Obs(ObstacleKind.Wall, 5.0, all: true)));
            Assert.AreEqual(ObstacleOutcome.Break, _m.Resolve(Obs(ObstacleKind.Wall, 6.0, all: true)));
            Assert.AreEqual(ObstacleOutcome.Hit, _m.Resolve(Obs(ObstacleKind.Wall, 7.0, all: true)));
        }

        [Test]
        public void InvalidRules_Throw()
        {
            Assert.Throws<System.ArgumentException>(() => new RunnerModel(new RunnerRules { JumpSeconds = 0, ChargeTaps = 4 }));
            Assert.Throws<System.ArgumentException>(() => new RunnerModel(new RunnerRules { JumpSeconds = 0.5, ChargeTaps = 1 }));
        }
    }
}
