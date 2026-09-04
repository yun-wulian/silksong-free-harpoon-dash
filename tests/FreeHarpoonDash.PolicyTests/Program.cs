using System;
using FreeHarpoonDash;

internal static class Program
{
    private static void Main()
    {
        AssertAimOverride(onGround: true, horizontal: 0f, vertical: 1f, expected: true, "ground pure up");
        AssertAimOverride(onGround: true, horizontal: 1f, vertical: 1f, expected: true, "ground up-right");
        AssertAimOverride(onGround: true, horizontal: -1f, vertical: 1f, expected: true, "ground up-left");
        AssertAimOverride(onGround: true, horizontal: 1f, vertical: 0f, expected: false, "ground horizontal");
        AssertAimOverride(onGround: true, horizontal: 0f, vertical: -1f, expected: false, "ground pure down");
        AssertAimOverride(onGround: true, horizontal: 1f, vertical: -1f, expected: false, "ground down-right");
        AssertAimOverride(onGround: true, horizontal: -1f, vertical: -1f, expected: false, "ground down-left");
        AssertAimOverride(onGround: false, horizontal: 0f, vertical: 1f, expected: true, "air pure up");
        AssertAimOverride(onGround: false, horizontal: 1f, vertical: 0f, expected: true, "air horizontal");
        AssertAimOverride(onGround: false, horizontal: 0f, vertical: -1f, expected: true, "air pure down");
        AssertAimOverride(onGround: false, horizontal: 1f, vertical: -1f, expected: true, "air down-right");

        AssertOctant(1f, 0f, 0, "right");
        AssertOctant(1f, 1f, 1, "up-right");
        AssertOctant(0f, 1f, 2, "up");
        AssertOctant(-1f, 1f, 3, "up-left");
        AssertOctant(-1f, 0f, 4, "left");
        AssertOctant(-1f, -1f, 5, "down-left");
        AssertOctant(0f, -1f, 6, "down");
        AssertOctant(1f, -1f, 7, "down-right");

        AssertDirection(0, 1f, 0f, "right");
        AssertDirection(1, 0.70710677f, 0.70710677f, "up-right");
        AssertDirection(2, 0f, 1f, "up");
        AssertDirection(3, -0.70710677f, 0.70710677f, "up-left");
        AssertDirection(4, -1f, 0f, "left");
        AssertDirection(5, -0.70710677f, -0.70710677f, "down-left");
        AssertDirection(6, 0f, -1f, "down");
        AssertDirection(7, 0.70710677f, -0.70710677f, "down-right");

        AssertChildAngle(0f, facingRight: true, expected: 0f, "right-facing horizontal");
        AssertChildAngle(180f, facingRight: false, expected: 0f, "left-facing horizontal");
        AssertChildAngle(45f, facingRight: true, expected: -45f, "right-facing up diagonal");
        AssertChildAngle(135f, facingRight: false, expected: -45f, "left-facing up diagonal");
        AssertChildAngle(-45f, facingRight: true, expected: 45f, "right-facing down diagonal");
        AssertChildAngle(-135f, facingRight: false, expected: 45f, "left-facing down diagonal");
        AssertChildAngle(90f, facingRight: true, expected: -90f, "up");
        AssertChildAngle(-90f, facingRight: true, expected: 90f, "down");

        AssertDebugUnlock(storyUnlocked: false, debugEnabled: false, expected: false);
        AssertDebugUnlock(storyUnlocked: false, debugEnabled: true, expected: true);
        AssertDebugUnlock(storyUnlocked: true, debugEnabled: false, expected: true);
        AssertDebugUnlock(storyUnlocked: true, debugEnabled: true, expected: true);

        AssertTerrainCollision(0f, -1f, targetIsTerrain: true, 0f, 1f, expected: true, "down into floor");
        AssertTerrainCollision(0.7071f, -0.7071f, targetIsTerrain: true, -1f, 0f, expected: true, "down-right into wall");
        AssertTerrainCollision(0.7071f, 0.7071f, targetIsTerrain: true, -1f, 0f, expected: true, "up-right into wall");
        AssertTerrainCollision(0f, 1f, targetIsTerrain: true, 0f, -1f, expected: true, "up into ceiling");
        AssertTerrainCollision(1f, 0f, targetIsTerrain: true, -1f, 0f, expected: false, "native horizontal into wall");
        AssertTerrainCollision(-0.7071f, 0.7071f, targetIsTerrain: true, -1f, 0f, expected: false, "diagonal moving away from wall");
        AssertTerrainCollision(0.7071f, -0.7071f, targetIsTerrain: false, -1f, 0f, expected: false, "diagonal into enemy");

        AssertSafeSpeed(requested: 70f, fixedDeltaTime: 0.02f, hitDistance: 2f, skin: 0.02f, expected: 70f, "floor beyond next step");
        AssertSafeSpeed(requested: 70f, fixedDeltaTime: 0.02f, hitDistance: 1f, skin: 0.02f, expected: 49f, "floor inside next step");
        AssertSafeSpeed(requested: 70f, fixedDeltaTime: 0.02f, hitDistance: 0.02f, skin: 0.02f, expected: 0f, "at safe contact");
        if (TerrainCollisionPolicy.TerrainStopEvent != "END")
        {
            throw new InvalidOperationException("Terrain collision must exit through END, not CATCH/To Needle.");
        }

        Console.WriteLine("Free Harpoon Dash policy tests passed.");
    }

    private static void AssertAimOverride(
        bool onGround,
        float horizontal,
        float vertical,
        bool expected,
        string scenario)
    {
        bool actual = HarpoonDirectionPolicy.ShouldEnableEightWayAim(
            onGround,
            horizontal,
            vertical);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Policy mismatch for {scenario}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertDebugUnlock(bool storyUnlocked, bool debugEnabled, bool expected)
    {
        bool actual = DebugUnlockPolicy.Resolve(storyUnlocked, debugEnabled);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Debug unlock mismatch for story={storyUnlocked}, debug={debugEnabled}: " +
                $"expected {expected}, got {actual}.");
        }
    }

    private static void AssertOctant(float horizontal, float vertical, int expected, string scenario)
    {
        int actual = HarpoonDirectionPolicy.SnapOctant(horizontal, vertical);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Direction mismatch for {scenario}: expected octant {expected}, got {actual}.");
        }
    }

    private static void AssertChildAngle(
        float worldAngle,
        bool facingRight,
        float expected,
        string scenario)
    {
        float actual = DashPosePolicy.GetDirectionalChildLocalAngle(worldAngle, facingRight);
        if (Math.Abs(actual - expected) > 0.001f)
        {
            throw new InvalidOperationException(
                $"Child angle mismatch for {scenario}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertDirection(int octant, float expectedX, float expectedY, string scenario)
    {
        HarpoonDirectionPolicy.GetUnitDirection(octant, out float actualX, out float actualY);
        if (Math.Abs(actualX - expectedX) > 0.000001f ||
            Math.Abs(actualY - expectedY) > 0.000001f)
        {
            throw new InvalidOperationException(
                $"Unit direction mismatch for {scenario}: " +
                $"expected ({expectedX}, {expectedY}), got ({actualX}, {actualY}).");
        }
    }

    private static void AssertTerrainCollision(
        float directionX,
        float directionY,
        bool targetIsTerrain,
        float normalX,
        float normalY,
        bool expected,
        string scenario)
    {
        bool actual = TerrainCollisionPolicy.IsNonHorizontalTerrainTarget(
            directionX,
            directionY,
            targetIsTerrain,
            normalX,
            normalY);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Ground collision mismatch for {scenario}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertSafeSpeed(
        float requested,
        float fixedDeltaTime,
        float hitDistance,
        float skin,
        float expected,
        string scenario)
    {
        float actual = TerrainCollisionPolicy.CalculateSafeSpeed(
            requested,
            fixedDeltaTime,
            hitDistance,
            skin);
        if (Math.Abs(actual - expected) > 0.001f)
        {
            throw new InvalidOperationException(
                $"Safe speed mismatch for {scenario}: expected {expected}, got {actual}.");
        }
    }
}
