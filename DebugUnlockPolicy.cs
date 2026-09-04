namespace FreeHarpoonDash;

internal static class DebugUnlockPolicy
{
    internal static bool Resolve(bool storyUnlocked, bool debugUnlockEnabled)
    {
        return storyUnlocked || debugUnlockEnabled;
    }
}
