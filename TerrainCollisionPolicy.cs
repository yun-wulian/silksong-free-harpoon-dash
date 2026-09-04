using System;

namespace FreeHarpoonDash;

internal static class TerrainCollisionPolicy
{
    internal const string TerrainStopEvent = "END";
    private const float DirectionEpsilon = 0.001f;
    private const float SurfaceApproachEpsilon = 0.001f;

    internal static bool IsNonHorizontalTerrainTarget(
        float directionX,
        float directionY,
        bool targetIsTerrain,
        float hitNormalX,
        float hitNormalY)
    {
        if (!targetIsTerrain || Math.Abs(directionY) <= DirectionEpsilon)
        {
            return false;
        }

        float approach = directionX * hitNormalX + directionY * hitNormalY;
        return approach < -SurfaceApproachEpsilon;
    }

    internal static float CalculateSafeSpeed(
        float requestedSpeed,
        float fixedDeltaTime,
        float hitDistance,
        float skin)
    {
        if (requestedSpeed <= 0f || fixedDeltaTime <= 0f)
        {
            return 0f;
        }

        float safeDistance = Math.Max(0f, hitDistance - skin);
        float maximumSafeSpeed = safeDistance / fixedDeltaTime;
        return Math.Min(requestedSpeed, maximumSafeSpeed);
    }
}
