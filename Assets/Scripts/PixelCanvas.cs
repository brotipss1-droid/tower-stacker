using UnityEngine;

/// <summary>
/// A small painter that draws shapes into a pixel buffer and turns it into a sprite,
/// so the game can have proper artwork without any image files in the project.
/// Coordinates are world units: the canvas covers a rectangle of the world, and the
/// sprite it produces is placed at <see cref="Center"/> to line up with that rectangle.
/// Shapes are drawn supersampled and averaged down, which gives smooth edges.
/// </summary>
public class PixelCanvas
{
    public readonly int Width;             // output size in pixels
    public readonly int Height;
    public readonly float PixelsPerUnit;
    public readonly float OriginX;         // world position of the bottom-left corner
    public readonly float OriginY;

    private readonly int _s;               // supersampling factor
    private readonly int _w;               // supersampled size
    private readonly int _h;
    private readonly Color[] _px;

    public PixelCanvas(float widthUnits, float heightUnits, float pixelsPerUnit, float originX, float originY, int supersample = 2)
    {
        PixelsPerUnit = pixelsPerUnit;
        OriginX = originX;
        OriginY = originY;
        Width = Mathf.Max(1, Mathf.RoundToInt(widthUnits * pixelsPerUnit));
        Height = Mathf.Max(1, Mathf.RoundToInt(heightUnits * pixelsPerUnit));
        _s = Mathf.Max(1, supersample);
        _w = Width * _s;
        _h = Height * _s;
        _px = new Color[_w * _h];
    }

    /// <summary>World position of the canvas centre: put a centred sprite here.</summary>
    public Vector3 Center => new Vector3(OriginX + Width / PixelsPerUnit / 2f, OriginY + Height / PixelsPerUnit / 2f, 0f);

    // ---------- shapes (x, y in world units) ----------

    /// <summary>A rectangle from its bottom-left corner.</summary>
    public void Rect(float x, float y, float w, float h, Color c)
    {
        Poly(c, x, y, x + w, y, x + w, y + h, x, y + h);
    }

    public void Tri(float x0, float y0, float x1, float y1, float x2, float y2, Color c)
    {
        Poly(c, x0, y0, x1, y1, x2, y2);
    }

    /// <summary>A thick line between two points.</summary>
    public void Line(float x0, float y0, float x1, float y1, float thickness, Color c)
    {
        float dx = x1 - x0, dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6f) len = 1e-6f;
        float nx = -dy / len * thickness / 2f, ny = dx / len * thickness / 2f;
        Poly(c, x0 + nx, y0 + ny, x1 + nx, y1 + ny, x1 - nx, y1 - ny, x0 - nx, y0 - ny);
    }

    public void Circle(float cx, float cy, float r, Color c)
    {
        Ellipse(cx, cy, r, r, c);
    }

    /// <summary>The upper half of an ellipse standing on y; squash flattens it.</summary>
    public void Dome(float cx, float y, float r, Color c, float squash = 1f)
    {
        const int n = 24;
        var xy = new float[(n + 1) * 2];
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.PI * i / n;
            xy[i * 2] = cx + r * Mathf.Cos(a);
            xy[i * 2 + 1] = y + r * squash * Mathf.Sin(a);
        }
        Poly(c, xy);
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(ToPxX(cx - rx)));
        int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(ToPxX(cx + rx)));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(ToPxY(cy - ry)));
        int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(ToPxY(cy + ry)));
        float pcx = ToPxX(cx), pcy = ToPxY(cy);
        float prx = rx * PixelsPerUnit * _s, pry = ry * PixelsPerUnit * _s;
        if (prx < 1e-4f || pry < 1e-4f) return;
        for (int y = y0; y <= y1; y++)
        {
            float dy = (y + 0.5f - pcy) / pry;
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - pcx) / prx;
                if (dx * dx + dy * dy <= 1f) Blend(y * _w + x, c);
            }
        }
    }

    /// <summary>A filled polygon given as x, y pairs in world units.</summary>
    public void Poly(Color c, params float[] xy)
    {
        int n = xy.Length / 2;
        if (n < 3) return;
        var px = new float[n];
        var py = new float[n];
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            px[i] = ToPxX(xy[i * 2]);
            py[i] = ToPxY(xy[i * 2 + 1]);
            if (py[i] < minY) minY = py[i];
            if (py[i] > maxY) maxY = py[i];
        }
        int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
        int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(maxY));
        var xs = new float[n];
        for (int y = y0; y <= y1; y++)
        {
            float yc = y + 0.5f;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float ay = py[i], by = py[j];
                if ((ay <= yc) == (by <= yc)) continue;
                xs[count++] = px[i] + (yc - ay) / (by - ay) * (px[j] - px[i]);
            }
            if (count < 2) continue;
            // sort the few crossings, then fill between each pair
            for (int i = 1; i < count; i++)
            {
                float v = xs[i];
                int k = i - 1;
                while (k >= 0 && xs[k] > v) { xs[k + 1] = xs[k]; k--; }
                xs[k + 1] = v;
            }
            for (int i = 0; i + 1 < count; i += 2)
            {
                int xa = Mathf.Max(0, Mathf.CeilToInt(xs[i] - 0.5f));
                int xb = Mathf.Min(_w - 1, Mathf.FloorToInt(xs[i + 1] - 0.5f));
                int row = y * _w;
                for (int x = xa; x <= xb; x++) Blend(row + x, c);
            }
        }
    }

    // ---------- output ----------

    /// <summary>Average the supersampled buffer down and wrap it in a centred sprite.</summary>
    public Sprite ToSprite()
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var outPx = new Color[Width * Height];
        float inv = 1f / (_s * _s);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int sy = 0; sy < _s; sy++)
                {
                    int row = (y * _s + sy) * _w + x * _s;
                    for (int sx = 0; sx < _s; sx++)
                    {
                        Color p = _px[row + sx];
                        r += p.r * p.a; g += p.g * p.a; b += p.b * p.a; a += p.a;
                    }
                }
                if (a > 0f) outPx[y * Width + x] = new Color(r / a, g / a, b / a, a * inv);
                else outPx[y * Width + x] = new Color(0f, 0f, 0f, 0f);
            }
        }
        tex.SetPixels(outPx);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ---------- internals ----------

    private float ToPxX(float x) => (x - OriginX) * PixelsPerUnit * _s;
    private float ToPxY(float y) => (y - OriginY) * PixelsPerUnit * _s;

    /// <summary>Paint over a pixel. A fully transparent colour erases instead, which is how holes are cut.</summary>
    private void Blend(int i, Color c)
    {
        if (c.a <= 0f) { _px[i] = new Color(0f, 0f, 0f, 0f); return; }
        if (c.a >= 1f) { _px[i] = c; return; }
        Color d = _px[i];
        float outA = c.a + d.a * (1f - c.a);
        if (outA <= 0f) { _px[i] = new Color(0f, 0f, 0f, 0f); return; }
        float k = d.a * (1f - c.a);
        _px[i] = new Color((c.r * c.a + d.r * k) / outA, (c.g * c.a + d.g * k) / outA, (c.b * c.a + d.b * k) / outA, outA);
    }
}
