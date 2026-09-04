using System;

namespace FreeHarpoonDash;

internal static class HarpoonDirectionPolicy
{
    internal const float InputThreshold = 0.3f;
    internal const float DiagonalComponent = 0.70710677f;
    private const float VerticalOctantBoundary = 0.41421356f;

    internal static int SnapOctant(float horizontalInput, float verticalInput)
    {
        double angle = Math.Atan2(verticalInput, horizontalInput) * 180.0 / Math.PI;
        int octant = (int)Math.Round(angle / 45.0, MidpointRounding.ToEven);
        return (octant % 8 + 8) % 8;
    }

    internal static void GetUnitDirection(int octant, out float x, out float y)
    {
        switch (octant)
        {
            case 0:
                x = 1f;
                y = 0f;
                return;
            case 1:
                x = DiagonalComponent;
                y = DiagonalComponent;
                return;
            case 2:
                x = 0f;
                y = 1f;
                return;
            case 3:
                x = -DiagonalComponent;
                y = DiagonalComponent;
                return;
            case 4:
                x = -1f;
                y = 0f;
                return;
            case 5:
                x = -DiagonalComponent;
                y = -DiagonalComponent;
                return;
            case 6:
                x = 0f;
                y = -1f;
                return;
            case 7:
                x = DiagonalComponent;
                y = -DiagonalComponent;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(octant));
        }
    }

    internal static bool ShouldEnableEightWayAim(
        bool onGround,
        float horizontalInput,
        float verticalInput)
    {
        if (!onGround)
        {
            return true;
        }

        return verticalInput > InputThreshold &&
               Math.Abs(verticalInput) >= Math.Abs(horizontalInput) * VerticalOctantBoundary;
    }
}
