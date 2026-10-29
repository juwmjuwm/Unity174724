using System.Collections.Generic;
using UnityEngine;

public class CorridorEncounter : MonoBehaviour
{
    [Min(1)] [SerializeField] private int corridorNumber = 1;
    [SerializeField] private bool isDifferent;
    [SerializeField] private Color differentPaintingColor = Color.white;
    [SerializeField] private GameObject brickWallPrefab;

    private static CorridorEncounter currentCorridor;
    private static readonly HashSet<int> differentPaintingIndices = new HashSet<int>();
    public static int CurrentLevelNumber { get; private set; }
    private bool collected;
    private bool entered;
    private bool wallPending;
    private float entranceSide;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRunState()
    {
        currentCorridor = null;
        differentPaintingIndices.Clear();
        CurrentLevelNumber = 0;
    }

    private void Awake()
    {
        if (!isDifferent)
            return;

        PaintingColorManager painting = GetComponent<PaintingColorManager>();
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
        if (currentCorridor == this)
        {
            currentCorridor = null;
            CurrentLevelNumber = 0;
        }
    }
}
