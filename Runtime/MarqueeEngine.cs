namespace MarqueeText
{
    /// <summary>
    /// Allocation-free marquee state machine with no Unity or TextMeshPro dependencies.
    /// </summary>
    internal sealed class MarqueeEngine
    {
        private enum Phase
        {
            StartDelay,
            Scrolling,
            EndPause
        }

        private const float OverflowEpsilon = 0.01f;
        private const int MaxTransitionsPerTick = 64;

        private Phase _phase = Phase.StartDelay;
        private float _phaseTime;
        private int _pingPongDirection = 1;

        internal MarqueeScrollMode Mode { get; private set; } = MarqueeScrollMode.Continuous;
        internal float Speed { get; private set; } = 60f;
        internal float Gap { get; private set; } = 50f;
        internal float StartDelay { get; private set; } = 1f;
        internal float EndPause { get; private set; } = 1f;
        internal float ViewportWidth { get; private set; }
        internal float ContentWidth { get; private set; }
        internal float Offset { get; private set; }

        internal bool IsOverflowing => ContentWidth > ViewportWidth + OverflowEpsilon;
        internal float Period => ContentWidth + Gap;

        internal void Configure(
            MarqueeScrollMode mode,
            float speed,
            float gap,
            float startDelay,
            float endPause)
        {
            Mode = mode;
            Speed = Max(0.01f, speed);
            Gap = Max(0f, gap);
            StartDelay = Max(0f, startDelay);
            EndPause = Max(0f, endPause);
        }

        internal void SetGeometry(float viewportWidth, float contentWidth)
        {
            ViewportWidth = Max(0f, viewportWidth);
            ContentWidth = Max(0f, contentWidth);
        }

        internal void Restart()
        {
            Offset = 0f;
            _phaseTime = 0f;
            _pingPongDirection = 1;
            _phase = Phase.StartDelay;
        }

        /// <summary>
        /// Advances the state machine. Returns true only when the rendered position changed.
        /// </summary>
        internal bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f || !IsOverflowing)
            {
                return false;
            }

            float remaining = deltaTime;
            bool positionChanged = false;
            int transitions = 0;

            while (remaining > 0f && transitions++ < MaxTransitionsPerTick)
            {
                switch (_phase)
                {
                    case Phase.StartDelay:
                        ConsumeDelay(ref remaining, StartDelay, Phase.Scrolling);
                        break;

                    case Phase.Scrolling:
                        if (Mode == MarqueeScrollMode.Continuous)
                        {
                            float period = Max(0.01f, Period);
                            Offset = (Offset + Speed * remaining) % period;
                            remaining = 0f;
                            positionChanged = true;
                        }
                        else if (Mode == MarqueeScrollMode.Restart)
                        {
                            positionChanged |= AdvanceToBoundary(
                                ref remaining,
                                ContentWidth,
                                1);
                        }
                        else
                        {
                            float limit = Max(0f, ContentWidth - ViewportWidth);
                            float target = _pingPongDirection > 0 ? limit : 0f;
                            positionChanged |= AdvanceToBoundary(
                                ref remaining,
                                target,
                                _pingPongDirection);
                        }
                        break;

                    case Phase.EndPause:
                        if (!ConsumeDelay(ref remaining, EndPause, Phase.Scrolling))
                        {
                            break;
                        }

                        if (Mode == MarqueeScrollMode.Restart)
                        {
                            if (Offset != 0f)
                            {
                                Offset = 0f;
                                positionChanged = true;
                            }
                        }
                        else if (Mode == MarqueeScrollMode.PingPong)
                        {
                            _pingPongDirection = -_pingPongDirection;
                        }
                        break;
                }
            }

            return positionChanged;
        }

        private bool AdvanceToBoundary(ref float remaining, float target, int direction)
        {
            float distance = direction > 0 ? target - Offset : Offset - target;
            if (distance <= 0f)
            {
                Offset = target;
                _phase = Phase.EndPause;
                _phaseTime = 0f;
                return false;
            }

            float timeToBoundary = distance / Speed;
            if (remaining < timeToBoundary)
            {
                Offset += direction * Speed * remaining;
                remaining = 0f;
                return true;
            }

            Offset = target;
            remaining -= timeToBoundary;
            _phase = Phase.EndPause;
            _phaseTime = 0f;
            return true;
        }

        /// <summary>Returns true when the delay completed during this call.</summary>
        private bool ConsumeDelay(ref float remaining, float duration, Phase nextPhase)
        {
            float timeLeft = duration - _phaseTime;
            if (timeLeft > remaining)
            {
                _phaseTime += remaining;
                remaining = 0f;
                return false;
            }

            remaining -= Max(0f, timeLeft);
            _phaseTime = 0f;
            _phase = nextPhase;
            return true;
        }

        private static float Max(float a, float b) => a > b ? a : b;
    }
}
