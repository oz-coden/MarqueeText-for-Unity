using System;
using NUnit.Framework;

namespace MarqueeText.Tests
{
    [TestFixture]
    public sealed class MarqueeEngineTests
    {
        private MarqueeEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new MarqueeEngine();
            _engine.Configure(MarqueeScrollMode.Continuous, 100f, 50f, 1f, 0.5f);
            _engine.SetGeometry(100f, 200f);
            _engine.Restart();
        }

        [TestCase(100f, 200f, true)]
        [TestCase(100f, 100.005f, false)]
        [TestCase(100f, 100f, false)]
        [TestCase(100f, 80f, false)]
        public void OverflowDetection_UsesStableTolerance(
            float viewportWidth,
            float contentWidth,
            bool expected)
        {
            _engine.SetGeometry(viewportWidth, contentWidth);
            Assert.That(_engine.IsOverflowing, Is.EqualTo(expected));
        }

        [Test]
        public void NonOverflowingContent_DoesNotAdvance()
        {
            _engine.SetGeometry(200f, 100f);

            bool changed = _engine.Tick(10f);

            Assert.That(changed, Is.False);
            Assert.That(_engine.Offset, Is.Zero);
        }

        [Test]
        public void StartDelay_HoldsThenCarriesExcessTime()
        {
            Assert.That(_engine.Tick(0.75f), Is.False);
            Assert.That(_engine.Offset, Is.Zero);

            Assert.That(_engine.Tick(0.5f), Is.True);
            Assert.That(_engine.Offset, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void Speed_IsPixelsPerSecond()
        {
            _engine.Tick(1f);
            _engine.Tick(0.5f);

            Assert.That(_engine.Offset, Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void Continuous_WrapsUsingContentAndGap()
        {
            _engine.Tick(1f);
            _engine.Tick(2.6f);

            Assert.That(_engine.Period, Is.EqualTo(250f));
            Assert.That(_engine.Offset, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Restart_PausesAtEndAndDoesNotRepeatStartDelay()
        {
            _engine.Configure(MarqueeScrollMode.Restart, 100f, 0f, 1f, 0.5f);
            _engine.Restart();
            _engine.Tick(1f);
            _engine.Tick(2f);

            Assert.That(_engine.Offset, Is.EqualTo(200f).Within(0.001f));
            Assert.That(_engine.Tick(0.25f), Is.False);
            Assert.That(_engine.Offset, Is.EqualTo(200f).Within(0.001f));

            Assert.That(_engine.Tick(0.5f), Is.True);
            Assert.That(_engine.Offset, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void PingPong_UsesOnlyTheOverflowDistance()
        {
            _engine.Configure(MarqueeScrollMode.PingPong, 100f, 0f, 0f, 0.5f);
            _engine.SetGeometry(100f, 300f);
            _engine.Restart();

            _engine.Tick(2f);
            Assert.That(_engine.Offset, Is.EqualTo(200f).Within(0.001f));

            _engine.Tick(0.5f);
            _engine.Tick(2f);
            Assert.That(_engine.Offset, Is.Zero.Within(0.001f));

            _engine.Tick(0.75f);
            Assert.That(_engine.Offset, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void Restart_RestoresInitialState()
        {
            _engine.Tick(1.5f);
            Assert.That(_engine.Offset, Is.GreaterThan(0f));

            _engine.Restart();

            Assert.That(_engine.Offset, Is.Zero);
            Assert.That(_engine.Tick(0.5f), Is.False);
        }

        [Test]
        public void LargeDeltaTime_RemainsWithinContinuousPeriod()
        {
            _engine.Configure(MarqueeScrollMode.Continuous, 100f, 50f, 0f, 0f);
            _engine.Restart();

            _engine.Tick(123.456f);

            Assert.That(_engine.Offset, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_engine.Offset, Is.LessThan(_engine.Period));
        }

        [Test]
        public void SteadyStateTick_DoesNotAllocateManagedMemory()
        {
            _engine.Configure(MarqueeScrollMode.Continuous, 100f, 50f, 0f, 0f);
            _engine.Restart();
            _engine.Tick(1f / 60f);
            _ = GC.GetAllocatedBytesForCurrentThread();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10_000; i++)
            {
                _engine.Tick(1f / 60f);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }
    }
}
