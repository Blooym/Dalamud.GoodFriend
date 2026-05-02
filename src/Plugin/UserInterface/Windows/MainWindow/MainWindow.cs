using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Localization;
using GoodFriend.Plugin.UserInterface.Components;
using GoodFriend.Plugin.UserInterface.Windows.MainWindow.Screens;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;

namespace GoodFriend.Plugin.UserInterface.Windows.MainWindow;

internal sealed class MainWindow : Window
{
    /// <summary>
    ///     The width of the sidebar.
    /// </summary>
    private const float SidebarWidthPercentage = 0.25f;

    /// <summary>
    ///     The width of the list.
    /// </summary>
    private const float ListWidthPercentage = 0.75f;

    /// <summary>
    ///     The space left at the bottom of the window wrapper.
    /// </summary>
    private const uint WindowWrapperBottomSpace = 35;

    /// <summary>
    ///     The currently selected tab.
    /// </summary>
    private MainWindowScreen CurrentScreen { get; set; } = MainWindowScreen.Modules;

    /// <inheritdoc />
    public MainWindow() : base(DalamudInjections.PluginInterface.Manifest.Name)
    {
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new(850, 550),
        };
        this.Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse;
        this.AllowPinning = false;
        this.AllowClickthrough = false;
    }

    /// <inheritdoc />
    public override void Draw()
    {
        SiGui.TextDisabledWrapped(this.CurrentScreen.ToString());
        using (var child = ImRaii.Child("MainWindowWrapper", new(0, ImGui.GetContentRegionAvail().Y - (WindowWrapperBottomSpace * ImGuiHelpers.GlobalScale)), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (child.Success)
            {
                switch (this.CurrentScreen)
                {
                    case MainWindowScreen.Modules:
                        DrawModules();
                        break;
                    case MainWindowScreen.Settings:
                        DrawSettings();
                        break;
                }
            }
        }
        ImGui.Dummy(Spacing.ReadableSpacing);
        ButtonRowComponent.DrawRow(new Dictionary<(FontAwesomeIcon, Vector4?, string), Action>
                {
                    { (FontAwesomeIcon.Home, null, Strings.UI_MainWindow_Button_Modules), () => this.CurrentScreen = MainWindowScreen.Modules },
                    { (FontAwesomeIcon.Cog, null, Strings.UI_MainWindow_Button_Settings), () => this.CurrentScreen = MainWindowScreen.Settings },
                    { (FontAwesomeIcon.Heart, ImGuiColors.ParsedPurple, Strings.UI_MainWindow_Button_Donate), () => Util.OpenLink(Constants.Link.Donate) },
                });
    }

    /// <summary>
    ///     Draws the module content.
    /// </summary>
    private static void DrawModules()
    => PanelComponent.DrawSplitPanels("MainWindowPanel", ImGui.GetContentRegionAvail().X * SidebarWidthPercentage * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X * ListWidthPercentage,
        ModuleScreen.DrawModuleList,
        ModuleScreen.DrawModuleDetails);

    /// <summary>
    ///     Draws the settings content.
    /// </summary>
    private static void DrawSettings()
        => PanelComponent.DrawSplitPanels("MainWindowPanel", ImGui.GetContentRegionAvail().X * SidebarWidthPercentage, ImGui.GetContentRegionAvail().X * ListWidthPercentage,
            SettingsScreen.DrawSettingsList,
            SettingsScreen.DrawSettingDetails);

    /// <summary>
    ///     The tabs of the main window.
    /// </summary>
    private enum MainWindowScreen
    {
        /// <summary>
        ///     The modules tab.
        /// </summary>
        Modules,

        /// <summary>
        ///     The settings tab.
        /// </summary>
        Settings,
    }
}
