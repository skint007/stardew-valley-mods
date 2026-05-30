using System;
using LevelUp.Config;
using LevelUp.State;
using LevelUp.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace LevelUp.Ui;

/// <summary>
/// Draws the XP bar HUD. Two layouts, chosen by <see cref="ModConfig.UseVerticalXpBar"/>:
///   - Horizontal (default): a "LVL &lt;n&gt;" box plus a progress bar, centered above the toolbar.
///   - Vertical: a framed bar with an "L" cap to the left of the vanilla HP/Energy bars.
/// Both use the game's standard sprites so they match any UI recolor.
/// </summary>
public class XpBarHud
{
    // ── Horizontal layout ─────────────────────────────────────────────────────
    private const int TotalWidth = 800; // matches the vanilla toolbar box width
    private const int BarHeight  = 64;
    private const int BoxGap     = 8;   // gap between the LVL box and the XP box
    private const int ToolbarGap = 4;   // gap between our bar and the toolbar below it
    private const int Border     = 16;  // menu-box frame thickness to inset content by

    private static readonly Rectangle FrameSource = new(0, 256, 60, 60);

    // ── Vertical layout (matches the vanilla HP/Energy bar metrics) ────────────
    private const int VBarWidth    = 48;  // 12 px sprite × 4 scale
    private const int VCapHeight   = 64;
    private const int VTotalHeight = 224;
    private const int VRightInset  = 168; // 56 px left of the (potential) health bar at vw - 112

    // ── Shared colors ──────────────────────────────────────────────────────────
    private static readonly Color FillColor      = new(148, 91, 192);
    private static readonly Color FillHighlight  = new(214, 178, 240);
    private static readonly Color TrackShadow    = new(196, 168, 122);
    private static readonly Color LabelColor     = new(132, 78, 196);
    private static readonly Color LetterColor    = new(206, 168, 240);
    private static readonly Color LetterShadow   = new(92, 54, 132);

    // Floating "+N XP" popup. Rapid gains accumulate into one popup and refresh its timer
    // (reads as a recent-gain total rather than spamming overlapping numbers).
    private const double PopupDurationMs = 1400;
    private long   _popupAmount;
    private double _popupTimer;
    private float  _popScale = 1f;

    // Idle fade for the vertical bar (mirrors how the vanilla HP/energy bars fade when idle):
    // hold full opacity for a grace period after the last XP gain, then ease down to a faint
    // ghost. Hovering the bar forces it solid. Only the vertical layout fades; the horizontal
    // above-toolbar bar is a different context.
    private const double IdleFadeDelayMs    = 4000;  // stay solid this long after a gain / hover
    private const double IdleFadeDurationMs = 1200;  // then ease to IdleMinAlpha over this long
    private const float  IdleMinAlpha       = 0.2f;  // resting opacity once fully faded
    private double _idleMs = IdleFadeDelayMs + IdleFadeDurationMs; // start faded until first gain/hover

    // Cached wood color behind the vertical cap's baked-in letter, read once from the live
    // Cursors texture so we match whatever recolor the player has. Falls back to a wood brown.
    private Color? _capPanelColor;

    private readonly ModConfig _config;
    private readonly SaveDataManager _saveData;
    private readonly LevelCalculator _calculator;
    private readonly ITranslationHelper _i18n;
    private readonly IMonitor _monitor;

    // Hover text computed during Draw() (pre-HUD) but drawn in DrawTooltip() (post-HUD), so
    // the tooltip sits on top of the toolbar instead of tucking behind it.
    private string? _hoverTip;

    public XpBarHud(
        ModConfig config,
        SaveDataManager saveData,
        LevelCalculator calculator,
        ITranslationHelper i18n,
        IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _calculator = calculator;
        _i18n = i18n;
        _monitor = monitor;
    }

    /// <summary>Hook this to XpTracker.XpAwarded — queues a floating "+N XP" popup.</summary>
    public void RegisterGain(long amount, string source)
    {
        if (amount <= 0) return;
        _popupAmount += amount;
        _popupTimer = PopupDurationMs;
        _popScale = 1.35f; // brief pop, eased back toward 1.0 each frame
        _idleMs = 0;       // a gain wakes the bar back to full opacity
    }

    /// <summary>
    /// Draw the bar. Called from the Display.RenderingHud event (i.e. *before* the vanilla
    /// HUD) so the toolbar's item tooltip, drawn during the HUD, layers on top of us instead
    /// of being hidden behind the bar.
    /// </summary>
    public void Draw()
    {
        _hoverTip = null;
        if (!_config.Enabled || !_config.ShowXpBar) return;
        if (!Context.IsWorldReady) return;
        if (Game1.eventUp || Game1.farmEvent != null) return;
        if (Game1.activeClickableMenu != null) return;
        if (Game1.player == null) return;

        var sb = Game1.spriteBatch;
        int vw = Game1.uiViewport.Width;
        int vh = Game1.uiViewport.Height;

        long  totalXp  = _saveData.Current.TotalXp;
        int   level    = _saveData.Current.Level;
        float progress = _calculator.ProgressToNext(totalXp, level);

        if (_config.UseVerticalXpBar)
            DrawVertical(sb, vw, vh, level, totalXp, progress);
        else
            DrawHorizontal(sb, vw, vh, level, totalXp, progress);
    }

    // ── Horizontal bar (above the toolbar) ─────────────────────────────────────
    private void DrawHorizontal(SpriteBatch sb, int vw, int vh, int level, long totalXp, float progress)
    {
        // Scale the layout for small-screen / mobile users. Geometry scales; the SpriteText
        // "LVL N" label uses a fixed-size bitmap font that can't be cleanly scaled, so the LVL
        // box widens via Math.Max to fit the label at any scale.
        float s = Math.Clamp(_config.XpBarScale, 0.5f, 1.5f);
        int totalWidth = (int)Math.Round(TotalWidth   * s);
        int barHeight  = (int)Math.Round(BarHeight    * s);
        int boxGap     = (int)Math.Round(BoxGap       * s);
        int toolbarGap = (int)Math.Round(ToolbarGap   * s);
        int border     = (int)Math.Round(Border       * s);
        int shadow     = Math.Max(1, (int)Math.Round(3 * s));
        int highlight  = Math.Max(1, (int)Math.Round(4 * s));

        // Anchor just above the toolbar (when it's at the bottom; otherwise fall back to the
        // bottom position so we stay bottom-center even if the toolbar flips up).
        int toolbarTop = vh - 104;
        foreach (var menu in Game1.onScreenMenus)
        {
            if (menu is Toolbar tb)
            {
                if (tb.yPositionOnScreen > vh / 2)
                    toolbarTop = tb.yPositionOnScreen - 104;
                break;
            }
        }

        int barBottom = toolbarTop - toolbarGap;
        int barTop    = barBottom - barHeight;
        int left      = vw / 2 - totalWidth / 2;

        // Use smallFont (a regular-weight TrueType font) for the level label so it scales
        // smoothly with the bar at any size. The original SpriteText is a fixed-size bitmap
        // font that overshoots the bar height at small scales.
        string label = _i18n.Get("hud.level-label", new { level });
        float labelScale = 1.5f * s;
        Vector2 labelSize = Game1.smallFont.MeasureString(label) * labelScale;
        int labelW = (int)Math.Ceiling(labelSize.X);
        int labelH = (int)Math.Ceiling(labelSize.Y);
        int labelPad = (int)Math.Round(16 * s);
        int lvlBoxW = labelW + 2 * border + labelPad;
        int xpBoxX  = left + lvlBoxW + boxGap;
        int xpBoxW  = totalWidth - lvlBoxW - boxGap;

        IClickableMenu.drawTextureBox(sb, Game1.menuTexture, FrameSource,
            left, barTop, lvlBoxW, barHeight, Color.White, scale: 1f, drawShadow: false);

        // Dark inset plate behind the LVL text. Gives a high-contrast backing for the label
        // regardless of how the wood frame is colored by recolor mods.
        sb.Draw(Game1.staminaRect,
            new Rectangle(left + border, barTop + border, lvlBoxW - 2 * border, barHeight - 2 * border),
            new Color(18, 18, 28) * 0.6f);

        var labelPos = new Vector2(
            left + (lvlBoxW - labelW) / 2f,
            barTop + (barHeight - labelH) / 2f
        );
        sb.DrawString(Game1.smallFont, label, labelPos, Color.White,
            0f, Vector2.Zero, labelScale, SpriteEffects.None, 1f);

        IClickableMenu.drawTextureBox(sb, Game1.menuTexture, FrameSource,
            xpBoxX, barTop, xpBoxW, barHeight, Color.White, scale: 1f, drawShadow: false);

        int trackX = xpBoxX + border;
        int trackY = barTop  + border;
        int trackW = xpBoxW  - 2 * border;
        int trackH = barHeight - 2 * border;

        sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, trackW, shadow), TrackShadow);
        sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, shadow, trackH), TrackShadow);

        int fillW = (int)Math.Round(trackW * progress);
        if (fillW > 0)
        {
            sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, fillW, trackH), FillColor);
            sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, fillW, highlight), FillHighlight);
        }

        DrawPopup(sb, xpBoxX + xpBoxW / 2f, barTop);
        SetHoverTip(new Rectangle(left, barTop, totalWidth, barHeight), level, totalXp);
    }

    // ── Vertical bar (left of the HP/Energy bars) ──────────────────────────────
    private void DrawVertical(SpriteBatch sb, int vw, int vh, int level, long totalXp, float progress)
    {
        var cursors = Game1.mouseCursors;

        int barBottomY = vh - 16;
        int barTopY    = barBottomY - VTotalHeight;
        int barX       = vw - VRightInset;

        var hitRect = new Rectangle(barX, barTopY, VBarWidth, VTotalHeight);
        bool hovered = hitRect.Contains(Game1.getMousePosition());
        float a = ComputeIdleAlpha(hovered);

        // Top wood cap with a recessed "L" plate.
        DrawLevelCap(sb, cursors, barX, barTopY, a);

        // Parchment middle, tile-stretched between the caps.
        int middleY = barTopY + VCapHeight;
        int middleH = (barBottomY - VCapHeight) - middleY;
        sb.Draw(cursors, new Rectangle(barX, middleY, VBarWidth, middleH),
            new Rectangle(256, 424, 12, 16), Color.White * a);

        // Bottom wood cap.
        sb.Draw(cursors, new Vector2(barX, barBottomY - VCapHeight),
            new Rectangle(256, 448, 12, 16), Color.White * a, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

        // Purple fill rising from the bottom inside the parchment (geometry matches vanilla:
        // 24 px wide, inset 12 px; from 48 px below the top down to 8 px above the bottom).
        int fillX       = barX + 12;
        int fillW       = 24;
        int fillAreaTop = barTopY    + 48;
        int fillAreaBot = barBottomY - 8;
        int fillH       = (int)Math.Round((fillAreaBot - fillAreaTop) * progress);
        if (fillH > 0)
        {
            sb.Draw(Game1.staminaRect, new Rectangle(fillX, fillAreaBot - fillH, fillW, fillH), FillColor * a);
            sb.Draw(Game1.staminaRect, new Rectangle(fillX, fillAreaBot - fillH, fillW, 4), FillHighlight * a);
        }

        DrawPopup(sb, barX + VBarWidth / 2f, barTopY);
        SetHoverTip(hitRect, level, totalXp);
    }

    /// <summary>
    /// Opacity for the idle-fading vertical bar. Returns 1.0 while recently active or hovered,
    /// then eases down to <see cref="IdleMinAlpha"/> after <see cref="IdleFadeDelayMs"/> of no
    /// gains. Returns 1.0 unconditionally when the fade is disabled in config.
    /// </summary>
    private float ComputeIdleAlpha(bool hovered)
    {
        if (!_config.FadeVerticalBarWhenIdle) return 1f;

        if (hovered)
        {
            _idleMs = 0; // pointing at the bar keeps it solid (and discoverable)
            return 1f;
        }

        _idleMs += Game1.currentGameTime?.ElapsedGameTime.TotalMilliseconds ?? 16.0;
        if (_idleMs <= IdleFadeDelayMs) return 1f;

        double into = _idleMs - IdleFadeDelayMs;
        if (into >= IdleFadeDurationMs) return IdleMinAlpha;
        return MathHelper.Lerp(1f, IdleMinAlpha, (float)(into / IdleFadeDurationMs));
    }

    /// <summary>
    /// Draw the vertical bar's top cap: the real wood-frame sprite, a recessed wood plate
    /// covering the sprite's baked-in letter, and a chunky pixel-art "L" on a 4 px grid.
    /// </summary>
    private void DrawLevelCap(SpriteBatch sb, Texture2D cursors, int barX, int barTopY, float a)
    {
        // Use the plain bottom-cap sprite flipped vertically rather than the top-cap sprite,
        // which has a letter baked in. The cover plate below hides that letter at full opacity,
        // but when the bar fades the plate turns translucent and the letter bleeds through
        // (looked like the energy bar's "E"). A letterless cap fades cleanly.
        sb.Draw(cursors, new Vector2(barX, barTopY),
            new Rectangle(256, 448, 12, 16),
            Color.White * a, 0f, Vector2.Zero, 4f, SpriteEffects.FlipVertically, 1f);

        Color panel = GetCapPanelColor(cursors);
        int plateX = barX + 8, plateY = barTopY + 8, plateW = 32, plateH = 40;
        sb.Draw(Game1.staminaRect, new Rectangle(plateX, plateY, plateW, plateH), panel * a);

        const int u = 4; // grid unit = one source pixel at 4× scale
        int boxW = 5 * u, boxH = 7 * u;
        int lx = plateX + (plateW - boxW) / 2;
        int ly = plateY + (plateH - boxH) / 2 - 2;

        void Cell(int gx, int gy, int gw, int gh, Color c) =>
            sb.Draw(Game1.staminaRect, new Rectangle(lx + gx * u, ly + gy * u, gw * u, gh * u), c * a);

        Cell(1, 1, 2, 7, LetterShadow); // vertical stroke shadow
        Cell(1, 6, 5, 2, LetterShadow); // foot shadow
        Cell(0, 0, 2, 7, LetterColor);  // vertical stroke
        Cell(0, 5, 5, 2, LetterColor);  // foot
    }

    /// <summary>
    /// Read (once, cached) the dominant wood color behind the cap sprite's baked-in letter so
    /// our blank panel blends with the surrounding wood. Falls back to a wood brown.
    /// </summary>
    private Color GetCapPanelColor(Texture2D cursors)
    {
        if (_capPanelColor.HasValue) return _capPanelColor.Value;

        Color result = new(120, 70, 34);
        try
        {
            var buf = new Color[12 * 16];
            cursors.GetData(0, new Rectangle(256, 408, 12, 16), buf, 0, buf.Length);

            var counts = new System.Collections.Generic.Dictionary<Color, int>();
            for (int y = 1; y <= 14; y++)
                for (int x = 1; x <= 10; x++)
                {
                    Color c = buf[y * 12 + x];
                    if (c.A < 200) continue;
                    counts.TryGetValue(c, out int n);
                    counts[c] = n + 1;
                }

            int best = 0;
            foreach (var kv in counts)
                if (kv.Value > best) { best = kv.Value; result = kv.Key; }
        }
        catch
        {
            // keep the fallback
        }

        _capPanelColor = result;
        return result;
    }

    // ── Shared popup + hover ────────────────────────────────────────────────────
    private void DrawPopup(SpriteBatch sb, float centerX, float topY)
    {
        if (_popupTimer <= 0) return;

        double dt = Game1.currentGameTime?.ElapsedGameTime.TotalMilliseconds ?? 16.0;
        _popupTimer -= dt;
        _popScale += (1f - _popScale) * 0.18f; // ease the pop back to 1.0
        if (_popupTimer <= 0)
        {
            _popupTimer = 0;
            _popupAmount = 0;
            _popScale = 1f;
            return;
        }

        float t     = (float)(_popupTimer / PopupDurationMs);
        float rise  = (1f - t) * 38f;
        float alpha = t > 0.30f ? 1f : t / 0.30f;

        string txt    = _i18n.Get("hud.xp-popup", new { amount = _popupAmount.ToString("N0") });
        var    font   = Game1.smallFont;
        Vector2 size   = font.MeasureString(txt);
        Vector2 origin = size / 2f;
        var pos = new Vector2(centerX, topY - 8 - rise);

        sb.DrawString(font, txt, pos + new Vector2(2f, 2f),
            new Color(0, 0, 0) * (0.55f * alpha), 0f, origin, _popScale,
            SpriteEffects.None, 1f);
        sb.DrawString(font, txt, pos,
            FillHighlight * alpha, 0f, origin, _popScale,
            SpriteEffects.None, 1f);
    }

    private void SetHoverTip(Rectangle hitRect, int level, long totalXp)
    {
        if (!hitRect.Contains(Game1.getMousePosition())) return;

        long needed = _calculator.XpToNextLevel(level);
        long into   = _calculator.XpIntoCurrentLevel(totalXp, level);
        _hoverTip = level >= _calculator.LevelCap
            ? _i18n.Get("hud.tooltip.max", new { level, total = totalXp.ToString("N0") })
            : _i18n.Get("hud.tooltip.progress", new
            {
                level,
                into = into.ToString("N0"),
                needed = needed.ToString("N0"),
                total = totalXp.ToString("N0"),
            });
    }

    /// <summary>
    /// Draw the hover tooltip queued by <see cref="Draw"/>. Called from Display.RenderedHud
    /// (after the vanilla HUD) so the tooltip renders above the toolbar.
    /// </summary>
    public void DrawTooltip()
    {
        if (_hoverTip == null) return;
        IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverTip, Game1.smallFont);
    }
}
