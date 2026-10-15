using UnityEngine;

/// <summary>
/// Camera shake and sound. The sounds are short tones generated in code, so the
/// prototype needs no audio files; real sounds replace them once the theme is chosen.
/// </summary>
public class Effects : MonoBehaviour
{
    /// <summary>Offset the camera should add to its position this frame.</summary>
    public Vector2 ShakeOffset { get; private set; }

    private float _shake;
    private AudioSource _thuds;
    private AudioSource _tones;
    private AudioClip _place;
    private AudioClip _cut;
    private AudioClip _perfect;
    private AudioClip _gameOver;

    private void Awake()
    {
        _thuds = gameObject.AddComponent<AudioSource>();
        _thuds.playOnAwake = false;
        _tones = gameObject.AddComponent<AudioSource>();
        _tones.playOnAwake = false;

        _place = Tone("place", 0.09f, 170f, 70f, 0.35f);
        _cut = Tone("cut", 0.12f, 900f, 250f, 0.75f);
        _perfect = Tone("perfect", 0.28f, 523f, 523f, 0f);
        _gameOver = Tone("game over", 0.7f, 330f, 90f, 0.15f);
    }

    private void Update()
    {
        if (_shake > 0.002f)
        {
            ShakeOffset = Random.insideUnitCircle * _shake;
            _shake = Mathf.Lerp(_shake, 0f, Time.deltaTime * 12f);
        }
        else
        {
            _shake = 0f;
            ShakeOffset = Vector2.zero;
        }
    }

    /// <summary>Shake the camera; strength is in world units (0.1 is subtle, 0.5 is a crash).</summary>
    public void Shake(float strength)
    {
        _shake = Mathf.Max(_shake, strength);
    }

    public void PlayPlace()
    {
        _thuds.pitch = Random.Range(0.92f, 1.08f);
        _thuds.PlayOneShot(_place, 0.7f);
    }

    public void PlayCut()
    {
        _thuds.pitch = Random.Range(0.9f, 1.1f);
        _thuds.PlayOneShot(_cut, 0.45f);
    }

    /// <summary>Each perfect in a row plays one semitone higher, up to an octave.</summary>
    public void PlayPerfect(int combo)
    {
        int steps = Mathf.Clamp(combo - 1, 0, 12);
        _tones.pitch = Mathf.Pow(1.05946f, steps);
        _tones.PlayOneShot(_perfect, 0.6f);
    }

    public void PlayGameOver()
    {
        _tones.pitch = 1f;
        _tones.PlayOneShot(_gameOver, 0.8f);
    }

    /// <summary>
    /// Builds a short clip: a sine wave sliding from startHz to endHz, mixed with
    /// white noise (0 = pure tone, 1 = pure noise), fading out over its length.
    /// </summary>
    private static AudioClip Tone(string name, float seconds, float startHz, float endHz, float noise)
    {
        const int rate = 44100;
        int count = Mathf.CeilToInt(seconds * rate);
        var data = new float[count];
        var rng = new System.Random(7);
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += 2f * Mathf.PI * hz / rate;

            float wave = Mathf.Sin(phase) * (1f - noise);
            float hiss = ((float)rng.NextDouble() * 2f - 1f) * noise;
            float fadeOut = (1f - t) * (1f - t);
            float fadeIn = Mathf.Min(1f, i / (0.004f * rate));
            data[i] = (wave + hiss) * fadeOut * fadeIn * 0.8f;
        }

        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
