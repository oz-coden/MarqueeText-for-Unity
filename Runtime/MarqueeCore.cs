using System;
using UnityEngine;

namespace MarqueeText
{
    /// <summary>
    /// Pure computation engine for Marquee text scrolling and position calculations.
    /// Decoupled from Unity UI rendering components to allow isolated unit testing.
    /// </summary>
    public class MarqueeCore
    {
        public enum State
        {
            StartDelay,
            Scrolling,
            EndDelay,
            PingPongReturnDelay
        }

        // Configuration
        public MarqueeDirection Direction { get; set; } = MarqueeDirection.RightToLeft;
        public MarqueeScrollMode ScrollMode { get; set; } = MarqueeScrollMode.Continuous;
        public float Speed { get; set; } = 60f;
        public float Gap { get; set; } = 50f;
        public float StartDelay { get; set; } = 1.0f;
        public float LoopDelay { get; set; } = 1.0f;

        // Viewport & Content Dimensions
        public float ViewportWidth { get; set; }
        public float ViewportHeight { get; set; }
        public float ContentWidth { get; set; }
        public float ContentHeight { get; set; }

        // Runtime State
        public State CurrentState { get; private set; } = State.StartDelay;
        public float CurrentOffset { get; private set; } = 0f;
        public float StateTimer { get; private set; } = 0f;
        public int PingPongDirection { get; private set; } = 1; // 1 = forward, -1 = reverse

        // Events
        public event Action OnScrollStarted;
        public event Action OnScrollLoopCompleted;

        public bool IsHorizontal =>
            Direction == MarqueeDirection.RightToLeft || Direction == MarqueeDirection.LeftToRight;

        public float MainViewportDim => IsHorizontal ? ViewportWidth : ViewportHeight;
        public float MainContentDim => IsHorizontal ? ContentWidth : ContentHeight;
        public float CrossViewportDim => IsHorizontal ? ViewportHeight : ViewportWidth;
        public float CrossContentDim => IsHorizontal ? ContentHeight : ContentWidth;

        public bool IsOverflowing => MainContentDim > MainViewportDim;

        /// <summary>
        /// Resets the scroll offset, timers, and state to initial conditions.
        /// </summary>
        public void Reset()
        {
            CurrentOffset = 0f;
            StateTimer = 0f;
            PingPongDirection = 1;
            CurrentState = State.StartDelay;
        }

        /// <summary>
        /// Advances the marquee simulation by deltaTime.
        /// </summary>
        /// <param name="deltaTime">Time step in seconds.</param>
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            float speed = Mathf.Max(0.01f, Speed);
            float startDelay = Mathf.Max(0f, StartDelay);
            float loopDelay = Mathf.Max(0f, LoopDelay);

            switch (CurrentState)
            {
                case State.StartDelay:
                    StateTimer += deltaTime;
                    if (StateTimer >= startDelay)
                    {
                        float excessTime = StateTimer - startDelay;
                        StateTimer = 0f;
                        CurrentState = State.Scrolling;
                        OnScrollStarted?.Invoke();
                        if (excessTime > 0f)
                        {
                            StepScroll(speed * excessTime);
                        }
                    }
                    break;

                case State.Scrolling:
                    StepScroll(speed * deltaTime);
                    break;

                case State.EndDelay:
                    StateTimer += deltaTime;
                    if (StateTimer >= loopDelay)
                    {
                        float excessTime = StateTimer - loopDelay;
                        StateTimer = 0f;
                        if (ScrollMode == MarqueeScrollMode.Restart)
                        {
                            CurrentOffset = 0f;
                            CurrentState = State.StartDelay;
                            OnScrollLoopCompleted?.Invoke();
                            if (startDelay <= 0f && excessTime > 0f)
                            {
                                CurrentState = State.Scrolling;
                                StepScroll(speed * excessTime);
                            }
                        }
                        else if (ScrollMode == MarqueeScrollMode.PingPong)
                        {
                            PingPongDirection = -1;
                            CurrentState = State.Scrolling;
                            if (excessTime > 0f)
                            {
                                StepScroll(speed * excessTime);
                            }
                        }
                    }
                    break;

                case State.PingPongReturnDelay:
                    StateTimer += deltaTime;
                    if (StateTimer >= loopDelay)
                    {
                        float excessTime = StateTimer - loopDelay;
                        StateTimer = 0f;
                        PingPongDirection = 1;
                        CurrentState = State.Scrolling;
                        OnScrollLoopCompleted?.Invoke();
                        if (excessTime > 0f)
                        {
                            StepScroll(speed * excessTime);
                        }
                    }
                    break;
            }
        }

        private void StepScroll(float step)
        {
            float contentDim = MainContentDim;
            float viewportDim = MainViewportDim;

            switch (ScrollMode)
            {
                case MarqueeScrollMode.Continuous:
                    float period = contentDim + Mathf.Max(0f, Gap);
                    if (period <= 0f) period = 1f;

                    CurrentOffset += step;
                    if (CurrentOffset >= period)
                    {
                        CurrentOffset %= period;
                        OnScrollLoopCompleted?.Invoke();
                    }
                    break;

                case MarqueeScrollMode.Restart:
                    float maxRestartOffset = contentDim;
                    CurrentOffset += step;
                    if (CurrentOffset >= maxRestartOffset)
                    {
                        CurrentOffset = maxRestartOffset;
                        CurrentState = State.EndDelay;
                    }
                    break;

                case MarqueeScrollMode.PingPong:
                    float maxPingPongOffset = Mathf.Max(0f, contentDim - viewportDim);
                    if (PingPongDirection == 1)
                    {
                        CurrentOffset += step;
                        if (CurrentOffset >= maxPingPongOffset)
                        {
                            CurrentOffset = maxPingPongOffset;
                            CurrentState = State.EndDelay;
                        }
                    }
                    else
                    {
                        CurrentOffset -= step;
                        if (CurrentOffset <= 0f)
                        {
                            CurrentOffset = 0f;
                            CurrentState = State.PingPongReturnDelay;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Calculates the primary and ghost anchored positions in the 2D local rect coordinates.
        /// Assumes child RectTransforms have Pivot=(0, 0) and Anchor=(0, 0).
        /// </summary>
        public void CalculatePositions(out Vector2 primaryPos, out Vector2 ghostPos, out bool showGhost)
        {
            float period = MainContentDim + Mathf.Max(0f, Gap);
            if (period <= 0f) period = 1f;

            float crossCenter = (CrossViewportDim - CrossContentDim) * 0.5f;

            primaryPos = Vector2.zero;
            ghostPos = Vector2.zero;
            showGhost = false;

            switch (Direction)
            {
                case MarqueeDirection.RightToLeft:
                    // Primary moves left: X from 0 towards negative
                    primaryPos = new Vector2(-CurrentOffset, crossCenter);
                    ghostPos = new Vector2(-CurrentOffset + period, crossCenter);
                    showGhost = ScrollMode == MarqueeScrollMode.Continuous;
                    break;

                case MarqueeDirection.LeftToRight:
                    // Primary moves right: X from (V - C) towards positive
                    float ltrBase = ViewportWidth - ContentWidth;
                    primaryPos = new Vector2(ltrBase + CurrentOffset, crossCenter);
                    ghostPos = new Vector2(ltrBase + CurrentOffset - period, crossCenter);
                    showGhost = ScrollMode == MarqueeScrollMode.Continuous;
                    break;

                case MarqueeDirection.BottomToTop:
                    // Primary moves up: Y from 0 towards positive
                    primaryPos = new Vector2(crossCenter, CurrentOffset);
                    ghostPos = new Vector2(crossCenter, CurrentOffset - period);
                    showGhost = ScrollMode == MarqueeScrollMode.Continuous;
                    break;

                case MarqueeDirection.TopToBottom:
                    // Primary moves down: Y from (V - C) towards negative
                    float ttbBase = ViewportHeight - ContentHeight;
                    primaryPos = new Vector2(crossCenter, ttbBase - CurrentOffset);
                    ghostPos = new Vector2(crossCenter, ttbBase - CurrentOffset + period);
                    showGhost = ScrollMode == MarqueeScrollMode.Continuous;
                    break;
            }
        }

        /// <summary>
        /// Computes static position when scrolling is not active (e.g. content fits within viewport).
        /// Aligns according to standard horizontal/vertical alignment flags.
        /// </summary>
        public Vector2 CalculateStaticPosition(int horizontalAlign, int verticalAlign)
        {
            // horizontalAlign: -1 = Left, 0 = Center, 1 = Right
            // verticalAlign:   -1 = Bottom, 0 = Middle, 1 = Top

            float x = horizontalAlign switch
            {
                1 => ViewportWidth - ContentWidth,
                0 => (ViewportWidth - ContentWidth) * 0.5f,
                _ => 0f
            };

            float y = verticalAlign switch
            {
                1 => ViewportHeight - ContentHeight,
                0 => (ViewportHeight - ContentHeight) * 0.5f,
                _ => 0f
            };

            return new Vector2(x, y);
        }
    }
}
