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
        GUI.Label(new Rect(0, h * 0.06f, w, 160), _game.Score.ToString(), _big);
        _big.fontSize = _bigSize;
        GUI.Label(new Rect(0, h * 0.16f, w, 60), "best " + _game.Best, _small);

        switch (_game.Current)
        {
            case StackManager.State.Ready:
                GUI.Label(new Rect(0, h * 0.55f, w, 60), "tap to drop a block", _small);
                break;

            case StackManager.State.Playing:
                if (_game.LastPerfect && _game.PlacedAgo < 0.9f)
                {
                    string text = _game.Combo > 1 ? "perfect x" + _game.Combo : "perfect!";
                    float fade = 1f - _game.PlacedAgo / 0.9f;
                    var c = _small.normal.textColor;
                    _small.normal.textColor = new Color(1f, 1f, 1f, fade);
                    GUI.Label(new Rect(0, h * 0.22f, w, 60), text, _small);
                    _small.normal.textColor = c;
                }
                break;

            case StackManager.State.GameOver:
                GUI.Label(new Rect(0, h * 0.5f, w, 100), "game over", _big);
                if (_game.NewBest)
                    GUI.Label(new Rect(0, h * 0.6f, w, 60), "new best!", _small);
                GUI.Label(new Rect(0, h * 0.66f, w, 60), "tap to try again", _small);
                break;
        }
    }
}
