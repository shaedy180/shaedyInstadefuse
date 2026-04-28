# shaedy InstaDefuse

A CounterStrikeSharp plugin that instantly defuses the bomb when all enemies are dead.

## Features

- Instant defuse when a CT begins defusing and no enemies are alive
- Safety checks: blocks if enemies are alive, bomb is in fire, or projectiles are nearby
- Real-time bomb timer overlay for players near the bomb
- Defuse status messages (kit check, can defuse info)
- 175 unit safety radius for fire and projectile detection
- 400 unit proximity radius for bomb timer display

## Dependencies

- [shaedyHudManager](https://github.com/shaedy180/shaedyHudManager) - Centralized HUD overlay manager (must be installed alongside this plugin, connects via runtime reflection)

## Installation

Drop the plugin folder into your CounterStrikeSharp `plugins` directory.

## Configuration

No config needed. Safety radius and proximity radius are hardcoded.

## Support

If you find a bug, have a feature request, or something isn't working as expected, feel free to [open an issue](../../issues). I'll take a look when I can.

Custom plugins are available on request, potentially for a small fee depending on scope. Reach out via an issue or at access@shaedy.de.

> Note: These repos may not always be super active since most of my work happens in private repositories. But issues and requests are still welcome.

## Donate

If you want to support my work: [ko-fi.com/shaedy](https://ko-fi.com/shaedy)
