# MarqueeText for Unity

Japanese section is [available](#目次) below this section.

## Table of Contents

1. [Summary](#summary)
2. [Background of Development](#background-of-development)
3. [Features](#features)
4. [Usage](#usage)
5. [Requirements, Dependencies](#requirements-dependencies)
6. [Installation](#installation)
7. [Runtime API](#runtime-api)
8. [Performance](#performance)
9. [LICENSE](#license)

## Summary

MarqueeText is a lightweight horizontal scrolling text component for `TextMeshProUGUI`.  
It automatically scrolls text when the text is wider than its RectTransform, while text that fits remains static.

It supports multiple scrolling modes, runtime text changes, playback control, and Edit Mode Preview.

## Background of Development

When displaying long text in Unity UI, developers sometimes need to implement scrolling text such as news tickers, item names, music titles, or other labels that do not fit within the available space.

Although moving text itself is simple, implementing seamless loops, overflow detection, pause behavior, runtime text changes, and editor previews can make the implementation unnecessarily complicated.

I made this component to provide a small and reusable solution for these cases.

## Features

- Automatic Overflow Detection  
Text scrolls only when its width exceeds the width of its RectTransform.

- Multiple Scroll Modes  
  - `Continuous`  
    Continuously loops the text with a configurable gap.
  - `Restart`  
    Scrolls the text completely and then restarts from the beginning.
  - `PingPong`  
    Scrolls between both ends repeatedly.

- Horizontal Scroll Directions  
  - Right to Left
  - Left to Right

- Playback Control  
  - `Play()`
  - `Pause()`
  - `Restart()`

- Runtime Text Change Support  
Changes to `TextMeshProUGUI.text` are automatically detected.

- Edit Mode Preview  
Scrolling can be previewed directly from the Inspector without entering Play Mode.

- Unscaled Time Support  
Scrolling can continue while `Time.timeScale` is `0`.

- Lightweight Runtime Behavior  
Non-overflowing and paused text avoids unnecessary position updates.

## Usage

1. Create a **Text - TextMeshPro** object under a Canvas.

2. Resize its RectTransform to the desired visible width.

3. Add the **Marquee Text (TextMeshPro)** component.

4. Configure the scrolling behavior from the Inspector.

5. Enter Play Mode.

Text automatically scrolls only when it exceeds the width of its RectTransform.

### Inspector Settings

| Setting | Description |
| --- | --- |
| Mode | Selects `Continuous`, `Restart`, or `PingPong` |
| Direction | Selects `RightToLeft` or `LeftToRight` |
| Speed | Scroll speed in pixels per second |
| Start Delay | Delay before scrolling begins |
| Gap | Space between copies in Continuous mode |
| End Pause | Pause at the edge in Restart and PingPong modes |
| Use Unscaled Time | Continues scrolling while `Time.timeScale` is `0` |

You can also use the **Preview** button in the Inspector to preview the scrolling animation in Edit Mode.

## Requirements, Dependencies

Already Checked:

- Unity 6
- Unity UI / TextMeshPro
- `TextMeshProUGUI`

World-space `TextMeshPro` is not supported.

## Installation

### Method 1: Using UPM (Unity Package Manager)

1. Open the Unity menu and select **Window > Package Manager**.
2. Click the **+** button in the upper-left corner and select **Add package from git URL...**.
3. Enter the following URL and click **Add**.

```text
https://github.com/oz-coden/MarqueeText-for-Unity.git
```

### Method 2: Using UnityPackage  

1. Download the latest .unitypackage from the GitHub Releases page.
2. Drag and drop the downloaded file into your Unity project to import it.

## Runtime API

MarqueeText provides a small runtime API.

```C#
using TMPro;
using UnityEngine;
using Marquee = MarqueeText.MarqueeText;

public class Example : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Marquee marquee;

    public void SetText(string text)
    {
        label.text = text;
        marquee.Restart();
    }

    public void Pause()
    {
        marquee.Pause();
    }

    public void Resume()
    {
        marquee.Play();
    }
}
```

Available members:

- `Play()`  
Resumes scrolling from the current position.

- `Pause()`  
Pauses scrolling at the current position.

- `Restart()`  
Returns the text to the starting position and starts scrolling again.

- `Speed`  
Gets or sets the scrolling speed.

- `IsPlaying`  
Returns whether the marquee is currently playing.

- `IsOverflowing`  
Returns whether the text exceeds the viewport width.

To change the displayed text, simply change `TextMeshProUGUI.text`.  
MarqueeText automatically detects the change, so a separate `SetText` API is not required.

## Performance

MarqueeText is designed to avoid unnecessary processing when possible.

- Text that does not overflow uses the original `TextMeshProUGUI`.
- Internal renderers are created only when scrolling is required.
- Continuous mode creates an additional renderer only when necessary.
- Text size is recalculated only when relevant text or layout settings change.
- Paused text does not update its position every frame.
- Non-overflowing text does not perform scrolling position updates.
- The scrolling state machine does not allocate memory during normal steady-state updates.

Edit Mode Preview is temporary and runs only while Preview is enabled in the Inspector.

## LICENSE

This project is released under the MIT License.

---

# MarqueeText for Unity

## 目次

1. [概要](#概要)
2. [開発背景](#開発背景)
3. [機能](#機能)
4. [使い方](#使い方)
5. [前提・依存](#前提依存)
6. [導入方法](#導入方法)
7. [ランタイムAPI](#ランタイムapi)
8. [パフォーマンス](#パフォーマンス)
9. [ライセンス](#ライセンス)

## 概要

MarqueeTextは、`TextMeshProUGUI`用の軽量な横スクロールテキストコンポーネントです。  
テキストがRectTransformの幅を超えた場合に自動的にスクロールし、幅に収まっている場合は通常のテキストとして表示されます。

複数のスクロールモード、実行中のテキスト変更、再生制御、Edit Mode Previewなどに対応しています。

## 開発背景

UnityのUIで長いテキストを表示するとき、ニュースティッカー、アイテム名、楽曲名など、表示領域に収まらないテキストをスクロールさせたい場合があります。

テキスト自体を移動させるだけであれば簡単ですが、シームレスなループ、はみ出し判定、一時停止、実行中のテキスト変更、エディター上でのプレビューなどまで対応すると、実装が複雑になりがちです。

そのような処理を簡単に再利用できる、小さなコンポーネントとして使用できるようにするために作成しました。

## 機能

- はみ出しの自動判定  
テキストの幅がRectTransformの幅を超えた場合のみスクロールします。

- 複数のスクロールモード  
  - `Continuous`  
    指定した間隔を空けながら、テキストを連続してループします。
  - `Restart`  
    テキストを最後までスクロールした後、最初の位置から再開します。
  - `PingPong`  
    両端の間を往復してスクロールします。

- 横方向のスクロール  
  - 右から左
  - 左から右

- 再生制御  
  - `Play()`
  - `Pause()`
  - `Restart()`

- 実行中のテキスト変更への対応  
`TextMeshProUGUI.text`の変更を自動的に検知します。

- Edit Mode Preview  
Play Modeに入らなくても、Inspectorからスクロールをプレビューできます。

- Unscaled Time対応  
`Time.timeScale`が`0`の場合でもスクロールさせることができます。

- 軽量な実行時処理  
スクロールが不要な場合や一時停止中は、不必要な位置更新を行いません。

## 使い方

1. Canvasの下に **Text - TextMeshPro** を作成します。

2. RectTransformを、表示したい幅に調整します。

3. **Marquee Text (TextMeshPro)** コンポーネントを追加します。

4. Inspectorからスクロール方法を設定します。

5. Play Modeを開始します。

テキストがRectTransformの幅を超えた場合のみ、自動的にスクロールします。

### Inspectorの設定

| 設定 | 説明 |
| --- | --- |
| Mode | `Continuous`、`Restart`、`PingPong`から選択します |
| Direction | `RightToLeft`または`LeftToRight`を選択します |
| Speed | 1秒あたりのスクロール速度をピクセル単位で指定します |
| Start Delay | スクロール開始までの待ち時間です |
| Gap | Continuousモードでテキスト同士の間に空ける間隔です |
| End Pause | Restart / PingPongモードで端に到達した際の待ち時間です |
| Use Unscaled Time | `Time.timeScale`が`0`でもスクロールを継続します |

Inspectorの **Preview** ボタンを使用すると、Edit Modeのままスクロールを確認できます。

## 前提・依存

確認済み：

- Unity 6
- Unity UI / TextMeshPro
- `TextMeshProUGUI`

ワールド空間用の`TextMeshPro`には対応していません。

## 導入方法

### 方法 1: UPM（Unity Package Manager）を使用する

1. Unityメニューを開き、**Window > Package Manager**を選択します。
2. 左上の **+** ボタンをクリックし、**Add package from git URL...**を選択します。
3. 以下のURLを入力し、**Add**をクリックします。

```text
https://github.com/oz-coden/MarqueeText-for-Unity.git
```

### 方法 2: UnityPackage を使用する  

1. GitHub の「Releases」ページから最新の .unitypackage をダウンロードします。
2. ダウンロードしたファイルを Unity プロジェクトにドラッグ＆ドロップしてインポートします。

## ランタイムAPI

MarqueeTextには、シンプルなランタイムAPIが用意されています。

```C#
using TMPro;
using UnityEngine;
using Marquee = MarqueeText.MarqueeText;

public class Example : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Marquee marquee;

    public void SetText(string text)
    {
        label.text = text;
        marquee.Restart();
    }

    public void Pause()
    {
        marquee.Pause();
    }

    public void Resume()
    {
        marquee.Play();
    }
}
```

利用できる主なメンバー：

- `Play()`  
現在位置からスクロールを再開します。

- `Pause()`  
現在位置でスクロールを一時停止します。

- `Restart()`  
開始位置に戻し、最初からスクロールを開始します。

- `Speed`  
スクロール速度を取得・変更します。

- `IsPlaying`  
現在再生中かどうかを取得します。

- `IsOverflowing`  
テキストが表示領域からはみ出しているかどうかを取得します。

表示するテキストを変更する場合は、通常どおり`TextMeshProUGUI.text`を変更してください。  
MarqueeText側で変更を自動的に検知するため、専用の`SetText` APIは必要ありません。

## パフォーマンス

MarqueeTextは、必要のない処理をできるだけ行わないように設計されています。

- テキストがはみ出していない場合は、元の`TextMeshProUGUI`をそのまま使用します。
- 内部レンダラーは、スクロールが必要になった場合のみ生成されます。
- Continuousモードでは、必要な場合のみ追加のレンダラーを生成します。
- テキストサイズの再計算は、テキストやレイアウトに関係する設定が変更された場合のみ行います。
- 一時停止中は、毎フレーム位置を書き換えません。
- テキストがはみ出していない場合は、スクロール位置の更新を行いません。
- 通常のスクロール処理では、状態管理部分による継続的なメモリアロケーションを行いません。

Edit Mode Previewは一時的な機能であり、InspectorでPreviewを有効にしている間だけ動作します。

## ライセンス

This project is released under the MIT License.
