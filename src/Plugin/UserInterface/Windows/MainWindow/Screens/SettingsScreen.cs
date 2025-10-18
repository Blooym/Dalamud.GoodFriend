using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Configuration;
using GoodFriend.Plugin.Localization;
using Sirensong.Game.Enums;
using Sirensong.Game.Helpers;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;

namespace GoodFriend.Plugin.UserInterface.Windows.MainWindow.Screens;

internal static class SettingsScreen
{
    private static SettingsOption CurrentSetting { get; set; } = SettingsOption.Privacy;
    private static bool restartRequired;

    /// <summary>
    ///     Draws the settings list.
    /// </summary>
    public static void DrawSettingsList()
    {
        foreach (var setting in Enum.GetValues<SettingsOption>())
        {
            if (ImGui.Selectable(setting.ToString(), setting == CurrentSetting))
            {
                CurrentSetting = setting;
            }
        }
    }

    /// <summary>
    ///     Draws the settings menu.
    /// </summary>
    public static void DrawSettingDetails()
    {
        if (restartRequired)
        {
            SiGui.TextWrappedColoured(Colours.Warning, Strings.UI_MainWindow_SettingsScreen_RestartNeeded);
            ImGui.Dummy(Spacing.ReadableSpacing);
        }

        SiGui.Heading(CurrentSetting.ToString());
        switch (CurrentSetting)
        {
            case SettingsOption.Privacy:
                DrawPrivacySettings();
                break;
            case SettingsOption.API:
                DrawApiSettings();
                break;
        }
    }

    /// <summary>
    ///     Draws the privacy settings.
    /// </summary>
    private static void DrawPrivacySettings()
    {
        var groupKey = Services.PluginConfiguration.ApiConfig.PrivateGroupKey;
        SiGui.Text(Strings.UI_MainWindow_SettingsScreen_Setting_GroupKey);
        if (SiGui.InputText("##groupKey", ref groupKey, 250, false, ImGuiInputTextFlags.Password))
        {
            Services.PluginConfiguration.ApiConfig.PrivateGroupKey = groupKey;
            Services.PluginConfiguration.Save();
        }
        SiGui.TextDisabledWrapped(Strings.UI_MainWindow_SettingsScreen_Setting_GroupKey_Description);
        ImGui.Dummy(Spacing.ReadableSpacing);
    }

    /// <summary>
    ///     Draws the general settings.
    /// </summary>
    private static void DrawApiSettings()
    {
        var apiUrl = Services.PluginConfiguration.ApiConfig.BaseUrl.ToString();
        SiGui.Text(Strings.UI_MainWindow_SettingsScreen_Setting_APIURL);
        if (SiGui.InputText("##apiUrl", ref apiUrl, 250, true))
        {
            try
            {
                var uri = new Uri(apiUrl);
                if (uri != Services.PluginConfiguration.ApiConfig.BaseUrl)
                {
                    restartRequired = true;
                    Services.PluginConfiguration.ApiConfig.BaseUrl = uri;
                    Services.PluginConfiguration.Save();
                }
            }
            catch (UriFormatException)
            {
                ToastHelper.ShowErrorToast(Strings.UI_MainWindow_SettingsScreen_Setting_APIURL_Invalid);
                SoundEffectHelper.PlaySound(SoundEffect.Se11);
            }
        }
        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, Strings.UI_ResetToDefault))
        {
            Services.PluginConfiguration.ApiConfig.BaseUrl = PluginConfiguration.ApiConfiguration.DefaultBaseUri;
            Services.PluginConfiguration.Save();
        }
        SiGui.TextDisabledWrapped(Strings.UI_MainWindow_SettingsScreen_Setting_APIURL_Description);
    }

    private enum SettingsOption
    {
        Privacy,
        API,
    }
}
