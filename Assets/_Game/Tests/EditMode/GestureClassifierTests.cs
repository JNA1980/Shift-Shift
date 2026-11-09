using System.Collections.Generic;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class GestureClassifierTests
    {
        private GestureClassifier _c;
        private List<GestureEvent> _events;

        [SetUp]
        public void SetUp()
        {
            _c = new GestureClassifier(new GestureSettings { BothWindow = 0.080, HoldThreshold = 0.250, AlternateMaxGap = 0.600 });
            _events = new List<GestureEvent>();
            _c.Detected += e => _events.Add(e);
        }

        private GestureType[] Types()
        {
            var t = new GestureType[_events.Count];
            for (int i = 0; i < t.Length; i++) t[i] = _events[i].Type;
            return t;
        }

        private void Tap(Side s, double down, double up)
        {
            _c.Press(s, down);
            _c.Release(s, up);
        }

        // ---- taps ----

        [Test]
        public void ShortLeftPress_IsTapLeft_TimedAtKeyDown()
        {
            Tap(Side.Left, 1.000, 1.100);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft }, Types());
            Assert.AreEqual(1.000, _events[0].Time, 1e-9);
            Assert.AreEqual(1.100, _events[0].EmitTime, 1e-9);
        }

        [Test]
        public void ShortRightPress_IsTapRight()
        {
            Tap(Side.Right, 2.0, 2.05);
            CollectionAssert.AreEqual(new[] { GestureType.TapRight }, Types());
        }

        [Test]
        public void PressJustUnderThreshold_IsTap()
        {
            Tap(Side.Left, 0, 0.249);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft }, Types());
        }

        [Test]
        public void DuplicatePress_IsIgnored()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Left, 0.05); // key repeat
            _c.Release(Side.Left, 0.1);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft }, Types());
            Assert.AreEqual(0.0, _events[0].Time, 1e-9);
        }

        [Test]
        public void ReleaseWithoutPress_IsIgnored()
        {
            _c.Release(Side.Right, 1);
            Assert.IsEmpty(_events);
        }

        // ---- holds ----

        [Test]
        public void HeldPress_EmitsHoldStartOnUpdate_BeforeRelease()
        {
            _c.Press(Side.Left, 1.0);
            _c.Update(1.2);
            Assert.IsEmpty(_events);
            _c.Update(1.26);
            CollectionAssert.AreEqual(new[] { GestureType.HoldLeftStart }, Types());
            Assert.AreEqual(1.0, _events[0].Time, 1e-9, "hold is judged at key-down");
            Assert.AreEqual(1.25, _events[0].EmitTime, 1e-9);

            _c.Release(Side.Left, 2.0);
            CollectionAssert.AreEqual(new[] { GestureType.HoldLeftStart, GestureType.HoldLeftEnd }, Types());
            Assert.AreEqual(2.0, _events[1].Time, 1e-9);
        }

        [Test]
        public void LongPressWithNoUpdateInBetween_IsStillHold()
        {
            // A long frame hitch: release arrives without any Update after the threshold.
            _c.Press(Side.Right, 0);
            _c.Release(Side.Right, 0.4);
            CollectionAssert.AreEqual(new[] { GestureType.HoldRightStart, GestureType.HoldRightEnd }, Types());
        }

        [Test]
        public void HoldOneKey_TapOther_AreIndependent()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3);           // slide starts
            Tap(Side.Right, 0.5, 0.6); // lane change while sliding
            _c.Release(Side.Left, 1.0);
            CollectionAssert.AreEqual(new[]
            {
                GestureType.HoldLeftStart, GestureType.TapRight, GestureType.HoldLeftEnd,
            }, Types());
        }

        // ---- both ----

        [Test]
        public void BothWithinWindow_QuickRelease_IsBothTap_TimedAtFirstKeyDown()
        {
            _c.Press(Side.Left, 1.000);
            _c.Press(Side.Right, 1.060);
            _c.Release(Side.Left, 1.120);
            _c.Release(Side.Right, 1.130);
            CollectionAssert.AreEqual(new[] { GestureType.BothTap }, Types());
            Assert.AreEqual(1.000, _events[0].Time, 1e-9);
        }

        [Test]
        public void BothAtExactWindowEdge_CountsAsBoth()
        {
            _c.Press(Side.Right, 0);
            _c.Press(Side.Left, 0.080);
            _c.Release(Side.Right, 0.1);
            _c.Release(Side.Left, 0.1);
            CollectionAssert.AreEqual(new[] { GestureType.BothTap }, Types());
        }

        [Test]
        public void SecondKeyOutsideWindow_IsNotBoth()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.1);
            _c.Release(Side.Left, 0.15);
            _c.Release(Side.Right, 0.2);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft, GestureType.TapRight, GestureType.Alternate }, Types());
        }

        [Test]
        public void SecondKeyAfterFirstReleased_IsNotBoth()
        {
            Tap(Side.Left, 0, 0.03);
            Tap(Side.Right, 0.05, 0.1);
            Assert.AreEqual(GestureType.TapLeft, _events[0].Type);
            Assert.AreEqual(GestureType.TapRight, _events[1].Type);
        }

        [Test]
        public void BothHeld_IsBothHoldStartThenEnd()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.02);
            _c.Update(0.26);
            CollectionAssert.AreEqual(new[] { GestureType.BothHoldStart }, Types());
            Assert.AreEqual(0.0, _events[0].Time, 1e-9);

            _c.Release(Side.Right, 1.5);
            _c.Release(Side.Left, 1.6);
            CollectionAssert.AreEqual(new[] { GestureType.BothHoldStart, GestureType.BothHoldEnd }, Types());
            Assert.AreEqual(1.5, _events[1].Time, 1e-9);
        }

        [Test]
        public void BothHold_NeverAlsoEmitsSingleHolds()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.01);
            _c.Update(1.0);
            CollectionAssert.AreEqual(new[] { GestureType.BothHoldStart }, Types());
        }

        [Test]
        public void AfterBothHoldEnds_KeyStillHeld_DoesNotBecomeSprint()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.01);
            _c.Update(0.3);
            _c.Release(Side.Left, 1.0); // phase ends
            _c.Update(2.0);             // right still held long after
            _c.Release(Side.Right, 2.1);
            CollectionAssert.AreEqual(new[] { GestureType.BothHoldStart, GestureType.BothHoldEnd }, Types());
        }

        [Test]
        public void Lockout_IgnoresRepressUntilBothUp_ThenClears()
        {
            _c.Press(Side.Left, 0);
            _c.Press(Side.Right, 0.01);
            _c.Release(Side.Left, 0.1);  // BothTap, right still down -> lockout
            Tap(Side.Left, 0.2, 0.25);    // ignored
            _c.Release(Side.Right, 0.3); // both up -> lockout clears
            Tap(Side.Left, 0.5, 0.55);    // counts
            CollectionAssert.AreEqual(new[] { GestureType.BothTap, GestureType.TapLeft }, Types());
        }

        [Test]
        public void HoldingKey_DoesNotPairWithLaterPress()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3); // HoldLeftStart
            _c.Press(Side.Right, 0.31);
            _c.Release(Side.Right, 0.35);
            CollectionAssert.AreEqual(new[] { GestureType.HoldLeftStart, GestureType.TapRight }, Types());
        }

        // ---- alternate ----

        [Test]
        public void AlternatingTaps_CountUp()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.5, 0.55);
            Tap(Side.Left, 1.0, 1.05);
            Tap(Side.Right, 1.5, 1.55);
            var counts = new List<int>();
            foreach (var e in _events) if (e.Type == GestureType.Alternate) counts.Add(e.AlternateCount);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, counts);
        }

        [Test]
        public void SameSideTap_RestartsAlternateStreak()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.3, 0.35);
            Tap(Side.Right, 0.6, 0.65);
            Tap(Side.Left, 0.9, 0.95);
            var counts = new List<int>();
            foreach (var e in _events) if (e.Type == GestureType.Alternate) counts.Add(e.AlternateCount);
            CollectionAssert.AreEqual(new[] { 2, 2 }, counts);
        }

        [Test]
        public void SlowAlternation_DoesNotCount()
        {
            Tap(Side.Left, 0.0, 0.05);
            Tap(Side.Right, 0.7, 0.75);
            CollectionAssert.AreEqual(new[] { GestureType.TapLeft, GestureType.TapRight }, Types());
        }

        [Test]
        public void HoldBreaksAlternateStreak()
        {
            Tap(Side.Left, 0.0, 0.05);
            _c.Press(Side.Right, 0.3);
            _c.Release(Side.Right, 0.6); // hold, not tap
            Tap(Side.Left, 0.7, 0.75);
            CollectionAssert.DoesNotContain(Types(), GestureType.Alternate);
        }

        // ---- misc ----

        [Test]
        public void ReleaseAll_EndsHeldGestures()
        {
            _c.Press(Side.Left, 0);
            _c.Update(0.3);
            _c.ReleaseAll(0.5);
            CollectionAssert.AreEqual(new[] { GestureType.HoldLeftStart, GestureType.HoldLeftEnd }, Types());
            Assert.IsFalse(_c.IsDown(Side.Left));
        }

        [Test]
        public void Reset_ClearsState()
        {
            _c.Press(Side.Left, 0);
            _c.Reset();
            _c.Update(1.0);
            Assert.IsEmpty(_events);
            Assert.IsFalse(_c.IsDown(Side.Left));
        }

        [Test]
        public void InvalidSettings_Throw()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new GestureClassifier(new GestureSettings { BothWindow = 0.3, HoldThreshold = 0.25 }));
        }
    }
}
