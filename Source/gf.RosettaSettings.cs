using UnityEngine;

// IMGUI-based settings panel for GfMod.
//
// Uses Unity's immediate-mode GUI (OnGUI), which is always available in any
// MelonLoader mod regardless of whether the game uses UI Toolkit or UGUI.
//
// Method names are identical to the RosettaUI version so Runtime.cs and
// Settings.cs do not need changes:
//   F6 → ToggleRosettaSettings()
//   Escape inside panel → CloseRosettaSettings()
//
// No UIDocument, no PanelSettings, no external library needed at runtime.

public partial class GfMod
{
    // ── State ────────────────────────────────────────────────────────────────

    private Rect _imRect;
    private bool _imRectInit;
    private Vector2 _imFeaturesScroll;
    private Vector2 _imKeysScroll;

    // ── Styles (lazy init on first OnGUI call) ────────────────────────────────

    private bool        _imStylesReady;
    private GUIStyle    _imWindowStyle;
    private GUIStyle    _imTitleStyle;
    private GUIStyle    _imTabActive;
    private GUIStyle    _imTabIdle;
    private GUIStyle    _imSectionLabel;
    private GUIStyle    _imToggleOn;
    private GUIStyle    _imToggleOff;
    private GUIStyle    _imActionLabel;
    private GUIStyle    _imKeyBadge;
    private GUIStyle    _imRebindActive;
    private GUIStyle    _imButton;
    private GUIStyle    _imCloseBtn;
    private GUIStyle    _imInfoLabel;
    private GUIStyle    _imDescLabel;
    private Texture2D   _imTxDark;
    private Texture2D   _imTxBlue;
    private Texture2D   _imTxGrey;
    private Texture2D   _imTxDarker;
    private Texture2D   _imTxRed;
    private Texture2D   _imTxGreen;
    private Texture2D   _imTxSeparator;
    private Texture2D   _imTxPanel;
    private Texture2D   _imTxAccentHover;
    private Texture2D   _imTxBackdrop;
    private Texture2D   _imTxCard;
    private Texture2D   _imTxCyan;
    private Texture2D   _imTxCyanHover;

    private static Texture2D MakeTex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    // A tiny 9-sliced rounded texture gives the IMGUI controls the same soft
    // card treatment as the in-game WAND menu, without requiring external art.
    private static Texture2D MakeRoundedTex(Color c, int radius)
    {
        const int size = 32;
        Texture2D t = new Texture2D(size, size);
        float r = Mathf.Max(1f, radius);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(r - x, x - (size - 1 - r), 0f);
            float dy = Mathf.Max(r - y, y - (size - 1 - r), 0f);
            Color pixel = c;
            if (dx * dx + dy * dy > r * r) pixel.a = 0f;
            t.SetPixel(x, y, pixel);
        }
        t.Apply();
        return t;
    }

    private void InitImStyles()
    {
        if (_imStylesReady) return;
        _imStylesReady = true;

        _imTxDark        = MakeRoundedTex(new Color(0.035f, 0.043f, 0.055f, 0.985f), 11);
        _imTxBlue        = MakeRoundedTex(new Color(0.00f, 0.62f, 0.79f, 1.00f), 8);
        _imTxAccentHover = MakeRoundedTex(new Color(0.00f, 0.72f, 0.90f, 1.00f), 8);
        _imTxGrey        = MakeRoundedTex(new Color(0.105f, 0.118f, 0.137f, 1.00f), 8);
        _imTxDarker      = MakeTex(new Color(0.027f, 0.031f, 0.040f, 0.98f));
        _imTxPanel       = MakeRoundedTex(new Color(0.055f, 0.062f, 0.075f, 0.96f), 12);
        _imTxCard        = MakeRoundedTex(new Color(0.070f, 0.078f, 0.094f, 0.92f), 10);
        _imTxBackdrop    = MakeTex(new Color(0f, 0f, 0f, 0.64f));
        _imTxCyan        = MakeRoundedTex(new Color(0.00f, 0.66f, 0.84f, 1f), 8);
        _imTxCyanHover   = MakeRoundedTex(new Color(0.08f, 0.76f, 0.94f, 1f), 8);
        _imTxRed         = MakeRoundedTex(new Color(0.78f, 0.20f, 0.27f, 1.00f), 8);
        _imTxGreen       = MakeRoundedTex(new Color(0.13f, 0.70f, 0.43f, 1.00f), 8);
        _imTxSeparator   = MakeTex(new Color(1f, 1f, 1f, 0.08f));

        _imWindowStyle = new GUIStyle(GUI.skin.window);
        _imWindowStyle.normal.background   = _imTxDark;
        _imWindowStyle.onNormal.background = _imTxDark;
        _imWindowStyle.border   = new RectOffset(11, 11, 11, 11);
        _imWindowStyle.padding  = new RectOffset(0, 0, 0, 0);
        _imWindowStyle.normal.textColor = Color.clear;   // hide default title

        _imTitleStyle = new GUIStyle(GUI.skin.label);
        _imTitleStyle.fontSize  = 21;
        _imTitleStyle.fontStyle = FontStyle.Bold;
        _imTitleStyle.normal.textColor = Color.white;
        _imTitleStyle.alignment = TextAnchor.MiddleLeft;

        _imTabActive = new GUIStyle(GUI.skin.button);
        _imTabActive.normal.background   = _imTxCyan;
        _imTabActive.hover.background    = _imTxAccentHover;
        _imTabActive.active.background   = _imTxBlue;
        _imTabActive.normal.textColor    = Color.white;
        _imTabActive.fontStyle           = FontStyle.Bold;
        _imTabActive.fontSize            = 11;
        _imTabActive.alignment           = TextAnchor.MiddleCenter;
        _imTabActive.padding             = new RectOffset(8, 8, 0, 0);

        _imTabIdle = new GUIStyle(GUI.skin.button);
        _imTabIdle.normal.background  = _imTxCard;
        _imTabIdle.hover.background   = _imTxGrey;
        _imTabIdle.active.background  = _imTxGrey;
        _imTabIdle.normal.textColor   = new Color(0.70f, 0.70f, 0.75f);
        _imTabIdle.fontSize           = 11;
        _imTabIdle.alignment          = TextAnchor.MiddleCenter;
        _imTabIdle.padding            = new RectOffset(8, 8, 0, 0);

        _imSectionLabel = new GUIStyle(GUI.skin.label);
        _imSectionLabel.fontSize  = 12;
        _imSectionLabel.fontStyle = FontStyle.Bold;
        _imSectionLabel.normal.textColor = new Color(0.70f, 0.74f, 0.78f);

        _imActionLabel = new GUIStyle(GUI.skin.label);
        _imActionLabel.fontSize = 15;
        _imActionLabel.normal.textColor = new Color(0.90f, 0.91f, 0.93f);
        _imActionLabel.alignment = TextAnchor.MiddleLeft;

        _imToggleOn = new GUIStyle(GUI.skin.button);
        _imToggleOn.normal.background = _imTxCyan;
        _imToggleOn.hover.background  = _imTxCyanHover;
        _imToggleOn.normal.textColor  = Color.white;
        _imToggleOn.fontSize          = 11;
        _imToggleOn.fontStyle         = FontStyle.Bold;

        _imToggleOff = new GUIStyle(GUI.skin.button);
        _imToggleOff.normal.background = _imTxGrey;
        _imToggleOff.hover.background  = _imTxGrey;
        _imToggleOff.normal.textColor  = new Color(0.68f, 0.72f, 0.80f);
        _imToggleOff.fontSize          = 11;

        _imKeyBadge = new GUIStyle(GUI.skin.button);
        _imKeyBadge.normal.background = _imTxGrey;
        _imKeyBadge.hover.background  = _imTxGrey;
        _imKeyBadge.normal.textColor  = Color.white;
        _imKeyBadge.fontSize          = 11;

        _imRebindActive = new GUIStyle(_imKeyBadge);
        _imRebindActive.normal.background = _imTxGreen;
        _imRebindActive.hover.background  = _imTxGreen;

        _imButton = new GUIStyle(GUI.skin.button);
        _imButton.normal.background = _imTxCyan;
        _imButton.hover.background  = _imTxCyanHover;
        _imButton.normal.textColor  = Color.white;
        _imButton.fontSize          = 13;

        _imCloseBtn = new GUIStyle(GUI.skin.button);
        _imCloseBtn.normal.background = _imTxRed;
        _imCloseBtn.hover.background  = _imTxRed;
        _imCloseBtn.normal.textColor  = new Color(1f, 0.5f, 0.5f);
        _imCloseBtn.fontStyle         = FontStyle.Bold;
        _imCloseBtn.fontSize          = 14;

        _imInfoLabel = new GUIStyle(GUI.skin.label);
        _imInfoLabel.fontSize  = 12;
        _imInfoLabel.wordWrap  = true;
        _imInfoLabel.normal.textColor = new Color(0.62f, 0.66f, 0.71f);

        _imDescLabel = new GUIStyle(GUI.skin.label);
        _imDescLabel.fontSize  = 10;
        _imDescLabel.wordWrap  = false;
        _imDescLabel.normal.textColor = new Color(0.51f, 0.56f, 0.62f);
    }

    // ── OnGUI (called from entry shim) ───────────────────────────────────────

    internal void GfOnGUI()
    {
        if (!settingsPanelOpen) return;

        InitImStyles();

        if (!_imRectInit)
        {
            float width = Mathf.Min(1420f, Mathf.Max(720f, Screen.width - 48f));
            float height = Mathf.Min(820f, Mathf.Max(500f, Screen.height - 72f));
            _imRect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width, height);
            _imRectInit = true;
        }

        _imRect.x = Mathf.Clamp(_imRect.x, 0f, Mathf.Max(0f, Screen.width - _imRect.width));
        _imRect.y = Mathf.Clamp(_imRect.y, 0f, Mathf.Max(0f, Screen.height - _imRect.height));

        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _imTxBackdrop);
        _imRect = GUI.Window(9901, _imRect, DrawImWindow, string.Empty, _imWindowStyle);
    }

    // ── Window contents ──────────────────────────────────────────────────────

    private void DrawImWindow(int id)
    {
        float w = _imRect.width;
        float h = _imRect.height;
        float titleH = 92f;

        // ── Title bar ──────────────────────────────────────────────────────
        GUI.DrawTexture(new Rect(0, 0, w, titleH), _imTxDarker);
        GUI.Label(new Rect(26f, 12f, w - 210f, 30f), "BIRDIE MOD", _imTitleStyle);
        GUI.Label(new Rect(27f, 42f, w - 210f, 20f), GetImTabTitle(settingsTabIndex) + "  ·  " + settingsKeyLabel + " — открыть/закрыть  ·  Esc — закрыть", _imInfoLabel);

        if (GUI.Button(new Rect(w - 54f, 17f, 32f, 30f), "X", _imCloseBtn))
            CloseRosettaSettings();

        // thin separator below title
        GUI.DrawTexture(new Rect(0, titleH, w, 1f), _imTxSeparator);

        string[] tabLabels = { "FEATURES", "KEYBINDS", "HUD", "CREDITS", "ITEMS", "WEATHER" };
        float tabH = 30f;
        float tabGap = 8f;
        float tabW = (w - 52f - tabGap * (tabLabels.Length - 1)) / tabLabels.Length;
        for (int i = 0; i < tabLabels.Length; i++)
        {
            GUIStyle s = (settingsTabIndex == i) ? _imTabActive : _imTabIdle;
            if (GUI.Button(new Rect(26f + i * (tabW + tabGap), 58f, tabW, tabH), tabLabels[i], s))
                settingsTabIndex = i;
        }

        // ── Content area ───────────────────────────────────────────────────
        float cx = 20f;
        float cy = titleH + 16f;
        float cw = w - 40f;
        float ch = h - cy - 20f;

        GUI.DrawTexture(new Rect(cx - 6f, cy - 6f, cw + 12f, ch + 12f), _imTxPanel);

        GUILayout.BeginArea(new Rect(cx, cy, cw, ch));
        switch (settingsTabIndex)
        {
            case 0: DrawImFeatures(cw); break;
            case 1: DrawImKeys(cw);     break;
            case 2: DrawImHud(cw);      break;
            case 3: DrawImCredits(cw);  break;
            case 4: DrawImItems(cw);    break;
            case 5: DrawImWeather(cw);  break;
        }
        GUILayout.EndArea();

        GUI.DragWindow(new Rect(0, 0, w, titleH));
    }

    private static string GetImTabTitle(int index)
    {
        string[] titles = { "Gameplay controls", "Keybinds", "HUD preferences", "Credits", "Item spawner", "Weather controls" };
        return index >= 0 && index < titles.Length ? titles[index] : "Settings";
    }

    // ── Features tab ─────────────────────────────────────────────────────────

    private void DrawImFeatures(float w)
    {
        _imFeaturesScroll = GUILayout.BeginScrollView(_imFeaturesScroll);

        ImSectionHeader("CORE");
        ImToggleRowWithDesc("Ice Immunity [" + iceToggleKeyLabel + "]", iceImmunityEnabled, "Slide on ice without slipping", () => ToggleIceImmunity());
        ImToggleRowWithDesc("Assist [" + assistToggleKeyLabel + "]", assistEnabled, "Auto-aims and releases at the perfect moment", () => ToggleAssist());

        GUILayout.Space(6f);
        ImSectionHeader("EXTRAS");
        ImToggleRowWithDesc("No Wind [" + noWindKeyLabel + "]", noWindEnabled, "Removes wind deflection from your ball", () => ToggleNoWind());
        ImToggleRowWithDesc("No Air Drag [" + noAirDragKeyLabel + "]", noAirDragEnabled, "Removes air resistance — ball flies further", () => ToggleNoAirDrag());
        ImToggleRowWithDesc("Speed Boost [" + speedMultiplierKeyLabel + "]", speedMultiplierEnabled, "Multiplies your movement speed", () => ToggleSpeedMultiplier());
        ImToggleRowWithDesc("Inf. Item Usage [" + infiniteAmmoKeyLabel + "]", infiniteAmmoEnabled, "Weapons and items never run out", () => ToggleInfiniteAmmo());
        ImToggleRowWithDesc("No Knockback [" + noKnockbackKeyLabel + "]", noKnockbackEnabled, "Prevents weapons from flinging you back", () => ToggleNoKnockback());
        ImToggleRowWithDesc("Landmine Immunity [" + landmineImmunityKeyLabel + "]", landmineImmunityEnabled, "Walk over landmines without being hit", () => ToggleLandmineImmunity());

            GUILayout.Space(6f);
            ImSectionHeader("SPEED FACTOR");
            GUILayout.BeginHorizontal();
            GUILayout.Label("x" + speedMultiplierFactor.ToString("F1"), _imActionLabel, GUILayout.Width(36f));
            float newFactor = GUILayout.HorizontalSlider(speedMultiplierFactor, 0.5f, 10f);
            if (System.Math.Abs(newFactor - speedMultiplierFactor) > 0.01f)
            {
                speedMultiplierFactor = Mathf.Round(newFactor * 10f) / 10f;
                if (speedMultiplierEnabled) ApplySpeedMultiplierState();
                SaveCurrentConfig();
            }
            GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
    }

    // ── Keys tab ──────────────────────────────────────────────────────────────

    private void DrawImKeys(float w)
    {
        // Status line
        GUIStyle statusStyle = new GUIStyle(_imInfoLabel);
        statusStyle.normal.textColor = keybindRebindMode
            ? new Color(0.4f, 0.9f, 0.5f)
            : new Color(0.55f, 0.55f, 0.62f);
        GUILayout.Label(keybindRebindMode
            ? "Press any key to bind: " + SettingsKeybindDisplayNames[keybindRebindIndex]
            : "Click a key badge to rebind",
            statusStyle);

        GUILayout.Space(4f);
        _imKeysScroll = GUILayout.BeginScrollView(_imKeysScroll);

        for (int i = 0; i < SettingsKeybindDisplayNames.Length; i++)
        {
            int captured = i;
            GUILayout.BeginHorizontal();
            GUILayout.Label(SettingsKeybindDisplayNames[i], _imActionLabel);
            bool isRebinding = keybindRebindMode && keybindRebindIndex == i;
            GUIStyle badge = isRebinding ? _imRebindActive : _imKeyBadge;
            string badgeText = isRebinding ? "..." : FormatKeyLabel(GetKeybindNameByIndex(i));
            if (GUILayout.Button(badgeText, badge, GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                StartKeybindRebindForIndex(captured);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        GUILayout.EndScrollView();
    }

    // ── HUD tab ───────────────────────────────────────────────────────────────

    private void DrawImHud(float w)
    {
        ImSectionHeader("HUD VISIBILITY");
        ImToggleRow("Bottom keybind bar",    hudShowBottomBar,    () => { hudShowBottomBar    = !hudShowBottomBar;    SaveCurrentConfig(); MarkHudDirty(); });
        ImToggleRow("Ball distance to hole", hudShowBallDistance, () => { hudShowBallDistance = !hudShowBallDistance; SaveCurrentConfig(); MarkHudDirty(); });
        ImToggleRow("Ice immunity indicator",hudShowIceIndicator, () => { hudShowIceIndicator = !hudShowIceIndicator; SaveCurrentConfig(); MarkHudDirty(); });
        ImToggleRow("Center title",          hudShowCenterTitle,  () => { hudShowCenterTitle  = !hudShowCenterTitle;  SaveCurrentConfig(); MarkHudDirty(); });
        ImToggleRow("Player info (left)",    hudShowPlayerInfo,   () => { hudShowPlayerInfo   = !hudShowPlayerInfo;   SaveCurrentConfig(); MarkHudDirty(); });
    }

    // ── Credits tab ───────────────────────────────────────────────────────────

    private void DrawImCredits(float w)
    {
        GUILayout.Label("Grant credits to yourself\n(client-local, no server sync)", _imInfoLabel);
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Amount:", _imActionLabel, GUILayout.Width(70f));
        GUILayout.Label(creditsGrantAmount.ToString(), _imActionLabel, GUILayout.Width(60f));
        GUILayout.EndHorizontal();

        int newAmount = Mathf.RoundToInt(GUILayout.HorizontalSlider(creditsGrantAmount, 0f, 50000f));
        if (newAmount != creditsGrantAmount)
        {
            creditsGrantAmount = newAmount;
            SaveCurrentConfig();
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Grant " + creditsGrantAmount + " Credits", _imButton, GUILayout.Height(34f)))
            GrantCredits(creditsGrantAmount);

#if BEPINEX
        GUILayout.Space(12f);
        GUIStyle discordStyle = new GUIStyle(_imButton);
        discordStyle.normal.textColor = new Color(0.56f, 0.62f, 0.90f, 1f);
        if (GUILayout.Button("Join Discord", discordStyle, GUILayout.Height(30f)))
            UnityEngine.Application.OpenURL("https://discord.gg/wbhNEHFGMq");
#endif
    }

    // ── Items tab ─────────────────────────────────────────────────────────────

    private Vector2 _imItemsScroll;

    private void DrawImItems(float w)
    {
        bool hostReady = GfGrantBridge.IsReady();
        GUIStyle statusStyle = new GUIStyle(_imInfoLabel);
        statusStyle.normal.textColor = hostReady
            ? new Color(0.30f, 0.85f, 0.45f)
            : new Color(0.80f, 0.35f, 0.35f);
        GUILayout.Label(hostReady
            ? "Host has GfMod — items will spawn"
            : "Host does not have GfMod — items require host",
            statusStyle);

        GUILayout.Space(6f);
        _imItemsScroll = GUILayout.BeginScrollView(_imItemsScroll);

        float btnW = (w - 12f) * 0.5f;
        for (int i = 0; i < SpawnableItems.Length; i++)
        {
            if (i % 2 == 0) GUILayout.BeginHorizontal();
            SpawnableItemDefinition item = SpawnableItems[i];
            int captured = item.TypeId;
            string lbl = item.KeyLabel + "  " + item.DisplayName;
            if (GUILayout.Button(lbl, _imButton, GUILayout.Width(btnW), GUILayout.Height(30f)))
            {
                SpawnItemClientSide(captured);
            }
            if (i % 2 == 1 || i == SpawnableItems.Length - 1) GUILayout.EndHorizontal();
            if (i % 2 == 1) GUILayout.Space(2f);
        }

        GUILayout.EndScrollView();
    }

    // ── Weather tab (host only) ──────────────────────────────────────────────

    private Vector2 _imWeatherScroll;

    private void DrawImWeather(float w)
    {
        _imWeatherScroll = GUILayout.BeginScrollView(_imWeatherScroll);

        if (!IsNetworkHost())
        {
            GUILayout.Label("Weather controls are available to the lobby host only.", _imInfoLabel);
            GUILayout.EndScrollView();
            return;
        }

        ImSectionHeader("WEATHER EVENT");

        string[] weatherNames = {
            "None (Clear)", "Rain - Light", "Rain - Medium", "Rain - Heavy",
            "Wind Gusts - Light", "Wind Gusts - Medium", "Wind Gusts - Heavy",
            "Thunderstorm", "Tornado"
        };

        for (int i = 0; i < weatherNames.Length; i++)
        {
            bool isSelected = (_hostSelectedWeather == (byte)i);
            GUIStyle s = isSelected ? _imToggleOn : _imToggleOff;
            if (GUILayout.Button(weatherNames[i], s, GUILayout.Height(28f)))
            {
                _hostSelectedWeather = (byte)i;
            }
        }

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(_hostWeatherRunning ? "Stop Weather" : "Start Weather", _imButton, GUILayout.Height(34f)))
        {
            if (_hostWeatherRunning)
            {
                _hostWeatherRunning = false;
                GfWeatherBridge.BroadcastWeather(0, true);
                ApplyWeather(0);
            }
            else
            {
                _hostWeatherRunning = true;
                GfWeatherBridge.BroadcastWeather(_hostSelectedWeather, true);
                ApplyWeather(_hostSelectedWeather);
            }
        }
        GUILayout.EndHorizontal();

        if (_hostWeatherRunning)
        {
            GUIStyle activeStyle = new GUIStyle(_imInfoLabel);
            activeStyle.normal.textColor = new Color(0.30f, 0.85f, 0.45f, 1f);
            GUILayout.Label("Weather active: " + ((_hostSelectedWeather < 9) ? new string[]{ "None","Rain (Light)","Rain (Medium)","Rain (Heavy)","Wind Gusts (Light)","Wind Gusts (Medium)","Wind Gusts (Heavy)","Thunderstorm","Tornado" }[_hostSelectedWeather] : "Unknown"), activeStyle);
        }

        GUILayout.Space(10f);
        ImSectionHeader("AUTO WEATHER");

        GUILayout.BeginHorizontal();
        GUIStyle autoStyle = autoWeatherEnabled ? _imToggleOn : _imToggleOff;
        if (GUILayout.Button(autoWeatherEnabled ? "Auto Weather  ON" : "Auto Weather  OFF", autoStyle, GUILayout.Height(28f)))
            autoWeatherEnabled = !autoWeatherEnabled;
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Spawn Chance", _imInfoLabel, GUILayout.Width(110f));
        autoWeatherChance = Mathf.RoundToInt(GUILayout.HorizontalSlider(autoWeatherChance, 0f, 100f, GUILayout.Width(w - 180f)));
        GUILayout.Label(autoWeatherChance + "%", _imInfoLabel, GUILayout.Width(40f));
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.Label("Per-Type Spawn Weight (0 = disabled):", _imInfoLabel);
        string[] autoNames = { "Rain - Light", "Rain - Medium", "Rain - Heavy",
                               "Wind - Light", "Wind - Medium", "Wind - Heavy",
                               "Thunderstorm", "Tornado" };
        for (int i = 0; i < autoNames.Length; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(autoNames[i], _imInfoLabel, GUILayout.Width(115f));
            autoWeatherChances[i] = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(autoWeatherChances[i], 0f, 100f, GUILayout.Width(w - 195f)));
            GUILayout.Label(autoWeatherChances[i] + "%", _imInfoLabel, GUILayout.Width(40f));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10f);
        ImSectionHeader("NOTES");
        GUILayout.Label("Wind changes affect all players. Rain drag and visual effects apply to Gf clients only.", _imInfoLabel);

        GUILayout.EndScrollView();
    }

    private bool IsNetworkHost()
    {
        try
        {
            foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type nsType = asm.GetType("Mirror.NetworkServer");
                if (nsType == null) continue;
                System.Reflection.PropertyInfo p = nsType.GetProperty("active",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (p != null) return (bool)p.GetValue(null);
                break;
            }
        }
        catch { }
        return false;
    }

    // ── Shared row helpers ────────────────────────────────────────────────────

    private void ImSectionHeader(string text)
    {
        GUILayout.Label(text, _imSectionLabel);
    }

    private void ImToggleRow(string label, bool value, System.Action onToggle)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _imActionLabel);
        GUIStyle s = value ? _imToggleOn : _imToggleOff;
        if (GUILayout.Button(value ? "ON" : "OFF", s, GUILayout.Width(48f), GUILayout.Height(24f)))
            onToggle();
        GUILayout.EndHorizontal();
        GUILayout.Space(1f);
    }

    private void ImToggleRowWithDesc(string label, bool value, string desc, System.Action onToggle)
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label(label, _imActionLabel);
        GUILayout.Label(desc, _imDescLabel);
        GUILayout.EndVertical();
        GUIStyle s = value ? _imToggleOn : _imToggleOff;
        if (GUILayout.Button(value ? "ON" : "OFF", s, GUILayout.Width(48f), GUILayout.Height(34f)))
            onToggle();
        GUILayout.EndHorizontal();
        GUILayout.Space(2f);
    }

    // ── Open / close ──────────────────────────────────────────────────────────

    private void OpenRosettaSettings()
    {
        settingsPanelOpen = true;
        keybindRebindMode = false;
        keybindRebindIndex = -1;
        ApplyCursorUnlockForSettings(true);
        ApplyGameInputSuppressionForSettings(true);
        MarkHudDirty();
    }

    private void CloseRosettaSettings()
    {
        settingsPanelOpen = false;
        keybindRebindMode = false;
        keybindRebindIndex = -1;
        ApplyCursorUnlockForSettings(false);
        ApplyGameInputSuppressionForSettings(false);
        MarkHudDirty();
    }

    private void ToggleRosettaSettings()
    {
        if (settingsPanelOpen)
            CloseRosettaSettings();
        else
            OpenRosettaSettings();
    }

    // ── No-ops kept for compiler compatibility ────────────────────────────────

    private void EnsureRosettaRoot() { }
}
