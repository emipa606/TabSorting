using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace TabSorting;

[HarmonyPatch]
public static class MainTabWindow_Architect_TabRectHeight
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MainTabWindow_Architect),
            nameof(MainTabWindow_Architect.DoWindowContents));
        yield return AccessTools.Method(typeof(MainTabWindow_Architect),
            $"get_{nameof(MainTabWindow_Architect.WinHeight)}");
    }

    public static void Prefix(ref List<ArchitectCategoryTab> ___desPanelsCached, out List<ArchitectCategoryTab> __state)
    {
        __state = ___desPanelsCached.ToList();
        if (!TabSortingMod.instance.Settings.HideEmptyTabs)
        {
            return;
        }

        ___desPanelsCached.RemoveAll(tab => !tab.Visible);
    }

    public static void Postfix(ref List<ArchitectCategoryTab> ___desPanelsCached, List<ArchitectCategoryTab> __state)
    {
        if (!TabSortingMod.instance.Settings.HideEmptyTabs)
        {
            return;
        }

        ___desPanelsCached = __state;
    }
}