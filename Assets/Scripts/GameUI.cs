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

    private void Start()
    {
        _game = GetComponent<StackManager>();
    }

    private void OnGUI()
    {
        if (_big == null)
        {
            _big = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = Screen.height / 12,
                fontStyle = FontStyle.Bold
            };
            _big.normal.textColor = Color.white;
            _small = new GUIStyle(_big) { fontSize = Screen.height / 28, fontStyle = FontStyle.Normal };
        }

        float w = Screen.width;
        GUI.Label(new Rect(0, Screen.height * 0.06f, w, 100), _game.Score.ToString(), _big);
        GUI.Label(new Rect(0, Screen.height * 0.16f, w, 60), "best " + _game.Best, _small);

        switch (_game.Current)
        {
            case StackManager.State.Ready:
                GUI.Label(new Rect(0, Screen.height * 0.55f, w, 60), "tap to drop a block", _small);
                break;
            case StackManager.State.Playing:
                if (_game.Combo > 1)
                    GUI.Label(new Rect(0, Screen.height * 0.22f, w, 60), "perfect x" + _game.Combo, _small);
                break;
            case StackManager.State.GameOver:
                GUI.Label(new Rect(0, Screen.height * 0.5f, w, 100), "game over", _big);
                GUI.Label(new Rect(0, Screen.height * 0.6f, w, 60), "tap to try again", _small);
                break;
        }
    }
}
