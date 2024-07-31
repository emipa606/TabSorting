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
        var origIndexInFullList = originalIndex;
        var newIndexInFullList = newIndex;

        var defsToMove = new List<BuildableDef>
        {
            originalDef
        };

        if (TabSortingMod.instance.Settings.GroupSameDesignator)
        {
            defsToMove.AddRange(allDefsInCategory.Where(def =>
                def != originalDef && def.designatorDropdown == originalDef.designatorDropdown));
            origIndexInFullList = allDefsInCategory.FindIndex(def => def == originalDef);
            newIndexInFullList = allDefsInCategory.FindIndex(def => def == allCurrentDefsInCategory[newIndex]);
            if (originalDef.designatorDropdown != null)
            {
                defsToMove.AddRange(allDefsInCategory.Where(def =>
                    def != originalDef && def.designatorDropdown == originalDef.designatorDropdown));
            }

            // If moving down the listings (up in uiOrder)
            if (origIndexInFullList < newIndexInFullList)
            {
                // If trying to move past another group...
                if (allDefsInCategory[newIndexInFullList].designatorDropdown != null)
                {
                    var sizeOfGroupAtNewIndex = allDefsInCategory.Count(def =>
                        def.designatorDropdown == allCurrentDefsInCategory[newIndex].designatorDropdown);
                    newIndexInFullList += sizeOfGroupAtNewIndex - 1;
                    uiOrderAtNewIndex = allDefsInCategory[newIndexInFullList].uiOrder;
                }
            }
        }

        TabSorting.LogMessage(
            $"Reorder '{string.Join(", ", defsToMove)}' from index/uiOrder {originalIndex}/{allCurrentDefsInCategory[originalIndex].uiOrder} to index/uiOrder {newIndex}/{allCurrentDefsInCategory[newIndex].uiOrder}");


        // Decrement all uiOrders above original def location down.
        for (var decIndex = origIndexInFullList; decIndex < allDefsInCategory.Count; decIndex++)
        {
            if (defsToMove.Contains(allDefsInCategory[decIndex]))
            {
                continue;
            }

            TabSortingMod.instance.Settings.ManualThingSorting[allDefsInCategory[decIndex].defName] =
                allDefsInCategory[decIndex].uiOrder;
        }

        var increment = 0;
        allCurrentDefsInCategory.Remove(originalDef);
        allCurrentDefsInCategory.Insert(newIndex, originalDef);

        foreach (var buildableDef in defsToMove)
        {
            // Remove def
            allDefsInCategory.Remove(buildableDef);
            // Add def at new index with new uiOrder.
            allDefsInCategory.Insert(newIndexInFullList, buildableDef);
            buildableDef.uiOrder = uiOrderAtNewIndex + increment;
            TabSortingMod.instance.Settings.ManualThingSorting[buildableDef.defName] = buildableDef.uiOrder;
            increment += 1;
        }

        var previousUiOrder = allDefsInCategory[newIndexInFullList].uiOrder;
        // Next add increment above new index.
        for (var upIndex = newIndexInFullList; upIndex < allDefsInCategory.Count; upIndex++)
        {
            if (defsToMove.Contains(allDefsInCategory[upIndex]))
            {
                previousUiOrder = allDefsInCategory[upIndex].uiOrder;
                continue;
            }

            allDefsInCategory[upIndex].uiOrder += defsToMove.Count;
            if (allDefsInCategory[upIndex].uiOrder <= previousUiOrder)
            {
                allDefsInCategory[upIndex].uiOrder += 1 + previousUiOrder - allDefsInCategory[upIndex].uiOrder;
            }

            TabSortingMod.instance.Settings.ManualThingSorting[allDefsInCategory[upIndex].defName] =
                allDefsInCategory[upIndex].uiOrder;
            previousUiOrder = allDefsInCategory[upIndex].uiOrder;
        }
    }
}