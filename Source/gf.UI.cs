using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class BirdieMod
{
    private void MarkHudDirty()
    {
        hudDirty = true;
        nextHudRefreshTime = 0f;
    }

    private void MarkTrailVisualSettingsDirty()
    {
        trailVisualSettingsDirty = true;
        actualTrailLineDirty = true;
        predictedTrailLineDirty = true;
        frozenTrailLineDirty = true;
    }

    private void SetHudText(TextMeshProUGUI textComponent, string nextValue, ref string cachedValue)
    {
        if (textComponent == null || string.Equals(cachedValue, nextValue, System.StringComparison.Ordinal))
        {
            return;
        }

        cachedValue = nextValue;
        textComponent.text = nextValue;
    }

    private void CreateHud()
    {
        if (hudCanvas != null)
        {
            return;
        }

        hudCanvas = new GameObject("BirdieHudCanvas");
        Canvas canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = hudCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        hudCanvas.AddComponent<GraphicRaycaster>();
        UnityEngine.Object.DontDestroyOnLoad(hudCanvas);

        leftHudText = CreateHudText(
            hudCanvas.transform,
            "BirdieHudLeft",
            new Vector2(0f, 0.86f),
            new Vector2(0.28f, 1f),
            new Vector2(14f, -12f),
            new Vector2(-14f, -14f),
            18,
            TextAlignmentOptions.TopLeft);

        centerHudText = CreateHudText(
            hudCanvas.transform,
            "BirdieHudCenter",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-64f, -34f),
            new Vector2(64f, -10f),
            12,
            TextAlignmentOptions.Center);

        rightHudText = CreateHudText(
            hudCanvas.transform,
            "BirdieHudRight",
            new Vector2(0.72f, 0.86f),
            new Vector2(1f, 1f),
            new Vector2(14f, -12f),
            new Vector2(-14f, -14f),
            18,
            TextAlignmentOptions.TopRight);

        bottomHudText = CreateHudText(
            hudCanvas.transform,
            "BirdieHudBottom",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-640f, 22f),
            new Vector2(640f, 72f),
            14,
            TextAlignmentOptions.Bottom);
        bottomHudText.textWrappingMode = TextWrappingModes.Normal;

        CreateItemMenuPanel(hudCanvas.transform);
        EnsureEventSystemExists();
        MarkHudDirty();
        UpdateHud(true);
    }

    private TextMeshProUGUI CreateHudText(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        text.outlineWidth = 0.2f;
        text.color = Color.white;
        return text;
    }

    private void UpdateHud(bool force = false)
    {
        if (leftHudText == null || centerHudText == null || rightHudText == null || bottomHudText == null)
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        if (!force && !hudDirty && currentTime < nextHudRefreshTime)
        {
            return;
        }

        nextHudRefreshTime = currentTime + hudRefreshInterval;
        hudDirty = false;

        SetHudText(leftHudText, BuildLeftHudText(), ref cachedLeftHudText);
        SetHudText(centerHudText, BuildCenterHudText(), ref cachedCenterHudText);
        SetHudText(rightHudText, BuildRightHudText(), ref cachedRightHudText);
        SetHudText(bottomHudText, BuildBottomHudText(), ref cachedBottomHudText);
    }

    private string BuildLeftHudText()
    {
        StringBuilder builder = new StringBuilder(128);

        if (hudShowPlayerInfo)
        {
            builder.Append("<b><color=#8ED9FF>Player</color></b>\n");

            string displayName = GetLocalPlayerDisplayName();
            if (playerFound && playerMovement != null)
            {
                builder.Append("<color=#FFFFFF>").Append(ModTextHelper.EscapeRichText(displayName)).Append("</color>");
            }
            else
            {
                builder.Append("<color=#A8A8A8>Searching...</color>");
            }
        }

        if (hudShowBallDistance && golfBall != null && holePosition != Vector3.zero)
        {
            float dist = Vector3.Distance(golfBall.transform.position, holePosition);
            if (hudShowPlayerInfo)
            {
                builder.Append("\n");
            }

            builder.Append("<color=#A8A8A8>hole </color><color=#FFD06A>")
                   .Append(dist.ToString("F1"))
                   .Append("m</color>");
        }

        return builder.ToString();
    }

    private string BuildCenterHudText()
    {
        if (pendingCrateTeleport)
        {
            return "<b><color=#FF8C00>Press <color=#FFFFFF>[" +
                   ModTextHelper.EscapeRichText(randomItemKeyLabel) +
                   "]</color> again to teleport to crate</color></b>\n" +
                   "<color=#FF8C00><size=10>Server pickup not guaranteed.</size></color>";
        }

        if (!hudShowCenterTitle)
        {
            return "";
        }

        return "<b><color=#FFB255>Birdie</color></b>";
    }

    private string BuildRightHudText()
    {
        StringBuilder builder = new StringBuilder(128);
        string assistColor = assistEnabled ? "#39FF8F" : "#FF5E5E";
        builder.Append("<b><color=").Append(assistColor).Append(">Assist [")
               .Append(ModTextHelper.EscapeRichText(assistToggleKeyLabel))
               .Append("]</color></b>");

        if (hudShowIceIndicator && iceImmunityEnabled)
        {
            builder.Append("\n<b><color=#7FE8FF>Ice Immunity [")
                   .Append(ModTextHelper.EscapeRichText(iceToggleKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (noWindEnabled)
        {
            builder.Append("\n<b><color=#A8E8FF>No Wind [")
                   .Append(ModTextHelper.EscapeRichText(noWindKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (noAirDragEnabled)
        {
            builder.Append("\n<b><color=#B8FFB8>No Air Drag [")
                   .Append(ModTextHelper.EscapeRichText(noAirDragKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (speedMultiplierEnabled)
        {
            builder.Append("\n<b><color=#FFB255>Speed x")
                   .Append(speedMultiplierFactor.ToString("F1"))
                   .Append(" [")
                   .Append(ModTextHelper.EscapeRichText(speedMultiplierKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (infiniteAmmoEnabled)
        {
            builder.Append("\n<b><color=#FF8CE8>Inf Item Usage [")
                   .Append(ModTextHelper.EscapeRichText(infiniteAmmoKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (noKnockbackEnabled)
        {
            builder.Append("\n<b><color=#C8A0FF>No Knockback [")
                   .Append(ModTextHelper.EscapeRichText(noKnockbackKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (landmineImmunityEnabled)
        {
            builder.Append("\n<b><color=#FFAA44>Landmine Imm [")
                   .Append(ModTextHelper.EscapeRichText(landmineImmunityKeyLabel))
                   .Append("] ON</color></b>");
        }

        if (nearestAnyBallModeEnabled)
        {
            builder.Append("\n<b><color=#39FF8F>Nearest Ball [")
                   .Append(ModTextHelper.EscapeRichText(nearestBallModeKeyLabel))
                   .Append("] ON</color></b>");
        }

        return builder.ToString();
    }

    private string BuildBottomHudText()
    {
        if (!hudShowBottomBar)
        {
            return "";
        }

        string assistHint = assistEnabled
            ? "<color=#8ED9FF>Hold RMB to aim camera</color>"
            : "<color=#A8A8A8>Press " + ModTextHelper.EscapeRichText(assistToggleKeyLabel) + " to enable assist</color>";
        string nearestBallColor = nearestAnyBallModeEnabled ? "#39FF8F" : "#A8A8A8";
        string hudToggleColor = hudVisible ? "#FFD06A" : "#FF5E5E";
        string iceColor = iceImmunityEnabled ? "#7FE8FF" : "#A8A8A8";
        return assistHint +
               "  <color=#FFD06A>|</color>  <color=#FFD06A>" + ModTextHelper.EscapeRichText(coffeeBoostKeyLabel) + " - speed</color>" +
               "  <color=#FFD06A>|</color>  <color=" + nearestBallColor + ">" + ModTextHelper.EscapeRichText(nearestBallModeKeyLabel) + " - nearest ball</color>" +
               "  <color=#FFD06A>|</color>  <color=#FFD06A>" + ModTextHelper.EscapeRichText(unlockAllCosmeticsKeyLabel) + " - cosmetics</color>" +
               "  <color=#FFD06A>|</color>  <color=#FFD06A>" + ModTextHelper.EscapeRichText(randomItemKeyLabel) + " - crate</color>" +
               "  <color=#FFD06A>|</color>  <color=" + iceColor + ">" + ModTextHelper.EscapeRichText(iceToggleKeyLabel) + " - ice</color>" +
               "  <color=#FFD06A>|</color>  <color=#FFD06A>" + ModTextHelper.EscapeRichText(settingsKeyLabel) + " - settings</color>" +
               "  <color=#FFD06A>|</color>  <color=" + hudToggleColor + ">" + ModTextHelper.EscapeRichText(hudToggleKeyLabel) + " - hud</color>";
    }

    private void EnsureTrailRenderers()
    {
        bool createdRenderer = false;
        if (shotPathLine == null)
        {
            CreateActualTrailRenderer();
            createdRenderer = true;
        }

        if (predictedPathLine == null)
        {
            CreatePredictedTrailRenderer();
            createdRenderer = true;
        }

        if (frozenPredictedPathLine == null)
        {
            CreateFrozenTrailRenderer();
            createdRenderer = true;
        }

        if (createdRenderer || trailVisualSettingsDirty)
        {
            ApplyTrailVisualSettings();
            trailVisualSettingsDirty = false;
        }
    }

    private void CreateActualTrailRenderer()
    {
        shotPathLine = CreateTrailRenderer(
            "BirdieShotTrail", 0.06f, 0.04f, 10, 32760,
            new Color(1f, 0.58f, 0.20f, 1f), 5000,
            out shotPathObject, out shotPathMaterial);
    }

    private void CreatePredictedTrailRenderer()
    {
        predictedPathLine = CreateTrailRenderer(
            "BirdiePredictedTrail", 0.03f, 0.02f, 8, 32759,
            new Color(0.36f, 0.95f, 0.46f, 0.95f), 4999,
            out predictedPathObject, out predictedPathMaterial);
    }

    private void CreateFrozenTrailRenderer()
    {
        frozenPredictedPathLine = CreateTrailRenderer(
            "BirdieFrozenTrail", 0.034f, 0.024f, 8, 32758,
            new Color(0.36f, 0.74f, 1f, 0.92f), 4998,
            out frozenPredictedPathObject, out frozenPredictedPathMaterial);
    }

    private LineRenderer CreateTrailRenderer(
        string objectName,
        float startWidth,
        float endWidth,
        int capVertices,
        int sortingOrder,
        Color color,
        int renderQueue,
        out GameObject trailObject,
        out Material trailMaterial)
    {
        trailObject = new GameObject(objectName);
        UnityEngine.Object.DontDestroyOnLoad(trailObject);

        LineRenderer lineRenderer = trailObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = capVertices;
        lineRenderer.numCornerVertices = capVertices;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = sortingOrder;

        trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = color;
        trailMaterial.renderQueue = renderQueue;
        if (trailMaterial.HasProperty("_ZWrite"))
        {
            trailMaterial.SetInt("_ZWrite", 0);
        }
        if (trailMaterial.HasProperty("_ZTest"))
        {
            trailMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        lineRenderer.material = trailMaterial;
        return lineRenderer;
    }

    private void ResetTrailState()
    {
        shotPathPoints.Clear();
        predictedPathPoints.Clear();
        frozenPredictedPathPoints.Clear();
        isRecordingShotPath = false;
        observedBallMotionSinceLastShot = false;
        lockLivePredictedPath = false;
        predictedPathCacheValid = false;
        predictedTrajectoryHideStartTime = 0f;
        lastShotPathMoveTime = 0f;
        lastShotPathBallPosition = golfBall != null ? golfBall.transform.position + Vector3.up * shotPathHeightOffset : Vector3.zero;
        actualTrailLineDirty = false;
        predictedTrailLineDirty = false;
        frozenTrailLineDirty = false;

        if (shotPathLine != null)
        {
            shotPathLine.positionCount = 0;
        }
        if (predictedPathLine != null)
        {
            predictedPathLine.positionCount = 0;
        }
        if (frozenPredictedPathLine != null)
        {
            frozenPredictedPathLine.positionCount = 0;
        }

    }

    private void SyncLineRendererPoints(LineRenderer lineRenderer, System.Collections.Generic.List<Vector3> points, ref bool lineDirty, bool allowIncrementalAppend)
    {
        if (lineRenderer == null)
        {
            return;
        }

        int pointCount = points.Count;
        if (pointCount <= 0)
        {
            if (lineRenderer.positionCount != 0)
            {
                lineRenderer.positionCount = 0;
            }
            lineDirty = false;
            return;
        }

        if (!lineDirty && allowIncrementalAppend && lineRenderer.positionCount == pointCount - 1)
        {
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPosition(pointCount - 1, points[pointCount - 1]);
            return;
        }

        if (lineRenderer.positionCount != pointCount)
        {
            lineRenderer.positionCount = pointCount;
        }

        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }

        lineDirty = false;
    }

    private void ApplyActualTrailToLine()
    {
        if (shotPathLine == null || !actualTrailEnabled)
        {
            if (shotPathLine != null)
            {
                shotPathLine.positionCount = 0;
            }
            return;
        }

        SyncLineRendererPoints(shotPathLine, shotPathPoints, ref actualTrailLineDirty, true);
    }

    private void ApplyPredictedTrailToLine()
    {
        if (predictedPathLine == null || !predictedTrailEnabled)
        {
            if (predictedPathLine != null)
            {
                predictedPathLine.positionCount = 0;
            }
            return;
        }

        SyncLineRendererPoints(predictedPathLine, predictedPathPoints, ref predictedTrailLineDirty, false);
    }

    private void ApplyFrozenTrailToLine()
    {
        if (frozenPredictedPathLine == null || !frozenTrailEnabled)
        {
            if (frozenPredictedPathLine != null)
            {
                frozenPredictedPathLine.positionCount = 0;
            }
            return;
        }

        SyncLineRendererPoints(frozenPredictedPathLine, frozenPredictedPathPoints, ref frozenTrailLineDirty, false);
    }

    private void ClearPredictedTrails(bool hideFrozenSnapshot)
    {
        predictedPathPoints.Clear();
        predictedPathCacheValid = false;
        predictedTrailLineDirty = false;
        if (predictedPathLine != null)
        {
            predictedPathLine.positionCount = 0;
        }

        if (hideFrozenSnapshot)
        {
            frozenPredictedPathPoints.Clear();
            frozenTrailLineDirty = false;
            if (frozenPredictedPathLine != null)
            {
                frozenPredictedPathLine.positionCount = 0;
            }
        }
    }

    // ── Item spawner menu ────────────────────────────────────────────────────

    // Creates a centered overlay panel that lists all spawnable items with their
    // key shortcuts. Hidden by default; shown when itemMenuOpen is true.
    private void CreateItemMenuPanel(Transform parent)
    {
        if (itemMenuPanelObject != null)
        {
            return;
        }

        itemMenuPanelObject = new GameObject("BirdieItemMenuPanel");
        itemMenuPanelObject.transform.SetParent(parent, false);

        // Semi-transparent dark background, centered on screen.
        RectTransform panelRect = itemMenuPanelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = new Vector2(-310f, -230f);
        panelRect.offsetMax = new Vector2(310f, 230f);

        Image background = itemMenuPanelObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.095f, 0.97f);

        // Single text object inside the panel.
        GameObject textObject = new GameObject("BirdieItemMenuText");
        textObject.transform.SetParent(itemMenuPanelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(26f, 22f);
        textRect.offsetMax = new Vector2(-26f, -20f);

        itemMenuText = textObject.AddComponent<TextMeshProUGUI>();
        itemMenuText.fontSize = 18;
        itemMenuText.alignment = TextAlignmentOptions.TopLeft;
        itemMenuText.richText = true;
        itemMenuText.textWrappingMode = TextWrappingModes.Normal;
        itemMenuText.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        itemMenuText.outlineWidth = 0.2f;
        itemMenuText.color = Color.white;

        itemMenuPanelObject.SetActive(false);
        RefreshItemMenuText();
    }

    // Builds the static item list string once and sets it on the text component.
    // The content never changes (item names and key labels are fixed), so we only
    // need to build it on creation.
    private void RefreshItemMenuText()
    {
        if (itemMenuText == null)
        {
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
        sb.AppendLine(
            "<size=125%><b><color=#49BFFF>ITEM SPAWNER</color></b></size>\n" +
            "<color=#A9B8CC>Press an item key to spawn it  ·  " +
            ModTextHelper.EscapeRichText(itemSpawnerKeyLabel) +
            " or Esc to close</color>");
        sb.AppendLine();

        for (int i = 0; i < SpawnableItems.Length; i++)
        {
            SpawnableItemDefinition item = SpawnableItems[i];
            sb.Append("<color=#49BFFF><b>[ ")
              .Append(ModTextHelper.EscapeRichText(item.KeyLabel))
              .Append(" ]</b></color>  <b>")
              .Append(ModTextHelper.EscapeRichText(item.DisplayName))
              .Append("</b>");

            if (item.TypeId != CoffeeItemTypeInt)
            {
                sb.Append("  <color=#8291A8>HOST REQUIRED</color>");
            }

            sb.AppendLine();
        }

        itemMenuText.text = sb.ToString();
    }

    // Shows or hides the item menu panel to match itemMenuOpen.
    private void UpdateItemMenuVisibility()
    {
        if (itemMenuPanelObject == null)
        {
            return;
        }

        itemMenuPanelObject.SetActive(itemMenuOpen);
    }

    private void EnsureEventSystemExists()
    {
        try
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("BirdieEventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Use StandaloneInputModule as fallback (game has InputSystemUIInputModule already)
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(eventSystemObj);
            }
        }
        catch
        {
        }
    }

    // ── HUD text visibility ──────────────────────────────────────────────────

    // Toggles the four HUD text labels (player info, assist indicator, bottom
    // hotkey bar). The impact preview panel and item menu panel are unaffected.
    private void ApplyHudVisibility()
    {
        // Left panel is visible when master is on AND at least one left element is enabled.
        bool leftActive = hudVisible && (hudShowPlayerInfo || hudShowBallDistance);
        if (leftHudText != null)
        {
            leftHudText.gameObject.SetActive(leftActive);
        }

        // Center panel: master on AND center title enabled (always shown for crate warning).
        if (centerHudText != null)
        {
            centerHudText.gameObject.SetActive(hudVisible);
        }

        // Right panel is always shown when master on (contains assist + optional ice).
        if (rightHudText != null)
        {
            rightHudText.gameObject.SetActive(hudVisible);
        }

        // Bottom bar: master on AND bottom bar flag.
        if (bottomHudText != null)
        {
            bottomHudText.gameObject.SetActive(hudVisible && hudShowBottomBar);
        }
    }
}
