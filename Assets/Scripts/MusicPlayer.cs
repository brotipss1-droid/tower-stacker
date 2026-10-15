using UnityEngine;

/// <summary>
/// Plays the background music loop from the moment the game starts.
/// The clip lives in Assets/Resources/Music so it can be loaded by name
/// without a scene or prefab, matching how the rest of the game is built.
///
/// Track: "Contemplation" by Joth, CC0 (public domain, no credit required).
/// https://opengameart.org/node/85636
/// </summary>
public static class MusicPlayer
{
    private const string ClipPath = "Music/Contemplation";
    private const float Volume = 0.35f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindFirstObjectByType<MusicMarker>() != null) return;

        var clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogWarning($"MusicPlayer: no clip at Resources/{ClipPath}");
            return;
        }

        var go = new GameObject("Music");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<MusicMarker>();

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = Volume;
        source.playOnAwake = false;
        source.Play();
    }
}

/// <summary>Marks the music object so it is only created once.</summary>
public class MusicMarker : MonoBehaviour { }
