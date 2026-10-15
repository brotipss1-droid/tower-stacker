using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The core loop: a block slides left and right above the tower, one tap drops it,
/// the overhang is cut off, and the next block is a little harder to place.
/// </summary>
public class StackManager : MonoBehaviour
{
    [Header("Tuning")]
    public float blockHeight = 0.7f;
    public float startWidth = 4f;
    public float slideRange = 4.5f;      // how far the moving block travels from the centre
    public float startSpeed = 5f;        // units per second
    public float speedPerBlock = 0.12f;  // speed added for every block placed
    public float perfectTolerance = 0.12f;
    public int comboGrowFrom = 3;        // perfects in a row before the block starts growing back
    public float comboGrow = 0.3f;       // width regained per perfect once the combo is going

    public enum State { Ready, Playing, GameOver }
    public State Current { get; private set; } = State.Ready;
    public int Score { get; private set; }
    public int Best { get; private set; }
    public int Combo { get; private set; }
    public bool NewBest { get; private set; }

    /// <summary>True when the most recent drop was a perfect one.</summary>
    public bool LastPerfect { get; private set; }
    /// <summary>Seconds since the last block was placed.</summary>
    public float PlacedAgo => Time.time - _lastPlaceTime;

    private readonly List<Block> _tower = new List<Block>();
    private Block _moving;
    private float _slideTime;
    private int _direction = 1;
    private int _paletteIndex;
    private float _camY;
    private float _lastPlaceTime = -10f;
    private Camera _cam;
    private Effects _fx;

    private const string BestKey = "tower-stacker.best";

    // Facade colours of the old wooden houses along the Bosphorus; the base is stone.
    private static readonly Color StoneColor = new Color(0.56f, 0.55f, 0.52f);
    private static readonly Color[] HousePalette =
    {
        new Color(0.93f, 0.87f, 0.74f),  // cream
        new Color(0.80f, 0.42f, 0.33f),  // terracotta
        new Color(0.88f, 0.68f, 0.35f),  // ochre
        new Color(0.85f, 0.60f, 0.58f),  // dusty rose
        new Color(0.60f, 0.70f, 0.55f),  // sage
        new Color(0.45f, 0.55f, 0.70f),  // slate blue
        new Color(0.95f, 0.94f, 0.90f),  // whitewash
        new Color(0.55f, 0.38f, 0.28f),  // walnut
    };

    private void Start()
    {
        _cam = Camera.main;
        _fx = GetComponent<Effects>();
        Best = PlayerPrefs.GetInt(BestKey, 0);
        Application.targetFrameRate = 60;
        ResetGame();
    }

    private void Update()
    {
        if (_moving != null)
        {
            _slideTime += Time.deltaTime * (startSpeed + speedPerBlock * Score);
            float x = Mathf.PingPong(_slideTime, slideRange * 2f) - slideRange;
            _moving.SetX(x * _direction);
        }

        if (Tapped())
        {
            switch (Current)
            {
                case State.Ready: Current = State.Playing; SpawnMoving(); break;
                case State.Playing: Place(); break;
                case State.GameOver: ResetGame(); break;
            }
        }

        // Camera follows the tower upwards, keeping the top block in view, plus any shake.
        float targetY = Mathf.Max(5f, TowerTop() + 2.5f);
        _camY = Mathf.Lerp(_camY, targetY, Time.deltaTime * 4f);
        Vector2 shake = _fx != null ? _fx.ShakeOffset : Vector2.zero;
        _cam.transform.position = new Vector3(shake.x, _camY + shake.y, -10f);
    }

    private static bool Tapped()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
        return Input.GetMouseButtonDown(0);
    }

    private void ResetGame()
    {
        foreach (var b in _tower) if (b != null) Destroy(b.gameObject);
        _tower.Clear();
        if (_moving != null) Destroy(_moving.gameObject);
        _moving = null;
        Score = 0;
        Combo = 0;
        NewBest = false;
        LastPerfect = false;
        _paletteIndex = Random.Range(0, HousePalette.Length);
        _camY = 5f;
        _cam.transform.position = new Vector3(0f, _camY, -10f);

        // The base block the tower grows from.
        _tower.Add(Block.Create("Base", 0f, 0f, startWidth, blockHeight, StoneColor, true));
        Current = State.Ready;
    }

    private void SpawnMoving()
    {
        var top = _tower[_tower.Count - 1];
        _direction = -_direction;
        _slideTime = 0f;
        float y = top.transform.position.y + blockHeight;
        _moving = Block.Create("Block " + (Score + 1), -slideRange * _direction, y, top.Width, blockHeight, NextColor());
    }

    private void Place()
    {
        var top = _tower[_tower.Count - 1];
        float topX = top.transform.position.x;
        float x = _moving.transform.position.x;
        float dx = x - topX;
        float overlap = top.Width - Mathf.Abs(dx);

        if (overlap <= 0f)
        {
            // Missed the tower completely.
            _moving.Fall(Mathf.Sign(dx));
            _moving = null;
            GameOver();
            return;
        }

        if (Mathf.Abs(dx) <= perfectTolerance)
        {
            // Perfect drop: snap to the centre, keep the full width, build a combo.
            _moving.SetX(topX);
            Combo++;
            LastPerfect = true;

            // A long combo earns some width back, so a careful player can recover.
            if (Combo >= comboGrowFrom && top.Width < startWidth)
                _moving.SetSize(Mathf.Min(startWidth, top.Width + comboGrow), blockHeight);

            Flash.Spawn(_moving.transform.position, _moving.Width, _moving.Height);
            if (_fx != null) _fx.PlayPerfect(Combo);
        }
        else
        {
            Combo = 0;
            LastPerfect = false;
            // Keep the overlapping part, cut the rest off and let it fall.
            float keptCenter = topX + dx / 2f;
            float cutWidth = Mathf.Abs(dx);
            float cutCenter = x + Mathf.Sign(dx) * (overlap / 2f);
            var piece = Block.Create("Cut", cutCenter, _moving.transform.position.y, cutWidth, blockHeight, _moving.Color);
            piece.Fall(Mathf.Sign(dx));
            _moving.SetSize(overlap, blockHeight);
            _moving.SetX(keptCenter);

            if (_fx != null)
            {
                _fx.PlayCut();
                _fx.Shake(0.08f + Mathf.Min(cutWidth, 2f) * 0.08f);
            }
        }

        _moving.Punch();
        if (_fx != null) _fx.PlayPlace();

        _tower.Add(_moving);
        _moving = null;
        Score++;
        _lastPlaceTime = Time.time;
        SpawnMoving();
    }

    private void GameOver()
    {
        Current = State.GameOver;
        if (_fx != null)
        {
            _fx.Shake(0.45f);
            _fx.PlayGameOver();
        }
        if (Score > Best)
        {
            Best = Score;
            NewBest = Score > 0;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }
    }

    private float TowerTop()
    {
        return _tower.Count == 0 ? 0f : _tower[_tower.Count - 1].transform.position.y;
    }

    private Color NextColor()
    {
        _paletteIndex = (_paletteIndex + 1) % HousePalette.Length;
        return HousePalette[_paletteIndex];
    }
}
