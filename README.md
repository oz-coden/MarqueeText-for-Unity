# MarqueeText for Unity

`MarqueeText` is a small horizontal marquee component for `TextMeshProUGUI`. Attach it to a TMP UI text object, set a speed, and overflowing text scrolls automatically. Text that fits remains a normal TMP object with no internal renderer.

## Requirements

- Unity 6.0 or newer
- Unity UI / TextMeshPro (`com.unity.ugui` 2.0.0 or newer)
- `TextMeshProUGUI`; world-space `TextMeshPro` is not supported

Unity 2021/2022 compatibility is intentionally not claimed. Supporting their separate TMP package would add a second dependency and API compatibility path.

## Installation

### Unity Package Manager

In **Window > Package Manager**, choose **Add package from git URL** and enter:

```text
https://github.com/oz-coden/MarqueeText-for-Unity.git?path=Assets/MarqueeText
```

To pin a release, append a tag after the path, for example:

```text
https://github.com/oz-coden/MarqueeText-for-Unity.git?path=Assets/MarqueeText#v1.0.0
```

### Manual installation

Choose one location:

- Copy `Assets/MarqueeText` into your project's `Assets` directory, or
- Copy it to `Packages/com.github.oz-coden.marqueetext` as an embedded package.

Do not copy the entire development repository into `Packages`; the package root is `Assets/MarqueeText`.

## Quick start

1. Create **UI > Text - TextMeshPro** under a Canvas.
2. Resize its RectTransform to the desired visible width.
3. Add **UI > Marquee Text (TextMeshPro)**.
4. Enter Play Mode. The text moves only when its unwrapped width exceeds the RectTransform width.

`RectMask2D` and internal renderers are managed automatically.

## Inspector

| Setting | Purpose |
| --- | --- |
| Mode | `Continuous`, `Restart`, or `PingPong` |
| Direction | `RightToLeft` or `LeftToRight` |
| Speed | Pixels per second |
| Start Delay | Delay applied after enable or manual restart |
| Gap | Space between copies in Continuous mode |
| End Pause | Edge pause in Restart and PingPong modes |
| Use Unscaled Time | Advanced option for pause menus |

Edit Mode Preview is a temporary Inspector action. It runs only while that Inspector is open, updates at 30 FPS, and is not saved into the Scene or Prefab.

## Runtime API

```csharp
using TMPro;
using UnityEngine;
using Marquee = MarqueeText.MarqueeText;

public sealed class NowPlayingLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Marquee marquee;

    public void SetTitle(string title)
    {
        // Normal TMP changes are detected automatically.
        label.text = title;
        marquee.Restart();
    }

    public void Pause() => marquee.Pause();
    public void Resume() => marquee.Play();
}
```

Public surface:

- `Play()` resumes at the current position.
- `Pause()` freezes the current position.
- `Restart()` returns to the start, reapplies Start Delay, and plays.
- `Speed` can be changed at runtime.
- `IsPlaying` and `IsOverflowing` are read-only state.

Set text through `TextMeshProUGUI.text`; `MarqueeText` deliberately has no duplicate `SetText` API.

## Rendering and performance

- Non-overflowing text uses the original TMP renderer and performs no position work.
- Overflowing Restart/PingPong text creates one hidden internal TMP renderer.
- Continuous mode creates a second renderer for the following copy.
- Text measurement runs only after text, layout, material, RectTransform, or Inspector configuration changes.
- Delay and Pause frames do not write RectTransform positions.
- The state engine and steady-state component tick are covered by zero-allocation tests.

Frequent per-frame changes to TMP layout or material properties necessarily trigger remeasurement and renderer synchronization. For animated color effects, prefer a shared material animation when possible.

## Samples and tests

Import **Marquee Text Demo** from Package Manager's Samples tab, then open `MarqueeTextDemo.unity` and enter Play Mode.

The package includes EditMode tests for the state engine and PlayMode tests for TMP integration, runtime text updates, Pause/Restart, enable/disable, steady-state GC allocation, and 1/10/100 instance benchmarks.

## License

MIT. See [LICENSE.md](LICENSE.md).
