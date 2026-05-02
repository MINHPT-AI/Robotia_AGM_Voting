using MudBlazor;

namespace Mms.Web.Components.Layout;

public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1275BC",           // Obsidian Primary
            PrimaryContrastText = "#ffffff",
            Secondary = "#14171C",         // Obsidian Secondary
            SecondaryContrastText = "#ffffff",
            Tertiary = "#393939",          // Obsidian Tertiary
            TertiaryContrastText = "#ffffff",
            AppbarBackground = "#1275BC",
            AppbarText = "#ffffff",
            DrawerBackground = "#F5F7F9",
            DrawerText = "#14171C",
            Surface = "#ffffff",
            Background = "#F5F7F9",        // Obsidian Neutral
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Manrope", "Inter", "Helvetica", "Arial", "sans-serif"],
                FontSize = "0.875rem",
            },
            Button = new ButtonTypography
            {
                FontFamily = ["Inter", "Helvetica", "Arial", "sans-serif"],
                FontSize = "0.875rem",
                TextTransform = "none" // Inter thường không cần viết hoa toàn bộ để giữ độ dễ đọc
            }
        }
    };
}
