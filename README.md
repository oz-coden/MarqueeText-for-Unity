# MarqueeText for TextMeshPro

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

**MarqueeText** is a lightweight, zero-configuration ticker / marquee text component for Unity's TextMeshPro. When text overflows its RectTransform bounding box, it automatically smoothly scrolls like an LED electric bulletin board (電光掲示板).

---

## ✨ Features (特徴)

- **⚡ Zero Setup (アタッチするだけ)**: Just attach `MarqueeText` to any `TextMeshProUGUI` component. Masks and internal renderers are handled automatically.
- **🔄 Multiple Scroll Modes (多彩なループ方式)**:
  - **`Continuous`**: Seamless endless ticker with no gaps — next repetition follows seamlessly.
  - **`Restart`**: Scrolls completely to the end, then restarts from the beginning.
  - **`PingPong`**: Bounces back and forth between start and end.
- **🧭 4-Way Direction (4方向対応)**: Horizontal (Right-to-Left / Left-to-Right) and Vertical (Bottom-to-Top / Top-to-Bottom).
- **📐 Smart Overflow Detection (自動オーバーフロー検知)**: Only scrolls when text exceeds the bounding box. If the text fits, it aligns naturally (Left, Center, Right, etc.).
- **🎨 Dynamic & Rich Text Compatible (動的更新・リッチテキスト対応)**: Supports colors, font size, sprite tags, and runtime text changes seamlessly.
- **⏱ Timing Controls (柔軟なタイミング設定)**: Configurable start delay, loop pause, speed (px/sec), and unscaled time support.
- **🎛 Live Editor Preview (エディタプレビュー)**: Preview and test marquee scroll directly inside the Scene/Game view without entering Play mode.

---

## 📦 Installation (インストール方法)

### Option 1: Unity Package Manager (via Git URL)
1. Open the **Package Manager** in Unity (`Window` > `Package Manager`).
2. Click the `+` button in the top-left corner and select **"Add package from git URL..."**.
3. Enter the repository URL:
   ```
   https://github.com/white/MarqueeText.git?path=Assets/MarqueeText
   ```

### Option 2: Manual / Local Package
Copy or clone the `Assets/MarqueeText` folder into your Unity project's `Assets` or `Packages` directory.

---

## 🚀 Quick Start (使い方)

1. Create a **TextMeshPro - Text (UI)** in your Canvas.
2. Set the `RectTransform` size (Width / Height) to your desired viewing window.
3. Attach the **`MarqueeText`** component to the TextMeshPro GameObject (`Add Component` > `UI` > `Marquee Text (TMPro)`).
4. That's it! If the text overflows the box, it will automatically scroll!

---

## ⚙️ Inspector Options (インスペクター設定)

| Setting | Description |
|---|---|
| **Direction** | `RightToLeft` (Standard ticker), `LeftToRight`, `BottomToTop` (Credits), `TopToBottom`. |
| **Scroll Mode** | `Continuous` (Seamless loop), `Restart` (Reset to start), `PingPong` (Bounce back and forth). |
| **Speed** | Scroll velocity in pixels per second. |
| **Gap** | Spacing in pixels between the end and the start in `Continuous` mode. |
| **Start Delay** | Delay (seconds) before scrolling begins. |
| **Loop Delay** | Pause duration (seconds) at the ends for `Restart` and `PingPong` modes. |
| **Use Unscaled Time** | When enabled, scrolls even when `Time.timeScale == 0` (e.g. pause menus). |
| **Trigger Mode** | `OnlyIfOverflowing` (Default), `Always`, `OnHover` (Mouse hover), or `Manual`. |
| **Preview In Editor** | Enables live animation in Edit Mode. |

---

## 💻 Scripting API (スクリプトからの操作)

```csharp
using UnityEngine;
using MarqueeText;

public class ExampleController : MonoBehaviour
{
    [SerializeField] private MarqueeText _marquee;

    void Start()
    {
        // Update text dynamically (automatically recalculates bounds)
        _marquee.SetText("Now Playing: Amazing Track Title - Artist Name");

        // Control playback
        _marquee.Play();
        _marquee.Pause();
        _marquee.Restart();
        _marquee.Stop();

        // Adjust settings at runtime
        _marquee.Speed = 100f;
        _marquee.Gap = 80f;
        _marquee.ScrollMode = MarqueeScrollMode.Continuous;
        _marquee.Direction = MarqueeDirection.RightToLeft;
        
        // Status checks
        Debug.Log($"Is Overflowing: {_marquee.IsOverflowing}");
        Debug.Log($"Is Scrolling: {_marquee.IsScrolling}");
    }
}
```

### Events (UnityEvent)
- `onScrollStarted` : Invoked when scrolling begins after initial start delay.
- `onScrollLoopCompleted` : Invoked every time a full loop/cycle finishes.
- `onScrollPaused` : Invoked when paused or stopped.
- `onScrollResumed` : Invoked when playback resumes.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE.md).
