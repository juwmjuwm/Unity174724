using System.Collections.Generic;
using UnityEngine;

public class CorridorEncounter : MonoBehaviour
{
    [Header("Corridor Settings")]
    [Min(1)] [SerializeField] private int corridorNumber = 1;
    [SerializeField] private bool isDifferent;
    [SerializeField] private Color differentPaintingColor = Color.white;
    [SerializeField] private GameObject brickWallPrefab;

    [Header("Randomization")]
    [Tooltip("If enabled, a random set of corridors will be chosen to have differing painting colors each time the game is opened.")]
    [SerializeField] private bool randomizeCorridorsOnStart = true;
    [Tooltip("Number of corridors (excluding the first corridor) that should have a differing painting color.")]
    [Min(1)] [SerializeField] private int differingCorridorsCount = 4;

    private static CorridorEncounter currentCorridor;
    private static readonly HashSet<int> differentPaintingIndices = new HashSet<int>();
    private static bool corridorsInitialized;
    public static int CurrentLevelNumber { get; private set; }
    private bool collected;
    private bool entered;
    private bool wallPending;
    private float entranceSide;

    private static readonly Color[] DefaultDifferingColorPalette = new Color[]
    {
        new Color(1f, 0f, 0f),        // Red
        new Color(0f, 0.15f, 1f),     // Blue
        new Color(1f, 0.45f, 0f),     // Orange
        Color.white,                  // White
        new Color(1f, 0.92f, 0.016f), // Yellow
        new Color(0f, 1f, 1f),        // Cyan
        new Color(1f, 0f, 1f),        // Magenta
        new Color(0.2f, 1f, 0.2f)     // Lime
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRunState()
    {
        currentCorridor = null;
        differentPaintingIndices.Clear();
        CurrentLevelNumber = 0;
        corridorsInitialized = false;
    }

    private void Awake()
    {
        if (!corridorsInitialized)
        {
            corridorsInitialized = true;
            InitializeAllCorridors();
        }
    }

    private static void InitializeAllCorridors()
    {
        CorridorEncounter[] found = FindObjectsByType<CorridorEncounter>(FindObjectsInactive.Include);
        if (found == null || found.Length == 0)
            return;

        List<CorridorEncounter> allCorridors = new List<CorridorEncounter>(found);
        allCorridors.Sort((a, b) => a.corridorNumber.CompareTo(b.corridorNumber));

        CorridorEncounter firstCorridor = allCorridors.Find(c => c.corridorNumber == 1);
        if (firstCorridor == null && allCorridors.Count > 0)
            firstCorridor = allCorridors[0];

        bool shouldRandomize = firstCorridor != null ? firstCorridor.randomizeCorridorsOnStart : true;
        int targetCount = firstCorridor != null ? firstCorridor.differingCorridorsCount : 4;

        List<CorridorEncounter> candidateCorridors = allCorridors.FindAll(c => c != firstCorridor && c.corridorNumber > 1);

        if (shouldRandomize && candidateCorridors.Count > 0)
        {
            for (int i = 0; i < allCorridors.Count; i++)
            {
                allCorridors[i].isDifferent = false;
            }

            Shuffle(candidateCorridors);
            int countToSelect = Mathf.Clamp(targetCount, 1, candidateCorridors.Count);
            List<CorridorEncounter> chosenCorridors = candidateCorridors.GetRange(0, countToSelect);
            chosenCorridors.Sort((a, b) => a.corridorNumber.CompareTo(b.corridorNumber));

            List<Color> palette = new List<Color>(DefaultDifferingColorPalette);
            Shuffle(palette);

            for (int i = 0; i < chosenCorridors.Count; i++)
            {
                chosenCorridors[i].isDifferent = true;
                chosenCorridors[i].differentPaintingColor = palette[i % palette.Count];
            }

            List<string> chosenDescriptions = new List<string>(chosenCorridors.Count);
            for (int i = 0; i < chosenCorridors.Count; i++)
            {
                chosenDescriptions.Add($"Corridor {chosenCorridors[i].corridorNumber} (Level {chosenCorridors[i].corridorNumber - 1})");
            }
            Debug.Log($"[CorridorEncounter] Randomized differing corridors: {string.Join(", ", chosenDescriptions)}");
        }

        PaintingColorManager firstPainting = firstCorridor != null ? firstCorridor.GetComponent<PaintingColorManager>() : null;
        List<Color> baseColors = firstPainting != null && firstPainting.cubeColors != null ? new List<Color>(firstPainting.cubeColors) : null;

        differentPaintingIndices.Clear();

        for (int i = 0; i < allCorridors.Count; i++)
        {
            CorridorEncounter corridor = allCorridors[i];
            PaintingColorManager painting = corridor.GetComponent<PaintingColorManager>();

            if (corridor.isDifferent)
            {
                if (painting != null && baseColors != null)
                {
                    painting.cubeColors = new List<Color>(baseColors);
                }
                corridor.ApplyPaintingAnomaly(painting);
            }
            else
            {
                if (painting != null && baseColors != null)
                {
                    painting.cubeColors = new List<Color>(baseColors);
                    painting.ApplyColors();
                }
            }
        }
    }

    private void ApplyPaintingAnomaly(PaintingColorManager painting)
    {
        if (painting == null)
            painting = GetComponent<PaintingColorManager>();

        if (painting == null)
        {
            Debug.LogWarning("Painting colors are missing on " + name, this);
            return;
        }

        int count = Mathf.Min(painting.cubes.Count, painting.cubeColors.Count);
        int candidates = 0;
        for (int i = 0; i < count; i++)
        {
            if (painting.cubes[i] != null && !differentPaintingIndices.Contains(i) &&
                IsNoticeablyDifferent(painting.cubeColors[i], differentPaintingColor))
                candidates++;
        }

        // Reuse an index only if every suitable painting was already chosen.
        bool reuseIndex = false;
        if (candidates == 0)
        {
            reuseIndex = true;
            for (int i = 0; i < count; i++)
            {
                if (painting.cubes[i] != null && IsNoticeablyDifferent(painting.cubeColors[i], differentPaintingColor))
                    candidates++;
            }

            if (candidates == 0)
            {
                Debug.LogWarning("No painting can display a different color on " + name, this);
                return;
            }
        }

        int selected = Random.Range(0, candidates);
        for (int i = 0; i < count; i++)
        {
            if (painting.cubes[i] == null || !IsNoticeablyDifferent(painting.cubeColors[i], differentPaintingColor) ||
                (!reuseIndex && differentPaintingIndices.Contains(i)))
                continue;

            if (selected-- != 0)
                continue;

            painting.cubeColors[i] = differentPaintingColor;
            painting.ApplyColors();
            differentPaintingIndices.Add(i);
            break;
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static bool IsNoticeablyDifferent(Color original, Color changed)
    {
        float red = original.r - changed.r;
        float green = original.g - changed.g;
        float blue = original.b - changed.b;
        return red * red + green * green + blue * blue > 0.25f;
    }

    public void MarkCollected(Transform player)
    {
        if (collected)
            return;

        collected = true;
        if (!isDifferent)
            JumpscareMove.RunAtPlayer(player, JumpscareSpawnPosition());
    }

    public void Enter(Collider player)
    {
        if (!player.CompareTag("Player"))
            return;

        if (currentCorridor == this)
        {
            TryPlaceEntranceWall(player.transform.position);
            return;
        }

        CorridorEncounter previousCorridor = currentCorridor;
        currentCorridor = this;
        CurrentLevelNumber = corridorNumber - 1;

        // Re-entering a visited corridor must not create another enemy.
        if (entered)
            return;

        entered = true;
        entranceSide = Mathf.Sign(transform.InverseTransformPoint(player.transform.position).x);
        if (previousCorridor != null)
        {
            wallPending = true;
            TryPlaceEntranceWall(player.transform.position);
        }

        if (previousCorridor != null && previousCorridor.isDifferent && !previousCorridor.collected)
            JumpscareMove.RunAtPlayer(player.transform, JumpscareSpawnPosition());
    }

    private Vector3 JumpscareSpawnPosition()
    {
        // The corridor runs along local X. The threat emerges from its far end.
        return transform.TransformPoint(new Vector3(-entranceSide * 45f, 4f, 0f));
    }

    private void TryPlaceEntranceWall(Vector3 playerPosition)
    {
        if (!wallPending)
            return;

        // Wait until the player is fully past the wall's inner face.
        Vector3 localPlayerPosition = transform.InverseTransformPoint(playerPosition);
        if (localPlayerPosition.x * entranceSide > 49f)
            return;

        wallPending = false;
        if (brickWallPrefab == null)
        {
            Debug.LogWarning("BrickWall prefab is missing on " + name, this);
            return;
        }

        GameObject wall = Instantiate(brickWallPrefab, transform);
        wall.transform.localPosition = new Vector3(entranceSide * 50.5f, 6f, 0f);
        wall.transform.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        corridorsInitialized = false;
        differentPaintingIndices.Clear();
        if (currentCorridor == this)
        {
            currentCorridor = null;
            CurrentLevelNumber = 0;
        }
    }
}
