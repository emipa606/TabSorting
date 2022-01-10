using ArchitectIcons;
using HarmonyLib;

namespace TabSortingArchitectIcons;

[HarmonyPatch(typeof(Resources), "FindArchitectTabCategoryIcon", typeof(string))]
public class Resources_FindArchitectTabCategoryIcon
{
    public static void Prefix(ref string categoryDefName)
    {
        categoryDefName = TabSorting.TabSorting.GetCustomTabIcon(categoryDefName);
    }
}