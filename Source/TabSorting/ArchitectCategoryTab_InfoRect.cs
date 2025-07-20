using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TabSorting;

[HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.InfoRect), MethodType.Getter)]
public static class ArchitectCategoryTab_InfoRect
{
    public static void Postfix(ref Rect __result)
    {
        if (!TabSortingMod.Instance.Settings.HideEmptyTabs)
        {
            return;
        }

        __result.y = UI.screenHeight - 35 - ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).WinHeight -
                     270f;
    }
}