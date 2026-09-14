using System;
using System.Diagnostics;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using MarqueeComponent = global::MarqueeText.MarqueeText;

namespace MarqueeText.Tests
{
    public sealed class MarqueeTextPlayModeTests
    {
        private GameObject _canvasObject;

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_canvasObject);
            }
        }

        [Test]
        public void NonOverflowingText_UsesSourceWithoutInternalRenderer()
        {
            MarqueeComponent marquee = CreateMarquee("Short");

            marquee.RefreshForTests();

            Assert.That(marquee.IsOverflowing, Is.False);
            Assert.That(marquee.PrimaryRectForTests, Is.Null);
            Assert.That(marquee.GetComponent<TextMeshProUGUI>().canvasRenderer.cull, Is.False);
        }

        [Test]
        public void PauseAndPlay_PreserveCurrentPosition()
        {
            MarqueeComponent marquee = CreateMarquee(LongText);
            marquee.RefreshForTests();
            marquee.TickForTests(1.25f);
            float beforePause = marquee.OffsetForTests;

            marquee.Pause();
            marquee.TickForTests(2f);

            Assert.That(marquee.OffsetForTests, Is.EqualTo(beforePause).Within(0.001f));
            Assert.That(marquee.IsPlaying, Is.False);

            marquee.Play();
            marquee.TickForTests(0.25f);
            Assert.That(marquee.OffsetForTests, Is.GreaterThan(beforePause));
        }

        [Test]
        public void Restart_ReturnsToStartAndPlays()
        {
            MarqueeComponent marquee = CreateMarquee(LongText);
            marquee.RefreshForTests();
            marquee.TickForTests(1.5f);
            Assert.That(marquee.OffsetForTests, Is.GreaterThan(0f));

            marquee.Restart();

            Assert.That(marquee.OffsetForTests, Is.Zero);
            Assert.That(marquee.IsPlaying, Is.True);
        }

        [Test]
        public void RuntimeTextChange_AutomaticallyRefreshesOverflowState()
        {
            MarqueeComponent marquee = CreateMarquee("Short");
            marquee.RefreshForTests();
            Assert.That(marquee.IsOverflowing, Is.False);

            marquee.GetComponent<TextMeshProUGUI>().text = LongText;
            marquee.TickForTests(0f);

            Assert.That(marquee.IsOverflowing, Is.True);
            Assert.That(marquee.PrimaryRectForTests, Is.Not.Null);
        }

        [Test]
        public void DisableAndEnable_RestoresThenReactivatesRenderer()
        {
            MarqueeComponent marquee = CreateMarquee(LongText);
            marquee.RefreshForTests();
            RectTransform primary = marquee.PrimaryRectForTests;
            Assert.That(primary.gameObject.activeSelf, Is.True);

            marquee.enabled = false;
            Assert.That(primary.gameObject.activeSelf, Is.False);
            Assert.That(marquee.GetComponent<TextMeshProUGUI>().canvasRenderer.cull, Is.False);

            marquee.enabled = true;
            marquee.TickForTests(0f);
            Assert.That(primary.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void SteadyStateComponentTick_DoesNotAllocateManagedMemory()
        {
            MarqueeComponent marquee = CreateMarquee(LongText);
            marquee.RefreshForTests();
            marquee.TickForTests(2f);
            marquee.TickForTests(1f / 60f);
            _ = GC.GetAllocatedBytesForCurrentThread();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1_000; i++)
            {
                marquee.TickForTests(1f / 60f);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }

        [Test, Timeout(60_000)]
        public void Benchmark_SteadyState_OneTenAndOneHundredInstances()
        {
            MarqueeComponent[] marquees = new MarqueeComponent[100];
            for (int i = 0; i < marquees.Length; i++)
            {
                marquees[i] = CreateMarqueeChild(LongText, i);
                marquees[i].RefreshForTests();
                marquees[i].TickForTests(2f);
            }

            BenchmarkResult one = Measure(marquees, 1, 600);
            BenchmarkResult ten = Measure(marquees, 10, 600);
            BenchmarkResult hundred = Measure(marquees, 100, 600);

            Debug.Log($"MarqueeText steady-state benchmark (600 ticks): " +
                      $"1={one.ElapsedMilliseconds:F3}ms, " +
                      $"10={ten.ElapsedMilliseconds:F3}ms, " +
                      $"100={hundred.ElapsedMilliseconds:F3}ms; " +
                      $"GC Alloc: {one.AllocatedBytes}/{ten.AllocatedBytes}/{hundred.AllocatedBytes} bytes");

            Assert.That(one.AllocatedBytes, Is.Zero);
            Assert.That(ten.AllocatedBytes, Is.Zero);
            Assert.That(hundred.AllocatedBytes, Is.Zero);
        }

        private MarqueeComponent CreateMarquee(string text)
        {
            EnsureCanvas();
            return CreateMarqueeChild(text, 0);
        }

        private MarqueeComponent CreateMarqueeChild(string value, int index)
        {
            EnsureCanvas();
            var child = new GameObject(
                $"Marquee {index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(RectMask2D),
                typeof(MarqueeComponent));
            child.transform.SetParent(_canvasObject.transform, false);

            var rect = (RectTransform)child.transform;
            rect.sizeDelta = new Vector2(100f, 40f);

            var text = child.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = false;
            text.fontSize = 24f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = value;
            return child.GetComponent<MarqueeComponent>();
        }

        private void EnsureCanvas()
        {
            if (_canvasObject != null)
            {
                return;
            }

            _canvasObject = new GameObject(
                "Test Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private static BenchmarkResult Measure(MarqueeComponent[] marquees, int count, int ticks)
        {
            for (int i = 0; i < count; i++)
            {
                marquees[i].TickForTests(1f / 60f);
            }

            _ = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int tick = 0; tick < ticks; tick++)
            {
                for (int i = 0; i < count; i++)
                {
                    marquees[i].TickForTests(1f / 60f);
                }
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            stopwatch.Stop();
            return new BenchmarkResult(stopwatch.Elapsed.TotalMilliseconds, allocated);
        }

        private readonly struct BenchmarkResult
        {
            internal BenchmarkResult(double elapsedMilliseconds, long allocatedBytes)
            {
                ElapsedMilliseconds = elapsedMilliseconds;
                AllocatedBytes = allocatedBytes;
            }

            internal double ElapsedMilliseconds { get; }
            internal long AllocatedBytes { get; }
        }

        private const string LongText =
            "This is a deliberately long TextMeshPro marquee message used for runtime integration tests.";
    }
}
