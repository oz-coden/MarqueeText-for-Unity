# Changelog

## 1.0.0 - 2026-09-14

- Rebuilt the runtime around a Unity-independent `MarqueeEngine` and a focused TMP renderer.
- Limited scrolling to horizontal Right-to-Left and Left-to-Right movement.
- Retained Continuous, Restart, and PingPong modes.
- Made overflow-only behavior unconditional.
- Added lazy Primary/Ghost renderer creation and change-driven TMP measurement.
- Replaced persistent 60 FPS Edit Mode preview with a temporary 30 FPS preview.
- Reduced the public API to Play, Pause, Restart, Speed, and read-only state.
- Removed Trigger modes, hover behavior, UnityEvents, Stop, SetText, and public layout metrics.
- Added EditMode, PlayMode, allocation, and 1/10/100-instance benchmark coverage.
