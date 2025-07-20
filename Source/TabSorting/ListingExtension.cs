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
        var rect = lister.GetRect(25f);
        if (!string.IsNullOrEmpty(loadImage) && TabSorting.ArchitectIconsLoaded)
        {
            rect.width -= TabSortingMod.TabIconSize.x;
            TabIconSelectable(new Rect(new Vector2(rect.x + rect.width, rect.y), TabSortingMod.TabIconSize),
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
        if (TabSorting.IconsCache == null || !TabSorting.IconsCache.ContainsKey("wrongsign"))
        {
            return false;
        }

        var currentIconName = iconName;

        if (TabSorting.IconsCache?.ContainsKey(iconName) == false)
        {
            currentIconName = "wrongsign";
        }

        GUI.DrawTexture(rect.ContractedBy((rect.width - TabSortingMod.TabIconSize.x) / 2),
            TabSorting.IconsCache?[currentIconName]);

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