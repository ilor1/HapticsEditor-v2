using UnityEngine;

public static class HapticColorUtility
{
    public static Color GetRandomLayerColor()
    {
        float hue = Random.value;
        float saturation = Random.Range(0.65f, 0.9f);
        float value = Random.Range(0.7f, 0.95f);
        Color color = Color.HSVToRGB(hue, saturation, value);
        color.a = 1f;
        return color;
    }
}
