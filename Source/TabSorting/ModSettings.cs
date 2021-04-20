using System.Collections.Generic;
using Verse;

namespace TabSorting
{
    /// <summary>
    ///     Definition of the settings for the mod
    /// </summary>
    internal class TabSortingModSettings : ModSettings
    {
        public readonly List<DesignationCategoryDef> VanillaCategoryMemory = new List<DesignationCategoryDef>();

        public readonly Dictionary<Def, DesignationCategoryDef> VanillaItemMemory = new Dictionary<Def, DesignationCategoryDef>();

        public readonly Dictionary<DesignationCategoryDef, int> VanillaOrderMemory = new Dictionary<DesignationCategoryDef, int>();

        public Dictionary<string, string> ManualSorting = new Dictionary<string, string>();

        public bool RemoveEmptyTabs = true;

        public bool SkipBuiltIn;

        public bool SortBedroomFurniture;

        public bool SortDecorations;

        public bool SortDoorsAndWalls;

        public bool SortFences;

        public bool SortFloors;

        public bool SortGarden;

        public bool SortHospitalFurniture;

        public bool SortLights = true;

        public bool SortStorage;

        public bool SortTablesAndChairs;

        public bool SortTabs;

        private List<string> CategoriesToIgnore = new List<string>();

        private List<string> manualSortingKeys;

        private List<string> manualSortingValues;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SortLights, "SortLights", true);
            Scribe_Values.Look(ref SortFloors, "SortFloors");
            Scribe_Values.Look(ref SortDoorsAndWalls, "SortDoorsAndWalls");
            Scribe_Values.Look(ref SortTablesAndChairs, "SortTablesAndChairs");
            Scribe_Values.Look(ref SortBedroomFurniture, "SortBedroomFurniture");
            Scribe_Values.Look(ref SortHospitalFurniture, "SortHospitalFurniture");
            Scribe_Values.Look(ref SortDecorations, "SortDecorations");
            Scribe_Values.Look(ref SortStorage, "SortStorage");
            Scribe_Values.Look(ref SortGarden, "SortGarden");
            Scribe_Values.Look(ref SortFences, "SortFences");

            Scribe_Values.Look(ref RemoveEmptyTabs, "RemoveEmptyTabs", true);
            Scribe_Values.Look(ref SortTabs, "SortTabs");
            Scribe_Values.Look(ref SkipBuiltIn, "SkipBuiltIn");

            Scribe_Collections.Look(ref CategoriesToIgnore, "CategoriesToIgnore");
            Scribe_Collections.Look(ref ManualSorting, "ManualSorting", LookMode.Value, LookMode.Value, ref manualSortingKeys, ref manualSortingValues);
        }

        public void ResetManualValues()
        {
            manualSortingKeys = new List<string>();
            manualSortingValues = new List<string>();
            ManualSorting = new Dictionary<string, string>();
        }
    }
}