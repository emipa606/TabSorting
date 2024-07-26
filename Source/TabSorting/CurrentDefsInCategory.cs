using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TabSorting;

[StaticConstructorOnStartup]
public static class AllCurrentDefsInCategory
{
    public static List<BuildableDef> allCurrentDefsInCategory;
    public static List<BuildableDef> allDefsInCategory;

    static AllCurrentDefsInCategory()
    {
        allCurrentDefsInCategory = [];
        allDefsInCategory = [];
    }

    public static void Reorder(int originalIndex, int newIndex)
    {
        if (originalIndex == newIndex)
        {
            return;
        }

        if (originalIndex < newIndex)
        {
            newIndex -= 1;
        }

        var uiOrderAtNewIndex = allCurrentDefsInCategory[newIndex].uiOrder;
        var originalDef = allCurrentDefsInCategory[originalIndex];

        var defsToMove = new List<BuildableDef>
        {
            originalDef
        };

        if (TabSortingMod.instance.Settings.GroupSameDesignator && originalDef.designatorDropdown != null)
        {
            defsToMove.AddRange(allDefsInCategory.Where(def =>
                def != originalDef && def.designatorDropdown == originalDef.designatorDropdown));
        }

        TabSorting.LogMessage(
            $"Reorder '{string.Join(", ", defsToMove)}' from index/uiOrder {originalIndex}/{allCurrentDefsInCategory[originalIndex].uiOrder} to index/uiOrder {newIndex}/{allCurrentDefsInCategory[newIndex].uiOrder}");


        // Increment all uiOrders above original def location down.
        for (var decIndex = originalIndex + 1; decIndex < allDefsInCategory.Count; decIndex++)
        {
            if (defsToMove.Contains(allDefsInCategory[decIndex]))
            {
                continue;
            }

            allDefsInCategory[decIndex].uiOrder -= 1;
            TabSortingMod.instance.Settings.ManualThingSorting[allDefsInCategory[decIndex].defName] =
                allDefsInCategory[decIndex].uiOrder;
        }

        foreach (var buildableDef in defsToMove)
        {
            // Remove def
            allCurrentDefsInCategory.Remove(buildableDef);
            allDefsInCategory.Remove(buildableDef);
            // Add def at new index with new uiOrder.
            allCurrentDefsInCategory.Insert(newIndex, buildableDef);
            allDefsInCategory.Insert(newIndex, buildableDef);
            buildableDef.uiOrder = uiOrderAtNewIndex;
            TabSortingMod.instance.Settings.ManualThingSorting[buildableDef.defName] = uiOrderAtNewIndex;
        }

        // Next add increment above new index.
        for (var upIndex = newIndex + 1; upIndex < allDefsInCategory.Count; upIndex++)
        {
            if (defsToMove.Contains(allDefsInCategory[upIndex]))
            {
                continue;
            }

            allDefsInCategory[upIndex].uiOrder += 1;
            TabSortingMod.instance.Settings.ManualThingSorting[allDefsInCategory[upIndex].defName] =
                allDefsInCategory[upIndex].uiOrder;
        }
    }
}