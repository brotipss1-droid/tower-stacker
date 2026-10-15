using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One block of the tower, drawn as a floor of a Bosphorus-side house: a coloured
/// body, a darker trim along the top and a row of windows that light up at night.
/// A block is either sitting in the tower, sliding across the screen waiting to be
/// dropped, or a cut-off piece falling away.
/// </summary>
public class Block : MonoBehaviour
{
    public float Width { get; private set; }
    public float Height { get; private set; }

    /// <summary>The block's daytime colour, before the time of day darkens it.</summary>
    public Color Color { get; private set; }

    private Transform _body;
    private SpriteRenderer _renderer;
    private SpriteRenderer _trim;
    private readonly List<SpriteRenderer> _windows = new List<SpriteRenderer>();
    private readonly List<bool> _windowLit = new List<bool>();
    private bool _plain;          // true for blocks without trim and windows (the base)
    private bool _falling;
    private float _fallSpeed;
    private float _spin;
    private float _punch;         // seconds left of the landing squash

    private const float PunchDuration = 0.16f;
    private const float WindowWidth = 0.22f;
    private const float WindowHeight = 0.32f;
    private const float WindowSpacing = 0.5f;
    private const float TrimHeight = 0.08f;

    private static Sprite _square;

    public static Block Create(string name, float x, float y, float width, float height, Color color, bool plain = false)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0f);
        var block = go.AddComponent<Block>();
        block._plain = plain;
        block.Color = color;

        var body = new GameObject("Body");
        body.transform.SetParent(go.transform, false);
        block._body = body.transform;
        block._renderer = body.AddComponent<SpriteRenderer>();
        block._renderer.sprite = SquareSprite();
        block._renderer.color = color;

        if (!plain)
        {
            var trim = new GameObject("Trim");
            trim.transform.SetParent(go.transform, false);
            block._trim = trim.AddComponent<SpriteRenderer>();
            block._trim.sprite = SquareSprite();
            block._trim.sortingOrder = 1;
        }

        block.SetSize(width, height);
        return block;
    }

    public void SetSize(float width, float height)
    {
        Width = width;
        Height = height;
        _body.localScale = new Vector3(width, height, 1f);
        if (_plain) return;

        _trim.transform.localPosition = new Vector3(0f, height / 2f - TrimHeight / 2f, 0f);
        _trim.transform.localScale = new Vector3(width, TrimHeight, 1f);
        BuildWindows();
    }

    public void SetX(float x)
    {
        var p = transform.position;
        p.x = x;
        transform.position = p;
    }

    /// <summary>Squash and stretch for a moment, as if the block just landed.</summary>
    public void Punch()
    {
        _punch = PunchDuration;
    }

    /// <summary>Turn this block into a piece that drops off the screen and disappears.</summary>
    public void Fall(float sideways)
    {
        _falling = true;
        _fallSpeed = 0f;
        _spin = sideways * 120f;
        Destroy(gameObject, 2.5f);
    }

    private void Update()
    {
        if (_punch > 0f)
        {
            _punch -= Time.deltaTime;
            float t = Mathf.Clamp01(1f - _punch / PunchDuration);
            float bump = Mathf.Sin(t * Mathf.PI) * 0.12f;
            _body.localScale = new Vector3(Width * (1f + bump), Height * (1f - bump), 1f);
            if (_punch <= 0f) _body.localScale = new Vector3(Width, Height, 1f);
        }

        // Daylight fades the colours; at night the windows glow instead.
        float light = BosphorusScenery.Daylight;
        _renderer.color = Color * light;
        if (!_plain)
        {
            _trim.color = Color * (0.72f * light);
            for (int i = 0; i < _windows.Count; i++)
                _windows[i].color = _windowLit[i] ? BosphorusScenery.LitWindow : BosphorusScenery.DarkWindow;
        }

        if (!_falling) return;
        _fallSpeed += 30f * Time.deltaTime;
        transform.position += Vector3.down * _fallSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, _spin * Time.deltaTime);
    }

    /// <summary>One window every half unit, centred on the block, below the trim.</summary>
    private void BuildWindows()
    {
        foreach (var w in _windows) if (w != null) Destroy(w.gameObject);
        _windows.Clear();
        _windowLit.Clear();

        int count = Mathf.FloorToInt((Width - 0.2f) / WindowSpacing);
        if (count < 1) return;
        float first = -(count - 1) * WindowSpacing / 2f;
        float y = -TrimHeight / 2f;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Window");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(first + i * WindowSpacing, y, 0f);
            go.transform.localScale = new Vector3(WindowWidth, WindowHeight, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = SquareSprite();
            r.sortingOrder = 1;
            _windows.Add(r);
            _windowLit.Add(Random.value < 0.8f);
        }
    }

    /// <summary>A 1x1 white sprite made in code, so the project needs no image files yet.</summary>
    public static Sprite SquareSprite()
    {
        if (_square != null) return _square;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _square;
    }
}
