using UnityEngine;

/// <summary>
/// One block of the tower. A block is either sitting in the tower, sliding across
/// the screen waiting to be dropped, or a cut-off piece falling away.
/// </summary>
public class Block : MonoBehaviour
{
    public float Width { get; private set; }
    public float Height { get; private set; }
    public Color Color => _renderer.color;

    private SpriteRenderer _renderer;
    private bool _falling;
    private float _fallSpeed;
    private float _spin;
    private float _punch;   // seconds left of the landing squash

    private const float PunchDuration = 0.16f;

    private static Sprite _square;

    public static Block Create(string name, float x, float y, float width, float height, Color color)
    {
        var go = new GameObject(name);
        var block = go.AddComponent<Block>();
        block._renderer = go.AddComponent<SpriteRenderer>();
        block._renderer.sprite = SquareSprite();
        block._renderer.color = color;
        block.SetSize(width, height);
        go.transform.position = new Vector3(x, y, 0f);
        return block;
    }

    public void SetSize(float width, float height)
    {
        Width = width;
        Height = height;
        transform.localScale = new Vector3(width, height, 1f);
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
            transform.localScale = new Vector3(Width * (1f + bump), Height * (1f - bump), 1f);
            if (_punch <= 0f) transform.localScale = new Vector3(Width, Height, 1f);
        }

        if (!_falling) return;
        _fallSpeed += 30f * Time.deltaTime;
        transform.position += Vector3.down * _fallSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, _spin * Time.deltaTime);
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
