using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";
    private const string DefaultGameSceneName = "TheTunnelGameScene";

    [SerializeField] private Font demonicFont;
    [SerializeField] private string gameSceneName = DefaultGameSceneName;

    public static bool IsMenuOpen { get; private set; } = true;

    private static MainMenu instance;
    private static bool skipMenuOnNextLoad;

    private bool showOptions;
    private float volume = 1f;

    private GUIStyle titleStyle;
    private GUIStyle titleShadowStyle;
    private GUIStyle titleGlowStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle sliderTrackStyle;
    private GUIStyle sliderThumbStyle;

    private Texture2D sliderTrackTexture;
    private Texture2D sliderThumbTexture;
    private Texture2D sliderThumbHoverTexture;
    private int lastTitleFontSize;
    private int lastMenuFontSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        IsMenuOpen = true;
        instance = null;
        skipMenuOnNextLoad = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMenuExists()
    {
        if (instance == null && FindFirstObjectByType<MainMenu>() == null)
        {
            GameObject menuObject = new GameObject("MainMenu");
            menuObject.AddComponent<MainMenu>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        volume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        AudioListener.volume = Mathf.Clamp01(volume);

        if (demonicFont == null)
        {
            demonicFont = Resources.Load<Font>("DemonicFont");
        }

        if (demonicFont == null)
        {
            demonicFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Chiller", "Viner Hand ITC", "Old English Text MT", "Blackadder ITC", "Georgia" },
                96
            );
        }

        if (skipMenuOnNextLoad)
        {
            skipMenuOnNextLoad = false;
            ResumeGameplay();
        }
        else
        {
            OpenMenu();
        }
    }

    private void Start()
    {
        if (IsMenuOpen)
        {
            PauseGameplayForMenu();
        }
    }

    private void Update()
    {
        if (!IsMenuOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (showOptions && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            showOptions = false;
        }
    }

    private void OpenMenu()
    {
        IsMenuOpen = true;
        showOptions = false;
        PauseGameplayForMenu();
    }

    private void PauseGameplayForMenu()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    private void ResumeGameplay()
    {
        IsMenuOpen = false;
        showOptions = false;
        Time.timeScale = 1f;

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void StartGame()
    {
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            ResumeGameplay();
            return;
        }

        CorridorEncounter.ResetRunState();
        Time.timeScale = 1f;
        skipMenuOnNextLoad = true;
        SceneManager.LoadScene(string.IsNullOrEmpty(gameSceneName) ? DefaultGameSceneName : gameSceneName);
    }

    public static void RestartCurrentGame()
    {
        CorridorEncounter.ResetRunState();
        Time.timeScale = 1f;
        skipMenuOnNextLoad = true;
        IsMenuOpen = false;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public static void ReturnToMenu()
    {
        CorridorEncounter.ResetRunState();
        Time.timeScale = 1f;
        skipMenuOnNextLoad = false;
        IsMenuOpen = true;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void OpenOptions()
    {
        showOptions = true;
    }

    public void CloseOptions()
    {
        showOptions = false;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void EnsureStyles()
    {
        int titleSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.14f), 64, 160);
        int menuSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.055f), 28, 64);

        if (sliderTrackTexture == null)
        {
            sliderTrackTexture = CreateSolidTexture(new Color(0.22f, 0.04f, 0.04f, 1f));
        }

        if (sliderThumbTexture == null)
        {
            sliderThumbTexture = CreateSolidTexture(new Color(0.78f, 0.08f, 0.08f, 1f));
        }

        if (sliderThumbHoverTexture == null)
        {
            sliderThumbHoverTexture = CreateSolidTexture(new Color(1f, 0.2f, 0.2f, 1f));
        }

        if (titleStyle == null || lastTitleFontSize != titleSize || lastMenuFontSize != menuSize)
        {
            lastTitleFontSize = titleSize;
            lastMenuFontSize = menuSize;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = demonicFont,
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
            titleStyle.normal.textColor = new Color(0.82f, 0.05f, 0.05f, 1f);

            titleGlowStyle = new GUIStyle(titleStyle);
            titleGlowStyle.normal.textColor = new Color(0.35f, 0f, 0f, 0.85f);

            titleShadowStyle = new GUIStyle(titleStyle);
            titleShadowStyle.normal.textColor = new Color(0.08f, 0f, 0f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = demonicFont,
                fontSize = menuSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };
            buttonStyle.normal.background = null;
            buttonStyle.hover.background = null;
            buttonStyle.active.background = null;
            buttonStyle.focused.background = null;
            buttonStyle.normal.textColor = new Color(0.88f, 0.85f, 0.85f, 1f);
            buttonStyle.hover.textColor = new Color(0.9f, 0.08f, 0.08f, 1f);
            buttonStyle.active.textColor = new Color(0.6f, 0.02f, 0.02f, 1f);
            buttonStyle.focused.textColor = new Color(0.9f, 0.08f, 0.08f, 1f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = demonicFont,
                fontSize = Mathf.Max(24, Mathf.RoundToInt(menuSize * 0.85f)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.88f, 0.85f, 0.85f, 1f);

            sliderTrackStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 10f,
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            sliderTrackStyle.normal.background = sliderTrackTexture;

            sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedWidth = 18f,
                fixedHeight = 26f,
                border = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 8, 8)
            };
            sliderThumbStyle.normal.background = sliderThumbTexture;
            sliderThumbStyle.hover.background = sliderThumbHoverTexture;
            sliderThumbStyle.active.background = sliderThumbHoverTexture;
        }
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {
        if (!IsMenuOpen)
            return;

        EnsureStyles();

        GUI.depth = -2000;

        // Full-screen black background
        Rect fullScreen = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.DrawTexture(fullScreen, Texture2D.blackTexture, ScaleMode.StretchToFill);

        // Top title: "The Tunnel Walk" in large demonic font
        string title = "The Tunnel Walk";
        Rect titleRect = new Rect(0f, Screen.height * 0.07f, Screen.width, Screen.height * 0.22f);

        GUI.Label(new Rect(titleRect.x + 5f, titleRect.y + 6f, titleRect.width, titleRect.height), title, titleShadowStyle);
        GUI.Label(new Rect(titleRect.x - 3f, titleRect.y - 2f, titleRect.width, titleRect.height), title, titleGlowStyle);
        GUI.Label(new Rect(titleRect.x + 3f, titleRect.y + 2f, titleRect.width, titleRect.height), title, titleGlowStyle);
        GUI.Label(titleRect, title, titleStyle);

        // Lower-left menu
        float leftMargin = Mathf.Max(60f, Screen.width * 0.08f);
        float menuTop = Screen.height * 0.46f;
        float itemHeight = Mathf.Max(46f, Screen.height * 0.08f);
        float itemSpacing = Mathf.Max(14f, Screen.height * 0.025f);
        float itemWidth = Mathf.Max(320f, Screen.width * 0.35f);

        if (!showOptions)
        {
            Rect startRect = new Rect(leftMargin, menuTop, itemWidth, itemHeight);
            Rect optionsRect = new Rect(leftMargin, menuTop + (itemHeight + itemSpacing), itemWidth, itemHeight);
            Rect exitRect = new Rect(leftMargin, menuTop + (itemHeight + itemSpacing) * 2f, itemWidth, itemHeight);

            if (DrawMenuButton(startRect, "Start Game"))
            {
                StartGame();
            }

            if (DrawMenuButton(optionsRect, "Options"))
            {
                OpenOptions();
            }

            if (DrawMenuButton(exitRect, "Exit"))
            {
                ExitGame();
            }
        }
        else
        {
            int volumePercent = Mathf.RoundToInt(volume * 100f);
            Rect labelRect = new Rect(leftMargin, menuTop, itemWidth, itemHeight);
            GUI.Label(labelRect, $"Sound Volume: {volumePercent}%", labelStyle);

            Rect sliderRect = new Rect(leftMargin, menuTop + itemHeight + itemSpacing * 0.5f, Mathf.Min(360f, itemWidth), 32f);
            float newVolume = GUI.HorizontalSlider(sliderRect, volume, 0f, 1f, sliderTrackStyle, sliderThumbStyle);
            if (!Mathf.Approximately(newVolume, volume))
            {
                SetVolume(newVolume);
            }

            Rect backRect = new Rect(leftMargin, menuTop + (itemHeight + itemSpacing) * 2f, itemWidth, itemHeight);
            if (DrawMenuButton(backRect, "Back"))
            {
                CloseOptions();
            }
        }
    }

    private bool DrawMenuButton(Rect rect, string text)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        string display = hovered ? $"> {text}" : text;
        return GUI.Button(rect, display, buttonStyle);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (sliderTrackTexture != null)
        {
            DestroyImmediate(sliderTrackTexture);
        }

        if (sliderThumbTexture != null)
        {
            DestroyImmediate(sliderThumbTexture);
        }

        if (sliderThumbHoverTexture != null)
        {
            DestroyImmediate(sliderThumbHoverTexture);
        }
    }
}
