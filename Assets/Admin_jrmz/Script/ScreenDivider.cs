using UnityEngine;

public class ScreenDivider : MonoBehaviour
{
    public Color lineColor = Color.black;
    public float lineWidth = 4f;

    private Texture2D tex;

    void Awake()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, lineColor);
        tex.Apply();
    }

    void OnGUI()
    {
        float x = Screen.width / 2f - lineWidth / 2f;
        GUI.DrawTexture(new Rect(x, 0, lineWidth, Screen.height), tex);
    }
}
