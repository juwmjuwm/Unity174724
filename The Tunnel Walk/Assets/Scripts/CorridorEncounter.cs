using UnityEngine;

public class CorridorEncounter : MonoBehaviour
{
    [Min(1)] [SerializeField] private int corridorNumber = 1;
    [SerializeField] private bool collectExpected = true;
    [SerializeField] private GameObject enemyCubePrefab;
    [SerializeField] private GameObject brickWallPrefab;

    private static CorridorEncounter currentCorridor;
    public static int CurrentLevelNumber { get; private set; }
    private bool collected;
    private bool entered;
    private bool wallPending;
    private float entranceSide;

    public void MarkCollected()
    {
        collected = true;
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
        if (previousCorridor != null)
        {
            entranceSide = Mathf.Sign(transform.InverseTransformPoint(player.transform.position).x);
            wallPending = true;
            TryPlaceEntranceWall(player.transform.position);
        }

        if (previousCorridor != null &&
            previousCorridor.collectExpected != previousCorridor.collected)
        {
            SpawnEnemyAtEntrance(player.transform.position);
        }
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

    private void SpawnEnemyAtEntrance(Vector3 playerPosition)
    {
        if (enemyCubePrefab == null)
        {
            Debug.LogWarning("EnemyCube prefab is missing on " + name, this);
            return;
        }

        Vector3 playerLocalPosition = transform.InverseTransformPoint(playerPosition);
        float entranceSide = playerLocalPosition.x >= 0f ? 1f : -1f;
        Vector3 localPosition = new Vector3(entranceSide * 45f, 2f, 0f);
        GameObject enemy = Instantiate(enemyCubePrefab, transform);
        enemy.transform.localPosition = localPosition;
        enemy.transform.localRotation = Quaternion.identity;
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
