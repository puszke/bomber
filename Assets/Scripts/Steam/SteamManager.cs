#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Initializes Steamworks once per game launch and keeps the Steam overlay (Shift+Tab) working.
/// Lives on the "Steam" object in the first scene and survives scene loads.
/// </summary>
[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
    // 480 = Valve's "Spacewar" test app. Replace with your own App ID once you have one,
    // and update steam_appid.txt in the project root to match.
    [SerializeField] uint appId = 480;

    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    /// <summary>Fired when the Steam overlay opens (true) or closes (false).</summary>
    public static event Action<bool> OverlayToggled;
    public static bool OverlayActive { get; private set; }
    /// <summary>Why the last init attempt failed, or null.</summary>
    public static string InitError { get; private set; }

#if !DISABLESTEAMWORKS
    Callback<GameOverlayActivated_t> _overlayCallback;
    SteamAPIWarningMessageHook_t _warningHook;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test())
            Debug.LogError("[Steam] Packsize test failed - wrong Steamworks.NET build for this platform.");

#if !UNITY_EDITOR
        // Outside Steam, relaunch through Steam so the overlay gets injected.
        // Skipped for the test App ID, which you don't own a real store page for.
        if (appId != 480 && SteamAPI.RestartAppIfNecessary(new AppId_t(appId)))
        {
            Application.Quit();
            return;
        }
#endif

        TryInit();
    }

    /// <summary>Initializes Steamworks; safe to call again after starting the Steam client.</summary>
    public void TryInit()
    {
        if (Initialized)
            return;

        // Steam reads the App ID from this variable when steam_appid.txt isn't in the working directory
        // (e.g. a macOS .app started from Finder). Steam sets it itself when it launches the game.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SteamAppId")))
            Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

        var result = SteamAPI.InitEx(out string error);
        if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
        {
            InitError = $"{result}: {error}";
            Debug.LogWarning($"[Steam] Init failed ({InitError}). Is the Steam client running and logged in?");
            return;
        }

        Initialized = true;
        InitError = null;
        // Kept in a field so the GC doesn't collect the delegate while native code still holds it.
        _warningHook = (severity, text) => Debug.LogWarning($"[Steam] {text}");
        SteamClient.SetWarningMessageHook(_warningHook);
        _overlayCallback = Callback<GameOverlayActivated_t>.Create(OnOverlayActivated);

        Debug.Log($"[Steam] Logged in as {SteamFriends.GetPersonaName()}, overlay enabled: {SteamUtils.IsOverlayEnabled()}");
    }

    void Update()
    {
        if (Initialized)
            SteamAPI.RunCallbacks();
    }

    void OnOverlayActivated(GameOverlayActivated_t data)
    {
        OverlayActive = data.m_bActive != 0;
        OverlayToggled?.Invoke(OverlayActive);
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        if (Initialized)
        {
            SteamAPI.Shutdown();
            Initialized = false;
        }
    }

    /// <summary>Opens the overlay programmatically. Dialog: "friends", "community", "players", "settings", "achievements", ...</summary>
    public static void OpenOverlay(string dialog = "friends")
    {
        if (Initialized)
            SteamFriends.ActivateGameOverlay(dialog);
    }

    public static void OpenOverlayWebPage(string url)
    {
        if (Initialized)
            SteamFriends.ActivateGameOverlayToWebPage(url);
    }
#else
    public void TryInit() { }
    public static void OpenOverlay(string dialog = "friends") { }
    public static void OpenOverlayWebPage(string url) { }
#endif
}
