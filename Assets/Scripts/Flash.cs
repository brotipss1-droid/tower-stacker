using UnityEngine;

/// <summary>
/// A white rectangle that grows and fades out in a fraction of a second. Spawned
/// on a perfect drop so the player sees that the block landed exactly right.
/// </summary>
public class Flash : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Vector3 _startScale;
    private float _grow;
    private float _life;
    private float _age;

    public static void Spawn(Vector3 position, float width, float height, float grow = 1.6f, float life = 0.35f)
    {
        var go = new GameObject("Flash");
        go.transform.position = position;
        var flash = go.AddComponent<Flash>();
        flash._renderer = go.AddComponent<SpriteRenderer>();
        flash._renderer.sprite = Block.SquareSprite();
        flash._renderer.color = new Color(1f, 1f, 1f, 0.85f);
        flash._renderer.sortingOrder = 5;
        flash._startScale = new Vector3(width, height, 1f);
        flash._grow = grow;
        flash._life = life;
        go.transform.localScale = flash._startScale;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / _life);
        float scale = 1f + (_grow - 1f) * t;
        transform.localScale = new Vector3(_startScale.x * scale, _startScale.y * scale, 1f);
        var c = _renderer.color;
        c.a = 0.85f * (1f - t) * (1f - t);
        _renderer.color = c;
        if (t >= 1f) Destroy(gameObject);
    }
}
