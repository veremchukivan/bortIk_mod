using System;
using System.Reflection;
using UnityEngine;

public partial class GfMod
{
    // ==================== NO WIND ====================
    // WindSettings lives on WindManager.windSettings (instance field), not on GameManager.

    private void InitializeWindExtrasReflection()
    {
        if (windExtrasReflectionInitialized)
        {
            return;
        }

        windExtrasReflectionInitialized = true;

        try
        {
            // Find WindManager type
            Type wmType = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                wmType = asm.GetType("WindManager");
                if (wmType != null)
                {
                    break;
                }
            }

            if (wmType == null)
            {
                GfLog.Warning("[Gf] WindManager type not found");
                return;
            }

            // Find the WindManager instance in the scene
            UnityEngine.Object wmObj = UnityEngine.Object.FindFirstObjectByType(wmType);
            if (wmObj == null)
            {
                GfLog.Warning("[Gf] WindManager not found in scene");
                return;
            }

            // Get the windSettings field on WindManager
            FieldInfo wsField = wmType.GetField(
                "windSettings",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (wsField == null)
            {
                GfLog.Warning("[Gf] WindManager.windSettings field not found");
                return;
            }

            object ws = wsField.GetValue(wmObj);
            if (ws == null)
            {
                GfLog.Warning("[Gf] WindManager.windSettings value is null");
                return;
            }

            // Get forceScale field on WindSettings
            cachedWindForceScaleField = ws.GetType().GetField(
                "forceScale",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (cachedWindForceScaleField != null && cachedWindForceScaleField.FieldType == typeof(float))
            {
                savedWindForceScale = (float)cachedWindForceScaleField.GetValue(ws);
                cachedWindSettingsInstance = ws;
            }
            else
            {
                GfLog.Warning("[Gf] WindSettings.forceScale field not found");
            }
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Wind reflection error: " + ex.Message);
        }
    }

    private void ToggleNoWind()
    {
        noWindEnabled = !noWindEnabled;
        InitializeWindExtrasReflection();

        GfLog.Msg(string.Format("[Gf] No Wind {0} | ws={1} field={2}",
            noWindEnabled ? "ON" : "OFF",
            cachedWindSettingsInstance != null ? "OK" : "NULL",
            cachedWindForceScaleField   != null ? cachedWindForceScaleField.Name : "NULL"));

        if (noWindEnabled)
        {
            ApplyNoWindState();
        }
        else
        {
            RestoreWindState();
        }

        MarkHudDirty();
    }

    private void ApplyNoWindState()
    {
        if (cachedWindSettingsInstance == null || cachedWindForceScaleField == null)
        {
            return;
        }

        try
        {
            cachedWindForceScaleField.SetValue(cachedWindSettingsInstance, 0f);
        }
        catch
        {
        }
    }

    private void RestoreWindState()
    {
        if (cachedWindSettingsInstance == null || cachedWindForceScaleField == null)
        {
            return;
        }

        try
        {
            cachedWindForceScaleField.SetValue(cachedWindSettingsInstance, savedWindForceScale);
        }
        catch
        {
        }
    }

    // ==================== NO AIR DRAG ====================

    private void InitializeAirDragExtrasReflection()
    {
        if (airDragExtrasReflectionInitialized)
        {
            return;
        }

        airDragExtrasReflectionInitialized = true;

        try
        {
            object ballSettings = GetGolfBallSettingsObject();
            if (ballSettings == null)
            {
                GfLog.Warning("[Gf] GolfBallSettings is null");
                return;
            }

            Type t = ballSettings.GetType();

            // GolfBallSettings uses auto-property backing fields: <LinearAirDragFactor>k__BackingField
            cachedLinearAirDragField = t.GetField(
                "<LinearAirDragFactor>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (cachedLinearAirDragField != null && cachedLinearAirDragField.FieldType == typeof(float))
            {
                savedLinearAirDrag = (float)cachedLinearAirDragField.GetValue(ballSettings);
            }
            else
            {
                GfLog.Warning("[Gf] <LinearAirDragFactor>k__BackingField not found on " + t.Name);
            }
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] AirDrag reflection error: " + ex.Message);
        }
    }

    private void ToggleNoAirDrag()
    {
        noAirDragEnabled = !noAirDragEnabled;
        InitializeAirDragExtrasReflection();

        if (noAirDragEnabled)
        {
            ApplyNoAirDragState();
            GfLog.Msg(string.Format("[Gf] No Air Drag ON (saved={0:F5})", savedLinearAirDrag));
        }
        else
        {
            RestoreAirDragState();
            GfLog.Msg("[Gf] No Air Drag OFF");
        }

        MarkHudDirty();
    }

    private void ApplyNoAirDragState()
    {
        if (cachedLinearAirDragField == null)
        {
            return;
        }

        try
        {
            object ballSettings = GetGolfBallSettingsObject();
            if (ballSettings == null)
            {
                return;
            }

            cachedLinearAirDragField.SetValue(ballSettings, 0.00001f);
        }
        catch
        {
        }
    }

    private void RestoreAirDragState()
    {
        if (cachedLinearAirDragField == null)
        {
            return;
        }

        try
        {
            object ballSettings = GetGolfBallSettingsObject();
            if (ballSettings == null)
            {
                return;
            }

            cachedLinearAirDragField.SetValue(ballSettings, savedLinearAirDrag);
        }
        catch
        {
        }
    }

    // ==================== SPEED MULTIPLIER ====================

    private void InitializeMoveSpeedExtrasReflection()
    {
        if (moveSpeedExtrasReflectionInitialized)
        {
            return;
        }

        moveSpeedExtrasReflectionInitialized = true;

        try
        {
            Type gmType = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                gmType = asm.GetType("GameManager");
                if (gmType != null)
                {
                    break;
                }
            }

            if (gmType == null)
            {
                GfLog.Warning("[Gf] GameManager type not found");
                return;
            }

            cachedPlayerMovSettingsProperty = gmType.GetProperty(
                "PlayerMovementSettings",
                BindingFlags.Public | BindingFlags.Static);

            if (cachedPlayerMovSettingsProperty == null)
            {
                GfLog.Warning("[Gf] GameManager.PlayerMovementSettings property not found");
                return;
            }

            object pms = cachedPlayerMovSettingsProperty.GetValue(null, null);
            if (pms == null)
            {
                GfLog.Warning("[Gf] PlayerMovementSettings value is null");
                return;
            }

            Type pmsType = pms.GetType();

            // All fields are auto-property backing fields: <FieldName>k__BackingField
            cachedDefaultMoveSpeedField = pmsType.GetField(
                "<DefaultMoveSpeed>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (cachedDefaultMoveSpeedField != null && cachedDefaultMoveSpeedField.FieldType == typeof(float))
            {
                savedDefaultMoveSpeed = (float)cachedDefaultMoveSpeedField.GetValue(pms);
            }
            else
            {
                GfLog.Warning("[Gf] <DefaultMoveSpeed>k__BackingField not found on " + pmsType.Name);
            }

        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] MoveSpeed reflection error: " + ex.Message);
        }
    }

    private void ToggleSpeedMultiplier()
    {
        speedMultiplierEnabled = !speedMultiplierEnabled;
        InitializeMoveSpeedExtrasReflection();

        if (speedMultiplierEnabled)
        {
            ApplySpeedMultiplierState();
            GfLog.Msg(string.Format("[Gf] Speed Multiplier ON (x{0:F1}, base={1:F2})", speedMultiplierFactor, savedDefaultMoveSpeed));
        }
        else
        {
            RestoreSpeedState();
            GfLog.Msg("[Gf] Speed Multiplier OFF");
        }

        MarkHudDirty();
    }

    private void ApplySpeedMultiplierState()
    {
        if (cachedPlayerMovSettingsProperty == null || cachedDefaultMoveSpeedField == null)
        {
            return;
        }

        try
        {
            object pms = cachedPlayerMovSettingsProperty.GetValue(null, null);
            if (pms == null)
            {
                return;
            }

            cachedDefaultMoveSpeedField.SetValue(pms, savedDefaultMoveSpeed * speedMultiplierFactor);
        }
        catch
        {
        }
    }

    private void RestoreSpeedState()
    {
        if (cachedPlayerMovSettingsProperty == null || cachedDefaultMoveSpeedField == null)
        {
            return;
        }

        try
        {
            object pms = cachedPlayerMovSettingsProperty.GetValue(null, null);
            if (pms == null)
            {
                return;
            }

            cachedDefaultMoveSpeedField.SetValue(pms, savedDefaultMoveSpeed);
        }
        catch
        {
        }
    }

    // ==================== INFINITE AMMO ====================
    // Targets PlayerInventory.localPlayerSlotOverrides (client-side ammo tracking dict).
    // Every LateUpdate, for each slot that holds a non-None item, we set the override
    // to {itemType, 99} so the game always sees 99 uses left.  We also maintain a backup
    // dict so we can restore an item even after the server empties the SyncList slot.

    private void InitializeAmmoInventoryReflection()
    {
        if (ammoInventoryReflectionReady)
        {
            return;
        }

        ammoInventoryReflectionReady = true; // prevent re-running

        try
        {
            Type gmType = null;
            Type piType = null;
            Type isType = null;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (gmType == null) gmType = asm.GetType("GameManager");
                if (piType == null) piType = asm.GetType("PlayerInventory");
                if (isType == null) isType = asm.GetType("InventorySlot");
                if (gmType != null && piType != null && isType != null)
                {
                    break;
                }
            }

            if (gmType == null || piType == null || isType == null)
            {
                GfLog.Warning("[Gf] Ammo: required types not found (GM=" +
                    (gmType != null ? "ok" : "NULL") + " PI=" +
                    (piType != null ? "ok" : "NULL") + " IS=" +
                    (isType != null ? "ok" : "NULL") + ")");
                return;
            }

            cachedLocalPlayerInventoryProperty = gmType.GetProperty(
                "LocalPlayerInventory",
                BindingFlags.Public | BindingFlags.Static);

            cachedInventorySlotsSyncListField = piType.GetField(
                "slots",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            cachedInventoryOverridesDictField = piType.GetField(
                "localPlayerSlotOverrides",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            cachedInvSlotItemTypeField = isType.GetField(
                "itemType",
                BindingFlags.Public | BindingFlags.Instance);

            Type itemTypeEnumType = cachedInvSlotItemTypeField?.FieldType;
            if (itemTypeEnumType != null)
            {
                cachedInvSlotConstructor = isType.GetConstructor(
                    new Type[] { itemTypeEnumType, typeof(int) });
            }

            bool ok = cachedLocalPlayerInventoryProperty != null
                && cachedInventorySlotsSyncListField != null
                && cachedInventoryOverridesDictField != null
                && cachedInvSlotItemTypeField != null
                && cachedInvSlotConstructor != null;

            if (ok)
            {
                GfLog.Msg("[Gf] Ammo inventory reflection ready");
            }
            else
            {
                GfLog.Warning("[Gf] Ammo: one or more fields missing — " +
                    "inv=" + (cachedLocalPlayerInventoryProperty != null ? "ok" : "NULL") +
                    " slots=" + (cachedInventorySlotsSyncListField != null ? "ok" : "NULL") +
                    " overrides=" + (cachedInventoryOverridesDictField != null ? "ok" : "NULL") +
                    " itemType=" + (cachedInvSlotItemTypeField != null ? "ok" : "NULL") +
                    " ctor=" + (cachedInvSlotConstructor != null ? "ok" : "NULL"));
            }
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Ammo inventory reflection error: " + ex.Message);
        }
    }

    private void ToggleInfiniteAmmo()
    {
        infiniteAmmoEnabled = !infiniteAmmoEnabled;

        if (!infiniteAmmoEnabled)
        {
            ammoItemTypeBackup.Clear();
            GfInfiniteAmmoBridge.LocalPlayerInventory = null;
        }

        GfInfiniteAmmoBridge.EnsurePatchApplied();
        GfInfiniteAmmoBridge.IsActive = infiniteAmmoEnabled;

        GfLog.Msg(infiniteAmmoEnabled ? "[Gf] Infinite Item Usage ON" : "[Gf] Infinite Item Usage OFF");
        MarkHudDirty();
    }

    // Called every LateUpdate. Keeps GfInfiniteAmmoBridge.LocalPlayerInventory
    // current so the Harmony prefix can identify the local player's inventory and
    // skip DecrementUseFromSlotAt (preventing server-side ammo consumption).
    // The override-dict logic below remains as a belt-and-suspenders display fix.
    private void TickInfiniteAmmo()
    {
        if (!infiniteAmmoEnabled)
        {
            return;
        }

        if (!ammoInventoryReflectionReady)
        {
            InitializeAmmoInventoryReflection();
        }

        if (cachedLocalPlayerInventoryProperty == null
            || cachedInventorySlotsSyncListField == null
            || cachedInventoryOverridesDictField == null
            || cachedInvSlotItemTypeField == null
            || cachedInvSlotConstructor == null)
        {
            return;
        }

        try
        {
            object localInventory = cachedLocalPlayerInventoryProperty.GetValue(null, null);
            if (localInventory == null)
            {
                return;
            }

            GfInfiniteAmmoBridge.LocalPlayerInventory = localInventory;

            System.Collections.IList slotsList =
                cachedInventorySlotsSyncListField.GetValue(localInventory) as System.Collections.IList;
            System.Collections.IDictionary overridesDict =
                cachedInventoryOverridesDictField.GetValue(localInventory) as System.Collections.IDictionary;

            if (slotsList == null || overridesDict == null)
            {
                return;
            }

            for (int i = 0; i < slotsList.Count; i++)
            {
                // Read item type from the server-synced SyncList slot
                object slotItem = slotsList[i];
                object slotsItemType = slotItem != null
                    ? cachedInvSlotItemTypeField.GetValue(slotItem)
                    : null;

                // Read item type from the existing client-side override (if any)
                object existingOverride = overridesDict.Contains(i) ? overridesDict[i] : null;
                object overrideItemType = existingOverride != null
                    ? cachedInvSlotItemTypeField.GetValue(existingOverride)
                    : null;

                // Determine which item type to restore:
                // prefer override > SyncList > backup (in case server already emptied the slot)
                object itemTypeToUse = null;
                if (overrideItemType != null && (int)overrideItemType != 0)
                {
                    itemTypeToUse = overrideItemType;
                }
                else if (slotsItemType != null && (int)slotsItemType != 0)
                {
                    itemTypeToUse = slotsItemType;
                }
                else
                {
                    ammoItemTypeBackup.TryGetValue(i, out itemTypeToUse);
                }

                if (itemTypeToUse == null || (int)itemTypeToUse == 0)
                {
                    continue;
                }

                // Keep backup fresh
                ammoItemTypeBackup[i] = itemTypeToUse;

                // Set override to {itemType, 99} — game reads this via GetEffectiveSlot
                overridesDict[i] = cachedInvSlotConstructor.Invoke(
                    new object[] { itemTypeToUse, 99 });
            }
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Infinite ammo tick error: " + ex.Message);
        }
    }

    private void InitializeKnockbackImmunityReflection()
    {
        if (knockbackImmunityReflectionReady)
        {
            return;
        }

        knockbackImmunityReflectionReady = true;
        try
        {
            Type pmType = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                pmType = asm.GetType("PlayerMovement");
                if (pmType != null)
                {
                    break;
                }
            }

            if (pmType == null)
            {
                GfLog.Warning("[Gf] No Knockback: PlayerMovement type not found");
                return;
            }

            cachedKnockoutImmunityStatusField = pmType.GetField("knockoutImmunityStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cachedKnockoutImmunityStatusField == null)
            {
                GfLog.Warning("[Gf] No Knockback: knockoutImmunityStatus field not found");
                return;
            }

            Type immunityType = cachedKnockoutImmunityStatusField.FieldType;
            cachedHasImmunityField = immunityType.GetField("hasImmunity", BindingFlags.Public | BindingFlags.Instance);
            if (cachedHasImmunityField == null)
            {
                GfLog.Warning("[Gf] No Knockback: hasImmunity field not found on " + immunityType.Name);
            }
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Knockback immunity reflection error: " + ex.Message);
            knockbackImmunityReflectionReady = false;
        }
    }

    private void ToggleNoKnockback()
    {
        noKnockbackEnabled = !noKnockbackEnabled;
        GfNoKnockbackBridge.EnsurePatchApplied();
        GfNoKnockbackBridge.IsActive = noKnockbackEnabled;
        GfLog.Msg(noKnockbackEnabled ? "[Gf] No Knockback ON" : "[Gf] No Knockback OFF");
        MarkHudDirty();
    }

    private void ToggleLandmineImmunity()
    {
        landmineImmunityEnabled = !landmineImmunityEnabled;
        GfLandmineImmunityBridge.EnsurePatchApplied();
        GfLandmineImmunityBridge.IsActive = landmineImmunityEnabled;
        GfLog.Msg(landmineImmunityEnabled ? "[Gf] Landmine Immunity ON" : "[Gf] Landmine Immunity OFF");
        MarkHudDirty();
    }

    // Writes hasImmunity=true directly to the backing field (blocks knockout STATE),
    // and keeps GfNoKnockbackBridge.LocalPlayerHittable current so the Harmony
    // prefix on Hittable.HitWithItem can skip AddForceAtPosition entirely.
    private void TickNoKnockback()
    {
        bool needHittable = noKnockbackEnabled || landmineImmunityEnabled;

        if (!needHittable || playerMovement == null)
        {
            GfNoKnockbackBridge.LocalPlayerHittable = null;
            GfLandmineImmunityBridge.LocalPlayerHittable = null;
            return;
        }

        // Lazily resolve the Hittable component on the local player's hierarchy.
        if (localPlayerHittable == null)
        {
            // Walk up the parent chain first
            Transform t = playerMovement.transform;
            while (t != null && localPlayerHittable == null)
            {
                Component[] comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] != null && comps[i].GetType().Name == "Hittable")
                    {
                        localPlayerHittable = comps[i];
                        break;
                    }
                }
                t = t.parent;
            }

            // If not found in parents, check children
            if (localPlayerHittable == null)
            {
                Component[] comps = playerMovement.GetComponentsInChildren<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] != null && comps[i].GetType().Name == "Hittable")
                    {
                        localPlayerHittable = comps[i];
                        break;
                    }
                }
            }
        }

        GfNoKnockbackBridge.LocalPlayerHittable = noKnockbackEnabled ? localPlayerHittable : null;
        GfLandmineImmunityBridge.LocalPlayerHittable = landmineImmunityEnabled ? localPlayerHittable : null;

        if (!noKnockbackEnabled)
        {
            return;
        }

        if (!knockbackImmunityReflectionReady)
        {
            InitializeKnockbackImmunityReflection();
        }

        if (cachedKnockoutImmunityStatusField == null || cachedHasImmunityField == null)
        {
            return;
        }

        try
        {
            // GetValue boxes the KnockOutImmunity struct — we can modify it and write back
            object immunityStatus = cachedKnockoutImmunityStatusField.GetValue(playerMovement);
            if (immunityStatus == null)
            {
                return;
            }

            if (!(bool)cachedHasImmunityField.GetValue(immunityStatus))
            {
                cachedHasImmunityField.SetValue(immunityStatus, true);
                cachedKnockoutImmunityStatusField.SetValue(playerMovement, immunityStatus);
            }
        }
        catch
        {
        }
    }
}

// Harmony prefix on Hittable.HitWithItem (public) and
// Hittable.HitWithRocketLauncherBackBlast (public).
// Patching the public methods skips both the local execution (HitWithItemInternal)
// AND the Command to the server (CmdHitWithItem), providing complete protection.
// The prefix returns false (skip original) when active and the hit target is the
// local player, preventing the force from ever being applied.
internal static class GfNoKnockbackBridge
{
    internal static bool IsActive;
    internal static object LocalPlayerHittable;

    private static bool patchApplied;

    internal static void EnsurePatchApplied()
    {
        if (patchApplied)
        {
            return;
        }

        patchApplied = true;

        try
        {
            // This bridge depends on private game methods that differ between
            // releases. Keep the feature disabled rather than risking a broken
            // Harmony patch during startup.
            GfLog.Warning("[gf Mod] No Knockback is unavailable for this game build.");
            patchApplied = false;
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] No Knockback patch error: " + ex.Message);
            patchApplied = false;
        }
    }

    private static object[] BuildArgs(ParameterInfo[] patchParams, MethodInfo original, object harmonyPrefix)
    {
        object[] args = new object[patchParams.Length];
        args[0] = original;
        for (int p = 1; p < patchParams.Length; p++)
        {
            string pname = patchParams[p].Name != null
                ? patchParams[p].Name.ToLowerInvariant()
                : string.Empty;
            if (pname == "prefix")
            {
                args[p] = harmonyPrefix;
            }
        }

        return args;
    }

    // Return false = skip the original method (and its AddForceAtPosition call).
    // Return true  = run the original as normal.
    private static bool Prefix(object __instance)
    {
        if (!IsActive || LocalPlayerHittable == null)
        {
            return true;
        }

        return !ReferenceEquals(__instance, LocalPlayerHittable);
    }
}

// Harmony prefix on PlayerInventory.DecrementUseFromSlotAt.
// Normally this method decrements the client override AND sends CmdDecrementUseFromSlotAt
// to the server, which then removes the item when uses reach zero. Skipping the
// entire method for the local player prevents the server from ever decrementing,
// so ammo is never consumed.
internal static class GfInfiniteAmmoBridge
{
    internal static bool IsActive;
    internal static object LocalPlayerInventory;

    private static bool patchApplied;

    internal static void EnsurePatchApplied()
    {
        if (patchApplied)
        {
            return;
        }

        patchApplied = true;

        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Type piType = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                piType = assemblies[i].GetType("PlayerInventory");
                if (piType != null)
                {
                    break;
                }
            }

            if (piType == null)
            {
                GfLog.Warning("[Gf] Infinite Ammo patch: PlayerInventory type not found");
                patchApplied = false;
                return;
            }

            MethodInfo decrementMethod = piType.GetMethod(
                "DecrementUseFromSlotAt",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (decrementMethod == null)
            {
                GfLog.Warning("[Gf] Infinite Ammo patch: DecrementUseFromSlotAt not found");
                patchApplied = false;
                return;
            }

            Type harmonyType = null;
            Type harmonyMethodType = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (harmonyType == null)
                {
                    harmonyType = assemblies[i].GetType("HarmonyLib.Harmony");
                }

                if (harmonyMethodType == null)
                {
                    harmonyMethodType = assemblies[i].GetType("HarmonyLib.HarmonyMethod");
                }

                if (harmonyType != null && harmonyMethodType != null)
                {
                    break;
                }
            }

            if (harmonyType == null || harmonyMethodType == null)
            {
                GfLog.Warning("[Gf] Infinite Ammo patch: HarmonyLib types not found");
                patchApplied = false;
                return;
            }

            object harmony = Activator.CreateInstance(harmonyType, "gf.infiniteammo");

            MethodInfo prefixMethod = typeof(GfInfiniteAmmoBridge).GetMethod(
                "Prefix",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (prefixMethod == null)
            {
                GfLog.Warning("[Gf] Infinite Ammo patch: Prefix method not found");
                patchApplied = false;
                return;
            }

            object harmonyPrefix = Activator.CreateInstance(harmonyMethodType, prefixMethod);

            MethodInfo patchMethod = null;
            MethodInfo[] candidates = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < candidates.Length; i++)
            {
                MethodInfo m = candidates[i];
                if (m.Name != "Patch" || m.IsGenericMethod)
                {
                    continue;
                }

                ParameterInfo[] prms = m.GetParameters();
                if (prms.Length >= 2 && prms[0].ParameterType.FullName == "System.Reflection.MethodBase")
                {
                    patchMethod = m;
                    break;
                }
            }

            if (patchMethod == null)
            {
                GfLog.Warning("[Gf] Infinite Ammo patch: Harmony.Patch overload not found");
                patchApplied = false;
                return;
            }

            ParameterInfo[] patchParams = patchMethod.GetParameters();
            object[] args = new object[patchParams.Length];
            args[0] = decrementMethod;
            for (int p = 1; p < patchParams.Length; p++)
            {
                string pname = patchParams[p].Name != null
                    ? patchParams[p].Name.ToLowerInvariant()
                    : string.Empty;
                if (pname == "prefix")
                {
                    args[p] = harmonyPrefix;
                }
            }

            patchMethod.Invoke(harmony, args);
            GfLog.Msg("[Gf] Infinite Ammo patch: registered prefix on PlayerInventory.DecrementUseFromSlotAt");
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Infinite Ammo patch error: " + ex.Message);
            patchApplied = false;
        }
    }

    // Return false = skip DecrementUseFromSlotAt entirely (no client decrement,
    // no CmdDecrementUseFromSlotAt sent to server).
    private static bool Prefix(object __instance)
    {
        if (!IsActive || LocalPlayerInventory == null)
        {
            return true;
        }

        return !ReferenceEquals(__instance, LocalPlayerInventory);
    }
}

// Harmony prefix on Hittable.HitWithItem (public).
// When active and the hit target is the local player and the itemType is Landmine,
// the prefix returns false to skip the entire method — preventing both local
// execution and the Command to the server.
internal static class GfLandmineImmunityBridge
{
    internal static bool IsActive;
    internal static object LocalPlayerHittable;
    internal static object LandmineItemTypeValue;

    private static bool patchApplied;

    internal static void EnsurePatchApplied()
    {
        if (patchApplied)
        {
            return;
        }

        patchApplied = true;

        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Type hittableType = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                hittableType = assemblies[i].GetType("Hittable");
                if (hittableType != null)
                {
                    break;
                }
            }

            if (hittableType == null)
            {
                GfLog.Warning("[Gf] Landmine Immunity patch: Hittable type not found");
                patchApplied = false;
                return;
            }

            MethodInfo hitWithItem = hittableType.GetMethod(
                "HitWithItemInternal",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (hitWithItem == null)
            {
                GfLog.Warning("[Gf] Landmine Immunity patch: HitWithItemInternal method not found");
                patchApplied = false;
                return;
            }

            // Cache the Landmine enum value for comparison at runtime
            Type itemTypeEnum = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                itemTypeEnum = assemblies[i].GetType("ItemType");
                if (itemTypeEnum != null) break;
            }
            if (itemTypeEnum != null)
            {
                try { LandmineItemTypeValue = Enum.Parse(itemTypeEnum, "Landmine"); } catch { }
            }

            Type harmonyType = null;
            Type harmonyMethodType = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (harmonyType == null)
                {
                    harmonyType = assemblies[i].GetType("HarmonyLib.Harmony");
                }

                if (harmonyMethodType == null)
                {
                    harmonyMethodType = assemblies[i].GetType("HarmonyLib.HarmonyMethod");
                }

                if (harmonyType != null && harmonyMethodType != null)
                {
                    break;
                }
            }

            if (harmonyType == null || harmonyMethodType == null)
            {
                GfLog.Warning("[Gf] Landmine Immunity patch: HarmonyLib types not found");
                patchApplied = false;
                return;
            }

            object harmony = Activator.CreateInstance(harmonyType, "gf.landmineimmunity");

            MethodInfo prefixMethod = typeof(GfLandmineImmunityBridge).GetMethod(
                "Prefix",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (prefixMethod == null)
            {
                GfLog.Warning("[Gf] Landmine Immunity patch: Prefix method not found");
                patchApplied = false;
                return;
            }

            object harmonyPrefix = Activator.CreateInstance(harmonyMethodType, prefixMethod);

            MethodInfo patchMethod = null;
            MethodInfo[] candidates = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < candidates.Length; i++)
            {
                MethodInfo m = candidates[i];
                if (m.Name != "Patch" || m.IsGenericMethod)
                {
                    continue;
                }

                ParameterInfo[] prms = m.GetParameters();
                if (prms.Length >= 2 && prms[0].ParameterType.FullName == "System.Reflection.MethodBase")
                {
                    patchMethod = m;
                    break;
                }
            }

            if (patchMethod == null)
            {
                GfLog.Warning("[Gf] Landmine Immunity patch: Harmony.Patch overload not found");
                patchApplied = false;
                return;
            }

            ParameterInfo[] patchParams = patchMethod.GetParameters();
            object[] args = new object[patchParams.Length];
            args[0] = hitWithItem;
            for (int p = 1; p < patchParams.Length; p++)
            {
                string pname = patchParams[p].Name != null
                    ? patchParams[p].Name.ToLowerInvariant()
                    : string.Empty;
                if (pname == "prefix")
                {
                    args[p] = harmonyPrefix;
                }
            }

            patchMethod.Invoke(harmony, args);
            GfLog.Msg("[Gf] Landmine Immunity patch: registered prefix on Hittable.HitWithItemInternal");
        }
        catch (Exception ex)
        {
            GfLog.Warning("[Gf] Landmine Immunity patch error: " + ex.Message);
            patchApplied = false;
        }
    }

    // Return false = skip HitWithItem entirely when itemType is Landmine and
    // the target is the local player.
    // Return true  = run the original as normal.
    private static bool Prefix(object __instance, object itemType)
    {
        if (!IsActive || LocalPlayerHittable == null)
        {
            return true;
        }

        if (!ReferenceEquals(__instance, LocalPlayerHittable))
        {
            return true;
        }

        if (LandmineItemTypeValue == null || !Equals(itemType, LandmineItemTypeValue))
        {
            return true;
        }

        return false; // skip landmine hit on local player
    }
}
