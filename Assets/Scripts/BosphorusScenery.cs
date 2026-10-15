using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Bosphorus around the tower, all drawn from code: a sky that goes from
/// morning to night as the tower grows, the sun setting and the moon rising,
/// stars, clouds, the Istanbul skyline, the sea with a ferry, and seagulls.
/// Everything near the tower is fixed in the world and scrolls away as the
/// camera climbs; the sky, sun, moon and stars follow the camera.
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

    private Transform _skyRoot;
    private SpriteRenderer _skyGradient;
    private SpriteRenderer _sun;
    private SpriteRenderer _moon;
    private readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _silhouettes = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _seaParts = new List<SpriteRenderer>();
    private readonly List<Transform> _waves = new List<Transform>();
    private readonly List<SpriteRenderer> _clouds = new List<SpriteRenderer>();
    private readonly List<Transform> _cloudRoots = new List<Transform>();
    private Transform _ferry;
    private bool _hornBlown;
    private float _nextGull;
    private readonly List<Gull> _gulls = new List<Gull>();

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
        public Transform Root;
        public Transform LeftWing;
        public Transform RightWing;
        public float Speed;
        public float Age;
        public float BaseY;
    }

    private void Start()
    {
        _game = GetComponent<StackManager>();
        _fx = GetComponent<Effects>();
        _cam = Camera.main;
        BuildSky();
        BuildSkyline();
        BuildSea();
        BuildClouds();
        _nextGull = 2f;
    }

    private void Update()
    {
        float target = _game != null ? Mathf.Clamp01(_game.Score / Mathf.Max(1f, blocksToNight)) : 0f;
        _day = Mathf.MoveTowards(_day, target, Time.deltaTime * 0.25f);
        float night = Mathf.Clamp01((_day - 0.55f) / 0.45f);   // 0 by day, 1 at full night

        Daylight = Mathf.Lerp(1f, 0.45f, night);
        LitWindow = Color.Lerp(new Color(0.30f, 0.38f, 0.50f), new Color(1.0f, 0.85f, 0.45f), night);
        DarkWindow = Color.Lerp(new Color(0.30f, 0.38f, 0.50f), new Color(0.12f, 0.12f, 0.18f), night);

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

        // The city darkens into a silhouette, the sea gets deep and dark.
        Color sil = Color.Lerp(new Color(0.52f, 0.60f, 0.72f), new Color(0.05f, 0.06f, 0.12f), Mathf.Clamp01(_day * 1.3f));
        foreach (var r in _silhouettes) r.color = sil;
        Color sea = Color.Lerp(new Color(0.20f, 0.45f, 0.62f), new Color(0.04f, 0.08f, 0.16f), _day);
        foreach (var r in _seaParts) r.color = sea;
        for (int i = 0; i < _waves.Count; i++)
        {
            var p = _waves[i].localPosition;
            p.x = Mathf.Sin(Time.time * 0.4f + i * 1.7f) * 1.5f;
            _waves[i].localPosition = p;
        }

        Color cloud = Color.Lerp(new Color(1f, 1f, 1f, 0.9f), new Color(0.25f, 0.22f, 0.32f, 0.8f), night);
        cloud = Color.Lerp(cloud, new Color(1f, 0.75f, 0.70f, 0.9f), Mathf.Clamp01(1f - Mathf.Abs(_day - 0.45f) * 5f) * 0.6f);
        foreach (var r in _clouds) r.color = cloud;
        for (int i = 0; i < _cloudRoots.Count; i++)
        {
            var p = _cloudRoots[i].position;
            p.x += (0.15f + 0.05f * (i % 3)) * Time.deltaTime;
            if (p.x > 16f) p.x = -16f;
            _cloudRoots[i].position = p;
        }

        UpdateFerry();
        UpdateGulls();
    }

    // ---------- sky ----------

    private void BuildSky()
    {
        _skyRoot = new GameObject("Sky").transform;

        _skyGradient = NewSprite("Gradient", _skyRoot, GradientSprite(), -100);
        _skyGradient.transform.localScale = new Vector3(60f, 30f, 1f);

        _sun = NewSprite("Sun", _skyRoot, CircleSprite(), -90);
        _sun.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        _moon = NewSprite("Moon", _skyRoot, CircleSprite(), -90);
        _moon.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

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

        // The far shore on both sides, low and wide.
        Box(root, -14f, -0.5f, 14f, 1.4f);
        Box(root, 14f, -0.5f, 14f, 1.1f);

        // A mosque on the left: base, dome, two minarets with their balconies.
        Mosque(root, -8.5f, 0f, 3.2f);
        // A smaller one far right.
        Mosque(root, 10.5f, 0f, 2.2f);

        // Galata Tower: a tall cylinder, a wider gallery and a cone-ish cap.
        Box(root, -4.2f, 1.0f, 0.9f, 3.6f);
        Box(root, -4.2f, 2.9f, 1.2f, 0.35f);
        Dot(root, -4.2f, 3.3f, 0.8f);
        Box(root, -4.2f, 3.8f, 0.3f, 0.8f);

        // Houses climbing the hill between the landmarks.
        var rng = new System.Random(3);
        for (float x = -7f; x < -4.8f; x += 0.55f) Box(root, x, 0.1f, 0.5f, 0.6f + (float)rng.NextDouble() * 0.7f);
        for (float x = -3.3f; x < 9f; x += 0.6f) Box(root, x, 0.1f, 0.55f, 0.5f + (float)rng.NextDouble() * 0.9f);

        // Maiden's Tower on its islet, out in the water to the right.
        Box(root, 7.5f, -0.2f, 1.6f, 0.35f);
        Box(root, 7.5f, 0.3f, 0.7f, 0.8f);
        Box(root, 7.5f, 0.85f, 0.35f, 0.5f);
        Dot(root, 7.5f, 1.15f, 0.3f);

        // The bridge in the distance behind everything: deck, two towers, hangers.
        Box(root, 3f, 1.6f, 9f, 0.12f);
        Box(root, 0.5f, 1.8f, 0.25f, 2.2f);
        Box(root, 5.5f, 1.8f, 0.25f, 2.2f);
        for (float x = 1.2f; x < 5.2f; x += 0.5f)
        {
            float h = 0.3f + Mathf.Abs(x - 3f) * 0.45f;
            Box(root, x, 1.65f + h / 2f, 0.04f, h);
        }
    }

    private void Mosque(Transform root, float x, float y, float size)
    {
        Box(root, x, y + size * 0.25f, size, size * 0.5f);
        Dot(root, x, y + size * 0.5f, size * 0.8f);
        Box(root, x, y + size * 0.95f, size * 0.07f, size * 0.25f);
        for (int side = -1; side <= 1; side += 2)
        {
            float mx = x + side * size * 0.62f;
            Box(root, mx, y + size * 0.75f, size * 0.10f, size * 1.5f);
            Box(root, mx, y + size * 1.1f, size * 0.18f, size * 0.06f);
            Box(root, mx, y + size * 1.55f, size * 0.06f, size * 0.2f);
        }
    }

    // ---------- sea ----------

    private void BuildSea()
    {
        var root = new GameObject("Sea").transform;

        var water = NewSprite("Water", root, Block.SquareSprite(), -50);
        water.transform.localPosition = new Vector3(0f, -15.35f, 0f);
        water.transform.localScale = new Vector3(80f, 30f, 1f);
        _seaParts.Add(water);

        for (int i = 0; i < 6; i++)
        {
            var wave = NewSprite("Wave", root, Block.SquareSprite(), -49);
            wave.transform.localPosition = new Vector3(0f, -0.6f - i * 0.5f, 0f);
            wave.transform.localScale = new Vector3(3f + i * 2f, 0.05f, 1f);
            wave.color = new Color(1f, 1f, 1f, 0.18f);
            _waves.Add(wave.transform);
        }

        // The stone pier the tower stands on, with two legs into the water.
        float pierWidth = _game != null ? _game.startWidth + 0.6f : 4.6f;
        var pier = NewSprite("Pier", root, Block.SquareSprite(), -20);
        pier.transform.localPosition = new Vector3(0f, -0.65f, 0f);
        pier.transform.localScale = new Vector3(pierWidth, 0.6f, 1f);
        pier.color = new Color(0.42f, 0.40f, 0.38f);
        for (int side = -1; side <= 1; side += 2)
        {
            var leg = NewSprite("Pier leg", root, Block.SquareSprite(), -45);
            leg.transform.localPosition = new Vector3(side * (pierWidth / 2f - 0.4f), -1.6f, 0f);
            leg.transform.localScale = new Vector3(0.35f, 1.6f, 1f);
            leg.color = new Color(0.30f, 0.28f, 0.27f);
        }

        // The ferry: hull, cabin, chimney.
        _ferry = new GameObject("Ferry").transform;
        _ferry.SetParent(root, false);
        _ferry.position = new Vector3(-14f, -0.75f, 0f);
        var hull = NewSprite("Hull", _ferry, Block.SquareSprite(), -46);
        hull.transform.localScale = new Vector3(2.4f, 0.35f, 1f);
        hull.color = new Color(0.95f, 0.95f, 0.92f);
        var stripe = NewSprite("Stripe", _ferry, Block.SquareSprite(), -45);
        stripe.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        stripe.transform.localScale = new Vector3(2.4f, 0.08f, 1f);
        stripe.color = new Color(0.15f, 0.25f, 0.45f);
        var cabin = NewSprite("Cabin", _ferry, Block.SquareSprite(), -45);
        cabin.transform.localPosition = new Vector3(0.1f, 0.33f, 0f);
        cabin.transform.localScale = new Vector3(1.6f, 0.32f, 1f);
        cabin.color = new Color(0.95f, 0.95f, 0.92f);
        var chimney = NewSprite("Chimney", _ferry, Block.SquareSprite(), -44);
        chimney.transform.localPosition = new Vector3(-0.3f, 0.65f, 0f);
        chimney.transform.localScale = new Vector3(0.18f, 0.35f, 1f);
        chimney.color = new Color(0.85f, 0.70f, 0.20f);
    }

    private void UpdateFerry()
    {
        var p = _ferry.position;
        p.x += ferrySpeed * Time.deltaTime;
        p.y = -0.75f + Mathf.Sin(Time.time * 1.3f) * 0.03f;
        if (p.x > 16f)
        {
            p.x = -16f;
            _hornBlown = false;
        }
        _ferry.position = p;

        // One horn per crossing, only while the sea is still on screen.
        if (!_hornBlown && p.x > -6f && _cam.transform.position.y < 12f)
        {
            _hornBlown = true;
            if (_fx != null) _fx.PlayHorn();
        }
    }

    // ---------- clouds ----------

    private void BuildClouds()
    {
        var rng = new System.Random(11);
        for (int i = 0; i < 10; i++)
        {
            var root = new GameObject("Cloud").transform;
            float x = (float)rng.NextDouble() * 28f - 14f;
            float y = 7f + (float)rng.NextDouble() * 40f;
            root.position = new Vector3(x, y, 0f);
            float size = 0.8f + (float)rng.NextDouble() * 0.8f;
            for (int k = -1; k <= 1; k++)
            {
                var puff = NewSprite("Puff", root, CircleSprite(), -80);
                float s = size * (k == 0 ? 1.3f : 1f);
                puff.transform.localPosition = new Vector3(k * size * 0.8f, k == 0 ? size * 0.15f : 0f, 0f);
                puff.transform.localScale = new Vector3(s, s * 0.7f, 1f);
                _clouds.Add(puff);
            }
            _cloudRoots.Add(root);
        }
    }

    // ---------- seagulls ----------

    private void UpdateGulls()
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
            var p = g.Root.position;
            p.x += g.Speed * Time.deltaTime;
            p.y = g.BaseY + Mathf.Sin(g.Age * 1.5f) * 0.3f;
            g.Root.position = p;
            float flap = Mathf.Sin(g.Age * 9f) * 25f;
            g.LeftWing.localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
            g.RightWing.localRotation = Quaternion.Euler(0f, 0f, -20f - flap);
            if (Mathf.Abs(p.x) > 12f)
            {
                Destroy(g.Root.gameObject);
                _gulls.RemoveAt(i);
            }
        }
    }

    private void SpawnGull()
    {
        float camY = _cam.transform.position.y;
        float dir = Random.value < 0.5f ? 1f : -1f;
        var g = new Gull
        {
            Root = new GameObject("Seagull").transform,
            Speed = dir * Random.Range(1.8f, 3f),
            BaseY = camY + Random.Range(-3f, 6f),
        };
        g.Root.position = new Vector3(-11f * dir, g.BaseY, 0f);
        float scale = Random.Range(0.6f, 1f);
        g.Root.localScale = new Vector3(scale, scale, 1f);

        g.LeftWing = Wing(g.Root, -0.14f);
        g.RightWing = Wing(g.Root, 0.14f);
        _gulls.Add(g);

        if (_fx != null && Random.value < 0.35f) _fx.PlayGull();
    }

    private static Transform Wing(Transform root, float x)
    {
        var pivot = new GameObject("Wing").transform;
        pivot.SetParent(root, false);
        pivot.localPosition = new Vector3(x, 0f, 0f);
        var r = NewSprite("Feather", pivot, Block.SquareSprite(), -70);
        r.transform.localPosition = new Vector3(x, 0f, 0f);
        r.transform.localScale = new Vector3(0.3f, 0.05f, 1f);
        r.color = new Color(0.95f, 0.95f, 0.95f);
        return pivot;
    }

    // ---------- helpers ----------

    private void Box(Transform root, float x, float y, float w, float h)
    {
        var r = NewSprite("Shape", root, Block.SquareSprite(), -60);
        r.transform.localPosition = new Vector3(x, y, 0f);
        r.transform.localScale = new Vector3(w, h, 1f);
        _silhouettes.Add(r);
    }

    private void Dot(Transform root, float x, float y, float diameter)
    {
        var r = NewSprite("Dome", root, CircleSprite(), -60);
        r.transform.localPosition = new Vector3(x, y, 0f);
        r.transform.localScale = new Vector3(diameter, diameter, 1f);
        _silhouettes.Add(r);
    }

    private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
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
    private static Sprite CircleSprite()
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
}
