using UnityEngine;
using UnityEngine.InputSystem;

public partial class GfMod
{
    internal void GfInit()
    {
        LoadOrCreateConfig();
        WeatherSystemInit();
    }

    internal void GfUpdate()
    {
        float currentTime = Time.time;

        // Ensure the grant bridge is initialized so the server-side command
        // handler is registered on the host before any client sends a request.
        // EnsureInitialized is idempotent — after the first run it returns
        // immediately with a single bool check.
        GfGrantBridge.EnsureInitialized();
        GfWeatherBridge.EnsureHandlersRegistered();
        WeatherSystemUpdate();

        PollDispenserPickup();
        PollCrateReturn();

        InvalidateResolvedContextIfLost();
        HandleInput();

        if ((playerMovement == null || playerGolfer == null) && currentTime >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = currentTime + playerSearchInterval;
            ResolvePlayerContext();
        }

        EnsureLocalGolfBallReference(false);

        if (playerGolfer != null && currentTime >= nextIdealSwingCalculationTime)
        {
            nextIdealSwingCalculationTime = currentTime + idealSwingCalculationInterval;
            CalculateIdealSwingParameters(false);
        }

        EnsureVisualsInitialized();
        if (visualsInitialized)
        {
            UpdateTrails();
            UpdateHud();
        }

    }

    internal void GfLateUpdate()
    {
        TickInfiniteAmmo();
        TickNoKnockback();
        AutoAimCamera();

        if (assistEnabled && isLeftMousePressed && !autoReleaseTriggeredThisCharge)
        {
            AutoSwingRelease();
        }
        WeatherSystemLateUpdate();
    }

    private void HandleInput()
    {
        UpdateMouseState();
        HandleKeyboardShortcuts();
    }

    private void UpdateMouseState()
    {
        bool previousLeft = isLeftMousePressed;

        if (Mouse.current != null)
        {
            isLeftMousePressed = Mouse.current.leftButton.isPressed;
            isRightMousePressed = Mouse.current.rightButton.isPressed;
        }
        else
        {
            isLeftMousePressed = false;
            isRightMousePressed = false;
        }

        if (isLeftMousePressed && !previousLeft)
        {
            ResetChargeState();
            ResetTrailState();
            if (assistEnabled)
            {
                CalculateIdealSwingParameters(true);
            }
        }
        else if (!isLeftMousePressed && previousLeft)
        {
            ResetChargeState();
            DisableAutoAimCamera();
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (WasConfiguredKeyPressed(keyboard, assistToggleKey))
        {
            ToggleAssist();
        }

        if (WasConfiguredKeyPressed(keyboard, coffeeBoostKey))
        {
            AddCoffeeBoost();
        }

        if (WasConfiguredKeyPressed(keyboard, nearestBallModeKey))
        {
            ToggleNearestBallMode();
        }

        if (WasConfiguredKeyPressed(keyboard, unlockAllCosmeticsKey))
        {
            UnlockAllCosmetics();
        }

        if (WasConfiguredKeyPressed(keyboard, hudToggleKey))
        {
            ToggleHudVisibility();
        }

        if (WasConfiguredKeyPressed(keyboard, randomItemKey))
        {
            TryCollectFromCrate();
        }

        if (WasConfiguredKeyPressed(keyboard, iceToggleKey))
        {
            ToggleIceImmunity();
        }

        if (WasConfiguredKeyPressed(keyboard, noWindKey))
        {
            ToggleNoWind();
        }

        if (WasConfiguredKeyPressed(keyboard, noAirDragKey))
        {
            ToggleNoAirDrag();
        }

        if (WasConfiguredKeyPressed(keyboard, speedMultiplierKey))
        {
            ToggleSpeedMultiplier();
        }

        if (WasConfiguredKeyPressed(keyboard, infiniteAmmoKey))
        {
            ToggleInfiniteAmmo();
        }

        if (WasConfiguredKeyPressed(keyboard, noKnockbackKey))
        {
            ToggleNoKnockback();
        }

        if (WasConfiguredKeyPressed(keyboard, landmineImmunityKey))
        {
            ToggleLandmineImmunity();
        }

        if (WasConfiguredKeyPressed(keyboard, settingsKey))
        {
            ToggleRosettaSettings();
        }

        // Settings panel swallows all further input while open.
        if (settingsPanelOpen)
        {
            HandleSettingsPanelInput();
            return;
        }

        if (WasConfiguredKeyPressed(keyboard, itemSpawnerKey))
        {
            itemMenuOpen = !itemMenuOpen;
            UpdateItemMenuVisibility();
        }

        if (itemMenuOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                itemMenuOpen = false;
                UpdateItemMenuVisibility();
                return;
            }

            for (int i = 0; i < SpawnableItems.Length; i++)
            {
                if (keyboard[SpawnableItems[i].SelectionKey].wasPressedThisFrame)
                {
                    SpawnItemClientSide(SpawnableItems[i].TypeId);
                    itemMenuOpen = false;
                    UpdateItemMenuVisibility();
                    return;
                }
            }
        }

    }

    private void ToggleHudVisibility()
    {
        hudVisible = !hudVisible;
        ApplyHudVisibility();
    }

    private void ToggleAssist()
    {
        assistEnabled = !assistEnabled;
        MarkHudDirty();

        if (assistEnabled)
        {
            ResolvePlayerContext();
            FindHoleOnly(true);
            CalculateIdealSwingParameters(true);
        }
        else
        {
            DisableAutoAimCamera();
            ResetChargeState();
            ClearPredictedTrails(true);
        }
    }

    private void AddCoffeeBoost()
    {
        if (playerMovement == null || addSpeedBoostMethod == null)
        {
            ResolvePlayerContext();
        }

        if (playerMovement == null || addSpeedBoostMethod == null)
        {
            return;
        }

        try
        {
            cachedSpeedBoostArgs[0] = 500f;
            addSpeedBoostMethod.Invoke(playerMovement, cachedSpeedBoostArgs);
        }
        catch
        {
        }
    }

    private void ToggleNearestBallMode()
    {
        nearestAnyBallModeEnabled = !nearestAnyBallModeEnabled;
        nextNearestAnyBallResolveTime = 0f;
        MarkHudDirty();

        ResolvePlayerContext();
        EnsureLocalGolfBallReference(true);
        ResetTrailState();

        if (playerGolfer != null)
        {
            FindHoleOnly(true);
            CalculateIdealSwingParameters(true);
        }
    }

    private void InvalidateResolvedContextIfLost()
    {
        if (hadResolvedPlayerContext &&
            (playerMovement == null ||
             playerGolfer == null ||
             playerMovement.gameObject == null ||
             playerGolfer.gameObject == null))
        {
            playerFound = false;
            playerMovement = null;
            playerGolfer = null;
            golfBall = null;
            addSpeedBoostMethod = null;
            localPlayerHittable = null;
            lastBallResolveSource = "missing";
            hadResolvedPlayerContext = false;
            hadResolvedBallContext = false;
            ClearRuntimeState();
            return;
        }

        if (hadResolvedBallContext &&
            (golfBall == null || golfBall.gameObject == null))
        {
            golfBall = null;
            lastBallResolveSource = "missing";
            hadResolvedBallContext = false;
            ClearRuntimeState();
        }
    }

    private void ResetChargeState()
    {
        autoReleaseTriggeredThisCharge = false;
        autoChargeSequenceStarted = false;
        nextTryStartChargingTime = 0f;
        lastAutoSwingReleaseFrame = -1;
        lastObservedSwingPower = 0f;
    }

    private void ClearRuntimeState()
    {
        dispenserPickupPending = false;
        dispenserPickupLocalInventory = null;
        pendingCrateTeleport = false;
        crateReturnPending = false;
        settingsPanelOpen = false;
        keybindRebindMode = false;
        keybindRebindIndex = -1;
        UpdateSettingsPanelVisibility();
        DisableAutoAimCamera();
        ResetChargeState();
        ResetTrailState();
        nextGolfBallCacheRefreshTime = 0f;
        cachedGolfBalls.Clear();
        localPlayerHittable = null;
        nextPredictedPathRefreshTime = 0f;
        currentAimTargetPosition = Vector3.zero;
        currentSwingOriginPosition = Vector3.zero;
        windAimOffset = Vector3.zero;
        holePosition = Vector3.zero;
        flagPosition = Vector3.zero;
        nextHoleSearchTime = 0f;
        nextIdealSwingCalculationTime = 0f;
        cachedLocalPlayerDisplayName = "";
        nextDisplayNameRefreshTime = 0f;
        MarkHudDirty();
    }

    private void EnsureVisualsInitialized()
    {
        if (visualsInitialized)
        {
            return;
        }

        if (Time.realtimeSinceStartup < visualsInitializationDelay)
        {
            return;
        }

        CreateHud();
        EnsureTrailRenderers();
        ApplyTrailVisualSettings();
        visualsInitialized = true;
    }
}
