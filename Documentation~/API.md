# MarqueeText API

Namespace: `MarqueeText`

## MarqueeText

`MarqueeText.MarqueeText` is a sealed `MonoBehaviour` for horizontal, overflow-only TextMeshProUGUI scrolling.

### Methods

- `Play()` — resume without changing the current offset or delay.
- `Pause()` — freeze without resetting the current offset or delay.
- `Restart()` — reset the offset and Start Delay, then play.

### Properties

- `float Speed` — scrolling pixels per second; values below `0.01` are clamped.
- `bool IsPlaying` — whether time is allowed to advance.
- `bool IsOverflowing` — whether the last measured unwrapped text width exceeds the viewport width.

Runtime text is changed on the attached `TextMeshProUGUI` component. MarqueeText observes TMP layout-dirty notifications and updates itself on the next tick.
