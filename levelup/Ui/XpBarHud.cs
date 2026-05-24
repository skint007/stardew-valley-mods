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
/// Draws a horizontal XP bar centered above the toolbar at the bottom of the screen:
/// a small "LVL &lt;n&gt;" box on the left and a long progress-bar box to its right. Both use
/// the game's standard menu-box frame (so they match any UI recolor) and the assembly is
/// aligned to the toolbar's 800 px width and anchored just above it.
/// </summary>
public class XpBarHud
{
    private const int TotalWidth = 800; // matches the vanilla toolbar box width
    private const int BarHeight  = 64;
    private const int BoxGap     = 8;   // gap between the LVL box and the XP box
    private const int ToolbarGap = 4;   // gap between our bar and the toolbar below it
    private const int Border     = 16;  // menu-box frame thickness to inset content by

    private static readonly Rectangle FrameSource = new(0, 256, 60, 60);
    private static readonly Color FillColor      = new(148, 91, 192);
    private static readonly Color FillHighlight  = new(214, 178, 240);
    private static readonly Color TrackShadow    = new(196, 168, 122);
    private static readonly Color LabelColor     = new(132, 78, 196);

    // Floating "+N XP" popup. Rapid gains accumulate into one popup and refresh its timer
    // (reads as a recent-gain total rather than spamming overlapping numbers).
    private const double PopupDurationMs = 1400;
    private long   _popupAmount;
    private double _popupTimer;
    private float  _popScale = 1f;

    private readonly ModConfig _config;
    private readonly SaveDataManager _saveData;
    private readonly LevelCalculator _calculator;
    private readonly IMonitor _monitor;

    // Hover text computed during Draw() (pre-HUD) but drawn in DrawTooltip() (post-HUD), so
    // the tooltip sits on top of the toolbar instead of tucking behind it.
    private string? _hoverTip;

    public XpBarHud(
        ModConfig config,
        SaveDataManager saveData,
        LevelCalculator calculator,
        IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _calculator = calculator;
        _monitor = monitor;
    }

    /// <summary>Hook this to XpTracker.XpAwarded — queues a floating "+N XP" popup.</summary>
    public void RegisterGain(long amount, string source)
    {
        if (amount <= 0) return;
        _popupAmount += amount;
        _popupTimer = PopupDurationMs;
        _popScale = 1.35f; // brief pop, eased back toward 1.0 each frame
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

        // ── Anchor just above the toolbar (when it's at the bottom; otherwise fall back to
        //    the bottom position so we stay bottom-center even if the toolbar flips up) ────
        int toolbarTop = vh - 104; // vanilla toolbar box top when docked at the bottom
        foreach (var menu in Game1.onScreenMenus)
        {
            if (menu is Toolbar tb)
            {
                if (tb.yPositionOnScreen > vh / 2)
                    toolbarTop = tb.yPositionOnScreen - 104;
                break;
            }
        }

        int barBottom = toolbarTop - ToolbarGap;
        int barTop    = barBottom - BarHeight;
        int left      = vw / 2 - TotalWidth / 2;

        // ── Layout: LVL box sized to its text, XP box takes the rest ─────────────────────
        string label   = $"LVL {level}";
        int    labelW  = SpriteText.getWidthOfString(label);
        int    lvlBoxW = Math.Max(140, labelW + 2 * Border + 16);
        int    xpBoxX  = left + lvlBoxW + BoxGap;
        int    xpBoxW  = TotalWidth - lvlBoxW - BoxGap;

        // ── LVL box ──────────────────────────────────────────────────────────────────────
        IClickableMenu.drawTextureBox(sb, Game1.menuTexture, FrameSource,
            left, barTop, lvlBoxW, BarHeight, Color.White, scale: 1f, drawShadow: false);

        int labelH = SpriteText.getHeightOfString(label);
        int labelX = left + (lvlBoxW - labelW) / 2;
        // +6: SpriteText reports a tall bounding box (trailing space below the glyphs), so a
        // pure center sits visually high — nudge down to optically center in the frame.
        int labelY = barTop + (BarHeight - labelH) / 2 + 6;
        SpriteText.drawString(sb, label, labelX, labelY, color: LabelColor);

        // ── XP box ───────────────────────────────────────────────────────────────────────
        IClickableMenu.drawTextureBox(sb, Game1.menuTexture, FrameSource,
            xpBoxX, barTop, xpBoxW, BarHeight, Color.White, scale: 1f, drawShadow: false);

        int trackX = xpBoxX + Border;
        int trackY = barTop  + Border;
        int trackW = xpBoxW  - 2 * Border;
        int trackH = BarHeight - 2 * Border;

        // Recessed inner shadow on the empty track (top + left), like the vanilla bars.
        sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, trackW, 3), TrackShadow);
        sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, 3, trackH), TrackShadow);

        // Purple fill, growing left → right.
        int fillW = (int)Math.Round(trackW * progress);
        if (fillW > 0)
        {
            sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, fillW, trackH), FillColor);
            sb.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, fillW, 4), FillHighlight);
        }

        // ── Floating "+N XP" popup above the XP box ──────────────────────────────────────
        if (_popupTimer > 0)
        {
            double dt = Game1.currentGameTime?.ElapsedGameTime.TotalMilliseconds ?? 16.0;
            _popupTimer -= dt;
            _popScale += (1f - _popScale) * 0.18f; // ease the pop back to 1.0
            if (_popupTimer <= 0)
            {
                _popupTimer = 0;
                _popupAmount = 0;
                _popScale = 1f;
            }
            else
            {
                float t     = (float)(_popupTimer / PopupDurationMs);
                float rise  = (1f - t) * 38f;
                float alpha = t > 0.30f ? 1f : t / 0.30f;

                string txt    = $"+{_popupAmount:N0} XP";
                var    font   = Game1.smallFont;
                Vector2 size   = font.MeasureString(txt);
                Vector2 origin = size / 2f;
                var pos = new Vector2(xpBoxX + xpBoxW / 2f, barTop - 8 - rise);

                sb.DrawString(font, txt, pos + new Vector2(2f, 2f),
                    new Color(0, 0, 0) * (0.55f * alpha), 0f, origin, _popScale,
                    SpriteEffects.None, 1f);
                sb.DrawString(font, txt, pos,
                    FillHighlight * alpha, 0f, origin, _popScale,
                    SpriteEffects.None, 1f);
            }
        }

        // ── Hover tooltip: detected here, but drawn later in DrawTooltip() so it sits on
        //    top of the toolbar rather than behind it ──────────────────────────────────────
        var mouse = Game1.getMousePosition();
        var hitRect = new Rectangle(left, barTop, TotalWidth, BarHeight);
        if (hitRect.Contains(mouse))
        {
            long needed = _calculator.XpToNextLevel(level);
            long into   = _calculator.XpIntoCurrentLevel(totalXp, level);
            _hoverTip = level >= _calculator.LevelCap
                ? $"Level {level} (MAX)\n{totalXp:N0} total XP"
                : $"Level {level}\n{into:N0} / {needed:N0} XP\nTotal: {totalXp:N0}";
        }
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
