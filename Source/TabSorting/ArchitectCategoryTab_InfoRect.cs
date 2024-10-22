using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TabSorting;

[HarmonyPatch(typeof(ArchitectCategoryTab), $"get_{nameof(ArchitectCategoryTab.InfoRect)}")]
public static class ArchitectCategoryTab_InfoRect
{
    public static void Postfix(ref Rect __result)
    {
        if (!TabSortingMod.instance.Settings.HideEmptyTabs)
        {
            return;
        }

        __result.y = UI.screenHeight - 35 - ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).WinHeight -
                     270f;
    }
}