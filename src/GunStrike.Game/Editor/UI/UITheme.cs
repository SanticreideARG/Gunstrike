using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// Centralized color/font/spacing palette for all editor UI.
/// Change values here to restyle every widget at once.
/// </summary>
public static class UITheme
{
    // ── Window ───────────────────────────────────────────────────────────────────
    public static Color WindowBg       = new(22,  24,  30, 255);

    // ── Panels ───────────────────────────────────────────────────────────────────
    public static Color PanelBg        = new(30,  33,  42, 255);
    public static Color PanelBorder    = new(50,  55,  70, 255);
    public static Color PanelTitle     = new(40,  44,  58, 255);
    public static Color PanelTitleText = new(180, 190, 210, 255);

    // ── Buttons ──────────────────────────────────────────────────────────────────
    public static Color ButtonNormal   = new(55,  90, 160, 255);
    public static Color ButtonHover    = new(75, 115, 195, 255);
    public static Color ButtonPressed  = new(40,  70, 130, 255);
    public static Color ButtonDisabled = new(45,  50,  65, 255);
    public static Color ButtonText     = new(230, 235, 245, 255);
    public static Color ButtonTextDis  = new(110, 115, 130, 255);

    // ── Danger button (delete, reset) ────────────────────────────────────────────
    public static Color DangerNormal   = new(160,  50,  50, 255);
    public static Color DangerHover    = new(195,  70,  70, 255);

    // ── Sliders ──────────────────────────────────────────────────────────────────
    public static Color SliderTrack    = new(40,  44,  58, 255);
    public static Color SliderFill     = new(60, 100, 180, 255);
    public static Color SliderHandle   = new(120, 160, 230, 255);
    public static Color SliderHandleHv = new(160, 200, 255, 255);

    // ── Text inputs ──────────────────────────────────────────────────────────────
    public static Color InputBg        = new(22,  24,  30, 255);
    public static Color InputBgFocus   = new(28,  30,  40, 255);
    public static Color InputBorder    = new(60,  65,  85, 255);
    public static Color InputBorderFoc = new(80, 120, 200, 255);
    public static Color InputText      = new(215, 220, 235, 255);
    public static Color InputCursor    = new(120, 160, 230, 255);
    public static Color InputPlacehol  = new( 90,  95, 115, 255);

    // ── Labels ───────────────────────────────────────────────────────────────────
    public static Color LabelPrimary   = new(200, 210, 230, 255);
    public static Color LabelSecondary = new(120, 130, 155, 255);
    public static Color LabelAccent    = new(100, 160, 255, 255);

    // ── Radio / Checkbox ─────────────────────────────────────────────────────────
    public static Color RadioBorder    = new(70,  80, 105, 255);
    public static Color RadioFill      = new(60, 100, 180, 255);
    public static Color RadioCheck     = new(160, 200, 255, 255);

    // ── Separator ────────────────────────────────────────────────────────────────
    public static Color Separator      = new(50,  55,  70, 255);

    // ── Scrollbar ────────────────────────────────────────────────────────────────
    public static Color ScrollTrack    = new(28,  30,  40, 255);
    public static Color ScrollThumb    = new(60,  65,  85, 255);
    public static Color ScrollThumbHv  = new(85,  95, 120, 255);

    // ── Tooltips ─────────────────────────────────────────────────────────────────
    public static Color TooltipBg      = new(15,  17,  22, 230);
    public static Color TooltipBorder  = new(60,  65,  85, 255);
    public static Color TooltipText    = new(200, 210, 230, 255);

    // ── Canvas / Map editor ──────────────────────────────────────────────────────
    public static Color CanvasBg       = new(18,  20,  26, 255);
    public static Color GridLine       = new(35,  38,  50, 255);
    public static Color GridLineMajor  = new(50,  55,  70, 255);
    public static Color SelectionBox   = new(80, 140, 255, 120);
    public static Color SelectionBord  = new(100, 160, 255, 255);

    // ── Typography ───────────────────────────────────────────────────────────────
    public const int FontSizeSmall  = 12;
    public const int FontSizeNormal = 14;
    public const int FontSizeMedium = 16;
    public const int FontSizeLarge  = 20;

    // ── Layout constants ─────────────────────────────────────────────────────────
    public const float ElementHeight   = 26f;
    public const float ElementGap      =  5f;
    public const float PanelPadding    =  8f;
    public const float TitleBarHeight  = 28f;
    public const float ScrollbarWidth  = 10f;
    public const float HandleSize      = 12f;
    public const float CornerRadius    =  3f;
}
