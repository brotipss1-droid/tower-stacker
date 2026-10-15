using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Bosphorus around the tower: a sky that goes from morning to night as the
/// tower grows, the sun setting and the moon rising, stars, clouds, the Istanbul
/// skyline in three depths with its lights coming on at dusk and reflecting in the
/// water, the stone quay, a ferry with smoke and a wake, and seagulls. All of the
/// artwork is painted at start by <see cref="BosphorusArt"/>. Everything near the
/// tower is fixed in the world and scrolls away as the camera climbs; the sky, sun,
/// moon and stars follow the camera.
/// </summary>
public class BosphorusScenery : MonoBehaviour
{
    [Header("Tuning")]
    public float blocksToNight = 40f;    // how many blocks it takes to go from morning to night
    public float gullEvery = 6f;         // average seconds between seagulls
    public float ferrySpeed = 1.1f;      // units per second

    /// <summary>How much daylight there is right now: 1 at noon, about 0.45 at night.</summary>
    public static float Daylight { get; private set; } = 1f;
    public static Color LitWindow { get; private set; } = new Color(0.30f, 0.38f, 0.50f);
    public static Color DarkWindow { get; private set; } = new Color(0.30f, 0.38f, 0.50f);

    private StackManager _game;
    private Effects _fx;
    private Camera _cam;
    private float _day;              // 0 = morning, 1 = night; eased towards the score

    // sky
    private Transform _skyRoot;
    private SpriteRenderer _skyGradient;
    private SpriteRenderer _sun;
    private SpriteRenderer _moon;
    private readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();

    // city
    private SpriteRenderer _far;
    private SpriteRenderer _mid;
    private SpriteRenderer _near;
    private SpriteRenderer _lights;
    private SpriteRenderer _reflection;

    // sea
    private SpriteRenderer _water;
    private readonly List<SpriteRenderer> _glints = new List<SpriteRenderer>();
    private readonly List<float> _glintSpeeds = new List<float>();
    private readonly List<SpriteRenderer> _daylit = new List<SpriteRenderer>();   // painted things that just darken at night

    // ferry
    private Transform _ferry;
    private SpriteRenderer _ferryBody;
    private SpriteRenderer _ferryLights;
    private readonly List<SpriteRenderer> _wake = new List<SpriteRenderer>();
    private int _ferryDir = 1;
    private bool _hornBlown;
    private float _nextPuff;
    private readonly List<Puff> _smoke = new List<Puff>();

    // clouds and gulls
    private readonly List<SpriteRenderer> _clouds = new List<SpriteRenderer>();
    private readonly List<float> _cloudSpeeds = new List<float>();
    private float _nextGull;
    private readonly List<Gull> _gulls = new List<Gull>();
    private Sprite[] _gullFrames;

    private static Sprite _circle;
    private static Sprite _gradient;

    // Sky colours at morning, golden hour, sunset, dusk and night: horizon then zenith.
    private static readonly Color[] Horizon =
    {
        new Color(0.82f, 0.90f, 0.97f), new Color(0.99f, 0.80f, 0.50f), new Color(0.95f, 0.47f, 0.40f),
        new Color(0.42f, 0.26f, 0.46f), new Color(0.07f, 0.09f, 0.20f),
    };
    private static readonly Color[] Zenith =
    {
        new Color(0.36f, 0.62f, 0.92f), new Color(0.50f, 0.62f, 0.88f), new Color(0.36f, 0.27f, 0.52f),
        new Color(0.10f, 0.10f, 0.30f), new Color(0.02f, 0.03f, 0.08f),
    };

    private class Gull
    {
        public SpriteRenderer Renderer;
        public float Speed;
        public float Age;
        public float BaseY;
        public float Flap;
    }

    private class Puff
    {
        public SpriteRenderer Renderer;
        public float Age;
        public float Drift;
    }

    private void Start()
    {
        _game = GetComponent<StackManager>();
        _fx = GetComponent<Effects>();
        _cam = Camera.main;
        BuildSky();
        BuildSkyline();
        BuildSea();
        BuildFerry();
        BuildClouds();
        _gullFrames = new[] { BosphorusArt.Gull(0).ToSprite(), BosphorusArt.Gull(1).ToSprite(), BosphorusArt.Gull(2).ToSprite() };
        _nextGull = 2f;
    }

    private void Update()
    {
        float target = _game != null ? Mathf.Clamp01(_game.Score / Mathf.Max(1f, blocksToNight)) : 0f;
        _day = Mathf.MoveTowards(_day, target, Time.deltaTime * 0.25f);
        float night = Mathf.Clamp01((_day - 0.55f) / 0.45f);   // 0 by day, 1 at full night
        float dusk = Mathf.Clamp01((_day - 0.4f) / 0.4f);      // the lights come on a little before full night

        Daylight = Mathf.Lerp(1f, 0.45f, night);
        LitWindow = Color.Lerp(new Color(0.30f, 0.38f, 0.50f), new Color(1.0f, 0.85f, 0.45f), dusk);
        DarkWindow = Color.Lerp(new Color(0.30f, 0.38f, 0.50f), new Color(0.12f, 0.12f, 0.18f), night);
        Color lit = new Color(Daylight, Daylight, Daylight, 1f);

        // Sky: the camera clears to the horizon colour, the gradient sprite adds the zenith on top.
        Color horizon = Sample(Horizon, _day);
        Color zenith = Sample(Zenith, _day);
        _cam.backgroundColor = horizon;
        _skyGradient.color = zenith;
        var camPos = _cam.transform.position;
        _skyRoot.position = new Vector3(camPos.x, camPos.y, 0f);

        // Sun sets during the first part of the day, the moon rises during the last part.
        float sunT = Mathf.Clamp01(_day / 0.6f);
        _sun.transform.localPosition = new Vector3(3.5f - 6f * sunT, Mathf.Lerp(6f, -9f, sunT * sunT), 0f);
        _sun.color = Color.Lerp(new Color(1f, 0.97f, 0.80f), new Color(1f, 0.55f, 0.30f), sunT);
        float moonT = Mathf.Clamp01((_day - 0.6f) / 0.4f);
        _moon.transform.localPosition = new Vector3(-3f + 4f * moonT, Mathf.Lerp(-9f, 5f, Mathf.Sqrt(moonT)), 0f);
        _moon.color = new Color(0.95f, 0.95f, 0.88f, moonT);

        foreach (var s in _stars)
        {
            var c = s.color;
            c.a = night * (0.5f + 0.5f * Mathf.Sin(Time.time * 2f + s.transform.localPosition.x * 7f));
            s.color = c;
        }

        // The city in three depths: the far shore fades into the sky's haze, the near shore goes almost black at night.
        float dark = Mathf.Clamp01(_day * 1.3f);
        float sunset = Mathf.Clamp01(1f - Mathf.Abs(_day - 0.45f) * 5f) * 0.5f;
        Color farDay = Color.Lerp(new Color(0.62f, 0.70f, 0.84f), horizon, 0.3f);
        _far.color = Color.Lerp(farDay, new Color(0.11f, 0.13f, 0.24f), dark);
        Color midDay = Color.Lerp(new Color(0.46f, 0.53f, 0.68f), new Color(0.46f, 0.36f, 0.46f), sunset);
        _mid.color = Color.Lerp(midDay, new Color(0.07f, 0.08f, 0.16f), dark);
        Color nearDay = Color.Lerp(new Color(0.27f, 0.31f, 0.44f), new Color(0.28f, 0.20f, 0.28f), sunset);
        _near.color = Color.Lerp(nearDay, new Color(0.04f, 0.05f, 0.10f), dark);
        _lights.color = new Color(1f, 0.85f, 0.5f, dusk * 0.95f);
        _reflection.color = new Color(1f, 0.85f, 0.5f, dusk * 0.30f);
        _reflection.transform.localScale = new Vector3(1f + 0.012f * Mathf.Sin(Time.time * 1.7f), -0.45f, 1f);

        // The sea gets deep and dark, with glints drifting on it.
        _water.color = Color.Lerp(new Color(0.20f, 0.45f, 0.62f), new Color(0.04f, 0.08f, 0.16f), _day);
        for (int i = 0; i < _glints.Count; i++)
        {
            var p = _glints[i].transform.position;
            p.x += _glintSpeeds[i] * Time.deltaTime;
            if (p.x > 12f) p.x -= 24f;
            if (p.x < -12f) p.x += 24f;
            _glints[i].transform.position = p;
            _glints[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(0.22f, 0.12f, night));
        }
        foreach (var r in _daylit) r.color = lit;

        Color cloud = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(0.25f, 0.22f, 0.32f, 0.85f), night);
        cloud = Color.Lerp(cloud, new Color(1f, 0.75f, 0.70f, 0.95f), Mathf.Clamp01(1f - Mathf.Abs(_day - 0.45f) * 5f) * 0.6f);
        for (int i = 0; i < _clouds.Count; i++)
        {
            _clouds[i].color = cloud;
            var p = _clouds[i].transform.position;
            p.x += _cloudSpeeds[i] * Time.deltaTime;
            if (p.x > 16f) p.x = -16f;
            _clouds[i].transform.position = p;
        }

        UpdateFerry(lit, dusk);
        UpdateGulls(lit);
    }

    // ---------- sky ----------

    private void BuildSky()
    {
        _skyRoot = new GameObject("Sky").transform;

        _skyGradient = NewSprite("Gradient", _skyRoot, GradientSprite(), -100);
        _skyGradient.transform.localScale = new Vector3(60f, 30f, 1f);

        _sun = NewSprite("Sun", _skyRoot, BosphorusArt.Sun(1f).ToSprite(), -90);
        _sun.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        _moon = NewSprite("Moon", _skyRoot, BosphorusArt.Moon(0.7f).ToSprite(), -90);

        var rng = new System.Random(42);
        for (int i = 0; i < 45; i++)
        {
            var star = NewSprite("Star", _skyRoot, CircleSprite(), -95);
            float x = (float)rng.NextDouble() * 16f - 8f;
            float y = (float)rng.NextDouble() * 14f - 2f;
            star.transform.localPosition = new Vector3(x, y, 0f);
            float size = 0.06f + (float)rng.NextDouble() * 0.08f;
            star.transform.localScale = new Vector3(size, size, 1f);
            star.color = new Color(1f, 1f, 1f, 0f);
            _stars.Add(star);
        }
    }

    // ---------- skyline ----------

    private void BuildSkyline()
    {
        var root = new GameObject("Skyline").transform;

        // Paint the three depths first; the lights layer collects every window they placed.
        var far = BosphorusArt.FarLayer();
        var mid = BosphorusArt.MidLayer();
        var near = BosphorusArt.NearLayer();
        var lights = BosphorusArt.LightsLayer();

        _far = NewSprite("Far shore", root, far.ToSprite(), -63);
        _far.transform.position = far.Center;
        _mid = NewSprite("Middle shore", root, mid.ToSprite(), -62);
        _mid.transform.position = mid.Center;
        _near = NewSprite("Near shore", root, near.ToSprite(), -60);
        _near.transform.position = near.Center;
        _lights = NewSprite("City lights", root, lights.ToSprite(), -59);
        _lights.transform.position = lights.Center;

        // The same lights mirrored in the water below the sea line, squashed and faint.
        _reflection = NewSprite("Reflection", root, _lights.sprite, -48);
        float mirroredCenter = BosphorusArt.Sea - (lights.Center.y - BosphorusArt.Sea) * 0.45f;
        _reflection.transform.position = new Vector3(0f, mirroredCenter, 0f);
        _reflection.transform.localScale = new Vector3(1f, -0.45f, 1f);
    }

    // ---------- sea ----------

    private void BuildSea()
    {
        var root = new GameObject("Sea").transform;

        _water = NewSprite("Water", root, Block.SquareSprite(), -50);
        _water.transform.localPosition = new Vector3(0f, BosphorusArt.Sea - 15f, 0f);
        _water.transform.localScale = new Vector3(80f, 30f, 1f);

        // Glints: short bright streaks that drift across the water in two bands.
        var glintSprite = GlintSprite();
        for (int band = 0; band < 2; band++)
        {
            for (int i = 0; i < 3; i++)
            {
                var g = NewSprite("Glints", root, glintSprite, -49);
                float y = band == 0 ? BosphorusArt.Sea - 0.55f : BosphorusArt.Sea - 1.35f;
                g.transform.position = new Vector3(-8f + i * 8f, y, 0f);
                g.transform.localScale = band == 0 ? new Vector3(1f, 1f, 1f) : new Vector3(1.3f, 1.4f, 1f);
                _glints.Add(g);
                _glintSpeeds.Add(band == 0 ? 0.25f : -0.18f);
            }
        }

        // The stone quay the tower stands on.
        float quayWidth = _game != null ? _game.startWidth + 0.6f : 4.6f;
        var quay = BosphorusArt.Quay(quayWidth);
        var q = NewSprite("Quay", root, quay.ToSprite(), -20);
        q.transform.position = new Vector3(0f, BosphorusArt.Sea + quay.Center.y, 0f);
        _daylit.Add(q);
    }

    private void BuildFerry()
    {
        _ferry = new GameObject("Ferry").transform;
        _ferry.position = new Vector3(-16f, BosphorusArt.Sea - 0.02f, 0f);

        PixelCanvas lights;
        var body = BosphorusArt.Ferry(out lights);
        _ferryBody = NewSprite("Hull", _ferry, body.ToSprite(), -46);
        _ferryBody.transform.localPosition = body.Center;
        _ferryLights = NewSprite("Windows", _ferry, lights.ToSprite(), -45);
        _ferryLights.transform.localPosition = lights.Center;
        _daylit.Add(_ferryBody);

        // Foam at the stern and along the hull.
        for (int i = 0; i < 3; i++)
        {
            var w = NewSprite("Wake", _ferry, CircleSprite(), -47);
            w.transform.localPosition = new Vector3(-1.5f - i * 0.45f, -0.08f, 0f);
            w.transform.localScale = new Vector3(0.7f - i * 0.12f, 0.10f, 1f);
            w.color = new Color(1f, 1f, 1f, 0.55f - i * 0.15f);
            _wake.Add(w);
        }
    }

    private void UpdateFerry(Color lit, float dusk)
    {
        var p = _ferry.position;
        p.x += ferrySpeed * _ferryDir * Time.deltaTime;
        p.y = BosphorusArt.Sea - 0.02f + Mathf.Sin(Time.time * 1.3f) * 0.025f;
        if (Mathf.Abs(p.x) > 16f)
        {
            // Turn round for the next crossing, so the bow always leads.
            _ferryDir = -_ferryDir;
            p.x = -16f * _ferryDir;
            _hornBlown = false;
            _ferryBody.flipX = _ferryDir < 0;
            _ferryLights.flipX = _ferryDir < 0;
            foreach (var w in _wake)
            {
                var wp = w.transform.localPosition;
                wp.x = -Mathf.Abs(wp.x) * _ferryDir;
                w.transform.localPosition = wp;
            }
        }
        _ferry.position = p;
        _ferry.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.1f) * 1.2f);
        _ferryLights.color = new Color(1f, 0.85f, 0.5f, dusk);

        // One horn per crossing, only while the sea is still on screen.
        if (!_hornBlown && p.x * _ferryDir > -6f && _cam.transform.position.y < 12f)
        {
            _hornBlown = true;
            if (_fx != null) _fx.PlayHorn();
        }

        // Smoke from the funnel: puffs that rise, drift back and fade.
        _nextPuff -= Time.deltaTime;
        if (_nextPuff <= 0f)
        {
            _nextPuff = 0.45f;
            var r = NewSprite("Smoke", null, CircleSprite(), -44);
            r.transform.position = _ferry.position + new Vector3(0.02f * _ferryDir, 1.32f, 0f);
            _smoke.Add(new Puff { Renderer = r, Drift = -_ferryDir * 0.35f });
        }
        for (int i = _smoke.Count - 1; i >= 0; i--)
        {
            var s = _smoke[i];
            s.Age += Time.deltaTime;
            float t = s.Age / 2.2f;
            if (t >= 1f)
            {
                Destroy(s.Renderer.gameObject);
                _smoke.RemoveAt(i);
                continue;
            }
            var sp = s.Renderer.transform.position;
            sp.x += s.Drift * Time.deltaTime;
            sp.y += 0.45f * Time.deltaTime;
            s.Renderer.transform.position = sp;
            float size = 0.12f + 0.45f * t;
            s.Renderer.transform.localScale = new Vector3(size, size, 1f);
            s.Renderer.color = new Color(0.9f * lit.r, 0.9f * lit.g, 0.9f * lit.b, 0.45f * (1f - t));
        }
    }

    // ---------- clouds ----------

    private void BuildClouds()
    {
        var rng = new System.Random(11);
        var shapes = new[]
        {
            BosphorusArt.Cloud(rng, 2.8f, 1.0f).ToSprite(),
            BosphorusArt.Cloud(rng, 2.0f, 0.8f).ToSprite(),
            BosphorusArt.Cloud(rng, 3.6f, 1.1f).ToSprite(),
        };
        for (int i = 0; i < 10; i++)
        {
            var c = NewSprite("Cloud", null, shapes[i % shapes.Length], -80);
            float x = (float)rng.NextDouble() * 28f - 14f;
            float y = 7f + (float)rng.NextDouble() * 40f;
            c.transform.position = new Vector3(x, y, 0f);
            float size = 0.7f + (float)rng.NextDouble() * 0.6f;
            c.transform.localScale = new Vector3(size, size, 1f);
            _clouds.Add(c);
            _cloudSpeeds.Add(0.12f + 0.05f * (i % 3));
        }
    }

    // ---------- seagulls ----------

    private void UpdateGulls(Color lit)
    {
        _nextGull -= Time.deltaTime;
        if (_nextGull <= 0f)
        {
            _nextGull = gullEvery * Random.Range(0.6f, 1.5f);
            SpawnGull();
        }

        for (int i = _gulls.Count - 1; i >= 0; i--)
        {
            var g = _gulls[i];
            g.Age += Time.deltaTime;
            var p = g.Renderer.transform.position;
            p.x += g.Speed * Time.deltaTime;
            p.y = g.BaseY + Mathf.Sin(g.Age * 1.5f) * 0.3f;
            g.Renderer.transform.position = p;

            // Flap: wings up, level, down, level, with a glide now and then.
            float cycle = Mathf.Sin(g.Age * g.Flap);
            bool gliding = Mathf.Sin(g.Age * 0.7f) > 0.6f;
            int frame = gliding ? 1 : cycle > 0.33f ? 0 : cycle < -0.33f ? 2 : 1;
            g.Renderer.sprite = _gullFrames[frame];
            g.Renderer.color = lit;

            if (Mathf.Abs(p.x) > 12f)
            {
                Destroy(g.Renderer.gameObject);
                _gulls.RemoveAt(i);
            }
        }
    }

    private void SpawnGull()
    {
        float camY = _cam.transform.position.y;
        float dir = Random.value < 0.5f ? 1f : -1f;
        var r = NewSprite("Seagull", null, _gullFrames[1], -70);
        var g = new Gull
        {
            Renderer = r,
            Speed = dir * Random.Range(1.8f, 3f),
            BaseY = camY + Random.Range(-3f, 6f),
            Flap = Random.Range(7f, 10f),
        };
        r.transform.position = new Vector3(-11f * dir, g.BaseY, 0f);
        float scale = Random.Range(0.6f, 1f);
        r.transform.localScale = new Vector3(scale, scale, 1f);
        r.flipX = dir < 0f;
        _gulls.Add(g);

        if (_fx != null && Random.value < 0.35f) _fx.PlayGull();
    }

    // ---------- helpers ----------

    private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = order;
        return r;
    }

    private static Color Sample(Color[] keys, float t)
    {
        float pos = Mathf.Clamp(t * (keys.Length - 1), 0f, keys.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(pos), keys.Length - 1);
        int j = Mathf.Min(i + 1, keys.Length - 1);
        return Color.Lerp(keys[i], keys[j], pos - i);
    }

    /// <summary>A soft-edged white disc, one unit across.</summary>
    public static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _circle;
    }

    /// <summary>A one-unit square that is opaque at the top and transparent at the bottom.</summary>
    private static Sprite GradientSprite()
    {
        if (_gradient != null) return _gradient;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float a = Mathf.Pow((float)y / (size - 1), 0.8f);
            for (int x = 0; x < size; x++) pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _gradient = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _gradient;
    }

    /// <summary>Eight units of short white streaks, the way light catches small waves.</summary>
    private static Sprite GlintSprite()
    {
        var c = new PixelCanvas(8f, 0.8f, 32f, -4f, -0.4f);
        var rng = new System.Random(19);
        for (int i = 0; i < 70; i++)
        {
            float x = (float)rng.NextDouble() * 7.6f - 3.8f;
            float y = (float)rng.NextDouble() * 0.7f - 0.35f;
            float w = 0.12f + (float)rng.NextDouble() * 0.5f;
            c.Rect(x, y, w, 0.03f, new Color(1f, 1f, 1f, 0.6f + 0.4f * (float)rng.NextDouble()));
        }
        return c.ToSprite();
    }
}
