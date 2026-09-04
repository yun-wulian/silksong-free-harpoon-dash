using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FreeHarpoonDash;

internal static class EightWayHarpoonDash
{
    internal const float ThreadLength = 10.5f;
    private static readonly HashSet<GameObject> RotatedObjects = new();
    private static readonly HashSet<GameObject> RotatedDashObjects = new();
    private static readonly List<RaycastHit2D> TerrainHitCandidates = new(6);
    private static readonly RaycastHit2D[] TerrainCastResults = new RaycastHit2D[8];

    private static Vector2 direction = Vector2.right;
    private static Vector2 needleTarget;
    private static bool eightWayActive;
    private static bool nonHorizontalTerrainTarget;

    internal static float Angle => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    internal static Vector2 Direction => direction;

    internal static Vector2 NeedleTarget => needleTarget;

    internal static bool IsHarpoonDash(FsmStateAction action)
    {
        return string.Equals(action.Fsm?.Name, "Harpoon Dash", StringComparison.Ordinal);
    }

    internal static bool IsRingCheck(FsmStateAction action)
    {
        if (!IsEightWayActive(action))
        {
            return false;
        }

        string? stateName = action.State?.Name;
        return string.Equals(stateName, "Ring Check L", StringComparison.Ordinal) ||
               string.Equals(stateName, "Ring Check R", StringComparison.Ordinal);
    }

    internal static bool IsDirectionalDashState(FsmStateAction action)
    {
        if (!IsEightWayActive(action))
        {
            return false;
        }

        string? stateName = action.State?.Name;
        return string.Equals(stateName, "Dash", StringComparison.Ordinal) ||
               string.Equals(stateName, "Dash To Enemy", StringComparison.Ordinal);
    }

    internal static bool IsEightWayActive(FsmStateAction action)
    {
        return eightWayActive && IsHarpoonDash(action);
    }

    internal static void CaptureDirection()
    {
        ResetRotations();

        HeroController? hero = HeroController.instance;
        InputHandler? inputHandler = ManagerSingleton<InputHandler>.UnsafeInstance;
        Vector2 input = inputHandler?.inputActions?.MoveVector.Vector ?? Vector2.zero;

        if (hero != null &&
            !HarpoonDirectionPolicy.ShouldEnableEightWayAim(
                hero.cState.onGround,
                input.x,
                input.y))
        {
            return;
        }

        eightWayActive = true;

        if (input.sqrMagnitude <
            HarpoonDirectionPolicy.InputThreshold * HarpoonDirectionPolicy.InputThreshold)
        {
            direction = hero != null && !hero.cState.facingRight ? Vector2.left : Vector2.right;
            return;
        }

        int octant = HarpoonDirectionPolicy.SnapOctant(input.x, input.y);
        HarpoonDirectionPolicy.GetUnitDirection(octant, out float directionX, out float directionY);
        direction = new Vector2(directionX, directionY);

        if (hero == null)
        {
            return;
        }

        if (direction.x > 0f && !hero.cState.facingRight)
        {
            hero.FaceRight();
        }
        else if (direction.x < 0f && hero.cState.facingRight)
        {
            hero.FaceLeft();
        }
    }

    internal static void BeginRayCheck()
    {
        TerrainHitCandidates.Clear();
        nonHorizontalTerrainTarget = false;
    }

    internal static void RecordTerrainHit(RaycastHit2D hit)
    {
        TerrainHitCandidates.Add(hit);
    }

    internal static Vector2 RotateRayOrigin(Vector2 origin, Vector2 heroPosition, float originalDirection)
    {
        float baseAngle = originalDirection < 0f ? 180f : 0f;
        float deltaRadians = (Angle - baseAngle) * Mathf.Deg2Rad;
        float sine = Mathf.Sin(deltaRadians);
        float cosine = Mathf.Cos(deltaRadians);
        Vector2 offset = origin - heroPosition;

        return heroPosition + new Vector2(
            offset.x * cosine - offset.y * sine,
            offset.x * sine + offset.y * cosine);
    }

    internal static void SetNeedleTarget(HarpoonDashRayCheck action)
    {
        GameObject hero = action.Hero.GetSafe(action);
        if (hero == null)
        {
            return;
        }

        if (action.StoreHitObject.Value != null)
        {
            needleTarget = action.StoreHitPoint.Value;
            ResolveNonHorizontalTerrainTarget(action);
            return;
        }

        nonHorizontalTerrainTarget = false;
        Vector2 heroPosition = hero.transform.position;
        Vector2 origin = RotateRayOrigin(
            heroPosition + new Vector2(0f, 0.05f),
            heroPosition,
            action.Direction.Value);
        needleTarget = origin + direction * ThreadLength;
    }

    private static void ResolveNonHorizontalTerrainTarget(HarpoonDashRayCheck action)
    {
        nonHorizontalTerrainTarget = false;

        GameObject target = action.StoreHitObject.Value;
        if (target == null || target.layer != 8)
        {
            return;
        }

        Vector2 selectedPoint = action.StoreHitPoint.Value;
        float nearestPointDistance = float.PositiveInfinity;
        RaycastHit2D selectedHit = default;

        foreach (RaycastHit2D candidate in TerrainHitCandidates)
        {
            if (candidate.collider == null || candidate.collider.gameObject != target)
            {
                continue;
            }

            float pointDistance = (candidate.point - selectedPoint).sqrMagnitude;
            if (pointDistance < nearestPointDistance)
            {
                nearestPointDistance = pointDistance;
                selectedHit = candidate;
            }
        }

        nonHorizontalTerrainTarget = selectedHit.collider != null &&
                                     TerrainCollisionPolicy.IsNonHorizontalTerrainTarget(
                                         direction.x,
                                         direction.y,
                                         targetIsTerrain: true,
                                         selectedHit.normal.x,
                                         selectedHit.normal.y);
    }

    internal static void PositionNeedle(ActivateGameObject action)
    {
        string? stateName = action.State?.Name;
        if (!IsEightWayActive(action) ||
            stateName == null ||
            (!stateName.Contains("Wall Needle") &&
             !stateName.Contains("Enemy Needle") &&
             !stateName.Contains("Tink Needle") &&
             !stateName.Contains("Air Needle")))
        {
            return;
        }

        GameObject target = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (target == null || !string.Equals(target.name, "Harpoon Needle", StringComparison.Ordinal))
        {
            return;
        }

        Vector3 position = target.transform.position;
        target.transform.position = new Vector3(needleTarget.x, needleTarget.y, position.z);
        SetWorldRotation2D(target.transform, Angle);

        HeroController? hero = HeroController.instance;
        float horizontalSign = direction.x != 0f
            ? Mathf.Sign(direction.x)
            : hero != null && !hero.cState.facingRight ? -1f : 1f;
        Vector3 scale = target.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * horizontalSign;
        target.transform.localScale = scale;
        RotatedObjects.Add(target);
    }

    internal static void OrientBreaker(ActivateGameObject action)
    {
        if (!IsEightWayActive(action) ||
            action.State?.Name == null ||
            !action.State.Name.Contains("Throw"))
        {
            return;
        }

        GameObject target = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (target == null ||
            (!string.Equals(target.name, "Harpoon Breaker", StringComparison.Ordinal) &&
             !string.Equals(target.name, "Harpoon Breaker Extend", StringComparison.Ordinal)))
        {
            return;
        }

        HeroController? hero = HeroController.instance;
        float rotation = Angle;
        if (hero != null && !hero.cState.facingRight)
        {
            rotation += 180f;
        }

        SetWorldRotation2D(target.transform, rotation);
        RotatedObjects.Add(target);
    }

    internal static void OrientDashObject(ActivateGameObject action)
    {
        if (!IsDirectionalDashState(action))
        {
            return;
        }

        GameObject target = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (target == null ||
            (!string.Equals(target.name, "Harpoon Dash Damager", StringComparison.Ordinal) &&
             !string.Equals(target.name, "Hornet_harpoon_dash", StringComparison.Ordinal)))
        {
            return;
        }

        HeroController? hero = HeroController.instance;
        if (hero == null)
        {
            return;
        }

        float rotation = DashPosePolicy.GetDirectionalChildLocalAngle(
            Angle,
            hero.cState.facingRight);
        SetLocalRotation2D(target.transform, rotation);
        RotatedDashObjects.Add(target);
    }

    internal static void ProtectFromNonHorizontalTerrain(SetVelocityAsAngle action)
    {
        if (!nonHorizontalTerrainTarget || !IsDirectionalDashState(action))
        {
            return;
        }

        HeroController? hero = HeroController.instance;
        if (hero == null)
        {
            return;
        }

        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        BoxCollider2D collider = hero.GetComponent<BoxCollider2D>();
        if (body == null || collider == null)
        {
            return;
        }

        float requestedSpeed = body.linearVelocity.magnitude;
        float fixedDeltaTime = Time.fixedDeltaTime;
        float skin = Mathf.Max(Physics2D.defaultContactOffset * 2f, 0.02f);
        float castDistance = requestedSpeed * fixedDeltaTime + skin;
        if (requestedSpeed <= 0f || castDistance <= 0f)
        {
            return;
        }

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = 1 << 8,
            useTriggers = false,
        };

        int hitCount = collider.Cast(
            direction,
            filter,
            TerrainCastResults,
            castDistance);

        float nearestTerrainDistance = float.PositiveInfinity;
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit2D hit = TerrainCastResults[index];
            if (TerrainCollisionPolicy.IsNonHorizontalTerrainTarget(
                    direction.x,
                    direction.y,
                    targetIsTerrain: true,
                    hit.normal.x,
                    hit.normal.y) &&
                hit.distance < nearestTerrainDistance)
            {
                nearestTerrainDistance = hit.distance;
            }
        }

        if (float.IsPositiveInfinity(nearestTerrainDistance))
        {
            return;
        }

        float safeSpeed = TerrainCollisionPolicy.CalculateSafeSpeed(
            requestedSpeed,
            fixedDeltaTime,
            nearestTerrainDistance,
            skin);
        body.linearVelocity = direction * safeSpeed;

        if (safeSpeed <= 0.01f)
        {
            body.linearVelocity = Vector2.zero;
            action.Fsm.Event(TerrainCollisionPolicy.TerrainStopEvent);
        }
    }

    internal static void ResetDashObjects()
    {
        foreach (GameObject target in RotatedDashObjects)
        {
            if (target != null)
            {
                target.transform.localRotation = Quaternion.identity;
            }
        }

        RotatedDashObjects.Clear();
    }

    internal static void ResetRotations()
    {
        ResetDashObjects();

        foreach (GameObject target in RotatedObjects)
        {
            if (target != null)
            {
                target.transform.localRotation = Quaternion.identity;
            }
        }

        RotatedObjects.Clear();
        TerrainHitCandidates.Clear();
        eightWayActive = false;
        nonHorizontalTerrainTarget = false;
    }

    private static void SetWorldRotation2D(Transform transform, float angle)
    {
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.z = angle;
        transform.eulerAngles = eulerAngles;
    }

    private static void SetLocalRotation2D(Transform transform, float angle)
    {
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.z = angle;
        transform.localEulerAngles = eulerAngles;
    }
}

[HarmonyPatch(typeof(SendEventToRegister), nameof(SendEventToRegister.OnEnter))]
internal static class HarpoonDashDirectionCapturePatch
{
    private static void Prefix(SendEventToRegister __instance)
    {
        if (EightWayHarpoonDash.IsHarpoonDash(__instance) &&
            string.Equals(__instance.State?.Name, "Antic", StringComparison.Ordinal))
        {
            EightWayHarpoonDash.CaptureDirection();
        }
    }
}

[HarmonyPatch(typeof(HarpoonDashRayCheck), "CheckRay")]
internal static class HarpoonDashRayPatch
{
    private static readonly FieldInfo ResultsField =
        AccessTools.Field(typeof(HarpoonDashRayCheck), "results") ??
        throw new MissingFieldException(typeof(HarpoonDashRayCheck).FullName, "results");

    private static readonly Type HitCheckType =
        AccessTools.Inner(typeof(HarpoonDashRayCheck), "HitCheck") ??
        throw new MissingMemberException(typeof(HarpoonDashRayCheck).FullName, "HitCheck");

    private static readonly Type HitTypesType =
        AccessTools.Inner(typeof(HarpoonDashRayCheck), "HitTypes") ??
        throw new MissingMemberException(typeof(HarpoonDashRayCheck).FullName, "HitTypes");

    private static readonly FieldInfo HitTypeField =
        AccessTools.Field(HitCheckType, "HitType") ??
        throw new MissingFieldException(HitCheckType.FullName, "HitType");

    private static readonly FieldInfo HitField =
        AccessTools.Field(HitCheckType, "Hit") ??
        throw new MissingFieldException(HitCheckType.FullName, "Hit");

    private static bool Prefix(
        HarpoonDashRayCheck __instance,
        Vector2 origin,
        bool isTerrainCheck,
        ref object __result)
    {
        if (!EightWayHarpoonDash.IsRingCheck(__instance))
        {
            return true;
        }

        GameObject hero = __instance.Hero.GetSafe(__instance);
        if (hero == null)
        {
            return true;
        }

        origin = EightWayHarpoonDash.RotateRayOrigin(
            origin,
            hero.transform.position,
            __instance.Direction.Value);

        RaycastHit2D[] results = (RaycastHit2D[])ResultsField.GetValue(__instance);
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = isTerrainCheck ? 256 : 657408,
            useTriggers = true,
        };

        int hitCount = Physics2D.Raycast(
            origin,
            EightWayHarpoonDash.Direction,
            filter,
            results,
            EightWayHarpoonDash.ThreadLength);

        for (int index = 0; index < Mathf.Min(hitCount, results.Length); index++)
        {
            RaycastHit2D hit = results[index];
            Collider2D collider = hit.collider;

            if (collider.gameObject.layer == 11)
            {
                if (!HitTaker.TryGetHealthManager(collider.gameObject, out HealthManager healthManager) ||
                    !healthManager.IsInvincible ||
                    !healthManager.PreventInvincibleEffect ||
                    healthManager.InvincibleFromDirection == 2 ||
                    healthManager.InvincibleFromDirection == 4 ||
                    healthManager.InvincibleFromDirection == 7)
                {
                    __result = CreateHitCheck("Enemy", hit);
                    return false;
                }

                continue;
            }

            if (collider.CompareTag("Bounce Pod") ||
                collider.GetComponent<BouncePod>() != null ||
                collider.GetComponent<HarpoonHook>() != null)
            {
                __result = CreateHitCheck("BouncePod", hit);
                return false;
            }

            if (collider.CompareTag("Harpoon Ring"))
            {
                __result = CreateHitCheck("HarpoonRing", hit);
                return false;
            }

            TinkEffect? tinkEffect = collider.GetComponent<TinkEffect>();
            if (collider.gameObject.layer == 17 && tinkEffect != null && !tinkEffect.noHarpoonHook)
            {
                __result = CreateHitCheck("Tinker", hit);
                return false;
            }

            if (collider.gameObject.layer == 8)
            {
                EightWayHarpoonDash.RecordTerrainHit(hit);
                __result = CreateHitCheck("Terrain", hit);
                return false;
            }
        }

        __result = CreateHitCheck("None", default);
        return false;
    }

    private static object CreateHitCheck(string hitTypeName, RaycastHit2D hit)
    {
        object hitCheck = Activator.CreateInstance(HitCheckType) ??
                          throw new InvalidOperationException("Could not create HarpoonDashRayCheck.HitCheck.");
        HitTypeField.SetValue(hitCheck, Enum.Parse(HitTypesType, hitTypeName));
        HitField.SetValue(hitCheck, hit);
        return hitCheck;
    }
}

[HarmonyPatch(typeof(HarpoonDashRayCheck), nameof(HarpoonDashRayCheck.OnEnter))]
internal static class HarpoonDashRayResultPatch
{
    private static void Prefix(HarpoonDashRayCheck __instance)
    {
        if (EightWayHarpoonDash.IsRingCheck(__instance))
        {
            EightWayHarpoonDash.BeginRayCheck();
        }
    }

    private static void Postfix(HarpoonDashRayCheck __instance)
    {
        if (EightWayHarpoonDash.IsRingCheck(__instance))
        {
            EightWayHarpoonDash.SetNeedleTarget(__instance);
        }
    }
}

[HarmonyPatch(typeof(SetVelocityAsAngle), "DoSetVelocity")]
internal static class HarpoonDashTerrainCollisionPatch
{
    private static void Postfix(SetVelocityAsAngle __instance)
    {
        EightWayHarpoonDash.ProtectFromNonHorizontalTerrain(__instance);
    }
}

[HarmonyPatch(typeof(SetFloatValue), nameof(SetFloatValue.OnEnter))]
internal static class HarpoonDashTravelAnglePatch
{
    private static void Prefix(SetFloatValue __instance)
    {
        if (EightWayHarpoonDash.IsRingCheck(__instance))
        {
            __instance.floatValue.Value = EightWayHarpoonDash.Angle;
        }
    }
}

[HarmonyPatch(typeof(SetDamageEnemyDirection), nameof(SetDamageEnemyDirection.OnEnter))]
internal static class HarpoonDashDamageDirectionPatch
{
    private static void Prefix(SetDamageEnemyDirection __instance)
    {
        if (EightWayHarpoonDash.IsRingCheck(__instance))
        {
            __instance.damageDirection.Value = EightWayHarpoonDash.Angle;
        }
    }
}

[HarmonyPatch(typeof(ActivateGameObject), "DoActivateGameObject")]
internal static class HarpoonDashObjectTransformPatch
{
    private static void Prefix(ActivateGameObject __instance)
    {
        EightWayHarpoonDash.OrientDashObject(__instance);
    }

    private static void Postfix(ActivateGameObject __instance)
    {
        EightWayHarpoonDash.PositionNeedle(__instance);
        EightWayHarpoonDash.OrientBreaker(__instance);
    }
}

[HarmonyPatch(typeof(SendMessageOnExit), nameof(SendMessageOnExit.OnExit))]
internal static class HarpoonDashStateExitPatch
{
    private static void Prefix(SendMessageOnExit __instance)
    {
        if (EightWayHarpoonDash.IsDirectionalDashState(__instance))
        {
            EightWayHarpoonDash.ResetDashObjects();
        }
    }
}

[HarmonyPatch(typeof(Tk2dPlayAnimationWithEvents), nameof(Tk2dPlayAnimationWithEvents.OnExit))]
internal static class HarpoonDashCatchAnimationExitPatch
{
    private static void Postfix(Tk2dPlayAnimationWithEvents __instance)
    {
        if (EightWayHarpoonDash.IsEightWayActive(__instance) &&
            __instance.State?.Name != null &&
            __instance.State.Name.Contains("Catch") &&
            string.Equals(__instance.clipName.Value, "Harpoon Catch", StringComparison.Ordinal))
        {
            EightWayHarpoonDash.ResetRotations();
        }
    }
}

[HarmonyPatch(typeof(GetXDistance), "DoGetDistance")]
internal static class HarpoonDashDistancePatch
{
    private static bool Prefix(GetXDistance __instance)
    {
        if (!EightWayHarpoonDash.IsDirectionalDashState(__instance))
        {
            return true;
        }

        GameObject source = __instance.Fsm.GetOwnerDefaultTarget(__instance.gameObject);
        GameObject target = __instance.target.Value;
        if (source != null && target != null && __instance.storeResult != null)
        {
            __instance.storeResult.Value = Vector2.Distance(source.transform.position, target.transform.position);
        }

        return false;
    }
}

[HarmonyPatch(typeof(ClampVelocity2D), "DoClampVelocity")]
internal static class HarpoonDashVelocityClampPatch
{
    private static bool Prefix(ClampVelocity2D __instance)
    {
        return !EightWayHarpoonDash.IsEightWayActive(__instance) ||
               !string.Equals(__instance.State?.Name, "Dash", StringComparison.Ordinal);
    }
}

[HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.SendMessage), "DoSendMessage")]
internal static class HarpoonDashResetPatch
{
    private static void Postfix(HutongGames.PlayMaker.Actions.SendMessage __instance)
    {
        if (!EightWayHarpoonDash.IsHarpoonDash(__instance))
        {
            return;
        }

        string? stateName = __instance.State?.Name;
        if (string.Equals(stateName, "Cancel All", StringComparison.Ordinal) ||
            string.Equals(stateName, "Harpoon Dash End", StringComparison.Ordinal))
        {
            EightWayHarpoonDash.ResetRotations();
        }
    }
}
