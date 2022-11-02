using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TabSorting;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class TabSortingModSettings : ModSettings
{
    public readonly Dictionary<MainButtonDef, int> VanillaButtonOrderMemory =
        new Dictionary<MainButtonDef, int>();

    public readonly List<DesignationCategoryDef> VanillaCategoryMemory = new List<DesignationCategoryDef>();

    public readonly Dictionary<Def, DesignationCategoryDef> VanillaItemMemory =
        new Dictionary<Def, DesignationCategoryDef>();

    public readonly Dictionary<DesignationCategoryDef, int> VanillaTabOrderMemory =
        new Dictionary<DesignationCategoryDef, int>();

    public readonly Dictionary<BuildableDef, float> VanillaThingOrderMemory =
        new Dictionary<BuildableDef, float>();

    private List<string> CategoriesToIgnore = new List<string>();
    public bool GroupSameDesignator;

    public Dictionary<string, int> ManualButtonSorting = new Dictionary<string, int>();

    private List<string> manualButtonSortingKeys;

    private List<int> manualButtonSortingValues;
    public List<DesignationCategoryDef> ManualCategoryMemory = new List<DesignationCategoryDef>();

    public Dictionary<string, string> ManualSorting = new Dictionary<string, string>();

    private List<string> manualSortingKeys;

    private List<string> manualSortingValues;

    public Dictionary<string, string> ManualTabIcons = new Dictionary<string, string>();

    private List<string> manualTabIconsKeys;

    private List<string> manualTabIconsValues;

    public Dictionary<string, string> ManualTabs = new Dictionary<string, string>();

    private List<string> manualTabsKeys;

    public Dictionary<string, int> ManualTabSorting = new Dictionary<string, int>();

    private List<string> manualTabSortingKeys;

    private List<int> manualTabSortingValues;

    private List<string> manualTabsValues;
    public Dictionary<string, float> ManualThingSorting = new Dictionary<string, float>();
    private List<string> manualThingSortingKeys;

    private List<float> manualThingSortingValues;

    public bool RemoveEmptyTabs = true;

    public bool SkipBuiltIn;

    public bool SortBedroomFurniture;

    public bool SortDecorations;
    public bool SortDoors;

    public bool SortDoorsAndWalls;

    public bool SortFences;

    public bool SortFloors;

    public bool SortGarden;

    public bool SortHospitalFurniture;

    public bool SortIdeologyFurniture;

    public bool SortKitchenFurniture;

    public bool SortLights = true;

    public bool SortResearchFurniture;

    public bool SortStorage;

    public bool SortTablesAndChairs;

    public bool SortTabs;

    public bool VerboseLogging;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref SortLights, "SortLights", true);
        Scribe_Values.Look(ref SortFloors, "SortFloors");
        Scribe_Values.Look(ref SortDoorsAndWalls, "SortDoorsAndWalls");
        Scribe_Values.Look(ref SortDoors, "SortDoors");
        Scribe_Values.Look(ref SortTablesAndChairs, "SortTablesAndChairs");
        Scribe_Values.Look(ref SortBedroomFurniture, "SortBedroomFurniture");
        Scribe_Values.Look(ref SortHospitalFurniture, "SortHospitalFurniture");
        Scribe_Values.Look(ref SortIdeologyFurniture, "SortIdeologyFurniture");
        Scribe_Values.Look(ref SortDecorations, "SortDecorations");
        Scribe_Values.Look(ref SortStorage, "SortStorage");
        Scribe_Values.Look(ref SortGarden, "SortGarden");
        Scribe_Values.Look(ref SortFences, "SortFences");
        Scribe_Values.Look(ref SortKitchenFurniture, "SortKitchenFurniture");
        Scribe_Values.Look(ref SortResearchFurniture, "SortResearchFurniture");

        Scribe_Values.Look(ref RemoveEmptyTabs, "RemoveEmptyTabs", true);
        Scribe_Values.Look(ref GroupSameDesignator, "GroupSameDesignator");
        Scribe_Values.Look(ref SortTabs, "SortTabs");
        Scribe_Values.Look(ref SkipBuiltIn, "SkipBuiltIn");

        Scribe_Collections.Look(ref CategoriesToIgnore, "CategoriesToIgnore");
        Scribe_Collections.Look(ref ManualSorting, "ManualSorting", LookMode.Value, LookMode.Value,
            ref manualSortingKeys, ref manualSortingValues);
        Scribe_Collections.Look(ref ManualTabs, "ManualTabs", LookMode.Value, LookMode.Value,
            ref manualTabsKeys, ref manualTabsValues);
        Scribe_Collections.Look(ref ManualTabSorting, "ManualTabSorting", LookMode.Value, LookMode.Value,
            ref manualTabSortingKeys, ref manualTabSortingValues);
        Scribe_Collections.Look(ref ManualThingSorting, "ManualThingSorting", LookMode.Value, LookMode.Value,
            ref manualThingSortingKeys, ref manualThingSortingValues);
        Scribe_Collections.Look(ref ManualButtonSorting, "ManualButtonSorting", LookMode.Value, LookMode.Value,
            ref manualButtonSortingKeys, ref manualButtonSortingValues);
        Scribe_Collections.Look(ref ManualTabIcons, "ManualTabIcons", LookMode.Value, LookMode.Value,
            ref manualTabIconsKeys, ref manualTabIconsValues);
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
    }

    public void ResetManualValues()
    {
        manualSortingKeys = new List<string>();
        manualSortingValues = new List<string>();
        ManualSorting = new Dictionary<string, string>();
        TabSorting.DoTheSorting();
    }

    public void ResetManualTabSortingValues()
    {
        manualTabSortingKeys = new List<string>();
        manualTabSortingValues = new List<int>();
        ManualTabSorting = new Dictionary<string, int>();
        TabSorting.RecacheTheTabSorting();
        TabSorting.DoTheSorting();
    }

    public void ResetManualThingSortingValues()
    {
        manualThingSortingKeys = new List<string>();
        manualThingSortingValues = new List<float>();
        ManualThingSorting = new Dictionary<string, float>();
        TabSorting.RecacheTheThingSorting();
        TabSorting.DoTheSorting();
    }

    public void ResetManualButtonSortingValues()
    {
        manualButtonSortingKeys = new List<string>();
        manualButtonSortingValues = new List<int>();
        ManualButtonSorting = new Dictionary<string, int>();
        TabSorting.RecacheTheButtonSorting();
        TabSorting.DoTheSorting();
    }
}