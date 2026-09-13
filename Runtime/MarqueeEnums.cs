namespace MarqueeText
{
    /// <summary>
    /// Direction of the marquee text scroll.
    /// </summary>
    public enum MarqueeDirection
    {
        /// <summary> Scrolls from right to left (standard horizontal ticker / 電光掲示板). </summary>
        RightToLeft = 0,

        /// <summary> Scrolls from left to right. </summary>
        LeftToRight = 1,

        /// <summary> Scrolls from bottom to top (credits / vertical ticker). </summary>
        BottomToTop = 2,

        /// <summary> Scrolls from top to bottom. </summary>
        TopToBottom = 3
    }

    /// <summary>
    /// Looping behavior of the marquee text.
    /// </summary>
    public enum MarqueeScrollMode
    {
        /// <summary>
        /// Seamless endless scrolling where a duplicate copy follows seamlessly with a gap.
        /// (シームレス連続ループ - 隙間を空けて次のテキストが途切れず流れます)
        /// </summary>
        Continuous = 0,

        /// <summary>
        /// Scrolls until the text leaves the viewport, then restarts from the beginning after a delay.
        /// (端まで流れきったら先頭から再スタート)
        /// </summary>
        Restart = 1,

        /// <summary>
        /// Scrolls to the end, pauses, reverses direction back to start, and repeats.
        /// (端まで行ったら反転して往復)
        /// </summary>
        PingPong = 2
    }

    /// <summary>
    /// Conditions under which the marquee should scroll.
    /// </summary>
    public enum MarqueeTriggerMode
    {
        /// <summary>
        /// Only scrolls when the text content size exceeds the RectTransform bounding box.
        /// (枠からはみ出ている時だけスクロール)
        /// </summary>
        OnlyIfOverflowing = 0,

        /// <summary>
        /// Always scrolls regardless of whether text overflows.
        /// (はみ出ていなくても常にスクロール)
        /// </summary>
        Always = 1,

        /// <summary>
        /// Scrolls only when the pointer hovers over the RectTransform (requires EventSystem and RaycastTarget).
        /// (マウスホバー中のみスクロール)
        /// </summary>
        OnHover = 2,

        /// <summary>
        /// Manually controlled via script using Play() / Pause() / Stop().
        /// (スクリプトから手動制御)
        /// </summary>
        Manual = 3
    }
}
