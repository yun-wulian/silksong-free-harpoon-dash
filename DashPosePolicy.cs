namespace FreeHarpoonDash;

internal static class DashPosePolicy
{
    internal static float GetDirectionalChildLocalAngle(float worldAngle, bool facingRight)
    {
        float angle = facingRight ? -worldAngle : worldAngle - 180f;
        while (angle <= -180f)
        {
            angle += 360f;
        }

        while (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
