using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct RenderNotesJob : IJob
{
    public float2 Size;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds;

    [ReadOnly] public NativeArray<Note> Notes;
    
    [WriteOnly] public NativeList<Vector2> Coords;

    [BurstCompile]
    public void Execute()
    {
        Vector2 coord = Vector2.zero;

        for (int i = 0; i < Notes.Length; i++)
        {
            float at = Notes[i].at;
            float pos = Notes[i].text;

            // Note.At is before timeline
            if (at < TimeInMilliseconds - 0.5f * LengthInMilliseconds)
            {
                continue;
            }
            
            // Note.At is after timeline
            if (at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                continue;
            }

            // Draw point
            coord.x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
            coord.y = pos * -(Size.y / 100);
            Coords.AddNoResize(coord);
        }
    }
}