using System.Collections.Generic;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class JudgingTests
    {
        private RunnerModel _m;

        [SetUp]
        public void SetUp() => _m = new RunnerModel(new RunnerRules { JumpSeconds = 0.5, ChargeTaps = 4 });

        private static ObstacleInfo Obs(ObstacleKind kind, double t, int lane = 0, bool all = false) =>
            new ObstacleInfo { Kind = kind, Time = t, Lane = lane, AllLanes = all, Layer = ObstacleLayer.Solid };

        private static Grade G(Resolution r, double target) => Judge.GradeOf(r, target, 0.050, 0.120);

        // ---------- answers ----------

        [Test]
        public void Bar_DodgeOnBeat_IsPerfect()
        {
            _m.OnGestureFired(GestureType.TapLeft, 2.98, 1, 3.05);
            Assert.AreEqual(Grade.Perfect, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0)), 3.0));
        }

        [Test]
        public void Bar_Dodge100msEarly_IsGood()
        {
            _m.OnGestureFired(GestureType.TapLeft, 2.90, 1, 2.97);
            Assert.AreEqual(Grade.Good, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0)), 3.0));
        }

        [Test]
        public void Bar_DodgeLongBefore_IsOk()
        {
            _m.OnGestureFired(GestureType.TapLeft, 1.0, 1, 1.1);
            Assert.AreEqual(Grade.Ok, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0)), 3.0));
        }

        [Test]
        public void Bar_InAnotherLaneAllAlong_IsNotGraded()
        {
            Assert.AreEqual(Grade.None, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0, lane: 1)), 3.0));
        }

        [Test]
        public void Bar_NotDodged_IsMiss()
        {
            Assert.AreEqual(Grade.Miss, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0)), 3.0));
        }

        [Test]
        public void Low_SlideOnBeat_IsPerfect()
        {
            _m.OnHoldStarted(GestureType.HoldLeft, 3.02);
            Assert.AreEqual(Grade.Perfect, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Low, 3.03)), 3.0));
        }

        [Test]
        public void Gate_SprintHeldFromLongBefore_IsOk()
        {
            _m.OnHoldStarted(GestureType.HoldRight, 1.0);
            Assert.AreEqual(Grade.Ok, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Gate, 3.0, all: true)), 3.0));
        }

        [Test]
        public void Gap_JumpOnTakeOffBeat_IsPerfect()
        {
            _m.OnGestureFired(GestureType.BothTap, 3.0, 0);
            // the hole sits half a jump after the beat: checked at its edges
            Assert.AreEqual(ObstacleOutcome.Pass, _m.Resolve(Obs(ObstacleKind.Gap, 3.1, all: true)));
            Assert.AreEqual(Grade.Perfect, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Gap, 3.4, all: true)), 3.0));
        }

        [Test]
        public void PhaseBarrier_PhaseOnBeat_IsGood()
        {
            _m.OnHoldStarted(GestureType.BothHold, 2.92);
            Assert.AreEqual(Grade.Good, G(_m.ResolveWithAnswer(Obs(ObstacleKind.Block, 3.0, all: true)), 3.0));
        }

        [Test]
        public void Wall_ChargeFinishedOnBeat_IsPerfect()
        {
            _m.OnGestureFired(GestureType.Alternate, 3.0, 4);
            var r = _m.ResolveWithAnswer(Obs(ObstacleKind.Wall, 3.0, all: true));
            Assert.AreEqual(ObstacleOutcome.Break, r.Outcome);
            Assert.AreEqual(Grade.Perfect, G(r, 3.0));
        }

        // ---------- scoring ----------

        [Test]
        public void Score_PerfectAndGood_Points()
        {
            var s = new ScoreKeeper();
            s.Add(Grade.Perfect);
            s.Add(Grade.Good);
            Assert.AreEqual(150, s.Points);
            Assert.AreEqual(2, s.Streak);
        }

        [Test]
        public void Score_MultiplierFromTenthHit()
        {
            var s = new ScoreKeeper();
            for (int i = 0; i < 9; i++) s.Add(Grade.Perfect);
            Assert.AreEqual(900, s.Points);
            s.Add(Grade.Perfect); // 10th: x2
            Assert.AreEqual(1100, s.Points);
            Assert.AreEqual(2, s.Multiplier);
        }

        [Test]
        public void Score_MultiplierSteps()
        {
            var s = new ScoreKeeper();
            for (int i = 0; i < 25; i++) s.Add(Grade.Good);
            Assert.AreEqual(3, s.Multiplier);
            for (int i = 0; i < 25; i++) s.Add(Grade.Good);
            Assert.AreEqual(4, s.Multiplier);
        }

        [Test]
        public void Score_OkAndMissResetStreak_NoneDoesNot()
        {
            var s = new ScoreKeeper();
            s.Add(Grade.Perfect);
            s.Add(Grade.None);
            Assert.AreEqual(1, s.Streak);
            s.Add(Grade.Ok);
            Assert.AreEqual(0, s.Streak);
            s.Add(Grade.Perfect);
            s.Add(Grade.Miss);
            Assert.AreEqual(0, s.Streak);
            Assert.AreEqual(1, s.BestStreak);
        }

        [Test]
        public void Score_PerfectPercent()
        {
            var s = new ScoreKeeper();
            s.Add(Grade.Perfect);
            s.Add(Grade.Perfect);
            s.Add(Grade.Good);
            s.Add(Grade.Ok);
            s.Add(Grade.None);
            Assert.AreEqual(50.0, s.PerfectPercent, 1e-9);
        }

        // ---------- calibration ----------

        [Test]
        public void Calibration_MeanOfTapOffsets()
        {
            var clicks = new List<double> { 1.0, 1.6, 2.2, 2.8 };
            var taps = new List<double> { 1.08, 1.66, 2.29, 2.87 };
            var r = Calibration.Compute(clicks, taps, 0.3);
            Assert.AreEqual(4, r.Count);
            Assert.AreEqual(75.0, r.MeanMs, 1e-6);
            Assert.That(r.SpreadMs, Is.GreaterThan(0));
        }

        [Test]
        public void Calibration_IgnoresStrayTaps()
        {
            var clicks = new List<double> { 1.0, 2.0 };
            var taps = new List<double> { 1.05, 1.5, 2.05 };
            var r = Calibration.Compute(clicks, taps, 0.3);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(50.0, r.MeanMs, 1e-6);
        }

        [Test]
        public void Calibration_NoTaps_IsEmpty()
        {
            Assert.AreEqual(0, Calibration.Compute(new List<double> { 1.0 }, new List<double>(), 0.3).Count);
        }
    }
}
