using UnityEngine;

public class LevitatingRotatingCube : MonoBehaviour
{
    [Min(0f)] public float levitationHeight = 0.25f;
    [Min(0f)] public float levitationFrequency = 1f;
    public float rotationSpeed = 45f;

    private Vector3 startingLocalPosition;
    private float elapsedTime;

    private void Awake()
    {
        startingLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float offset = Mathf.Sin(elapsedTime * levitationFrequency * Mathf.PI * 2f) * levitationHeight;
        transform.localPosition = startingLocalPosition + Vector3.up * offset;
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CorridorEncounter corridor = GetComponentInParent<CorridorEncounter>();
            if (corridor != null)
                corridor.MarkCollected();

            Destroy(gameObject);
        }
    }
}
