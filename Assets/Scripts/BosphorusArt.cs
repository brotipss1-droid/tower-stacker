using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The artwork of the Bosphorus, painted with <see cref="PixelCanvas"/>: the Istanbul
/// skyline in two depths with its night lights, the ferry, the seagull, clouds, the
/// sun and moon, and the stone quay. Silhouettes are painted white so the scenery can
/// tint them with the time of day; the ferry and gull carry their own colours.
/// </summary>
public static class BosphorusArt
{
    public const float Sea = -0.35f;          // the water line: pier top, base block bottom
    public const float X0 = -8f, X1 = 8f;     // skyline extent in world units
    public const float Y0 = -0.6f, Y1 = 5.2f;
    public const float Ppu = 48f;

    private static readonly Color W = Color.white;

    private struct Dot { public float X, Y, W, H; }
    private static List<Dot> _lights = new List<Dot>();

    private static void Light(float x, float y, float w = 0.05f, float h = 0.06f)
    {
        _lights.Add(new Dot { X = x, Y = y, W = w, H = h });
    }

    private static float Uniform(System.Random rng, float a, float b) => a + (b - a) * (float)rng.NextDouble();

    // ---------- landmarks ----------

    private static void Minaret(PixelCanvas c, float x, float y, float h, float w, int balconies)
    {
        c.Rect(x - w / 2f, y, w, h * 0.80f, W);
        for (int i = 0; i < balconies; i++)
        {
            float by = y + h * (0.48f + 0.16f * i);
            c.Rect(x - w * 1.1f, by, w * 2.2f, w * 0.7f, W);
            Light(x - w * 1.4f, by + w * 0.7f, 0.035f, 0.035f);
            Light(x + w * 1.4f, by + w * 0.7f, 0.035f, 0.035f);
        }
        c.Rect(x - w * 0.4f, y + h * 0.80f, w * 0.8f, h * 0.03f, W);
        c.Tri(x - w * 0.75f, y + h * 0.82f, x + w * 0.75f, y + h * 0.82f, x, y + h, W);
        c.Line(x, y + h, x, y + h * 1.05f, w * 0.25f, W);
    }

    /// <summary>The classic Ottoman pile of domes. s is half the width of the main block.</summary>
    private static void Mosque(PixelCanvas c, float x, float y, float s, int minarets, int balconies, float squash = 0.92f, bool cascade = true)
    {
        c.Rect(x - s, y, 2f * s, s * 0.42f, W);
        c.Rect(x - s * 0.78f, y, s * 1.56f, s * 0.58f, W);
        for (int side = -1; side <= 1; side += 2)
        {
            c.Dome(x + side * s * 0.78f, y + s * 0.42f, s * 0.16f, W);
            c.Dome(x + side * s * 0.48f, y + s * 0.58f, s * 0.30f, W, 0.9f);
            if (cascade) c.Dome(x + side * s * 0.62f, y + s * 0.50f, s * 0.20f, W);
        }
        c.Rect(x - s * 0.44f, y + s * 0.58f, s * 0.88f, s * 0.14f, W);
        c.Dome(x, y + s * 0.72f, s * 0.46f, W, squash);
        float top = y + s * 0.72f + s * 0.46f * squash;
        c.Line(x, top, x, top + s * 0.14f, 0.02f, W);
        Light(x, top + s * 0.14f, 0.035f, 0.035f);

        float[] positions = minarets == 6 ? new[] { -1.5f, -1.08f, -0.62f, 0.62f, 1.08f, 1.5f }
            : minarets == 4 ? new[] { -1.08f, -0.6f, 0.6f, 1.08f }
            : new[] { -1.02f, 1.02f };
        foreach (float p in positions)
        {
            bool outer = Mathf.Abs(p) > 1.2f;
            Minaret(c, x + p * s, y, s * (outer ? 1.2f : 1.45f), s * 0.06f, outer ? 2 : balconies);
        }
    }

    private static void Galata(PixelCanvas c, float x, float y, float k = 0.85f)
    {
        c.Poly(W, x - 0.26f * k, y, x + 0.26f * k, y, x + 0.22f * k, y + 2.05f * k, x - 0.22f * k, y + 2.05f * k);
        for (int j = 0; j < 3; j++)
            for (int i = 0; i < 3; i++)
                Light(x + (i - 1) * 0.12f * k, y + (0.6f + j * 0.45f) * k, 0.03f, 0.05f);
        c.Rect(x - 0.31f * k, y + 2.05f * k, 0.62f * k, 0.11f * k, W);
        for (int i = 0; i < 5; i++) Light(x + (i - 2) * 0.11f * k, y + 2.1f * k, 0.04f, 0.05f);
        c.Rect(x - 0.20f * k, y + 2.16f * k, 0.40f * k, 0.26f * k, W);
        c.Tri(x - 0.27f * k, y + 2.42f * k, x + 0.27f * k, y + 2.42f * k, x, y + 3.10f * k, W);
        c.Circle(x, y + 3.12f * k, 0.045f * k, W);
        c.Line(x, y + 3.12f * k, x, y + 3.30f * k, 0.02f, W);
        Light(x, y + 3.30f * k, 0.035f, 0.035f);
    }

    private static void MaidensTower(PixelCanvas c, float x)
    {
        float y = Sea;
        c.Poly(W, x - 0.85f, y - 0.1f, x + 0.85f, y - 0.1f, x + 0.72f, y + 0.12f, x - 0.72f, y + 0.12f);
        c.Rect(x - 0.48f, y + 0.10f, 0.96f, 0.34f, W);
        c.Poly(W, x - 0.50f, y + 0.44f, x + 0.50f, y + 0.44f, x + 0.40f, y + 0.56f, x - 0.40f, y + 0.56f);
        for (int i = 0; i < 4; i++) Light(x - 0.33f + i * 0.22f, y + 0.25f, 0.045f, 0.06f);
        c.Rect(x - 0.15f, y + 0.40f, 0.30f, 0.58f, W);
        c.Rect(x - 0.19f, y + 0.98f, 0.38f, 0.05f, W);
        c.Rect(x - 0.11f, y + 1.03f, 0.22f, 0.20f, W);
        Light(x, y + 1.13f, 0.06f, 0.06f);
        c.Tri(x - 0.17f, y + 1.23f, x + 0.17f, y + 1.23f, x, y + 1.55f, W);
        c.Line(x, y + 1.55f, x, y + 1.72f, 0.015f, W);
        c.Poly(W, x, y + 1.72f, x + 0.12f, y + 1.69f, x, y + 1.66f);
    }

    /// <summary>The Bosphorus Bridge: portal towers, a sagging main cable, zigzag hangers, lights along the cables.</summary>
    private static void Bridge(PixelCanvas c, float x1, float x2, float deck, float h)
    {
        float span = x2 - x1;
        float a1 = x1 - span * 0.36f, a2 = x2 + span * 0.36f;
        c.Rect(a1, deck - 0.04f, a2 - a1, 0.08f, W);
        c.Rect(a1, deck - 0.10f, a2 - a1, 0.03f, W);
        foreach (float tx in new[] { x1, x2 })
        {
            for (int side = -1; side <= 1; side += 2)
                c.Poly(W, tx + side * 0.18f, deck - 0.45f, tx + side * 0.09f, deck - 0.45f, tx + side * 0.06f, deck + h, tx + side * 0.14f, deck + h);
            foreach (float f in new[] { 0.35f, 0.68f, 0.98f })
                c.Rect(tx - 0.14f, deck + h * f - 0.03f, 0.28f, 0.06f, W);
            Light(tx, deck + h + 0.03f, 0.04f, 0.04f);
        }
        float top = deck + h;
        var main = Cable(c, x1, top, x2, top, top - deck - 0.12f, 32);
        Cable(c, a1, deck, x1, top, -(top - deck) * 0.12f, 16);
        Cable(c, x2, top, a2, deck, -(top - deck) * 0.12f, 16);
        const int n = 32;
        for (int i = 1; i < n; i++)
        {
            float t = (float)i / n;
            float qx = x1 + span * (t + (i % 2 == 1 ? 0.5f / n : -0.5f / n));
            c.Line(main[i].x, main[i].y, qx, deck + 0.04f, 0.016f, W);
        }
    }

    private static Vector2[] Cable(PixelCanvas c, float xa, float ya, float xb, float yb, float sag, int n)
    {
        var pts = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            pts[i] = new Vector2(xa + (xb - xa) * t, ya + (yb - ya) * t - sag * 4f * t * (1f - t));
        }
        for (int i = 0; i < n; i++) c.Line(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, 0.04f, W);
        for (int i = 0; i <= n; i += 2) Light(pts[i].x, pts[i].y + 0.02f, 0.025f, 0.025f);
        return pts;
    }

    private static void TvTower(PixelCanvas c, float x, float y)
    {
        c.Poly(W, x - 0.07f, y, x + 0.07f, y, x + 0.02f, y + 2.7f, x - 0.02f, y + 2.7f);
        c.Ellipse(x, y + 1.75f, 0.13f, 0.17f, W);
        c.Rect(x - 0.09f, y + 1.95f, 0.18f, 0.06f, W);
        Light(x, y + 2.72f, 0.04f, 0.04f);
    }

    // ---------- houses and trees ----------

    private static void WoodenHouse(PixelCanvas c, System.Random rng, float x, float y, float w, float h)
    {
        c.Rect(x, y, w, h, W);
        if (rng.NextDouble() < 0.5)
        {
            float bx = x + w * (rng.NextDouble() < 0.5 ? 0.1f : 0.55f);
            c.Rect(bx, y + h * 0.45f, w * 0.35f, h * 0.4f, W);
        }
        c.Tri(x - 0.02f, y + h, x + w + 0.02f, y + h, x + w / 2f, y + h + w * 0.55f, W);
        if (rng.NextDouble() < 0.6) c.Rect(x + w * 0.7f, y + h + w * 0.1f, w * 0.1f, w * 0.35f, W);
        int cols = Mathf.Max(1, (int)(w / 0.15f)), rows = Mathf.Max(1, (int)(h / 0.2f));
        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
                if (rng.NextDouble() < 0.35) Light(x + w * (i + 0.5f) / cols, y + h * (j + 0.5f) / rows, 0.04f, 0.055f);
    }

    private static void Apartment(PixelCanvas c, System.Random rng, float x, float y, float w, float h)
    {
        c.Rect(x, y, w, h, W);
        c.Rect(x + w * 0.1f, y + h, w * 0.8f, 0.03f, W);
        if (rng.NextDouble() < 0.5) c.Rect(x + w * 0.3f, y + h, w * 0.2f, 0.08f, W);
        int cols = Mathf.Max(1, (int)(w / 0.14f)), rows = Mathf.Max(1, (int)(h / 0.17f));
        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
                if (rng.NextDouble() < 0.35) Light(x + w * (i + 0.5f) / cols, y + h * (j + 0.5f) / rows, 0.045f, 0.05f);
    }

    private static void Cypress(PixelCanvas c, float x, float y, float h)
    {
        c.Poly(W, x - h * 0.09f, y, x + h * 0.09f, y, x + h * 0.11f, y + h * 0.35f, x + h * 0.02f, y + h, x - h * 0.02f, y + h, x - h * 0.11f, y + h * 0.35f);
    }

    private static void PlaneTree(PixelCanvas c, float x, float y, float r)
    {
        c.Rect(x - r * 0.12f, y, r * 0.24f, r * 1.1f, W);
        c.Circle(x, y + r * 1.3f, r, W);
        c.Circle(x - r * 0.6f, y + r * 1.0f, r * 0.7f, W);
        c.Circle(x + r * 0.6f, y + r * 1.05f, r * 0.75f, W);
    }

    private static void LampPosts(PixelCanvas c, float x0, float x1, float y, float step)
    {
        for (float x = x0; x <= x1; x += step)
        {
            c.Line(x, y, x, y + 0.22f, 0.015f, W);
            Light(x, y + 0.23f, 0.03f, 0.03f);
        }
    }

    private static void Town(PixelCanvas c, System.Random rng, float x0, float x1, float y, float hmin, float hmax, float modern)
    {
        float x = x0;
        while (x < x1)
        {
            float w = Uniform(rng, 0.26f, 0.55f);
            float h = Uniform(rng, hmin, hmax);
            if (rng.NextDouble() < modern) Apartment(c, rng, x, y, w, h);
            else WoodenHouse(c, rng, x, y, w, h);
            x += w * Uniform(rng, 0.75f, 1.05f);
        }
    }

    // ---------- the skyline layers ----------

    private static PixelCanvas NewLayer() => new PixelCanvas(X1 - X0, Y1 - Y0, Ppu, X0, Y0);

    /// <summary>Two soft hills behind the old city, open horizon behind the bridge, a low hill far right, and the bridge: the lightest layer.</summary>
    public static PixelCanvas FarLayer()
    {
        var c = NewLayer();
        c.Poly(W, X0, -0.6f, X0, 0.9f, -6.5f, 1.25f, -5.0f, 1.1f, -3.6f, 1.35f, -2.2f, 1.15f, -1.0f, 0.8f, 0.4f, 0.55f, 1.4f, 0.3f, 1.9f, -0.6f);
        c.Poly(W, 6.0f, -0.6f, 6.4f, 0.45f, 7.2f, 0.7f, X1, 0.6f, X1, -0.6f);
        Bridge(c, 2.75f, 4.95f, 1.15f, 1.75f);
        return c;
    }

    /// <summary>Galata on its hill, the Blue Mosque off at the edge, and a tidy Üsküdar waterfront, in a middle tone.</summary>
    public static PixelCanvas MidLayer()
    {
        var c = NewLayer();
        var rng = new System.Random(5);
        c.Poly(W, X0, -0.6f, X0, 0.55f, -5.5f, 0.6f, -3.0f, 0.5f, -2.2f, 0.42f, -1.5f, 0.3f, -1.0f, -0.6f);
        Mosque(c, -6.3f, 0.55f, 0.95f, 6, 3);                      // Blue Mosque, at the edge of the view
        Galata(c, -2.35f, 0.5f, 0.72f);                            // Galata, with sky around it
        Town(c, rng, -4.3f, -2.7f, 0.5f, 0.2f, 0.4f, 0.0f);
        c.Poly(W, 1.7f, -0.6f, 1.8f, 0.25f, 2.4f, 0.4f, 6.0f, 0.45f, X1, 0.45f, X1, -0.6f);
        Town(c, rng, 2.0f, 5.6f, 0.4f, 0.22f, 0.42f, 0.3f);        // Üsküdar, low under the bridge deck
        Mosque(c, 6.6f, 0.45f, 0.6f, 2, 2);
        Town(c, rng, 7.3f, 7.9f, 0.45f, 0.25f, 0.5f, 0.5f);
        return c;
    }

    /// <summary>The historic point with Hagia Sophia and its trees, and Maiden's Tower on the water: the darkest layer.</summary>
    public static PixelCanvas NearLayer()
    {
        var c = NewLayer();
        var rng = new System.Random(3);
        c.Poly(W, X0, -0.6f, X0, 0.25f, -4.0f, 0.28f, -2.6f, 0.2f, -2.1f, 0.05f, -2.0f, -0.6f);
        Mosque(c, -3.9f, 0.26f, 1.0f, 4, 2, 0.8f, false);          // Hagia Sophia, flatter and heavier
        Cypress(c, -2.75f, 0.12f, 0.5f);
        Cypress(c, -2.15f, 0.12f, 0.55f);
        PlaneTree(c, -2.5f, 0.18f, 0.15f);
        Town(c, rng, -7.9f, -5.2f, 0.25f, 0.3f, 0.55f, 0.2f);
        LampPosts(c, -7.6f, -2.3f, 0.26f, 0.6f);
        MaidensTower(c, 3.3f);
        c.Poly(W, 6.2f, -0.6f, 6.3f, 0.1f, 7.0f, 0.25f, X1, 0.28f, X1, -0.6f);
        Town(c, rng, 6.5f, 7.9f, 0.25f, 0.3f, 0.5f, 0.3f);
        return c;
    }

    /// <summary>Every window and lamp collected while painting the layers. Paint the layers first.</summary>
    public static PixelCanvas LightsLayer()
    {
        var c = new PixelCanvas(X1 - X0, Y1 - Y0, Ppu, X0, Y0);
        foreach (var d in _lights) c.Rect(d.X - d.W / 2f, d.Y - d.H / 2f, d.W, d.H, W);
        _lights = new List<Dot>();
        return c;
    }

    // ---------- things that move ----------

    /// <summary>A Şehir Hatları ferry, 2.8 units long, bow to the right, waterline at y = 0. Its lit windows go to a second canvas.</summary>
    public static PixelCanvas Ferry(out PixelCanvas lights)
    {
        var c = new PixelCanvas(3.2f, 1.6f, 96f, -1.6f, -0.2f);
        lights = new PixelCanvas(3.2f, 1.6f, 96f, -1.6f, -0.2f);
        Color hull = new Color(0.96f, 0.96f, 0.93f), band = new Color(0.10f, 0.16f, 0.30f), deck = new Color(0.80f, 0.78f, 0.72f);
        Color funnel = new Color(0.93f, 0.76f, 0.18f), soot = new Color(0.12f, 0.12f, 0.12f), rail = new Color(0.75f, 0.75f, 0.72f);
        Color window = new Color(0.25f, 0.33f, 0.45f), roof = new Color(0.85f, 0.84f, 0.80f), flag = new Color(0.85f, 0.12f, 0.12f);
        const float L = 2.8f;
        float x0 = -L / 2f;

        c.Poly(hull, x0 + 0.15f, -0.12f, x0 + L - 0.05f, -0.12f, x0 + L + 0.08f, 0.16f, x0 + L - 0.02f, 0.30f, x0 + 0.02f, 0.30f, x0 - 0.06f, 0.12f);
        c.Poly(band, x0 + 0.12f, -0.12f, x0 + L - 0.05f, -0.12f, x0 + L + 0.02f, 0.02f, x0 - 0.02f, 0.02f);
        c.Rect(x0 + 0.05f, 0.06f, L - 0.1f, 0.025f, band);
        c.Rect(x0 + 0.25f, 0.30f, L - 0.65f, 0.26f, hull);
        for (int i = 0; i < 9; i++)
        {
            float wx = x0 + 0.38f + i * 0.24f;
            c.Rect(wx, 0.36f, 0.14f, 0.14f, window);
            lights.Rect(wx, 0.36f, 0.14f, 0.14f, W);
        }
        c.Rect(x0 + 0.2f, 0.56f, L - 0.5f, 0.03f, deck);
        c.Rect(x0 + 0.4f, 0.59f, L - 1.0f, 0.04f, rail);
        for (int i = 0; i < 7; i++) c.Rect(x0 + 0.45f + i * 0.27f, 0.59f, 0.025f, 0.28f, rail);
        c.Rect(x0 + 0.33f, 0.86f, L - 0.85f, 0.05f, roof);
        c.Rect(x0 + 0.5f, 0.63f, 0.5f, 0.2f, hull);
        c.Rect(x0 + 0.56f, 0.68f, 0.38f, 0.10f, window);
        lights.Rect(x0 + 0.56f, 0.68f, 0.38f, 0.10f, W);
        c.Poly(funnel, x0 + 1.28f, 0.88f, x0 + 1.52f, 0.88f, x0 + 1.56f, 1.30f, x0 + 1.34f, 1.30f);
        c.Poly(soot, x0 + 1.335f, 1.24f, x0 + 1.555f, 1.24f, x0 + 1.56f, 1.32f, x0 + 1.34f, 1.32f);
        c.Ellipse(x0 + 1.95f, 0.95f, 0.17f, 0.05f, hull);
        c.Line(x0 + 2.35f, 0.59f, x0 + 2.40f, 1.35f, 0.025f, rail);
        c.Poly(flag, x0 + 2.40f, 1.35f, x0 + 2.58f, 1.30f, x0 + 2.40f, 1.25f);
        lights.Rect(x0 + 2.38f, 1.35f, 0.04f, 0.04f, W);
        return c;
    }

    /// <summary>One flap frame of a seagull flying to the right, body at the origin, 0.8 units across.</summary>
    public static PixelCanvas Gull(int frame)
    {
        var c = new PixelCanvas(0.9f, 0.5f, 160f, -0.45f, -0.25f, 3);
        Color body = new Color(0.97f, 0.97f, 0.97f), tip = new Color(0.35f, 0.37f, 0.40f), beak = new Color(0.95f, 0.65f, 0.15f);
        float lift = frame == 0 ? 0.22f : frame == 1 ? 0f : -0.18f;
        for (int side = -1; side <= 1; side += 2)
        {
            float sx = side * 0.06f, sy = 0.02f;
            float ex = side * 0.22f, ey = 0.03f + lift * 0.5f;
            float tx = side * 0.40f, ty = lift;
            c.Poly(body, sx, sy + 0.03f, ex, ey + 0.035f, tx, ty + 0.01f, tx - side * 0.02f, ty - 0.02f, ex, ey - 0.03f, sx, sy - 0.03f);
            c.Poly(tip, tx - side * 0.1f, ty + 0.02f, tx, ty + 0.01f, tx - side * 0.02f, ty - 0.02f, tx - side * 0.1f, ty - 0.02f);
        }
        c.Ellipse(0f, 0f, 0.12f, 0.045f, body);
        c.Circle(0.11f, 0.015f, 0.035f, body);
        c.Poly(beak, 0.14f, 0.02f, 0.20f, 0.005f, 0.14f, -0.01f);
        return c;
    }

    /// <summary>A fluffy cloud with a flat base, painted white so it can be tinted.</summary>
    public static PixelCanvas Cloud(System.Random rng, float w, float h)
    {
        var c = new PixelCanvas(w + 0.4f, h + 0.3f, 64f, -(w + 0.4f) / 2f, -0.1f);
        Color shade = new Color(0.86f, 0.88f, 0.93f);
        int n = (int)(w / 0.35f) + 2;
        var px = new float[n]; var py = new float[n]; var pr = new float[n];
        for (int i = 0; i < n; i++)
        {
            px[i] = -w / 2f + w * (i + 0.5f) / n;
            pr[i] = h * Uniform(rng, 0.32f, 0.5f) * (1f - Mathf.Abs(px[i] / (w / 2f)) * 0.35f);
            py[i] = pr[i] * 0.7f;
        }
        for (int i = 0; i < n; i++) c.Circle(px[i], py[i] - 0.03f, pr[i], shade);
        c.Rect(-w / 2f + 0.1f, 0f, w - 0.2f, 0.06f, shade);
        for (int i = 0; i < n; i++) c.Circle(px[i], py[i] + 0.03f, pr[i] * 0.93f, W);
        return c;
    }

    /// <summary>A disc with a soft halo, three units across in all.</summary>
    public static PixelCanvas Sun(float r)
    {
        var c = new PixelCanvas(6f, 6f, 32f, -3f, -3f);
        for (int i = 24; i > 0; i--)
        {
            float k = (float)i / 24f;
            c.Circle(0f, 0f, r * (1f + 2.2f * k), new Color(1f, 1f, 1f, 0.05f * Mathf.Pow(1f - k, 1.5f)));
        }
        c.Circle(0f, 0f, r, W);
        return c;
    }

    /// <summary>A crescent with a faint glow.</summary>
    public static PixelCanvas Moon(float r)
    {
        var c = new PixelCanvas(4f, 4f, 32f, -2f, -2f);
        for (int i = 16; i > 0; i--)
        {
            float k = (float)i / 16f;
            c.Circle(0f, 0f, r * (1f + 1.6f * k), new Color(1f, 1f, 1f, 0.04f * Mathf.Pow(1f - k, 1.5f)));
        }
        c.Circle(0f, 0f, r, W);
        c.Circle(r * 0.45f, r * 0.15f, r * 0.82f, new Color(0f, 0f, 0f, 0f));   // transparent paint erases: the crescent
        return c;
    }

    /// <summary>The stone quay the tower stands on: coursed blocks, a wet band at the water, bollards.</summary>
    public static PixelCanvas Quay(float width)
    {
        const float h = 0.6f;
        var c = new PixelCanvas(width + 0.2f, h + 0.3f, 64f, -(width + 0.2f) / 2f, -h);
        Color stone = new Color(0.56f, 0.53f, 0.49f), joint = new Color(0.42f, 0.40f, 0.37f), wet = new Color(0.34f, 0.33f, 0.31f), cap = new Color(0.66f, 0.63f, 0.59f);
        c.Rect(-width / 2f, -h, width, h, stone);
        c.Rect(-width / 2f, -h, width, 0.14f, wet);
        for (int row = 0; row < 4; row++)
        {
            float y = -h + 0.14f + row * 0.115f;
            c.Rect(-width / 2f, y, width, 0.012f, joint);
            float offset = row % 2 == 0 ? 0f : 0.2f;
            for (float x = -width / 2f + offset; x < width / 2f; x += 0.4f) c.Rect(x, y, 0.012f, 0.115f, joint);
        }
        c.Rect(-width / 2f - 0.05f, -0.05f, width + 0.1f, 0.05f, cap);
        for (int side = -1; side <= 1; side += 2)
        {
            float bx = side * (width / 2f - 0.25f);
            c.Rect(bx - 0.05f, 0f, 0.10f, 0.16f, joint);
            c.Ellipse(bx, 0.17f, 0.065f, 0.04f, joint);
        }
        return c;
    }
}
