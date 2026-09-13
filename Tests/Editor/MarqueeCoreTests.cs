using NUnit.Framework;
using UnityEngine;

namespace MarqueeText.Tests
{
    [TestFixture]
    public class MarqueeCoreTests
    {
        private MarqueeCore _core;

        [SetUp]
        public void SetUp()
        {
            _core = new MarqueeCore
            {
                ViewportWidth = 100f,
                ViewportHeight = 40f,
                ContentWidth = 200f,
                ContentHeight = 30f,
                Speed = 100f,
                Gap = 50f,
                StartDelay = 1.0f,
                LoopDelay = 0.5f,
                Direction = MarqueeDirection.RightToLeft,
                ScrollMode = MarqueeScrollMode.Continuous
            };
            _core.Reset();
        }

        [Test]
        public void OverflowDetection_CalculatesCorrectlyForHorizontalAndVertical()
        {
            // Horizontal (Default)
            Assert.IsTrue(_core.IsOverflowing); // 200 > 100

            _core.ContentWidth = 80f;
            Assert.IsFalse(_core.IsOverflowing); // 80 < 100

            // Vertical
            _core.Direction = MarqueeDirection.BottomToTop;
            _core.ViewportHeight = 50f;
            _core.ContentHeight = 80f;
            Assert.IsTrue(_core.IsOverflowing); // 80 > 50

            _core.ContentHeight = 30f;
            Assert.IsFalse(_core.IsOverflowing); // 30 < 50
        }

        [Test]
        public void StartDelay_HoldsPositionUntilDelayExpires()
        {
            bool startedEventFired = false;
            _core.OnScrollStarted += () => startedEventFired = true;

            Assert.AreEqual(MarqueeCore.State.StartDelay, _core.CurrentState);
            Assert.AreEqual(0f, _core.CurrentOffset);

            // Advance by 0.6s (less than 1.0s delay)
            _core.Update(0.6f);
            Assert.AreEqual(MarqueeCore.State.StartDelay, _core.CurrentState);
            Assert.AreEqual(0f, _core.CurrentOffset);
            Assert.IsFalse(startedEventFired);

            // Advance by remaining 0.4s
            _core.Update(0.4f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);
            Assert.IsTrue(startedEventFired);
        }

        [Test]
        public void Scrolling_AdvancesOffsetAtSpecifiedSpeed()
        {
            // Bypass StartDelay
            _core.Update(1.0f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);

            // Speed = 100 px/s, deltaTime = 0.5s -> expected offset = 50
            _core.Update(0.5f);
            Assert.AreEqual(50f, _core.CurrentOffset, 0.001f);

            // Another 0.5s -> expected offset = 100
            _core.Update(0.5f);
            Assert.AreEqual(100f, _core.CurrentOffset, 0.001f);
        }

        [Test]
        public void ContinuousMode_WrapsOffsetAndTriggersLoopCompleted()
        {
            bool loopEventFired = false;
            _core.OnScrollLoopCompleted += () => loopEventFired = true;

            // Bypass StartDelay
            _core.Update(1.0f);

            // Period = ContentWidth(200) + Gap(50) = 250
            // Speed = 100 px/s -> 2.5s needed for full period
            _core.Update(2.0f); // offset = 200
            Assert.AreEqual(200f, _core.CurrentOffset, 0.001f);
            Assert.IsFalse(loopEventFired);

            // Advance 0.6s -> +60 px -> total 260 px -> wraps to 10 px
            _core.Update(0.6f);
            Assert.AreEqual(10f, _core.CurrentOffset, 0.001f);
            Assert.IsTrue(loopEventFired);
        }

        [Test]
        public void RestartMode_PausesAtEndDelayThenRestarts()
        {
            _core.ScrollMode = MarqueeScrollMode.Restart;
            bool loopEventFired = false;
            _core.OnScrollLoopCompleted += () => loopEventFired = true;

            // Bypass StartDelay
            _core.Update(1.0f);

            // Max offset = ContentWidth = 200. At speed 100, takes 2.0s
            _core.Update(2.0f);
            Assert.AreEqual(200f, _core.CurrentOffset, 0.001f);
            Assert.AreEqual(MarqueeCore.State.EndDelay, _core.CurrentState);

            // EndDelay = 0.5s. Advance 0.3s -> still in EndDelay
            _core.Update(0.3f);
            Assert.AreEqual(MarqueeCore.State.EndDelay, _core.CurrentState);
            Assert.AreEqual(200f, _core.CurrentOffset, 0.001f);
            Assert.IsFalse(loopEventFired);

            // Advance remaining 0.2s -> resets to StartDelay and fires loop completed
            _core.Update(0.2f);
            Assert.AreEqual(MarqueeCore.State.StartDelay, _core.CurrentState);
            Assert.AreEqual(0f, _core.CurrentOffset);
            Assert.IsTrue(loopEventFired);
        }

        [Test]
        public void PingPongMode_ReversesDirectionAtBoundaryAndLoops()
        {
            _core.ScrollMode = MarqueeScrollMode.PingPong;
            _core.ViewportWidth = 100f;
            _core.ContentWidth = 300f; // Max offset = 300 - 100 = 200
            _core.LoopDelay = 0.5f;

            bool loopEventFired = false;
            _core.OnScrollLoopCompleted += () => loopEventFired = true;

            // 1. StartDelay (1.0s)
            _core.Update(1.0f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);
            Assert.AreEqual(1, _core.PingPongDirection);

            // 2. Scroll forward to max offset (200px / 100px/s = 2.0s)
            _core.Update(2.0f);
            Assert.AreEqual(200f, _core.CurrentOffset, 0.001f);
            Assert.AreEqual(MarqueeCore.State.EndDelay, _core.CurrentState);

            // 3. Wait EndDelay (0.5s) -> switches to reverse scrolling
            _core.Update(0.5f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);
            Assert.AreEqual(-1, _core.PingPongDirection);

            // 4. Scroll backwards to 0 (2.0s)
            _core.Update(2.0f);
            Assert.AreEqual(0f, _core.CurrentOffset, 0.001f);
            Assert.AreEqual(MarqueeCore.State.PingPongReturnDelay, _core.CurrentState);

            // 5. Wait ReturnDelay (0.5s) -> switches back to forward and completes full loop
            _core.Update(0.5f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);
            Assert.AreEqual(1, _core.PingPongDirection);
            Assert.IsTrue(loopEventFired);
        }

        [Test]
        public void Positions_CalculateCorrectlyForAllFourDirections()
        {
            _core.ViewportWidth = 100f;
            _core.ViewportHeight = 40f;
            _core.ContentWidth = 200f;
            _core.ContentHeight = 30f;
            _core.Gap = 50f;

            // Bypass StartDelay and scroll to offset 30
            _core.Update(1.0f);
            _core.Update(0.3f); // offset = 30
            float offset = _core.CurrentOffset;
            Assert.AreEqual(30f, offset, 0.001f);

            // 1. RightToLeft
            _core.Direction = MarqueeDirection.RightToLeft;
            _core.CalculatePositions(out Vector2 primRTL, out Vector2 ghostRTL, out bool showGhostRTL);
            Assert.IsTrue(showGhostRTL);
            Assert.AreEqual(-offset, primRTL.x, 0.001f); // -30
            Assert.AreEqual(-offset + (200f + 50f), ghostRTL.x, 0.001f); // -30 + 250 = 220
            Assert.AreEqual((40f - 30f) * 0.5f, primRTL.y, 0.001f); // 5 (centered)

            // 2. LeftToRight
            _core.Direction = MarqueeDirection.LeftToRight;
            _core.CalculatePositions(out Vector2 primLTR, out Vector2 ghostLTR, out _);
            float ltrBase = 100f - 200f; // -100
            Assert.AreEqual(ltrBase + offset, primLTR.x, 0.001f); // -100 + 30 = -70
            Assert.AreEqual(ltrBase + offset - 250f, ghostLTR.x, 0.001f); // -70 - 250 = -320
            Assert.AreEqual(5f, primLTR.y, 0.001f);

            // 3. BottomToTop (Vertical)
            _core.Direction = MarqueeDirection.BottomToTop;
            _core.CalculatePositions(out Vector2 primBTT, out Vector2 ghostBTT, out _);
            Assert.AreEqual((100f - 200f) * 0.5f, primBTT.x, 0.001f); // -50 (centered)
            Assert.AreEqual(offset, primBTT.y, 0.001f); // +30 (upwards)
            Assert.AreEqual(offset - (30f + 50f), ghostBTT.y, 0.001f); // 30 - 80 = -50

            // 4. TopToBottom (Vertical)
            _core.Direction = MarqueeDirection.TopToBottom;
            _core.CalculatePositions(out Vector2 primTTB, out Vector2 ghostTTB, out _);
            float ttbBase = 40f - 30f; // 10
            Assert.AreEqual(-50f, primTTB.x, 0.001f);
            Assert.AreEqual(ttbBase - offset, primTTB.y, 0.001f); // 10 - 30 = -20
            Assert.AreEqual(ttbBase - offset + 80f, ghostTTB.y, 0.001f); // -20 + 80 = 60
        }

        [Test]
        public void ZeroDelays_TransitionImmediatelyToScrolling()
        {
            _core.StartDelay = 0f;
            _core.LoopDelay = 0f;
            _core.Reset();

            // Advance by tiny delta
            _core.Update(0.016f);
            Assert.AreEqual(MarqueeCore.State.Scrolling, _core.CurrentState);
            Assert.Greater(_core.CurrentOffset, 0f);
        }

        [Test]
        public void StaticPosition_AlignsLeftCenterRightAndVerticalProperly()
        {
            _core.ViewportWidth = 100f;
            _core.ViewportHeight = 40f;
            _core.ContentWidth = 60f;
            _core.ContentHeight = 20f;

            // Left / Middle
            Vector2 leftMiddle = _core.CalculateStaticPosition(-1, 0);
            Assert.AreEqual(0f, leftMiddle.x, 0.001f);
            Assert.AreEqual(10f, leftMiddle.y, 0.001f); // (40 - 20) / 2

            // Center / Middle
            Vector2 centerMiddle = _core.CalculateStaticPosition(0, 0);
            Assert.AreEqual(20f, centerMiddle.x, 0.001f); // (100 - 60) / 2
            Assert.AreEqual(10f, centerMiddle.y, 0.001f);

            // Right / Top
            Vector2 rightTop = _core.CalculateStaticPosition(1, 1);
            Assert.AreEqual(40f, rightTop.x, 0.001f); // 100 - 60
            Assert.AreEqual(20f, rightTop.y, 0.001f); // 40 - 20
        }
    }
}
