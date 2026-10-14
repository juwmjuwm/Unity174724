using UnityEngine;

public class CorridorLevelDisplay : MonoBehaviour
{
    private GUIStyle labelStyle;
    private GUIStyle shadowStyle;

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = Color.black;
        }

        int level = CorridorEncounter.CurrentLevelNumber;
        string caption = level == 0 ? "Poziom bazowy" : $"Poziom {level}";
        Rect bounds = new Rect(Screen.width - 320f, 20f, 300f, 40f);
        GUI.Label(new Rect(bounds.x + 2f, bounds.y + 2f, bounds.width, bounds.height), caption, shadowStyle);
        GUI.Label(bounds, caption, labelStyle);
    }
}
