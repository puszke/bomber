using UnityEngine;

/// <summary>
/// Small on-screen panel for checking the Steam connection and the overlay.
/// Also pauses the game while the overlay is open.
/// </summary>
public class SteamOverlayTest : MonoBehaviour
{
    [SerializeField] bool showPanel = true;
    [SerializeField] bool pauseWhileOverlayOpen = true;

    float _timeScaleBeforeOverlay = 1f;

    void OnEnable() => SteamManager.OverlayToggled += OnOverlayToggled;
    void OnDisable() => SteamManager.OverlayToggled -= OnOverlayToggled;

    void OnOverlayToggled(bool active)
    {
        Debug.Log($"[Steam] Overlay {(active ? "opened" : "closed")}");
        if (!pauseWhileOverlayOpen)
            return;

        if (active)
        {
            _timeScaleBeforeOverlay = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _timeScaleBeforeOverlay;
        }
    }

    void OnGUI()
    {
        if (!showPanel)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 220), GUI.skin.box);
        GUILayout.Label("<b>Steam</b>", new GUIStyle(GUI.skin.label) { richText = true });

        if (SteamManager.Initialized)
        {
            GUILayout.Label("Connected");
            GUILayout.Label($"Overlay: {(SteamManager.OverlayActive ? "open" : "closed")}  (Shift+Tab)");
            if (GUILayout.Button("Open overlay (friends)"))
                SteamManager.OpenOverlay("friends");
            if (GUILayout.Button("Open overlay web page"))
                SteamManager.OpenOverlayWebPage("https://store.steampowered.com");
        }
        else
        {
            GUILayout.Label("Not connected");
            if (!string.IsNullOrEmpty(SteamManager.InitError))
                GUILayout.Label(SteamManager.InitError);
            GUILayout.Label("Start Steam and log in, then retry.");
            if (SteamManager.Instance != null && GUILayout.Button("Retry"))
                SteamManager.Instance.TryInit();
        }

        GUILayout.EndArea();
    }
}
