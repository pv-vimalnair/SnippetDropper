# SnippetDropper Use Cases

## Problem

Developers and desktop users often repeat the same short text many times during a focused task. Retyping a command such as `flutter run` is individually minor but creates unnecessary friction when repeated throughout the day.

SnippetDropper reduces that repetition while keeping the user in control of where text goes and whether a pasted command is executed.

## Primary Use Case: Development Commands

### Scenario

A Flutter developer frequently moves between code and a terminal while debugging.

### Without SnippetDropper

1. Focus the terminal.
2. Type `flutter run`.
3. Press `Enter`.
4. Repeat after each relevant change.

### With SnippetDropper

1. Focus the terminal.
2. Hover over `SNIPS`.
3. Drag `flutter run` toward the terminal and release.
4. Press `Enter`.

### Benefit

The developer avoids retyping while retaining a visible pause before execution.

## Secondary Use Cases

### Web Development

Save commands such as:

```text
npm run dev
npm test
git status
```

### Debugging And Operations

Save frequently repeated diagnostic commands:

```text
flutter doctor
adb devices
git diff --stat
```

### Repeated Text Across Desktop Apps

SnippetDropper is not limited to terminals. It can paste short paths, phrases, IDs, or templates into editors and other desktop text fields.

### Accessibility And Ergonomics

Reducing repetitive typing can help users who experience hand fatigue or who prefer pointer-based workflows.

## Safety Model

SnippetDropper pastes text but does not automatically execute commands. The user chooses whether to press `Enter`.

Avoid saving:

- Passwords
- API keys
- Authentication tokens
- Private recovery codes

## Current Scope

SnippetDropper is intentionally small:

- Four snippets are visible on hover.
- The full snippet list is editable from the tray or context menu.
- Text is pasted through the clipboard.
- The app is local-only.

