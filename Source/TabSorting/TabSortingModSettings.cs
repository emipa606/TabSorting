using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TabSorting;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class TabSortingModSettings : ModSettings
{
    private List<string> categoriesToIgnore = [];
    public bool GroupSameDesignator;

    public bool HideEmptyTabs;

    public Dictionary<string, int> ManualButtonSorting = new();

    private List<string> manualButtonSortingKeys;

    private List<int> manualButtonSortingValues;
    public List<DesignationCategoryDef> ManualCategoryMemory = [];

    public Dictionary<string, string> ManualSorting = new();

    private List<string> manualSortingKeys;

    private List<string> manualSortingValues;

    public Dictionary<string, string> ManualTabIcons = new();

    private List<string> manualTabIconsKeys;

    private List<string> manualTabIconsValues;

    public Dictionary<string, string> ManualTabs = new();

    private List<string> manualTabsKeys;

    public Dictionary<string, int> ManualTabSorting = new();

    private List<string> manualTabSortingKeys;

    private List<int> manualTabSortingValues;

    private List<string> manualTabsValues;
    public Dictionary<string, float> ManualThingSorting = new();
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
    public Dictionary<MainButtonDef, int> VanillaButtonOrderMemory { get; } = new();
    public List<DesignationCategoryDef> VanillaCategoryMemory { get; } = [];
    public Dictionary<Def, DesignationCategoryDef> VanillaItemMemory { get; } = new();
    public Dictionary<DesignationCategoryDef, int> VanillaTabOrderMemory { get; } = new();
    public Dictionary<BuildableDef, float> VanillaThingOrderMemory { get; } = new();

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
        Scribe_Values.Look(ref HideEmptyTabs, "HideEmptyTabs");

        Scribe_Collections.Look(ref categoriesToIgnore, "CategoriesToIgnore");
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
        foreach (var manualSortingKey in ManualSorting.Keys)
        {
            TabSortingMod.ResetSortOrder(manualSortingKey);
        }

        manualSortingKeys = [];
        manualSortingValues = [];
        ManualSorting = new Dictionary<string, string>();
        TabSorting.DoTheSorting();
    }

    public void ResetManualTabSortingValues()
    {
        manualTabSortingKeys = [];
        manualTabSortingValues = [];
        ManualTabSorting = new Dictionary<string, int>();
        TabSorting.RecacheTheTabSorting();
        TabSorting.DoTheSorting();
    }

    public void ResetManualThingSortingValues()
    {
        manualThingSortingKeys = [];
        manualThingSortingValues = [];
        ManualThingSorting = new Dictionary<string, float>();
        TabSorting.RecacheTheThingSorting();
        TabSorting.DoTheSorting();
    }

    public void ResetManualButtonSortingValues()
    {
        manualButtonSortingKeys = [];
        manualButtonSortingValues = [];
        ManualButtonSorting = new Dictionary<string, int>();
        TabSorting.RecacheTheButtonSorting();
        TabSorting.DoTheSorting();
    }
}