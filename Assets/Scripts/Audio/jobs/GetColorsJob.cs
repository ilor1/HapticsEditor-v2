// Renders both left and right channel to one texture
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct GetColorsParallelJob : IJobParallelFor
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
    public void Execute(int colorIndex)
    {
        int x = colorIndex % Width;
        int y = colorIndex / Width;

        int height = Height / 2;
        int leftChannelOffset = Offset * 2;
        Color32 clear = new Color32(0, 0, 0, 0);
        
        // fill below with clears (won't have to do the more complex checks)
        int rightHighStartY = (int)((0.5f - RightHighestValues[x]) * height);
        if (y < rightHighStartY)
        {
            Colors[colorIndex] = clear;
            return;
        }

        // fill between with clears (won't have to do the more complex checks)
        int leftHighStartY = (int)((0.5f - LeftHighestValues[x]) * height + leftChannelOffset);
        int rightHighEndY = (int)((0.5f + RightHighestValues[x]) * height);
        if (y > rightHighEndY && y < leftHighStartY)
        {
            Colors[colorIndex] = clear;
            return;
        }

        // fill above with clears (won't have to do the more complex checks)
        int leftHighEndY = (int)((0.5f + LeftHighestValues[x]) * height + leftChannelOffset);
        while (y > leftHighEndY && y < Height)
        {
            Colors[colorIndex] = clear;
            return;
        }

        // fill center lines
        bool centerLine = y == Height - Offset || y == Offset;
        if (centerLine)
        {
            Colors[colorIndex] = ColorCenter;
            return;
        }

        int leftRmsStartY = (int)((0.5f - LeftRmsValues[x]) * height + leftChannelOffset);
        int leftRmsEndY = (int)((0.5f + LeftRmsValues[x]) * height + leftChannelOffset);

        int rightRmsStartY = (int)((0.5f - RightRmsValues[x]) * height);
        int rightRmsEndY = (int)((0.5f + RightRmsValues[x]) * height);

        bool rightChannelRms = y >= rightRmsStartY && y <= rightRmsEndY;
        bool leftChannelRms = y >= leftRmsStartY && y <= leftRmsEndY;
        
        if (leftChannelRms || rightChannelRms)
        {
            Colors[colorIndex] = ColorCenter;
        }
        else
        {
            Colors[colorIndex] = ColorOuter;
        }
    }
}