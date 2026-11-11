using System.Collections.Generic;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    /// <summary>
    /// The classifier is fed key events directly (Press/Release/Update with explicit times), which is the
    /// "fake input source": no real keys or Unity needed. 120 BPM => one beat = 0.5 s.
    /// </summary>
    public class GestureClassifierTests
    {
        private sealed class FakeClock : IClock
        {
            public double Bpm = 120;
            public double SongTime { get; set; }
            public double BeatAt(double songTime) => songTime * Bpm / 60.0;
            public double SongTimeAtBeat(double beat) => beat * 60.0 / Bpm;
        }

        private enum Kind { Fired, Started, Ended }

        private struct Rec
        {
            public Kind Kind;
            public GestureType Type;
            public double Time;
            public override string ToString() => $"{Kind}:{Type}@{Time:F3}";
        }

        private GestureClassifier _c;
        private List<Rec> _log;

        [SetUp]
        public void SetUp()
        {
            var thresholds = new GestureThresholds
            {
                TapHoldSeconds = 0.250,
                BothWindowSeconds = 0.080,
                AlternateMaxGapSeconds = 0.400,
            };
            _c = new GestureClassifier(thresholds, new FakeClock());
            _log = new List<Rec>();
            _c.GestureFired += (t, s) => _log.Add(new Rec { Kind = Kind.Fired, Type = t, Time = s });
            _c.HoldStarted += (t, s) => _log.Add(new Rec { Kind = Kind.Started, Type = t, Time = s });
            _c.HoldEnded += (t, s) => _log.Add(new Rec { Kind = Kind.Ended, Type = t, Time = s });
        }

        private string[] Log()
        {
            var a = new string[_log.Count];
            for (int i = 0; i < a.Length; i++) a[i] = $"{_log[i].Kind}:{_log[i].Type}";
            return a;
        }

        private List<GestureType> Fired()
        {
            var l = new List<GestureType>();
            foreach (var r in _log) if (r.Kind == Kind.Fired) l.Add(r.Type);
            return l;
        }

        private void Tap(Side s, double down, double up)
        {
            _c.Press(s, down);
            _c.Release(s, up);
        }

        // ---------- tap vs hold at the threshold boundary ----------

        [Test]
        public void PressJustUnderThreshold_IsTap_TimedAtKeyDown()
        {
            Tap(Side.Left, 0.1, 0.349);
            CollectionAssert.AreEqual(new[] { "Fired:TapLeft" }, Log());
            Assert.AreEqual(0.1, _log[0].Time, 1e-9);
        }

        [Test]
        public void PressAtThreshold_IsHold()
        {
            Tap(Side.Left, 0, 0.250);
            CollectionAssert.AreEqual(new[] { "Fired:HoldLeft", "Started:HoldLeft", "Ended:HoldLeft" }, Log());
        }

        [Test]
        public void TapRight_IsTapRight()
        {
            Tap(Side.Right, 2.0, 2.05);
            CollectionAssert.AreEqual(new[] { "Fired:TapRight" }, Log());
        }

        [Test]
        public void Hold_StartsOnUpdateWhileHeld_EndsOnRelease()
        {
            _c.Press(Side.Right, 1.0);
            _c.Update(1.2);
            Assert.IsEmpty(_log);
            _c.Update(1.26);
            CollectionAssert.AreEqual(new[] { "Fired:HoldRight", "Started:HoldRight" }, Log());
            Assert.AreEqual(1.0, _log[1].Time, 1e-9, "hold start is timed at key-down");

            _c.Release(Side.Right, 2.0);
            Assert.AreEqual("Ended:HoldRight", Log()[2]);
            Assert.AreEqual(2.0, _log[2].Time, 1e-9);
        }

        [Test]
        public void LongPressWithNoUpdateInBetween_IsStillHold()
        {
            _c.Press(Side.Right, 0);
            _c.Release(Side.Right, 0.4);
            CollectionAssert.AreEqual(new[] { "Fired:HoldRight", "Started:HoldRight", "Ended:HoldRight" }, Log());
        }

        [Test]
        public void DuplicatePress_IsIgnored()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Left, 0.05); // key repeat
            _c.Release(Side.Left, 0.1);
            CollectionAssert.AreEqual(new[] { "Fired:TapLeft" }, Log());
            Assert.AreEqual(0.0, _log[0].Time, 1e-9);
        }

        [Test]
        public void ReleaseWithoutPress_IsIgnored()
        {
            _c.Release(Side.Right, 1);
            Assert.IsEmpty(_log);
        }

        [Test]
        public void HoldOneKey_TapOther_AreIndependent()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3);
            Tap(Side.Right, 0.6, 0.65);
            _c.Release(Side.Left, 1.0);
            CollectionAssert.AreEqual(new[]
            {
                "Fired:HoldLeft", "Started:HoldLeft", "Fired:TapRight", "Ended:HoldLeft",
            }, Log());
        }

        // ---------- both-tap inside and outside the 80 ms window ----------

        [Test]
        public void BothInsideWindow_IsBothTap_TimedAtFirstKeyDown()
        {
            _c.Press(Side.Left, 1.000);
            _c.Press(Side.Right, 1.060);
            _c.Release(Side.Left, 1.120);
            _c.Release(Side.Right, 1.130);
            CollectionAssert.AreEqual(new[] { "Fired:BothTap" }, Log());
            Assert.AreEqual(1.000, _log[0].Time, 1e-9);
        }

        [Test]
        public void BothAtExactWindowEdge_IsBothTap()
        {
            _c.Press(Side.Right, 0);
            _c.Press(Side.Left, 0.080);
            _c.Release(Side.Right, 0.1);
            _c.Release(Side.Left, 0.1);
            CollectionAssert.AreEqual(new[] { "Fired:BothTap" }, Log());
        }

        [Test]
        public void BothJustOutsideWindow_IsTwoTaps()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.081);
            _c.Release(Side.Left, 0.15);
            _c.Release(Side.Right, 0.2);
            // Two separate taps, not a jump (a quick L then R also counts as alternate step 2).
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft, GestureType.TapRight, GestureType.Alternate }, Fired());
        }

        [Test]
        public void SecondKeyAfterFirstReleased_IsNotBoth()
        {
            Tap(Side.Left, 0, 0.03);
            Tap(Side.Right, 0.05, 0.1);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft, GestureType.TapRight, GestureType.Alternate }, Fired());
            CollectionAssert.DoesNotContain(Fired(), GestureType.BothTap);
        }

        [Test]
        public void BothHeld_IsBothHold_StartAndEnd()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.02);
            _c.Update(0.26);
            CollectionAssert.AreEqual(new[] { "Fired:BothHold", "Started:BothHold" }, Log());
            Assert.AreEqual(0.0, _log[1].Time, 1e-9);

            _c.Release(Side.Right, 1.5);
            _c.Release(Side.Left, 1.6);
            CollectionAssert.AreEqual(new[] { "Fired:BothHold", "Started:BothHold", "Ended:BothHold" }, Log());
            Assert.AreEqual(1.5, _log[2].Time, 1e-9);
        }

        [Test]
        public void AfterBothHoldEnds_KeyStillHeld_DoesNotBecomeSprint()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.01);
            _c.Update(0.3);
            _c.Release(Side.Left, 1.0);
            _c.Update(2.0);
            _c.Release(Side.Right, 2.1);
            CollectionAssert.AreEqual(new[] { "Fired:BothHold", "Started:BothHold", "Ended:BothHold" }, Log());
        }

        [Test]
        public void Lockout_IgnoresRepressUntilBothUp_ThenClears()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.01);
            _c.Release(Side.Left, 0.1);   // BothTap, right still down -> lockout
            Tap(Side.Left, 0.2, 0.25);     // ignored
            _c.Release(Side.Right, 0.3);  // both up -> lockout clears
            Tap(Side.Left, 0.5, 0.55);     // counts
            CollectionAssert.AreEqual(new[] { GestureType.BothTap, GestureType.TapLeft }, Fired());
        }

        [Test]
        public void HoldingKey_DoesNotPairWithLaterPress()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3);
            _c.Press(Side.Right, 0.31);
            _c.Release(Side.Right, 0.35);
            CollectionAssert.AreEqual(new[] { GestureType.HoldLeft, GestureType.TapRight }, Fired());
        }

        // ---------- alternate: quick alternating taps, no beat needed (gap <= 400 ms) ----------

        [Test]
        public void Alternate_QuickTaps_CountToFour()
        {
            Tap(Side.Left, 0.00, 0.05);
            Tap(Side.Right, 0.30, 0.35);
            Tap(Side.Left, 0.60, 0.65);
            Tap(Side.Right, 0.90, 0.95);
            Assert.AreEqual(4, _c.AlternateStreak);
            Assert.AreEqual(3, Fired().FindAll(t => t == GestureType.Alternate).Count);
        }

        [Test]
        public void Alternate_AnyRhythm_Counts()
        {
            // Off any beat grid, uneven spacing: still counts as long as each gap is within 400 ms.
            Tap(Side.Left, 0.13, 0.18);
            Tap(Side.Right, 0.29, 0.33);
            Tap(Side.Left, 0.68, 0.72);
            Assert.AreEqual(3, _c.AlternateStreak);
        }

        [Test]
        public void Alternate_GapAtLimit_Counts()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.4, 0.45);
            Assert.AreEqual(2, _c.AlternateStreak);
        }

        [Test]
        public void Alternate_GapTooLong_RestartsAtOne()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.41, 0.46);
            Assert.AreEqual(1, _c.AlternateStreak);
            CollectionAssert.DoesNotContain(Fired(), GestureType.Alternate);
        }

        [Test]
        public void Alternate_SameSideTwice_RestartsAtOne()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Left, 0.2, 0.25);
            Assert.AreEqual(1, _c.AlternateStreak);
        }

        [Test]
        public void Alternate_StreakExpires_AfterGapWithNoTap()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.3, 0.35);
            _c.Update(0.70);
            Assert.AreEqual(2, _c.AlternateStreak);
            _c.Update(0.71);
            Assert.AreEqual(0, _c.AlternateStreak);
        }

        [Test]
        public void Alternate_PendingPress_DoesNotExpireStreak()
        {
            Tap(Side.Left, 0.0, 0.05);
            _c.Press(Side.Right, 0.38);   // key-down inside the gap...
            _c.Update(0.45);              // ...still undecided past it
            _c.Release(Side.Right, 0.46);
            Assert.AreEqual(2, _c.AlternateStreak);
        }

        [Test]
        public void Alternate_HoldBreaksStreak()
        {
            Tap(Side.Left, 0.0, 0.05);
            _c.Press(Side.Right, 0.2);
            _c.Release(Side.Right, 0.5); // hold, not tap
            Assert.AreEqual(0, _c.AlternateStreak);
        }

        // ---------- unresolved press (used by the Runner for decision A) ----------

        [Test]
        public void UnresolvedPress_WhileUndecided_ReportsKeyDownTime()
        {
            Assert.IsFalse(_c.TryGetUnresolvedPress(out _));
            _c.Press(Side.Left, 1.0);
            Assert.IsTrue(_c.TryGetUnresolvedPress(out double down));
            Assert.AreEqual(1.0, down, 1e-9);
        }

        [Test]
        public void UnresolvedPress_ClearsOnTapOrHold()
        {
            Tap(Side.Left, 0, 0.1);
            Assert.IsFalse(_c.TryGetUnresolvedPress(out _));
            _c.Press(Side.Right, 1.0);
            _c.Update(1.3); // became a hold
            Assert.IsFalse(_c.TryGetUnresolvedPress(out _));
        }

        [Test]
        public void UnresolvedPress_PendingBoth_ReportsFirstKeyDown()
        {
            _c.Press(Side.Left, 2.0);
            _c.Press(Side.Right, 2.05);
            Assert.IsTrue(_c.TryGetUnresolvedPress(out double down));
            Assert.AreEqual(2.0, down, 1e-9);
            _c.Update(2.3); // became BothHold
            Assert.IsFalse(_c.TryGetUnresolvedPress(out _));
        }

        // ---------- misc ----------

        [Test]
        public void ReleaseAll_EndsHeldGestures()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3);
            _c.ReleaseAll(0.5);
            Assert.AreEqual("Ended:HoldLeft", Log()[2]);
            Assert.IsFalse(_c.IsDown(Side.Left));
        }

        [Test]
        public void Reset_ClearsState()
        {
            _c.Press(Side.Left, 0);
            _c.Reset();
            _c.Update(1.0);
            Assert.IsEmpty(_log);
            Assert.IsFalse(_c.IsDown(Side.Left));
        }

        [Test]
        public void ZeroLengthTap_WhileOtherKeyIsHeld_IsATap_AndTheHoldGoesOn()
        {
            // What the Web build feeds in for "tap right during a slide" (the browser hides the right key's release,
            // so ShiftInput presses and releases it at the same moment).
            _c.Press(Side.Left, 0.0);
            _c.Update(0.3); // HoldLeft has started
            _c.Press(Side.Right, 1.0);
            _c.Release(Side.Right, 1.0);
            _c.Update(1.2);
            Assert.IsTrue(_log.Exists(r => r.Kind == Kind.Fired && r.Type == GestureType.TapRight && r.Time == 1.0), "tap right");
            Assert.IsFalse(_log.Exists(r => r.Type == GestureType.BothTap || r.Type == GestureType.BothHold), "no both gesture");
            Assert.IsTrue(_c.IsDown(Side.Left), "the slide goes on");
            Assert.IsFalse(_c.IsDown(Side.Right));

            // A second tap during the same hold counts too.
            _c.Press(Side.Right, 1.5);
            _c.Release(Side.Right, 1.5);
            Assert.AreEqual(2, _log.FindAll(r => r.Kind == Kind.Fired && r.Type == GestureType.TapRight).Count);
        }

        [Test]
        public void InvalidThresholds_Throw()
        {
            Assert.Throws<System.ArgumentException>(() => new GestureClassifier(
                new GestureThresholds { TapHoldSeconds = 0.25, BothWindowSeconds = 0.3, AlternateMaxGapSeconds = 0.4 },
                new FakeClock()));
            Assert.Throws<System.ArgumentException>(() => new GestureClassifier(
                new GestureThresholds { TapHoldSeconds = 0.25, BothWindowSeconds = 0.08, AlternateMaxGapSeconds = 0 },
                new FakeClock()));
        }
    }
}
