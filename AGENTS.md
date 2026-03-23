# AGENTS.md

This file explains how to work safely in this repository as an AI coding agent.

## Project

- Project type: Unity project
- Unity version: `2022.3.62f2`
- Solution file: `hakaton_1.sln`
- Current visible scene: `Assets/Scenes/SampleScene.unity`

## Main Goal

When making changes, prefer the smallest working change that keeps the Unity project stable and easy to open in the editor.

## Where To Work

- Main source area: `Assets/`
- Packages config: `Packages/manifest.json`
- Project configuration: `ProjectSettings/`

## Files And Folders To Avoid Editing Unless Needed

Only touch these when the task explicitly requires it:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `Packages/packages-lock.json`

These are usually generated, machine-specific, or noisy.

## Unity-Specific Rules

- Keep `.meta` files in sync with moved or created assets.
- Do not rename, move, or delete assets casually; Unity references can break silently.
- Prefer editor-safe changes inside `Assets/` over manual edits to serialized scene or project files.
- Be careful when editing `.unity`, `.prefab`, `.asset`, and other serialized Unity files by hand.
- If a C# script is added, keep its namespace and folder placement tidy and predictable.

## Code Style

- Prefer clear, small C# scripts over large multipurpose classes.
- Avoid introducing new dependencies unless they are necessary.
- Match the existing project structure instead of inventing a new architecture.
- Add brief comments only where behavior is not obvious.

## Validation

When possible, validate with the lightest useful check:

- confirm file structure is correct
- confirm references and paths make sense
- confirm the project still matches Unity `2022.3.62f2`

If Unity Editor or automated tests are not available, say so clearly instead of pretending the change was fully verified.

## Collaboration Notes

- Prefer small, reversible edits.
- Explain assumptions briefly when the repository does not provide enough context.
- Do not rewrite unrelated files.
- Do not remove user changes unless explicitly asked.

## Practical Defaults For This Repo

- Assume this is an early-stage or hackathon-style Unity project unless the repository later shows stricter conventions.
- Prefer speed and clarity over premature abstraction.
- If there is only one obvious scene or script to change, work there first.
