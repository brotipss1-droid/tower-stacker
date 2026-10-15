# Tower Stacker

The first mobile game of Gaming Company's Mobile Studio (Murat and Kerem). Blocks slide across the screen; tap to drop one on the tower. The overhang is cut off, so every miss makes the next block narrower, until it is too thin to land. One thumb, portrait, sessions under three minutes.

## Open the project (Kerem's Mac, Unity 6.6)

1. Clone this repository, or download it as a zip and unzip it.
2. In Unity Hub, under Installs, install **Unity 6000.6.6f1** (Unity 6.6) with the **iOS Build Support** module. Then click **Add** and choose the `tower-stacker` folder. The project is pinned to 6000.6.6f1; if Hub offers a newer 6.6 patch, accept the upgrade.
3. Open the project. Unity creates the `Library` folder on first open; this takes a few minutes.
4. Open any scene (File > New Scene is fine) and press **Play**. The game builds itself from code, so there is no scene to set up.
5. For the phone preview: Edit > Project Settings > Player, set the default orientation to Portrait, then in the Game view choose a portrait resolution such as 1080x1920.

If Unity complains that `Input` is not available, go to Edit > Project Settings > Player > Other Settings > Active Input Handling and choose **Both**.

## How the code is organised

| File | Does |
| --- | --- |
| `Assets/Scripts/Bootstrap.cs` | Creates the camera and the game objects when the app starts. |
| `Assets/Scripts/StackManager.cs` | The whole core loop: sliding block, tap to drop, cutting the overhang, score, game over, restart. The numbers under `Tuning` are the ones to play with. |
| `Assets/Scripts/Block.cs` | One block, drawn as a floor of a Bosphorus house: shaded edge, cornice with dentils, framed windows with sills that light up at night; also how a cut-off piece falls away. |
| `Assets/Scripts/BosphorusScenery.cs` | The theme: sky from morning to night, sun, moon, stars, clouds, the Istanbul skyline with its lights and their reflection, the sea, the quay, a ferry with smoke and a wake, and seagulls. |
| `Assets/Scripts/BosphorusArt.cs` | The artwork itself: the skyline in two depths (Blue Mosque, Hagia Sophia, Galata, Maiden's Tower, the Bosphorus Bridge, houses, trees), the ferry, the gull, clouds, sun, moon and quay, painted at start. |
| `Assets/Scripts/PixelCanvas.cs` | A small painter that draws shapes into pixels and makes a sprite, so there are still no image files in the project. |
| `Assets/Scripts/Effects.cs` | Camera shake and the sounds (drops, perfects, ferry horn, seagulls). All generated in code, so there are no audio files yet. |
| `Assets/Scripts/Flash.cs` | The white flash that grows and fades on a perfect drop. |
| `Assets/Scripts/GameUI.cs` | Score and prompts on screen. Temporary; replaced by a real UI in week 2. |

## Plan

The two-week plan lives in the Studio Blueprint doc (Tower Stacker tab). Short version: week 1 is making the loop feel good on a phone, week 2 is a real UI, sound, and the first build on an iPhone.
