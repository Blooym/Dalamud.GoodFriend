using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Sirensong.UserInterface;

namespace GoodFriend.Plugin.UserInterface.Components;

internal static class ButtonRowComponent
{
    /// <summary>
    ///     Draws a row of buttons.
    /// </summary>
    /// <param name="buttons"></param>
    public static void DrawRow(Dictionary<(FontAwesomeIcon icon, Vector4? iconSize, string tooltip), Action> buttons)
    {
        if (buttons.Count is 0)
        {
            return;
        }

        foreach (var button in buttons)
        {
            if (ImGuiComponents.IconButton(button.Key.icon, button.Key.iconSize))
            {
                button.Value();
            }
            SiGui.AddTooltip(button.Key.tooltip);
            ImGui.SameLine();
        }
    }
}
