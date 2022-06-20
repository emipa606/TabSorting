using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting;

[StaticConstructorOnStartup]
public static class ListingExtension
{
    public static bool ListItemSelectable(this Listing lister, string header, Color hoverColor,
        bool selected = false, string loadImage = null)
    {
        var anchor = Text.Anchor;
        var color = GUI.color;
        var rect = lister.GetRect(20f);
        if (!string.IsNullOrEmpty(loadImage) && TabSorting.architectIconsLoaded)
        {
            rect.width -= TabSortingMod.tabIconSize.x;
            TabIconSelectable(new Rect(new Vector2(rect.x + rect.width, rect.y), TabSortingMod.tabIconSize),
                TabSorting.GetCustomTabIcon(loadImage), null, false, false);
        }

        if (selected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        }

        GUI.color = Color.white;
        if (Mouse.IsOver(rect))
        {
            GUI.color = hoverColor;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        if (header != null)
        {
            Widgets.Label(rect, header);
        }

        Text.Anchor = anchor;
        GUI.color = color;

        if (!Widgets.ButtonInvisible(rect))
        {
            return false;
        }

        SoundDefOf.Click.PlayOneShotOnCamera();
        return true;
    }

    public static bool TabIconSelectable(Rect rect, string iconName, string toolTip = null, bool selected = false,
        bool selectable = true)
    {
        if (TabSorting.iconsCache == null || !TabSorting.iconsCache.ContainsKey("wrongsign"))
        {
            return false;
        }

        var currentIconName = iconName;

        if (TabSorting.iconsCache?.ContainsKey(iconName) == false)
        {
            currentIconName = "wrongsign";
        }

        GUI.DrawTexture(rect.ContractedBy((rect.width - TabSortingMod.tabIconSize.x) / 2),
            TabSorting.iconsCache?[currentIconName]);

        if (!selectable)
        {
            return false;
        }

        if (string.IsNullOrEmpty(toolTip))
        {
            toolTip = iconName;
        }

        TooltipHandler.TipRegion(rect, toolTip);

        if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.1f, 0.1f, 0.1f, 0.0f),
                new Color(1f, 1f, 1f, 0.3f));
        }
        else
        {
            if (selected)
            {
                Widgets.DrawBoxSolidWithOutline(rect, new Color(0.1f, 0.1f, 0.1f, 0.0f),
                    new Color(0.3f, 0.8f, 0.3f, 0.3f));
            }
        }

        if (!Widgets.ButtonInvisible(rect))
        {
            return false;
        }

        SoundDefOf.Click.PlayOneShotOnCamera();
        return true;
    }
}