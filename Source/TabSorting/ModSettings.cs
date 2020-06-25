using System.Collections.Generic;
using Verse;

namespace TabSorting
{
    /// <summary>
    /// Definition of the settings for the mod
    /// </summary>
    internal class TabSortingModSettings : ModSettings
    {
        public bool SortLights = true;
        public bool SortFloors = false;
        public bool SortDoorsAndWalls = false;
        public bool SortTablesAndChairs = false;
        public bool SortBedroomFurniture = false;
        public bool SortDecorations = false;
        public bool SortStorage = false;

        public bool RemoveEmptyTabs = true;
        public List<string> CategoriesToIgnore = new List<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SortLights, "SortLights", true, false);
            Scribe_Values.Look(ref SortFloors, "SortFloors", false, false);
            Scribe_Values.Look(ref SortDoorsAndWalls, "SortDoorsAndWalls", false, false);
            Scribe_Values.Look(ref SortTablesAndChairs, "SortTablesAndChairs", false, false);
            Scribe_Values.Look(ref SortBedroomFurniture, "SortBedroomFurniture", false, false);
            Scribe_Values.Look(ref SortDecorations, "SortDecorations", false, false);
            Scribe_Values.Look(ref SortStorage, "SortStorage", false, false);

            Scribe_Values.Look(ref RemoveEmptyTabs, "RemoveEmptyTabs", true, false);

            Scribe_Collections.Look(ref CategoriesToIgnore, "CategoriesToIgnore");
        }
    }
}