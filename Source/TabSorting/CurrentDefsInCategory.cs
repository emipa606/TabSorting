using System.Collections.Generic;
using Verse;

namespace TabSorting;

[StaticConstructorOnStartup]
public static class AllCurrentDefsInCategory
{
    public static List<BuildableDef> allCurrentDefsInCategory;

    static AllCurrentDefsInCategory()
    {
        allCurrentDefsInCategory = [];
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

        TabSorting.LogMessage(
            $"Reorder def '{allCurrentDefsInCategory[originalIndex]}' from index/uiOrder {originalIndex}/{allCurrentDefsInCategory[originalIndex].uiOrder} to index/uiOrder {newIndex}/{allCurrentDefsInCategory[newIndex].uiOrder}");
        _ = allCurrentDefsInCategory[originalIndex].uiOrder;
        var uiOrderAtNewIndex = allCurrentDefsInCategory[newIndex].uiOrder;
        var originalDef = allCurrentDefsInCategory[originalIndex];
        // Increment all uiOrders above original def location down.
        for (var decIndex = originalIndex + 1; decIndex < allCurrentDefsInCategory.Count; decIndex++)
        {
            allCurrentDefsInCategory[decIndex].uiOrder -= 1;
            TabSortingMod.instance.Settings.ManualThingSorting[allCurrentDefsInCategory[decIndex].defName] =
                allCurrentDefsInCategory[decIndex].uiOrder;
        }

        // Remove def
        allCurrentDefsInCategory.RemoveAt(originalIndex);

        // Add def at new index with new uiOrder.
        allCurrentDefsInCategory.Insert(newIndex, originalDef);
        // Next add increment above new index.
        for (var upIndex = newIndex + 1; upIndex < allCurrentDefsInCategory.Count; upIndex++)
        {
            allCurrentDefsInCategory[upIndex].uiOrder += 1;
            TabSortingMod.instance.Settings.ManualThingSorting[allCurrentDefsInCategory[upIndex].defName] =
                allCurrentDefsInCategory[upIndex].uiOrder;
        }

        allCurrentDefsInCategory[newIndex].uiOrder = uiOrderAtNewIndex;
        TabSortingMod.instance.Settings.ManualThingSorting[allCurrentDefsInCategory[newIndex].defName] =
            allCurrentDefsInCategory[newIndex].uiOrder;
    }
}