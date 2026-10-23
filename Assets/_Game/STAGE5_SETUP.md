# Stage 5 - Bots, AI and Match Objectives

## Finish the scene installation

1. In Unity, use **Assets > Refresh**.
2. Wait for script compilation. `Stage5BotsBuilder` installs the stage once when `BotsStage5` is absent.
3. If it does not run automatically, use **Pixel Brawl > Stage 5 > Install Bots and Objectives**.

The installer creates `EnemyBot.prefab`, three scene instances, safe spawn points, patrol points, the round objective controller and HUD fields on the existing Canvas.

## NavMesh

The authorized official `AI Navigation` package is installed. The Stage 5 installer creates a `Navigation` object with `NavMeshSurface` and bakes the arena automatically from its physics colliders.

To rebuild after changing the arena, use **Pixel Brawl > Stage 5 > Rebuild Arena NavMesh** or select the `Navigation` object and use the `NavMeshSurface` Inspector.

## Controls and validation

- `F10`: skip to COMBAT.
- `F11`: eliminate all bots.
- `F12`: restart the current round.
- `Page Up`: cycle Easy, Normal and Hard.

These shortcuts only run in the Editor or a Development Build. Select a bot to see field-of-view, detection, attack-range and current-path gizmos.

Expected round behavior:

- BUY PHASE: bots wait and cannot deal or receive damage.
- COMBAT: bots patrol, detect, chase, investigate, attack and retreat according to difficulty.
- Killing every bot wins the round. Player death loses it.
- A timeout is a player loss while enemies remain alive.
- Round reward submission is guarded so it runs once.
