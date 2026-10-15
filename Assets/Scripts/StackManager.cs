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

    public enum State { Ready, Playing, GameOver }
    public State Current { get; private set; } = State.Ready;
    public int Score { get; private set; }
    public int Best { get; private set; }
    public int Combo { get; private set; }

    private readonly List<Block> _tower = new List<Block>();
    private Block _moving;
    private float _slideTime;
    private int _direction = 1;
    private float _hue;
    private Camera _cam;

    private const string BestKey = "tower-stacker.best";

    private void Start()
    {
        _cam = Camera.main;
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

        // Camera follows the tower upwards, keeping the top block in view.
        float targetY = TowerTop() + 2.5f;
        var camPos = _cam.transform.position;
        camPos.y = Mathf.Lerp(camPos.y, Mathf.Max(5f, targetY), Time.deltaTime * 4f);
        _cam.transform.position = camPos;
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
        _hue = Random.value;
        _cam.transform.position = new Vector3(0f, 5f, -10f);

        // The base block the tower grows from.
        _tower.Add(Block.Create("Base", 0f, 0f, startWidth, blockHeight, NextColor()));
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
        }
        else
        {
            Combo = 0;
            // Keep the overlapping part, cut the rest off and let it fall.
            float keptCenter = topX + dx / 2f;
            float cutWidth = Mathf.Abs(dx);
            float cutCenter = x + Mathf.Sign(dx) * (overlap / 2f);
            var piece = Block.Create("Cut", cutCenter, _moving.transform.position.y, cutWidth, blockHeight, _moving.GetComponent<SpriteRenderer>().color);
            piece.Fall(Mathf.Sign(dx));
            _moving.SetSize(overlap, blockHeight);
            _moving.SetX(keptCenter);
        }

        _tower.Add(_moving);
        _moving = null;
        Score++;
        SpawnMoving();
    }

    private void GameOver()
    {
        Current = State.GameOver;
        if (Score > Best)
        {
            Best = Score;
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
        _hue = (_hue + 0.045f) % 1f;
        return Color.HSVToRGB(_hue, 0.55f, 0.95f);
    }
}
