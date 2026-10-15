using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One block of the tower, drawn as a floor of a Bosphorus-side house: a coloured
/// facade with a shaded edge, a cornice with dentils along the top, and a row of
/// framed windows with sills that light up at night. A block is either sitting in
/// the tower, sliding across the screen waiting to be dropped, or a cut-off piece
/// falling away.
/// </summary>
public class Block : MonoBehaviour
{
    public float Width { get; private set; }
    public float Height { get; private set; }

    /// <summary>The block's daytime colour, before the time of day darkens it.</summary>
    public Color Color { get; private set; }

    private Transform _visual;            // everything drawn, squashed together on landing
    private SpriteRenderer _body;
    private SpriteRenderer _shade;        // the darker right edge
    private SpriteRenderer _cornice;
    private SpriteRenderer _corniceTop;   // the lit upper edge of the cornice
    private readonly List<SpriteRenderer> _dentils = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _frames = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _panes = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _bars = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _sills = new List<SpriteRenderer>();
    private readonly List<bool> _windowLit = new List<bool>();
    private bool _plain;          // true for blocks without trim and windows (the base)
    private bool _falling;
    private float _fallSpeed;
    private float _spin;
    private float _punch;         // seconds left of the landing squash

    private const float PunchDuration = 0.16f;
    private const float WindowSpacing = 0.5f;
    private const float FrameWidth = 0.28f;
    private const float FrameHeight = 0.40f;
    private const float PaneInset = 0.03f;
    private const float SillWidth = 0.34f;
    private const float SillHeight = 0.035f;
    private const float CorniceHeight = 0.09f;
    private const float CorniceEdge = 0.03f;
    private const float ShadeWidth = 0.07f;
    private const float WindowBottom = 0.12f;   // from the bottom of the block

    private static Sprite _square;

    public static Block Create(string name, float x, float y, float width, float height, Color color, bool plain = false)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0f);
        var block = go.AddComponent<Block>();
        block._plain = plain;
        block.Color = color;

        block._visual = new GameObject("Visual").transform;
        block._visual.SetParent(go.transform, false);
        block._body = block.Part("Body", 0);
        if (!plain)
        {
            block._shade = block.Part("Shade", 1);
            block._cornice = block.Part("Cornice", 1);
            block._corniceTop = block.Part("Cornice edge", 2);
        }

        block.SetSize(width, height);
        return block;
    }

    public void SetSize(float width, float height)
    {
        Width = width;
        Height = height;
        _body.transform.localScale = new Vector3(width, height, 1f);
        if (_plain) return;

        float top = height / 2f;
        _shade.transform.localPosition = new Vector3(width / 2f - ShadeWidth / 2f, 0f, 0f);
        _shade.transform.localScale = new Vector3(ShadeWidth, height, 1f);
        _cornice.transform.localPosition = new Vector3(0f, top - CorniceHeight / 2f, 0f);
        _cornice.transform.localScale = new Vector3(width, CorniceHeight, 1f);
        _corniceTop.transform.localPosition = new Vector3(0f, top - CorniceEdge / 2f, 0f);
        _corniceTop.transform.localScale = new Vector3(width, CorniceEdge, 1f);
        BuildDetails();
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
            _visual.localScale = new Vector3(1f + bump, 1f - bump, 1f);
            if (_punch <= 0f) _visual.localScale = new Vector3(1f, 1f, 1f);
        }

        // Daylight fades the colours; at night the windows glow instead.
        float light = BosphorusScenery.Daylight;
        Color body = Color * light;
        body.a = 1f;
        _body.color = body;
        if (!_plain)
        {
            Color dark = Tone(0.72f, light);
            Color darker = Tone(0.55f, light);
            Color pale = Tone(1.12f, light);
            _shade.color = dark;
            _cornice.color = dark;
            _corniceTop.color = pale;
            foreach (var d in _dentils) d.color = dark;
            foreach (var f in _frames) f.color = darker;
            foreach (var b in _bars) b.color = darker;
            foreach (var s in _sills) s.color = pale;
            for (int i = 0; i < _panes.Count; i++)
                _panes[i].color = _windowLit[i] ? BosphorusScenery.LitWindow : BosphorusScenery.DarkWindow;
        }

        if (!_falling) return;
        _fallSpeed += 30f * Time.deltaTime;
        transform.position += Vector3.down * _fallSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, _spin * Time.deltaTime);
    }

    /// <summary>The block colour scaled by a factor and the daylight, clamped to white.</summary>
    private Color Tone(float factor, float light)
    {
        float k = factor * light;
        return new Color(Mathf.Min(1f, Color.r * k), Mathf.Min(1f, Color.g * k), Mathf.Min(1f, Color.b * k), 1f);
    }

    /// <summary>Windows every half unit, centred on the block, and dentils under the cornice.</summary>
    private void BuildDetails()
    {
        foreach (var r in _frames) if (r != null) Destroy(r.gameObject);
        foreach (var r in _panes) if (r != null) Destroy(r.gameObject);
        foreach (var r in _bars) if (r != null) Destroy(r.gameObject);
        foreach (var r in _sills) if (r != null) Destroy(r.gameObject);
        foreach (var r in _dentils) if (r != null) Destroy(r.gameObject);
        _frames.Clear();
        _panes.Clear();
        _bars.Clear();
        _sills.Clear();
        _dentils.Clear();
        _windowLit.Clear();

        float bottom = -Height / 2f;
        float top = Height / 2f;

        int teeth = Mathf.FloorToInt(Width / WindowSpacing);
        float firstTooth = -(teeth - 1) * WindowSpacing / 2f;
        for (int i = 0; i < teeth; i++)
        {
            var d = Part("Dentil", 1);
            d.transform.localPosition = new Vector3(firstTooth + i * WindowSpacing, top - CorniceHeight - 0.02f, 0f);
            d.transform.localScale = new Vector3(0.08f, 0.04f, 1f);
            _dentils.Add(d);
        }

        int count = Mathf.FloorToInt((Width - 0.2f) / WindowSpacing);
        if (count < 1) return;
        float first = -(count - 1) * WindowSpacing / 2f;
        float frameY = bottom + WindowBottom + FrameHeight / 2f;
        float paneW = FrameWidth - PaneInset * 2f;
        float paneH = FrameHeight - PaneInset * 2f;

        for (int i = 0; i < count; i++)
        {
            float x = first + i * WindowSpacing;

            var frame = Part("Frame", 1);
            frame.transform.localPosition = new Vector3(x, frameY, 0f);
            frame.transform.localScale = new Vector3(FrameWidth, FrameHeight, 1f);
            _frames.Add(frame);

            var pane = Part("Pane", 2);
            pane.transform.localPosition = new Vector3(x, frameY, 0f);
            pane.transform.localScale = new Vector3(paneW, paneH, 1f);
            _panes.Add(pane);
            _windowLit.Add(Random.value < 0.8f);

            var barH = Part("Glazing bar", 3);
            barH.transform.localPosition = new Vector3(x, frameY, 0f);
            barH.transform.localScale = new Vector3(paneW, 0.02f, 1f);
            _bars.Add(barH);
            var barV = Part("Glazing bar", 3);
            barV.transform.localPosition = new Vector3(x, frameY, 0f);
            barV.transform.localScale = new Vector3(0.01f, paneH, 1f);
            _bars.Add(barV);

            var sill = Part("Sill", 2);
            sill.transform.localPosition = new Vector3(x, bottom + WindowBottom - SillHeight / 2f, 0f);
            sill.transform.localScale = new Vector3(SillWidth, SillHeight, 1f);
            _sills.Add(sill);
        }
    }

    private SpriteRenderer Part(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_visual, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = SquareSprite();
        r.sortingOrder = order;
        return r;
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
