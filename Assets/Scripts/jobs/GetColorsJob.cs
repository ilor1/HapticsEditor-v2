using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct GetColorsJob : IJob
{
    [ReadOnly] public Color32 ColorCenter;
    [ReadOnly] public Color32 ColorOuter;
    [ReadOnly] public NativeArray<float> LeftRmsValues;
    [ReadOnly] public NativeArray<float> RightRmsValues;
    [ReadOnly] public NativeArray<float> LeftHighestValues;
    [ReadOnly] public NativeArray<float> RightHighestValues;
    [ReadOnly] public int Width;
    [ReadOnly] public int Height;
    [ReadOnly] public int Offset;

    [WriteOnly] public NativeArray<Color32> Colors;

    [BurstCompile]
    public void Execute()
    {
        int height = Height / 2;

        for (int x = 0; x < Width; x++)
        {
            int leftHighStartY = (int)((0.5f - LeftHighestValues[x]) * height + Offset * 2);
            int leftHighEndY = (int)((0.5f + LeftHighestValues[x]) * height + Offset * 2);
            int leftRmsStartY = (int)((0.5f - LeftRmsValues[x]) * height + Offset * 2);
            int leftRmsEndY = (int)((0.5f + LeftRmsValues[x]) * height + Offset * 2);

            int rightHighStartY = (int)((0.5f - RightHighestValues[x]) * height);
            int rightHighEndY = (int)((0.5f + RightHighestValues[x]) * height);
            int rightRmsStartY = (int)((0.5f - RightRmsValues[x]) * height);
            int rightRmsEndY = (int)((0.5f + RightRmsValues[x]) * height);

            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < Height; y++)
            {
                Color32 color = clear;

                // Left (top)
                if ((y > leftRmsStartY && y < leftRmsEndY) || y == Height - Offset) color = ColorCenter;
                else if (y > leftHighStartY && y < leftHighEndY) color = ColorOuter;

                // Right (bottom)
                else if ((y > rightRmsStartY && y < rightRmsEndY) || y == Offset) color = ColorCenter;
                else if (y > rightHighStartY && y < rightHighEndY) color = ColorOuter;

                Colors[x + y * Width] = color;
            }
        }
    }
}