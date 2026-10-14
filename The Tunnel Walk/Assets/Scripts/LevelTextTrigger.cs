using UnityEngine;
using TMPro;

public class LevelTextTrigger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        // The player starts in the base corridor before trigger events are sent.
        if (levelNumber == 0)
            ShowLevel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ShowLevel();
        }
    }

    private void ShowLevel()
    {
        if (levelText == null)
            return;

        levelText.text = levelNumber == 0 ? "Poziom bazowy" : $"Poziom {levelNumber}";
        levelText.gameObject.SetActive(true);
    }
}
