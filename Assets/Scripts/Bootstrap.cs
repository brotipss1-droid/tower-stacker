using UnityEngine;

/// <summary>
/// Builds the whole game from code when the app starts, so the project needs no
/// hand-made scene or prefabs. Open any scene (even an empty one) and press Play.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindFirstObjectByType<StackManager>() != null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.18f);
        cam.transform.position = new Vector3(0f, 5f, -10f);

        var game = new GameObject("Game");
        game.AddComponent<StackManager>();
        game.AddComponent<GameUI>();
    }
}
