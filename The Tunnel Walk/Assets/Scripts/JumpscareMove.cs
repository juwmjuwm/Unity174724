using UnityEngine;
using UnityEngine.Video;

public class JumpscareMove : MonoBehaviour
{
    public float speed = 5f;
    [SerializeField] private Transform player;
    [SerializeField] private VideoClip jumpscareVideo;
    [SerializeField] private AudioClip jumpscareSound;
    [SerializeField] private Texture2D gameOverScreen;
    [SerializeField] private float triggerDistance = 1.5f;

    private static readonly Vector3 ModelCenterOffset = new Vector3(0.65f, 0f, -0.75f);

    private static JumpscareMove instance;
    public static bool IsTriggered => instance != null && instance.triggered;
    private bool running;
    private bool triggered;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private RenderTexture renderTexture;
    private bool videoFinished;
    private Font menuFont;
    private GUIStyle endButtonStyle;
    private int lastButtonFontSize;

    private void Awake()
    {
        instance = this;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

    public static void RunAtPlayer(Transform target, Vector3 corridorEnd)
    {
        if (instance == null || instance.running || target == null)
            return;

        instance.player = target;
        instance.transform.position = corridorEnd;
        instance.running = true;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            renderer.enabled = true;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else if (Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }

        if (transform.childCount > 0)
        {
            Transform model = transform.GetChild(0);
            Vector3 modelWorldPos = model.TransformPoint(ModelCenterOffset);
            transform.position = new Vector3(modelWorldPos.x, transform.position.y, modelWorldPos.z);
            model.localRotation = Quaternion.Euler(0f, 90f, 0f);
            model.localPosition = new Vector3(0.75f, model.localPosition.y, 0.65f);
        }

        EnsureCollider();
        SetupMediaComponents();
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.center = new Vector3(0f, -2f, 0f);
            boxCollider.size = new Vector3(2.5f, 8f, 2.5f);
        }
    }

    private void SetupMediaComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (jumpscareVideo != null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = jumpscareVideo;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Prepare();
        }
    }

    void Update()
    {
        if (!running || triggered || player == null)
            return;

        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        float horizontalDistance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(player.position.x, player.position.z)
        );

        if (horizontalDistance <= triggerDistance)
        {
            TriggerJumpscare();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (running && !triggered && IsPlayerCollider(other))
        {
            TriggerJumpscare();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (running && !triggered && IsPlayerCollider(collision.collider))
        {
            TriggerJumpscare();
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return player != null && (other.transform == player || other.transform.IsChildOf(player));
    }

    private void TriggerJumpscare()
    {
        if (triggered)
            return;

        triggered = true;
        DisablePlayerControl();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        if (jumpscareSound != null && audioSource != null)
        {
            audioSource.clip = jumpscareSound;
            audioSource.Play();
        }

        if (videoPlayer != null && jumpscareVideo != null)
        {
            if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }
                renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
                videoPlayer.targetTexture = renderTexture;
            }
            videoPlayer.Play();
        }
        else
        {
            videoFinished = true;
            UnlockCursor();
        }
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DisablePlayerControl()
    {
        if (player == null)
            return;

        PlayerMovement movement = player.GetComponentInParent<PlayerMovement>();
        if (movement == null)
        {
            movement = player.GetComponent<PlayerMovement>();
        }
        if (movement != null)
        {
            movement.enabled = false;
        }

        CharacterController controller = player.GetComponentInParent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
        DisablePlayerControl();
        UnlockCursor();
    }

    private void EnsureEndScreenStyle()
    {
        if (menuFont == null)
        {
            menuFont = Resources.Load<Font>("DemonicFont");
            if (menuFont == null)
            {
                menuFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Chiller", "Viner Hand ITC", "Old English Text MT", "Blackadder ITC", "Georgia" },
                    64
                );
            }
        }

        int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.065f), 32, 76);
        if (endButtonStyle == null || lastButtonFontSize != fontSize)
        {
            lastButtonFontSize = fontSize;
            endButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = menuFont,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };
            endButtonStyle.normal.background = null;
            endButtonStyle.hover.background = null;
            endButtonStyle.active.background = null;
            endButtonStyle.focused.background = null;
            endButtonStyle.normal.textColor = new Color(0.85f, 0.82f, 0.82f, 1f);
            endButtonStyle.hover.textColor = new Color(0.9f, 0.08f, 0.08f, 1f);
            endButtonStyle.active.textColor = new Color(0.55f, 0.02f, 0.02f, 1f);
            endButtonStyle.focused.textColor = new Color(0.9f, 0.08f, 0.08f, 1f);
        }
    }

    private bool DrawEndScreenButton(Rect rect, string text)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        string display = hovered ? $"> {text} <" : text;
        return GUI.Button(rect, display, endButtonStyle);
    }

    private void OnGUI()
    {
        if (!triggered)
            return;

        GUI.depth = -1000;
        Rect fullScreen = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.DrawTexture(fullScreen, Texture2D.blackTexture, ScaleMode.StretchToFill);

        if (videoFinished)
        {
            UnlockCursor();

            if (gameOverScreen != null)
            {
                GUI.DrawTexture(fullScreen, gameOverScreen, ScaleMode.StretchToFill);
            }

            EnsureEndScreenStyle();

            float buttonWidth = Mathf.Max(360f, Screen.width * 0.3f);
            float buttonHeight = Mathf.Max(52f, Screen.height * 0.085f);
            float buttonSpacing = Mathf.Max(16f, Screen.height * 0.025f);
            float centerX = (Screen.width - buttonWidth) * 0.5f;
            float firstButtonY = Screen.height * 0.66f;

            Rect playAgainRect = new Rect(centerX, firstButtonY, buttonWidth, buttonHeight);
            Rect backToMenuRect = new Rect(centerX, firstButtonY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight);

            if (DrawEndScreenButton(playAgainRect, "Play Again?"))
            {
                MainMenu.RestartCurrentGame();
            }

            if (DrawEndScreenButton(backToMenuRect, "Back to menu"))
            {
                MainMenu.ReturnToMenu();
            }

            return;
        }

        if (videoPlayer != null && renderTexture != null && (videoPlayer.isPlaying || videoPlayer.frame > 0))
        {
            GUI.DrawTexture(fullScreen, renderTexture, ScaleMode.StretchToFill, false);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
