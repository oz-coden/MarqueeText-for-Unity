namespace MarqueeText
{
    /// <summary>Horizontal marquee direction.</summary>
    public enum MarqueeDirection
    {
        /// <summary>Moves text from right to left.</summary>
        RightToLeft = 0,

        /// <summary>Moves text from left to right.</summary>
        LeftToRight = 1
    }

    /// <summary>Behavior used when the marquee reaches an edge.</summary>
    public enum MarqueeScrollMode
    {
        /// <summary>A second copy follows the first for an uninterrupted loop.</summary>
        Continuous = 0,

        /// <summary>The text leaves the viewport, pauses, and starts again.</summary>
        Restart = 1,

        /// <summary>The text moves between its first and last visible positions.</summary>
        PingPong = 2
    }
}
