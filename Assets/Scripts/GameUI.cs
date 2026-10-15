using UnityEngine;

/// <summary>
/// Minimal on-screen text: score, best score and the tap prompts. Drawn with
/// Unity's immediate-mode GUI so there is no Canvas to set up yet; a proper UI
/// replaces this in week 2.
/// </summary>
public class GameUI : MonoBehaviour
{
    private StackManager _game;
    private GUIStyle _big;
    private GUIStyle _small;
    private int _bigSize;
    private int _lastScore;
    private float _pop;   // 1 right after the score changes, fades to 0

    private void Start()
    {
        _game = GetComponent<StackManager>();
    }

    private void Update()
    {
        if (_game.Score != _lastScore)
        {
            _lastScore = _game.Score;
            if (_lastScore > 0) _pop = 1f;
        }
        _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime * 4f);
    }

    /// <summary>A label with a soft shadow so it reads on a bright morning sky too.</summary>
    private static void Label(Rect rect, string text, GUIStyle style)
    {
        Color keep = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.45f * keep.a);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), text, style);
        style.normal.textColor = keep;
        GUI.Label(rect, text, style);
    }

    private void OnGUI()
    {
        if (_big == null)
        {
            _bigSize = Screen.height / 12;
            _big = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = _bigSize,
                fontStyle = FontStyle.Bold
            };
            _big.normal.textColor = Color.white;
            _small = new GUIStyle(_big) { fontSize = Screen.height / 28, fontStyle = FontStyle.Normal };
        }

        float w = Screen.width;
        float h = Screen.height;

        // The score grows for a moment every time it changes.
        _big.fontSize = Mathf.RoundToInt(_bigSize * (1f + 0.3f * _pop));
        Label(new Rect(0, h * 0.06f, w, 160), _game.Score.ToString(), _big);
        _big.fontSize = _bigSize;
        Label(new Rect(0, h * 0.16f, w, 60), "best " + _game.Best, _small);

        switch (_game.Current)
        {
            case StackManager.State.Ready:
                Label(new Rect(0, h * 0.55f, w, 60), "tap to drop a block", _small);
                break;

            case StackManager.State.Playing:
                if (_game.LastPerfect && _game.PlacedAgo < 0.9f)
                {
                    string text = _game.Combo > 1 ? "perfect x" + _game.Combo : "perfect!";
                    float fade = 1f - _game.PlacedAgo / 0.9f;
                    var c = _small.normal.textColor;
                    _small.normal.textColor = new Color(1f, 1f, 1f, fade);
                    Label(new Rect(0, h * 0.22f, w, 60), text, _small);
                    _small.normal.textColor = c;
                }
                break;

            case StackManager.State.GameOver:
                Label(new Rect(0, h * 0.5f, w, 100), "game over", _big);
                if (_game.NewBest)
                    Label(new Rect(0, h * 0.6f, w, 60), "new best!", _small);
                Label(new Rect(0, h * 0.66f, w, 60), "tap to try again", _small);
                break;
        }
    }
}
