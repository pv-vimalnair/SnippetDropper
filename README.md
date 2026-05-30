# SnippetDropper

SnippetDropper is a tiny Windows desktop utility for people who repeatedly type the same commands, phrases, or short snippets.

Keep a compact floating widget on your desktop, hover to reveal your four most recent snippets, then drag one toward the terminal or editor where your typing cursor is already active. When you release the mouse, SnippetDropper pastes the text into that app.

It is designed for workflows such as repeatedly running `flutter run`, `flutter pub get`, `npm run dev`, or any other short command while developing.

## Features

- Small always-on-top desktop widget
- Low-profile transparent mode when idle
- Hover to reveal the four most recently added snippets
- Drag-to-paste gesture that works with terminals and editors
- Editable snippet list
- Transparency presets from nearly solid to very faint
- System tray controls for editing and exit
- Portable executable and per-user installer
- No external dependencies
- No administrator access required for installation

## Install

Download and run:

[`release/SnippetDropper-Setup.exe`](release/SnippetDropper-Setup.exe)

The installer creates Start Menu and optional Desktop shortcuts, then launches the app.

For a portable launch without installation, run:

[`release/SnippetDropper.exe`](release/SnippetDropper.exe)

## How To Use

1. Launch SnippetDropper.
2. Click in a terminal or editor so its typing caret is visible.
3. Hover over the small `SNIPS` widget.
4. Hold the left mouse button on a saved snippet.
5. Drag away from the widget and release the mouse button.
6. The text is pasted into the app that had focus before the drag.
7. Press `Enter` yourself if you want to execute the pasted terminal command.

SnippetDropper deliberately pastes text without automatically pressing `Enter`. This keeps command execution under your control.

## Manage Snippets

Right-click the widget or its tray icon, then choose `Manage snippets...`.

You can:

- Add a snippet
- Update an existing snippet
- Remove snippets
- Change their order

The hover widget always shows the four most recently ordered snippets.

## Transparency

Right-click the widget or tray icon, open `Transparency`, and select a preset:

- `10% transparent (nearly solid)`
- `30% transparent`
- `50% transparent`
- `70% transparent`
- `85% transparent (very faint)`

Your choice is remembered between launches.

## Common Use Cases

- Flutter development: `flutter run`, `flutter pub get`, `flutter clean`
- Web development: `npm run dev`, `npm test`, `git status`
- Repeated terminal commands during debugging
- Short responses, paths, IDs, or phrases used across desktop apps
- Accessibility support for reducing repetitive typing

Read the detailed [use-case guide](docs/USE_CASES.md) and [product design specification](docs/PRODUCT_DESIGN.md).

## Data And Privacy

SnippetDropper is local-only:

- Snippets are stored under `%APPDATA%\SnippetDropper\snippets.json`.
- Widget transparency and position are stored under `%APPDATA%\SnippetDropper\settings.json`.
- No analytics, network requests, or cloud sync are included.

Do not store passwords, API keys, or other secrets as snippets.

## Build From Source

Requirements:

- Windows
- Windows PowerShell
- Built-in .NET Framework C# compiler

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-SnippetDropper.ps1
```

The build creates:

- `release\SnippetDropper.exe`
- `release\SnippetDropper-Setup.exe`

See [technical design](docs/TECHNICAL_DESIGN.md) for implementation details and limitations.

