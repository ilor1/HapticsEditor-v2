using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct RenderFunActionJob : IJob
{
    public float2 Size;
    public int TimeInMilliseconds;
    public int LengthInMilliseconds;

    [ReadOnly] public NativeArray<FunAction> Actions;
    
    [WriteOnly] public NativeList<Vector2> Coords;

    [BurstCompile]
    public void Execute()
    {
        bool firstPoint = false;
        Vector2 coord = Vector2.zero;

        for (int i = 0; i < Actions.Length; i++)
        {
            float at = Actions[i].at;
            float pos = Actions[i].pos;

            // Action.At is before timeline
            if (at < TimeInMilliseconds - 0.5f * LengthInMilliseconds)
            {
                // if the last point is before the timeline start, draw a flat line
                if (i == Actions.Length - 1)
                {
                    coord.y = pos * -(Size.y / 100);
                    coord.x = 0;
                    Coords.AddNoResize(coord);

                    coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                    Coords.AddNoResize(coord);
                }

                continue;
            }

            // Get first point that is outside the screen
            if (!firstPoint && i > 0)
            {
                firstPoint = true;

                // Draw value at the start of the screen
                int at0 = Actions[i - 1].at;

                // if the first point is inside the timeline, we need to draw a separate coordinate at 0
                if (at0 > TimeInMilliseconds - 0.5f * LengthInMilliseconds)
                {
                    coord.x = 0;
                    coord.y = Actions[i - 1].pos * -(Size.y / 100);
                    Coords.AddNoResize(coord);
                }

                coord.x = (Actions[i - 1].at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
                coord.y = Actions[i - 1].pos * -(Size.y / 100);
                Coords.AddNoResize(coord);
            }

            // Draw point
            coord.x = (at - TimeInMilliseconds + LengthInMilliseconds * 0.5f) * (Size.x / LengthInMilliseconds);
            coord.y = pos * -(Size.y / 100);
            Coords.AddNoResize(coord);

            // Draw value at the end of the screen, when the last point is beyond timeline end
            if (i > 0 && at > TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                float t = (TimeInMilliseconds + 0.5f * LengthInMilliseconds - Actions[i - 1].at) / (Actions[i].at - Actions[i - 1].at);
                coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                coord.y = math.lerp(Actions[i - 1].pos, Actions[i].pos, t) * -(Size.y / 100);
                Coords.AddNoResize(coord);
                break;
            }

            // Draw value at the end of the screen, when the last point is inside timeline end
            if (i == Actions.Length - 1 && at < TimeInMilliseconds + 0.5f * LengthInMilliseconds)
            {
                // Add point to the end
                coord.x = LengthInMilliseconds * (Size.x / LengthInMilliseconds);
                coord.y = pos * -(Size.y / 100);
                Coords.AddNoResize(coord);
            }
        }
    }
}