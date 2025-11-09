using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TabSorting;

[StaticConstructorOnStartup]
public static class TabSorting
{
    private static readonly bool cherryPickerLoaded;
    private static readonly bool betterArchitechtMenuLoaded;
    private static readonly FieldInfo cherryPickerProcessedDefsField;
    private static readonly FieldInfo betterArchitectMenuParentCategoryField;

    private static readonly HashSet<string> modIdsToIgnore =
    [
        "atlas.androidtiers",
        "dubwise.dubsbadhygiene",
        "vanillaexpanded.vfepower",
        "dismarzero.vgp.vgpgardentools",
        "vanillaexpanded.vfepropsanddecor",
        "kentington.saveourship2",
        "flashpoint55.poweredfloorpanelmod"
    ];

    private static HashSet<string> defsToIgnore;

    private static readonly HashSet<string> defsToIgnoreStatic =
    [
        "FM_AIManager",
        "PRF_MiniDroneColumn",
        "PRF_RecipeDatabase",
        "PRF_TypeOneAssembler_I",
        "PRF_TypeTwoAssembler_I",
        "PRF_TypeTwoAssembler_II",
        "PRF_TypeTwoAssembler_III"
    ];

    private static readonly HashSet<string> defsPrefixToIgnoreStatic =
    [
        "VFEM2_CobblestoneWall_"
    ];

    private static HashSet<string> changedDefNames;

    private static readonly HashSet<string> tabsToIgnore =
    [
        "Planning",
        "Shapes",
        "Terraform"
    ];

    private static readonly HashSet<string> tabsToIgnoreThingsIn =
    [
        "Terraform"
    ];

    private static readonly HashSet<string> namespacesToIgnore =
    [
        "RimWorld",
        "DubRoss"
    ];

    private static readonly bool mintMenusLoaded;
    public static readonly bool GardenToolsLoaded;
    public static readonly bool FencesAndFloorsLoaded;
    public static readonly bool ArchitectIconsLoaded;
    private static readonly bool blueprintsLoaded;
    public static readonly Dictionary<string, Texture2D> IconsCache;
    private static readonly FieldInfo basePowerConsumptionField;

    static TabSorting()
    {
        var harmony = new Harmony("Mlie.TabSorting");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        basePowerConsumptionField = AccessTools.Field(typeof(CompProperties_Power), "basePowerConsumption");
        TabSortingMod.PlusTexture = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton");
        mintMenusLoaded = ModLister.GetActiveModWithIdentifier("Dubwise.DubsMintMenus", true) != null;
        betterArchitechtMenuLoaded = ModLister.GetActiveModWithIdentifier("ferny.BetterArchitect", true) != null;
        GardenToolsLoaded = ModLister.GetActiveModWithIdentifier("dismarzero.vgp.vgpgardentools", true) != null;
        FencesAndFloorsLoaded = ModLister.GetActiveModWithIdentifier("Mlie.FencesAndFloors", true) != null;
        ArchitectIconsLoaded = ModLister.GetActiveModWithIdentifier("com.bymarcin.ArchitectIcons", true) != null;
        cherryPickerLoaded = ModLister.HasActiveModWithName("Cherry Picker");
        if (cherryPickerLoaded)
        {
            cherryPickerProcessedDefsField =
                AccessTools.Field(AccessTools.TypeByName("CherryPicker.CherryPickerUtility"), "processedDefs");
            if (cherryPickerProcessedDefsField == null)
            {
                LogMessage(
                    "Failed to find the processedDefs field from Cherry Picker, will not be able to check for removed defs.");
                cherryPickerLoaded = false;
            }
        }

        if (betterArchitechtMenuLoaded)
        {
            betterArchitectMenuParentCategoryField =
                AccessTools.Field(AccessTools.TypeByName("BetterArchitect.NestedCategoryExtension"), "parentCategory");
            if (betterArchitectMenuParentCategoryField == null)
            {
                LogMessage(
                    "Failed to find the parentCategory field from Better Architect Menu, will not be able to check for categories not to remove.");
                betterArchitechtMenuLoaded = false;
            }
        }

        blueprintsLoaded = DefDatabase<DesignationDef>.GetNamedSilentFail("Blueprints") != null;

        refreshIgnoredDefs();

        DoTheSorting();

        if (!ArchitectIconsLoaded)
        {
            return;
        }

        IconsCache = new Dictionary<string, Texture2D>();
        var architectCustomPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "ArchitectIcons");
        if (Directory.Exists(architectCustomPath))
        {
            var customImages = Directory.GetFiles(architectCustomPath, "*.png");
            foreach (var image in customImages)
            {
                var texture = new Texture2D((int)TabSortingMod.TabIconSize.x, (int)TabSortingMod.TabIconSize.y);
                texture.LoadImage(File.ReadAllBytes(image));
                var imagePath = Path.GetFileNameWithoutExtension(image);
                IconsCache[imagePath] = texture;
            }
        }

        foreach (var image in ContentFinder<Texture2D>.GetAllInFolder("UI/ArchitectIcons"))
        {
            IconsCache[Path.GetFileNameWithoutExtension(image.name)] = image;
        }

        foreach (var image in ContentFinder<Texture2D>.GetAllInFolder("UI/ArchitectIcons/Default"))
        {
            IconsCache[Path.GetFileNameWithoutExtension(image.name)] = image;
        }

        LogMessage($"Found {IconsCache.Count} icons in ArchitectIcons to choose from.", true);
        var findArchitectTabCategoryIconMethod =
            AccessTools.Method("ArchitectIcons.Resources:FindArchitectTabCategoryIcon");
        harmony.Patch(findArchitectTabCategoryIconMethod,
            new HarmonyMethod(typeof(TabSorting), nameof(architectIconsPrefix)));
    }

    private static HashSet<string> DefsToIgnore
    {
        get
        {
            if (defsToIgnore != null)
            {
                return defsToIgnore;
            }

            defsToIgnore = defsToIgnoreStatic;

            if (!cherryPickerLoaded)
            {
                return defsToIgnore;
            }

            (cherryPickerProcessedDefsField.GetValue(null) as HashSet<Def>).Do(def => defsToIgnore.Add(def.defName));

            return defsToIgnore;
        }
    }

    private static void refreshIgnoredDefs()
    {
        defsToIgnore = null;
        var ignoreMods = (from mod in LoadedModManager.RunningModsListForReading
            where modIdsToIgnore.Contains(mod.PackageId)
            select mod).ToList();
        if (ignoreMods.Count > 0)
        {
            foreach (var mod in ignoreMods)
            {
                LogMessage($"{mod.Name} has {mod.AllDefs.Count()} definitions, adding to ignore.");
                foreach (var def in mod.AllDefs)
                {
                    defsToIgnoreStatic.Add(def.defName);
                }
            }
        }

        foreach (var thingDef in DefDatabase<ThingDef>.AllDefs.Where(def => def.designationCategory == null))
        {
            defsToIgnoreStatic.Add(thingDef.defName);
        }

        foreach (var terrainDef in DefDatabase<TerrainDef>.AllDefs.Where(def => def.designationCategory == null))
        {
            defsToIgnoreStatic.Add(terrainDef.defName);
        }

        if (tabsToIgnoreThingsIn.Any())
        {
            foreach (var tabDefName in tabsToIgnoreThingsIn)
            {
                var tab = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(tabDefName);
                if (tab == null)
                {
                    continue;
                }

                var thingsInTab = from td in DefDatabase<ThingDef>.AllDefsListForReading
                    where td.designationCategory == tab
                    select td;
                foreach (var thingDef in thingsInTab)
                {
                    defsToIgnoreStatic.Add(thingDef.defName);
                }

                var terrainInTab = from tr in DefDatabase<TerrainDef>.AllDefsListForReading
                    where tr.designationCategory == tab
                    select tr;
                foreach (var terrainDef in terrainInTab)
                {
                    defsToIgnoreStatic.Add(terrainDef.defName);
                }
            }
        }

        foreach (var def in DefDatabase<Def>.AllDefsListForReading)
        {
            if (defsPrefixToIgnoreStatic.Any(prefix => def.defName.StartsWith(prefix)))
            {
                defsToIgnoreStatic.Add(def.defName);
            }
        }
    }

    public static string GetCustomTabIcon(string tabName)
    {
        if (TabSortingMod.Instance.Settings.ManualTabIcons == null ||
            !TabSortingMod.Instance.Settings.ManualTabIcons.Any() ||
            !TabSortingMod.Instance.Settings.ManualTabIcons.ContainsKey(tabName))
        {
            return tabName;
        }

        if (IconsCache.ContainsKey(TabSortingMod.Instance.Settings.ManualTabIcons[tabName]))
        {
            return TabSortingMod.Instance.Settings.ManualTabIcons[tabName];
        }

        TabSortingMod.Instance.Settings.ManualTabIcons.Remove(tabName);

        return tabName;
    }

    public static void RemoveManualTab(DesignationCategoryDef manualTab)
    {
        TabSortingMod.Instance.Settings.ManualSorting.RemoveAll(pair => pair.Value == manualTab.defName);
        TabSortingMod.Instance.Settings.ManualTabs.RemoveAll(pair => pair.Key == manualTab.defName);
        TabSortingMod.Instance.Settings.ManualCategoryMemory.RemoveAll(def => def.defName == manualTab.defName);
        TabSortingMod.Instance.Settings.ManualTabSorting.RemoveAll(pair => pair.Key == manualTab.defName);
        TabSortingMod.Instance.Settings.ManualTabIcons.RemoveAll(pair => pair.Key == manualTab.defName);
        removeEmptyDesignationCategoryDef(manualTab);
    }

    public static void RecacheTheTabSorting()
    {
        foreach (var tabOrder in TabSortingMod.Instance.Settings.VanillaTabOrderMemory)
        {
            TabSortingMod.Instance.Settings.ManualTabSorting.Add(tabOrder.Key.defName, tabOrder.Value);
        }

        foreach (var settingsManualTab in TabSortingMod.Instance.Settings.ManualTabs)
        {
            TabSortingMod.Instance.Settings.ManualTabSorting.Add(settingsManualTab.Key, 1);
        }
    }

    public static void RecacheTheThingSorting()
    {
        foreach (var thingOrder in TabSortingMod.Instance.Settings.VanillaThingOrderMemory)
        {
            TabSortingMod.Instance.Settings.ManualThingSorting.Add(thingOrder.Key.defName, thingOrder.Value);
        }
    }

    public static void RecacheTheButtonSorting()
    {
        foreach (var buttonOrder in TabSortingMod.Instance.Settings.VanillaButtonOrderMemory)
        {
            TabSortingMod.Instance.Settings.ManualButtonSorting.Add(buttonOrder.Key.defName, buttonOrder.Value);
        }
    }

    public static void DoTheSorting()
    {
        LogMessage("Starting a new sorting-session");
        changedDefNames = [];
        defsToIgnore = null;
        if (!TabSortingMod.Instance.Settings.VanillaCategoryMemory.Any())
        {
            foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Where(def =>
                         !tabsToIgnore.Contains(def.defName)))
            {
                TabSortingMod.Instance.Settings.VanillaCategoryMemory.Add(categoryDef);
                TabSortingMod.Instance.Settings.VanillaTabOrderMemory.Add(categoryDef, categoryDef.order);
            }
        }

        if (!TabSortingMod.Instance.Settings.VanillaButtonOrderMemory.Any())
        {
            foreach (var buttonDef in DefDatabase<MainButtonDef>.AllDefsListForReading)
            {
                TabSortingMod.Instance.Settings.VanillaButtonOrderMemory.Add(buttonDef, buttonDef.order);
            }
        }

        TabSortingMod.Instance.Settings.ManualTabs ??= new Dictionary<string, string>();

        TabSortingMod.Instance.Settings.ManualCategoryMemory ??= [];

        if (!TabSortingMod.Instance.Settings.VanillaItemMemory.Any())
        {
            foreach (var buildableDef in DefDatabase<BuildableDef>.AllDefsListForReading.Where(def =>
                         def.designationCategory != null))
            {
                TabSortingMod.Instance.Settings.VanillaItemMemory.Add(buildableDef, buildableDef.designationCategory);
                TabSortingMod.Instance.Settings.VanillaThingOrderMemory.Add(buildableDef, buildableDef.uiOrder);
            }

            foreach (var terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading.Where(def =>
                         !DefsToIgnore.Contains(def.defName)))
            {
                TabSortingMod.Instance.Settings.VanillaItemMemory.TryAdd(terrainDef, terrainDef.designationCategory);
                TabSortingMod.Instance.Settings.VanillaThingOrderMemory.TryAdd(terrainDef, terrainDef.uiOrder);
            }
        }
        else
        {
            restoreVanillaSorting();
        }

        if (TabSortingMod.Instance.Settings.ManualTabs.Any())
        {
            foreach (var manualTab in TabSortingMod.Instance.Settings.ManualTabs)
            {
                DesignationCategoryDef designationCategory;
                if (TabSortingMod.Instance.Settings.ManualCategoryMemory.Any(def => def.defName == manualTab.Key))
                {
                    designationCategory = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(manualTab.Key);
                    designationCategory.specialDesignatorClasses =
                        [typeof(Designator_Cancel), typeof(Designator_Deconstruct)];
                    AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators")
                        .Invoke(designationCategory, []);
                    //designationCategory.ResolveDesignators();
                    continue;
                }

                var order = 1;
                TabSortingMod.Instance.Settings.ManualTabSorting ??= new Dictionary<string, int>();

                if (TabSortingMod.Instance.Settings.ManualTabSorting.TryGetValue(manualTab.Key, out var value))
                {
                    order = value;
                }

                var orders = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Select(def => def.order);
                while (orders.Contains(order))
                {
                    order++;
                }

                var newTab = new DesignationCategoryDef
                {
                    defName = manualTab.Key,
                    label = manualTab.Value,
                    order = order
                };

                LogMessage($"Recreating manual tab {manualTab.Key}");
                DefGenerator.AddImpliedDef(newTab);
                TabSortingMod.Instance.Settings.ManualCategoryMemory.Add(
                    DefDatabase<DesignationCategoryDef>.GetNamed(manualTab.Key));
                designationCategory = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(manualTab.Key);
                designationCategory.specialDesignatorClasses =
                    [typeof(Designator_Cancel), typeof(Designator_Deconstruct)];
                AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators")
                    .Invoke(designationCategory, []);
                //designationCategory.ResolveDesignators();
            }
        }

        TabSortingMod.Instance.Settings.ManualTabSorting ??= new Dictionary<string, int>();

        if (!TabSortingMod.Instance.Settings.ManualTabSorting.Any())
        {
            foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                TabSortingMod.Instance.Settings.ManualTabSorting[categoryDef.defName] = categoryDef.order;
            }
        }

        TabSortingMod.Instance.Settings.ManualButtonSorting ??= new Dictionary<string, int>();

        if (!TabSortingMod.Instance.Settings.ManualButtonSorting.Any())
        {
            foreach (var buttonDef in DefDatabase<MainButtonDef>.AllDefsListForReading)
            {
                TabSortingMod.Instance.Settings.ManualButtonSorting[buttonDef.defName] = buttonDef.order;
            }
        }

        TabSortingMod.Instance.Settings.ManualThingSorting ??= new Dictionary<string, float>();

        if (!TabSortingMod.Instance.Settings.ManualThingSorting.Any())
        {
            foreach (var buildableDef in DefDatabase<BuildableDef>.AllDefsListForReading)
            {
                TabSortingMod.Instance.Settings.ManualThingSorting[buildableDef.defName] = buildableDef.uiOrder;
            }
        }

        TabSortingMod.Instance.Settings.VanillaCategoryMemory.SortBy(def => def.label);

        TabSortingMod.Instance.Settings ??= new TabSortingModSettings
        {
            SortLights = true,
            SortFloors = false,
            SortDoorsAndWalls = false,
            SortDoors = false,
            SortBedroomFurniture = false,
            SortHospitalFurniture = false,
            SortKitchenFurniture = false,
            SortResearchFurniture = false,
            SortTablesAndChairs = false,
            SortDecorations = false,
            SortStorage = false,
            SortGarden = false,
            SortFences = false,
            RemoveEmptyTabs = true,
            SortTabs = false,
            SkipBuiltIn = false
        };

        sortManually();

        sortIdeologyFurniture();

        sortLights();

        sortFloors();

        sortDoorsAndWalls();

        sortTablesAndChairs();

        sortBedroomFurniture();

        sortKitchenFurniture();

        sortResearchFurniture();

        sortHospitalFurniture();

        sortGarden();

        sortStorage();

        sortFences();

        sortDecorations();

        var designationCategoriesToRemove = new List<DesignationCategoryDef>();

        foreach (var designationCategoryDef in from dd in DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                 where !tabsToIgnore.Contains(dd.defName)
                 select dd)
        {
            designationCategoryDef.ResolveReferences();
            if (checkEmptyDesignationCategoryDef(designationCategoryDef))
            {
                designationCategoriesToRemove.Add(designationCategoryDef);
            }
        }

        if (TabSortingMod.Instance.Settings.RemoveEmptyTabs)
        {
            removeEmptyCategories(designationCategoriesToRemove);
        }

        foreach (var buttonSortInfo in TabSortingMod.Instance.Settings.ManualButtonSorting)
        {
            var buttonDef = DefDatabase<MainButtonDef>.GetNamedSilentFail(buttonSortInfo.Key);
            if (buttonDef == null)
            {
                continue;
            }

            buttonDef.order = buttonSortInfo.Value;
        }

        foreach (var thingSortInfo in TabSortingMod.Instance.Settings.ManualThingSorting)
        {
            var buildableDef = DefDatabase<BuildableDef>.GetNamedSilentFail(thingSortInfo.Key);
            if (buildableDef == null)
            {
                continue;
            }

            buildableDef.uiOrder = thingSortInfo.Value;
        }

        if (Current.Game != null)
        {
            var mainRoot = Find.MainButtonsRoot;
            var prop = mainRoot.GetType().GetField("allButtonsInOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var currentButtons = (List<MainButtonDef>)prop.GetValue(mainRoot);
            prop.SetValue(mainRoot, (from buttonDef in DefDatabase<MainButtonDef>.AllDefs
                where currentButtons.Contains(buttonDef)
                orderby buttonDef.order
                select buttonDef).ToList());
        }

        if (!TabSortingMod.Instance.Settings.SortTabs)
        {
            foreach (var tabSortInfo in TabSortingMod.Instance.Settings.ManualTabSorting)
            {
                var tab = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(tabSortInfo.Key);
                if (tab == null)
                {
                    continue;
                }

                tab.order = tabSortInfo.Value;
            }

            refreshArchitectMenu();
            return;
        }

        var topValue = 800;
        var designationCategoryDefs =
            from dd in DefDatabase<DesignationCategoryDef>.AllDefs
            orderby dd.label
            select dd;
        var steps = (int)Math.Floor((decimal)((float)topValue / designationCategoryDefs.Count()));
        foreach (var designationCategoryDef in designationCategoryDefs)
        {
            topValue -= steps;
            if (TabSortingMod.Instance.Settings.SkipBuiltIn && designationCategoryDef.label is "orders" or "zone")
            {
                continue;
            }

            designationCategoryDef.order = topValue;
        }

        refreshArchitectMenu();
    }

    private static void removeEmptyCategories(List<DesignationCategoryDef> designationCategoriesToRemove)
    {
        LogMessage("Starting removal of empty categories");
        if (betterArchitechtMenuLoaded)
        {
            LogMessage("Better Architect Menu loaded, doing check for nested and special DesignationCategoryDefs");
            var filteredCategories = new List<DesignationCategoryDef>();
            var hasNestedCategories = new HashSet<DesignationCategoryDef>();
            var allCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading;
            foreach (var designationCategoryDef in allCategories)
            {
                var nestedCategoryExtension = designationCategoryDef.modExtensions?.FirstOrDefault(extension =>
                    extension.GetType().Name == "NestedCategoryExtension");
                if (nestedCategoryExtension == null)
                {
                    continue;
                }

                if (betterArchitectMenuParentCategoryField.GetValue(nestedCategoryExtension) is DesignationCategoryDef
                    parentCategory)
                {
                    hasNestedCategories.Add(parentCategory);
                }
            }

            foreach (var designationCategory in designationCategoriesToRemove)
            {
                if (hasNestedCategories.Contains(designationCategory))
                {
                    LogMessage(
                        $"Not removing {designationCategory.defName} as its used as a container for nested categories in Better Architect Menu.");
                    continue;
                }

                if (designationCategory.modExtensions?.Any() == false)
                {
                    filteredCategories.Add(designationCategory);
                    continue;
                }

                if (designationCategory.modExtensions?.Any(extension =>
                        extension.GetType().Name == "SpecialCategoryExtension") == true)
                {
                    LogMessage(
                        $"Not removing {designationCategory.defName} since it's a special category used in Better Architect Menu.");
                    continue;
                }

                filteredCategories.Add(designationCategory);
            }

            LogMessage($"Skipped {designationCategoriesToRemove.Count - filteredCategories.Count} categories");
            designationCategoriesToRemove = filteredCategories;
        }

        for (var i = designationCategoriesToRemove.Count - 1; i >= 0; i--)
        {
            LogMessage($"Removing {designationCategoriesToRemove[i].defName} since its empty now.", true);
            removeEmptyDesignationCategoryDef(designationCategoriesToRemove[i]);
        }
    }

    /// <summary>
    ///     Goes through all items and checks if there are any references to the selected category
    ///     Removes the category if there are none
    /// </summary>
    private static bool checkEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
    {
        if (currentCategory == null)
        {
            return false;
        }

        if (currentCategory.defName is "Orders" or "Zone")
        {
            return false;
        }

        if (blueprintsLoaded && currentCategory.defName == "Blueprints")
        {
            return false;
        }

        if (TabSortingMod.Instance.Settings.ManualTabs.ContainsKey(currentCategory.defName))
        {
            return false;
        }

        LogMessage($"Checking current defs in {currentCategory.defName}");
        var thingsLeft = from td in DefDatabase<ThingDef>.AllDefsListForReading
            where td.designationCategory == currentCategory
            select td;
        if (thingsLeft.Count() != 0)
        {
            return false;
        }

        LogMessage($"Checking current terrainDefs in {currentCategory.defName}");
        var moreThingsLeft = from tr in DefDatabase<TerrainDef>.AllDefsListForReading
            where tr.designationCategory == currentCategory
            select tr;
        if (moreThingsLeft.Count() != 0)
        {
            return false;
        }

        LogMessage($"Checking current specialDesignatorClasses in {currentCategory.defName}");
        if (!currentCategory.specialDesignatorClasses.Any())
        {
            return true;
        }

        foreach (var specialDesignatorClass in currentCategory.specialDesignatorClasses)
        {
            if (namespacesToIgnore.Contains(specialDesignatorClass.Namespace))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public static DesignationCategoryDef GetDesignationFromDatabase(string categoryString)
    {
        if (TabSortingMod.Instance.Settings.ManualTabs.ContainsKey(categoryString))
        {
            return TabSortingMod.Instance.Settings.ManualCategoryMemory.First(def => def.defName == categoryString);
        }

        if (!TabSortingMod.Instance.Settings.VanillaCategoryMemory.Any(def => def.defName == categoryString))
        {
            return null;
        }

        var returnValue =
            TabSortingMod.Instance.Settings.VanillaCategoryMemory.First(def => def.defName == categoryString);
        if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryString) != null)
        {
            return returnValue;
        }

        LogMessage($"Could not find tab {categoryString}, creating");
        DefGenerator.AddImpliedDef(returnValue);

        return returnValue;
    }

    private static void refreshArchitectMenu()
    {
        LogMessage("Sorting-session done");


        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }


        MainButtonDefOf.Architect.tabWindowClass
            .GetMethod("CacheDesPanels", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(MainButtonDefOf.Architect.TabWindow, null);

        if (!mintMenusLoaded)
        {
            return;
        }

        LogMessage("Recaching tabs in Dubs Mint Menus");
        try
        {
            var mainMenuTab = DefDatabase<MainButtonDef>.GetNamed("MintMenus");
            mainMenuTab.tabWindowClass.GetField("desPanelsCached", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(mainMenuTab.TabWindow, null);
        }
        catch (Exception exception)
        {
            LogMessage(
                $"Failed to update cache in Dubs Mint Menus, the new sort will not have effect.\n{exception}",
                true);
        }
    }

    /// <summary>
    ///     Removes a (hopefully) empty category
    /// </summary>
    /// <param name="currentCategory"></param>
    private static void removeEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
    {
        GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), typeof(DesignationCategoryDef), "Remove",
            currentCategory);
    }

    private static void restoreVanillaSorting()
    {
        if (ModLister.HasActiveModWithName("Vanilla Factions Expanded - Medieval 2"))
        {
            LogMessage(
                "Vanilla Factions Expanded - Medieval 2 loaded, will not restore order before sorting as VE add things in a silly way. This may cause issues with sorting.");
            return;
        }

        LogMessage("Restoring all things to vanilla sorting");

        foreach (var designationCategoryDef in TabSortingMod.Instance.Settings.VanillaCategoryMemory)
        {
            var designation = GetDesignationFromDatabase(designationCategoryDef.defName);
            if (designation != null)
            {
                designation.order = TabSortingMod.Instance.Settings.VanillaTabOrderMemory[designationCategoryDef];
            }
        }

        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(thingDef =>
                     TabSortingMod.Instance.Settings.VanillaItemMemory.ContainsKey(thingDef)))
        {
            def.designationCategory = TabSortingMod.Instance.Settings.VanillaItemMemory.GetValueOrDefault(def);
        }

        foreach (var def in DefDatabase<TerrainDef>.AllDefsListForReading.Where(terrainDef =>
                     TabSortingMod.Instance.Settings.VanillaItemMemory.ContainsKey(terrainDef)))
        {
            def.designationCategory = TabSortingMod.Instance.Settings.VanillaItemMemory.GetValueOrDefault(def);
        }

        foreach (var designationCategoryDef in TabSortingMod.Instance.Settings.VanillaCategoryMemory)
        {
            designationCategoryDef.ResolveReferences();
        }
    }

    /// <summary>
    ///     Sort Ideology furniture to the Ideology-tab if needed
    /// </summary>
    private static void sortIdeologyFurniture()
    {
        if (ModLister.GetActiveModWithIdentifier("ludeon.rimworld.ideology", true) == null)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("Ideology");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the IdeologyTab-def, will not sort Ideology items.",
                "IdeologyTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortIdeologyFurniture)
        {
            return;
        }

        var ideologyFurnitureInGame = (from furniture in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(furniture.defName) && !changedDefNames.Contains(furniture.defName) &&
                  furniture.designationCategory != null && furniture.designationCategory.defName != "Ideology" &&
                  (furniture.placeWorkers?.Contains(typeof(PlaceWorker_RitualPosition)) == true ||
                   furniture.placeWorkers?.Contains(typeof(PlaceWorker_RitualSeat)) == true ||
                   furniture.comps?.Any(properties => properties.compClass == typeof(CompRelicContainer)) == true ||
                   furniture.comps?.Any(properties => properties is CompProperties_Lightball) == true ||
                   furniture.comps?.Any(properties => properties is CompProperties_Loudspeaker) == true ||
                   furniture.thingClass == typeof(Building_StylingStation) ||
                   furniture.isAltar ||
                   furniture.ideoBuildingNamerBase != null ||
                   furniture.label?.ToLower().Contains("ritual") == true)
            select furniture).ToList();
        foreach (var furniture in ideologyFurnitureInGame)
        {
            LogMessage(
                $"Changing designation for fence {furniture.defName} from {furniture.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(furniture.defName);
            furniture.designationCategory = designationCategory;
        }

        LogMessage($"Moved {ideologyFurnitureInGame.Count} furniture to the Ideology-tab.", true);
    }

    /// <summary>
    ///     Sort kitchen furniture to the Kitchen-tab
    /// </summary>
    private static void sortKitchenFurniture()
    {
        var designationCategory = GetDesignationFromDatabase("KitchenTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the KitchenTab-def, will not sort kitchen items.",
                "KitchenTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortKitchenFurniture)
        {
            return;
        }

        var foodCategories = ThingCategoryDefOf.Foods.ThisAndChildCategoryDefs;

        var foods = (from food in DefDatabase<ThingDef>.AllDefsListForReading
            where food.thingCategories != null && food.thingCategories.SharesElementWith(foodCategories)
            select food).ToList();
        var foodRecipes = (from recipe in DefDatabase<RecipeDef>.AllDefsListForReading
            where recipe.ProducedThingDef != null && foods.Contains(recipe.ProducedThingDef)
            select recipe).ToList();
        var recipeMakers = (from foodMaker in DefDatabase<ThingDef>.AllDefsListForReading
            where foodMaker.recipes != null && foodMaker.recipes.SharesElementWith(foodRecipes)
            select foodMaker).ToList();
        var foodMakers = new HashSet<ThingDef>();
        foodMakers.AddRange(recipeMakers);
        foreach (var thingDef in foods)
        {
            if (thingDef.recipes == null || !thingDef.recipes.Any())
            {
                continue;
            }

            foreach (var thingDefRecipe in thingDef.recipes)
            {
                if (thingDefRecipe.recipeUsers == null || !thingDefRecipe.recipeUsers.Any())
                {
                    continue;
                }

                foodMakers.AddRange(thingDefRecipe.recipeUsers);
            }
        }

        foreach (var recipeDef in foodRecipes)
        {
            if (recipeDef.recipeUsers == null || !recipeDef.recipeUsers.Any())
            {
                continue;
            }

            foodMakers.AddRange(recipeDef.recipeUsers);
        }

        LogMessage($"Found {foodMakers.Count} food processing buildings");

        var foodMakersInGame = (from foodMaker in foodMakers
            where !DefsToIgnore.Contains(foodMaker.defName) && !changedDefNames.Contains(foodMaker.defName) &&
                  foodMaker.designationCategory != null
            select foodMaker).ToList();
        var affectedByFacilities = new HashSet<ThingDef>();
        foreach (var foodMaker in foodMakersInGame)
        {
            if (!foodMaker.comps.Any())
            {
                continue;
            }

            var affections = foodMaker.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (affections == null || !affections.linkableFacilities.Any())
            {
                continue;
            }

            foreach (var facility in affections.linkableFacilities)
            {
                if (changedDefNames.Contains(facility.defName))
                {
                    continue;
                }

                if (facility.designationCategory == null)
                {
                    continue;
                }

                var compProperties = facility.GetCompProperties<CompProperties_Facility>();
                if (compProperties?.statOffsets == null)
                {
                    continue;
                }

                var found = false;
                foreach (var offset in compProperties.statOffsets)
                {
                    if (offset.stat != StatDefOf.WorkTableEfficiencyFactor &&
                        offset.stat != StatDefOf.WorkTableWorkSpeedFactor)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    continue;
                }

                affectedByFacilities.Add(facility);
            }
        }

        foreach (var foodMaker in foodMakersInGame)
        {
            LogMessage(
                $"Changing designation for building {foodMaker.defName} from {foodMaker.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(foodMaker.defName);
            foodMaker.designationCategory = designationCategory;
        }

        foreach (var facility in affectedByFacilities)
        {
            LogMessage(
                $"Changing designation for facility {facility.defName} from {facility.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(facility.defName);
            facility.designationCategory = designationCategory;
        }

        LogMessage(
            $"Moved {affectedByFacilities.Count + foodMakersInGame.Count} kitchen furniture to the Kitchen tab.",
            true);
    }

    /// <summary>
    ///     Sort research furniture to the Research-tab
    /// </summary>
    private static void sortResearchFurniture()
    {
        var designationCategory = GetDesignationFromDatabase("ResearchTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the ResearchTab-def, will not sort research items.",
                "ResearchTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortResearchFurniture)
        {
            return;
        }

        var researchBuildings = new HashSet<ThingDef>();
        var requiredResearchBuildings =
            (from researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefsListForReading
                where researchProjectDef.requiredResearchBuilding != null
                select researchProjectDef.requiredResearchBuilding).ToHashSet();
        var researchBenches = (from building in DefDatabase<ThingDef>.AllDefsListForReading
            where building.thingClass != null && (building.thingClass == typeof(Building_ResearchBench) ||
                                                  building.thingClass.IsSubclassOf(typeof(Building_ResearchBench)))
            select building).ToList();

        LogMessage(
            $"Found {researchBenches.Count} research-benches and {requiredResearchBuildings.Count} researchBuildings");
        researchBuildings.AddRange(researchBenches);
        researchBuildings.AddRange(requiredResearchBuildings);
        var researchBuildingsInGame = (from researchBuilding in researchBuildings
            where !DefsToIgnore.Contains(researchBuilding.defName) &&
                  !changedDefNames.Contains(researchBuilding.defName) &&
                  researchBuilding.designationCategory != null
            select researchBuilding).ToList();

        var affectedByFacilities = new HashSet<ThingDef>();
        foreach (var researchBuilding in researchBuildingsInGame)
        {
            if (researchBuilding.comps == null || !researchBuilding.comps.Any())
            {
                continue;
            }

            var affections = researchBuilding.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (affections == null || !affections.linkableFacilities.Any())
            {
                continue;
            }

            foreach (var facility in affections.linkableFacilities)
            {
                if (changedDefNames.Contains(facility.defName))
                {
                    continue;
                }

                if (facility.designationCategory == null)
                {
                    continue;
                }

                var compProperties = facility.GetCompProperties<CompProperties_Facility>();
                if (compProperties?.statOffsets == null)
                {
                    continue;
                }

                var found = false;
                foreach (var offset in compProperties.statOffsets)
                {
                    if (offset.stat != StatDefOf.ResearchSpeed && offset.stat != StatDefOf.ResearchSpeedFactor)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    continue;
                }

                affectedByFacilities.Add(facility);
            }
        }

        foreach (var researchBuilding in researchBuildingsInGame)
        {
            LogMessage(
                $"Changing designation for building {researchBuilding.defName} from {researchBuilding.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(researchBuilding.defName);
            researchBuilding.designationCategory = designationCategory;
        }

        foreach (var facility in affectedByFacilities)
        {
            LogMessage(
                $"Changing designation for facility {facility.defName} from {facility.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(facility.defName);
            facility.designationCategory = designationCategory;
        }

        LogMessage(
            $"Moved {affectedByFacilities.Count + researchBuildingsInGame.Count} research-buildings to the Research tab.",
            true);
    }

    /// <summary>
    ///     Sort bedroom furniture to the Bedroom-tab
    /// </summary>
    private static void sortBedroomFurniture()
    {
        var designationCategory = GetDesignationFromDatabase("BedroomTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the BedroomTab-def, will not sort bedroom items.",
                "BedroomTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortBedroomFurniture)
        {
            return;
        }

        var bedsInGame = (from bed in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(bed.defName) && !changedDefNames.Contains(bed.defName) &&
                  bed.designationCategory != null && bed.IsBed && (bed.building == null ||
                                                                   !bed.building.bed_defaultMedical &&
                                                                   bed.building.bed_humanlike)
            select bed).ToList();
        var affectedByFacilities = new HashSet<ThingDef>();
        foreach (var bed in bedsInGame)
        {
            if (bed.comps == null || !bed.comps.Any())
            {
                continue;
            }

            var affections = bed.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (affections?.linkableFacilities == null)
            {
                continue;
            }

            foreach (var facility in affections.linkableFacilities)
            {
                if (changedDefNames.Contains(facility.defName))
                {
                    continue;
                }

                if (facility.designationCategory == null)
                {
                    continue;
                }

                var affectsStuff = false;
                var compProperties = facility.GetCompProperties<CompProperties_Facility>();
                if (compProperties?.statOffsets != null)
                {
                    foreach (var offset in compProperties.statOffsets)
                    {
                        if (offset.stat != StatDefOf.Comfort)
                        {
                            continue;
                        }

                        affectsStuff = true;
                        break;
                    }
                }

                if (affectsStuff)
                {
                    affectedByFacilities.Add(facility);
                }
            }
        }

        foreach (var bed in bedsInGame)
        {
            LogMessage(
                $"Changing designation for bed {bed.defName} from {bed.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(bed.defName);
            bed.designationCategory = designationCategory;
        }

        foreach (var facility in affectedByFacilities)
        {
            LogMessage(
                $"Changing designation for facility {facility.defName} from {facility.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(facility.defName);
            facility.designationCategory = designationCategory;
        }

        LogMessage(
            $"Moved {affectedByFacilities.Count + bedsInGame.Count} bedroom furniture to the Bedroom tab.",
            true);
    }

    /// <summary>
    ///     Sort decorative items to the Decorations-tab
    /// </summary>
    private static void sortDecorations()
    {
        var designationCategory = GetDesignationFromDatabase("DecorationTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the DecorationTab-def, will not sort decoration items.",
                "DecorationTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortDecorations)
        {
            return;
        }

        var rugsInGame = (from rug in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(rug.defName) && !changedDefNames.Contains(rug.defName) &&
                  rug.designationCategory != null && rug.altitudeLayer == AltitudeLayer.FloorEmplacement &&
                  !rug.clearBuildingArea && rug.passability == Traversability.Standable &&
                  rug.StatBaseDefined(StatDefOf.Beauty) && rug.GetStatValueAbstract(StatDefOf.Beauty) > 0
            select rug).ToList();
        var decorativePlantsInGame = (from decorativePlant in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(decorativePlant.defName) &&
                  !changedDefNames.Contains(decorativePlant.defName) &&
                  decorativePlant.designationCategory != null &&
                  decorativePlant.building is { sowTag: "Decorative" }
            select decorativePlant).ToList();
        var decorativeFurnitureInGame = (from decorativeFurniture in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(decorativeFurniture.defName) &&
                  !changedDefNames.Contains(decorativeFurniture.defName) &&
                  decorativeFurniture.designationCategory != null &&
                  decorativeFurniture.altitudeLayer == AltitudeLayer.BuildingOnTop &&
                  decorativeFurniture.StatBaseDefined(StatDefOf.Beauty) &&
                  decorativeFurniture.GetStatValueAbstract(StatDefOf.Beauty) > 0 &&
                  decorativeFurniture.GetCompProperties<CompProperties_Glower>() == null &&
                  !decorativeFurniture.neverMultiSelect &&
                  (decorativeFurniture.PlaceWorkers == null ||
                   !decorativeFurniture.placeWorkers.Contains(typeof(PlaceWorker_ShowFacilitiesConnections))) &&
                  !decorativeFurniture.IsBed && !decorativeFurniture.IsTable
            select decorativeFurniture).ToList();

        foreach (var rug in rugsInGame)
        {
            LogMessage(
                $"Changing designation for rug {rug.defName} from {rug.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(rug.defName);
            rug.designationCategory = designationCategory;
        }

        foreach (var planter in decorativePlantsInGame)
        {
            LogMessage(
                $"Changing designation for planter {planter.defName} from {planter.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(planter.defName);
            planter.designationCategory = designationCategory;
        }

        foreach (var furniture in decorativeFurnitureInGame)
        {
            LogMessage(
                $"Changing designation for furniture {furniture.defName} from {furniture.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(furniture.defName);
            furniture.designationCategory = designationCategory;
        }

        LogMessage(
            $"Moved {rugsInGame.Count + decorativePlantsInGame.Count + decorativeFurnitureInGame.Count} decorative items to the Decorations tab.",
            true);
    }

    /// <summary>
    ///     Sorts all walls and doors to the Structure-tab
    /// </summary>
    private static void sortDoorsAndWalls()
    {
        if (!TabSortingMod.Instance.Settings.SortDoorsAndWalls)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("Structure");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the StructureTab-def, will not sort doors and walls items.",
                "Structure".GetHashCode());
            return;
        }

        var doorCategory = GetDesignationFromDatabase("DoorTab");
        if (doorCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the DoorTab-def, will not sort doors.",
                "DoorTab".GetHashCode());
            return;
        }

        var staticStructureDefs = new List<string> { "GL_DoorFrame" };

        var doorsAndWallsInGame = (from doorOrWall in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(doorOrWall.defName) && !changedDefNames.Contains(doorOrWall.defName) &&
                  (doorOrWall.designationCategory != null &&
                   (doorOrWall.designationCategory.defName != "Structure" ||
                    TabSortingMod.Instance.Settings.SortDoors && (doorOrWall.IsDoor ||
                                                                  staticStructureDefs.Contains(doorOrWall.defName))) &&
                   (doorOrWall.fillPercent == 1f || doorOrWall.label.ToLower().Contains("column")) &&
                   (doorOrWall.holdsRoof || doorOrWall.IsDoor) ||
                   staticStructureDefs.Contains(doorOrWall.defName))
            select doorOrWall).ToList();
        var bridgesInGame = (from bridge in DefDatabase<TerrainDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(bridge.defName) && !changedDefNames.Contains(bridge.defName) &&
                  bridge.designationCategory != null && bridge.designationCategory.defName != "Structure" &&
                  bridge.destroyEffect != null && bridge.destroyEffect.defName.ToLower().Contains("bridge")
            select bridge).ToList();
        var nonDoors = 0;
        var doors = 0;
        foreach (var doorOrWall in doorsAndWallsInGame)
        {
            if (TabSortingMod.Instance.Settings.SortDoors && (doorOrWall.IsDoor ||
                                                              staticStructureDefs.Contains(doorOrWall.defName)))
            {
                LogMessage(
                    $"Changing designation for doorOrWall {doorOrWall.defName} from {doorOrWall.designationCategory} to {doorCategory.defName}");
                changedDefNames.Add(doorOrWall.defName);
                doorOrWall.designationCategory = doorCategory;
                doors++;
                continue;
            }

            LogMessage(
                $"Changing designation for doorOrWall {doorOrWall.defName} from {doorOrWall.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(doorOrWall.defName);
            doorOrWall.designationCategory = designationCategory;
            nonDoors++;
        }

        foreach (var bridge in bridgesInGame)
        {
            LogMessage(
                $"Changing designation for bridge {bridge.defName} from {bridge.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(bridge.defName);
            bridge.designationCategory = designationCategory;
            nonDoors++;
        }

        if (TabSortingMod.Instance.Settings.SortDoors)
        {
            LogMessage($"Moved {nonDoors} bridges and walls to the Structure tab and {doors} to the Doors tab.", true);
            return;
        }

        LogMessage($"Moved {doorsAndWallsInGame.Count} bridges, doors and walls to the Structure tab.", true);
    }

    /// <summary>
    ///     Sorts all fences-items to the Fences-tab if Fences and Floors is loaded
    /// </summary>
    private static void sortFences()
    {
        if (!TabSortingMod.Instance.Settings.SortFences)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("Fences");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the FencesTab-def, will not sort fences.",
                "Fences".GetHashCode());
            return;
        }

        var fencesInGame = (from fence in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(fence.defName) && !changedDefNames.Contains(fence.defName) &&
                  !fence.IsFrame && !fence.IsBlueprint &&
                  fence.designationCategory != null && fence.designationCategory.defName != "Fences" &&
                  ((fence.thingClass.isDerivedFrom(typeof(Building_Door)) ||
                    fence.thingClass.isDerivedFrom(typeof(Building)) &&
                    fence.graphicData?.linkType == LinkDrawerType.Basic &&
                    fence.passability == Traversability.Impassable) && fence.fillPercent is < 1f and > 0 ||
                   fence.label.ToLower().Contains("fence"))
            select fence).ToList();
        foreach (var fence in fencesInGame)
        {
            LogMessage(
                $"Changing designation for fence {fence.defName} from {fence.designationCategory} to {designationCategory.defName} passability: {fence.passability}");
            changedDefNames.Add(fence.defName);
            fence.designationCategory = designationCategory;
        }

        LogMessage($"Moved {fencesInGame.Count} fences to the Fences-tab.", true);
    }

    private static bool isDerivedFrom(this Type thingClass, Type baseClass)
    {
        return thingClass != null && baseClass.IsAssignableFrom(thingClass);
    }

    /// <summary>
    ///     Sorts all floors to the Floors-tab
    /// </summary>
    private static void sortFloors()
    {
        if (!TabSortingMod.Instance.Settings.SortFloors)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("Floors");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the FloorsTab-def, will not sort floors.",
                "Floors".GetHashCode());
            return;
        }

        var floorsInGame = (from floor in DefDatabase<TerrainDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(floor.defName) && !changedDefNames.Contains(floor.defName) &&
                  floor.designationCategory != null && floor.designationCategory.defName != "Floors" &&
                  floor.fertility == 0 && !floor.destroyBuildingsOnDestroyed
            select floor).ToList();
        foreach (var floor in floorsInGame)
        {
            LogMessage(
                $"Changing designation for floor {floor.defName} from {floor.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(floor.defName);
            floor.designationCategory = designationCategory;
        }

        LogMessage($"Moved {floorsInGame.Count} floors to the Floors tab.", true);
    }

    /// <summary>
    ///     Sorts all garden-items to the Garden-tab if VGP Garden tools is loaded
    /// </summary>
    private static void sortGarden()
    {
        if (!TabSortingMod.Instance.Settings.SortGarden)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("GardenTools");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the GardenToolsTab-def, will not sort garden items.",
                "GardenTools".GetHashCode());
            return;
        }

        var gardenThingsInGame = (from gardenThing in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(gardenThing.defName) && !changedDefNames.Contains(gardenThing.defName) &&
                  gardenThing.designationCategory?.defName != "GardenTools" &&
                  !gardenThing.IsFrame && !gardenThing.IsBlueprint &&
                  (gardenThing.thingClass.isDerivedFrom(AccessTools.TypeByName("RimWorld.Building_SunLamp")) ||
                   gardenThing.thingClass.isDerivedFrom(typeof(Building_PlantGrower)) &&
                   gardenThing.building?.sowTag != "Decorative" ||
                   gardenThing.label?.ToLower().Contains("sun lamp") == true ||
                   gardenThing.label?.ToLower().Contains("sprinkler") == true &&
                   gardenThing.label?.ToLower().Contains("fire") == false)
            select gardenThing).ToList();
        foreach (var gardenTool in gardenThingsInGame)
        {
            LogMessage(
                $"Changing designation for gardenTool {gardenTool.defName} from {gardenTool.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(gardenTool.defName);
            gardenTool.designationCategory = designationCategory;
        }

        LogMessage($"Moved {gardenThingsInGame.Count} garden-items to the Garden tab.", true);
    }

    /// <summary>
    ///     Sort hospital furniture to the Hospital-tab
    /// </summary>
    private static void sortHospitalFurniture()
    {
        var designationCategory = GetDesignationFromDatabase("HospitalTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the HospitalTab-def, will not sort hospital items.",
                "HospitalTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortHospitalFurniture)
        {
            return;
        }

        var hospitalBedsInGame = (from hoispitalBed in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(hoispitalBed.defName) && !changedDefNames.Contains(hoispitalBed.defName) &&
                  hoispitalBed.designationCategory != null && hoispitalBed.IsBed && hoispitalBed.building is
                      { bed_defaultMedical: true }
            select hoispitalBed).ToList();
        var affectedByFacilities = new HashSet<ThingDef>();
        foreach (var hospitalBed in hospitalBedsInGame)
        {
            if (hospitalBed.comps.Count == 0)
            {
                continue;
            }

            var affections = hospitalBed.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (affections?.linkableFacilities == null)
            {
                continue;
            }

            foreach (var facility in affections.linkableFacilities)
            {
                if (changedDefNames.Contains(facility.defName))
                {
                    continue;
                }

                if (facility.designationCategory == null)
                {
                    continue;
                }

                var affectees = facility.GetCompProperties<CompProperties_Facility>();
                if (affectees?.statOffsets == null)
                {
                    continue;
                }

                if (affectees.statOffsets.Any(offset => offset.stat == StatDefOf.SurgerySuccessChanceFactor ||
                                                        offset.stat == StatDefOf.MedicalTendQualityOffset ||
                                                        offset.stat == StatDefOf.ImmunityGainSpeedFactor))
                {
                    affectedByFacilities.Add(facility);
                }
            }
        }

        foreach (var hospitalBed in hospitalBedsInGame)
        {
            LogMessage(
                $"Changing designation for hospitalBed {hospitalBed.defName} from {hospitalBed.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(hospitalBed.defName);
            hospitalBed.designationCategory = designationCategory;
        }

        foreach (var facility in affectedByFacilities)
        {
            LogMessage(
                $"Changing designation for facility {facility.defName} from {facility.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(facility.defName);
            facility.designationCategory = designationCategory;
        }

        LogMessage(
            $"Moved {affectedByFacilities.Count + hospitalBedsInGame.Count} hospital furniture to the Hospital tab.",
            true);
    }

    /// <summary>
    ///     Sorts all lights to the Lights-tab
    /// </summary>
    private static void sortLights()
    {
        var designationCategory = GetDesignationFromDatabase("LightsTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the LightsTab-def, will not sort lights.",
                "LightsTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortLights)
        {
            return;
        }

        var gardenToolsExists = DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) != null;

        var lightsInGame = DefDatabase<ThingDef>.AllDefs.Where(lightValidator).ToList();
        foreach (var furniture in lightsInGame)
        {
            LogMessage(
                $"Changing designation for furniture {furniture.defName} from {furniture.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(furniture.defName);
            furniture.designationCategory = designationCategory;
        }

        LogMessage($"Moved {lightsInGame.Count} lights to the Lights tab.", true);
        return;

        bool lightValidator(ThingDef furniture)
        {
            if (DefsToIgnore.Contains(furniture.defName) || changedDefNames.Contains(furniture.defName) ||
                furniture.designationCategory == null || furniture.IsFrame || furniture.IsBlueprint ||
                furniture.category != ThingCategory.Building)
            {
                return false;
            }

            var compGlower = furniture.GetCompProperties<CompProperties_Glower>();
            if (compGlower is not { glowRadius: >= 3 })
            {
                return false;
            }

            var compPower = furniture.GetCompProperties<CompProperties_Power>();
            if (compPower != null)
            {
                var powerConsumption = (float)basePowerConsumptionField.GetValue(compPower);

                if (compPower.compClass == typeof(CompPowerPlant) ||
                    !(powerConsumption < 2000) &&
                    !furniture.thingClass.isDerivedFrom(
                        AccessTools.TypeByName("RimWorld.Building_SunLamp")))
                {
                    return false;
                }
            }

            if (furniture.recipes != null || furniture.placeWorkers != null &&
                furniture.placeWorkers.Contains(
                    typeof(PlaceWorker_ShowFacilitiesConnections)))
            {
                return false;
            }

            return furniture.GetCompProperties<CompProperties_ShipLandingBeacon>() == null &&
                furniture.GetCompProperties<CompProperties_Battery>() == null &&
                furniture.GetCompProperties<CompProperties_TempControl>() == null &&
                (furniture.GetCompProperties<CompProperties_HeatPusher>() == null ||
                 furniture.GetCompProperties<CompProperties_HeatPusher>()?.heatPerSecond < compGlower.glowRadius) &&
                furniture.surfaceType != SurfaceType.Eat &&
                furniture.terrainAffordanceNeeded != TerrainAffordanceDefOf.Heavy &&
                furniture.thingClass.isDerivedFrom(typeof(Building_TurretGun)) is false &&
                furniture.thingClass.isDerivedFrom(typeof(Building_PlantGrower)) is false &&
                furniture.thingClass.isDerivedFrom(typeof(Building_Heater)) is false &&
                (furniture.thingClass.isDerivedFrom(AccessTools.TypeByName("RimWorld.Building_SunLamp")) is false ||
                 !gardenToolsExists) &&
                (furniture.inspectorTabs == null || !furniture.inspectorTabs.Contains(typeof(ITab_Storage))) &&
                !furniture.hasInteractionCell || furniture.label != null &&
                (furniture.label.ToLower().Contains("wall") || furniture.label.ToLower().Contains("floor")) &&
                (furniture.label.ToLower().Contains("light") || furniture.label.ToLower().Contains("lamp"));
        }
    }

    /// <summary>
    ///     Sorts all manually assigned things
    /// </summary>
    private static void sortManually()
    {
        if (TabSortingMod.Instance.Settings.ManualSorting == null ||
            TabSortingMod.Instance.Settings.ManualSorting.Count == 0)
        {
            return;
        }

        foreach (var itemToSort in TabSortingMod.Instance.Settings.ManualSorting)
        {
            var designationCategory = GetDesignationFromDatabase(itemToSort.Value);
            if (designationCategory == null && itemToSort.Value != "None")
            {
                continue;
            }

            var thingDefToSort = DefDatabase<ThingDef>.GetNamedSilentFail(itemToSort.Key);
            var terrainDefToSort = DefDatabase<TerrainDef>.GetNamedSilentFail(itemToSort.Key);
            if (thingDefToSort == null && terrainDefToSort == null)
            {
                continue;
            }

            if (thingDefToSort != null)
            {
                if (itemToSort.Value != "None")
                {
                    thingDefToSort.designationCategory = designationCategory;
                    LogMessage($"Manually moving {thingDefToSort.defName} to {designationCategory?.defName}");
                }
                else
                {
                    thingDefToSort.designationCategory = null;
                    LogMessage($"Manually hiding {thingDefToSort.defName}");
                }
            }
            else
            {
                if (itemToSort.Value != "None")
                {
                    terrainDefToSort.designationCategory = designationCategory;
                    LogMessage($"Manually moving {terrainDefToSort.defName} to {designationCategory?.defName}");
                }
                else
                {
                    terrainDefToSort.designationCategory = null;
                    LogMessage($"Manually hiding {terrainDefToSort.defName}");
                }
            }

            changedDefNames.Add(itemToSort.Key);
        }

        LogMessage(
            $"Moved {TabSortingMod.Instance.Settings.ManualSorting.Count} items to manually designated categories.",
            true);
    }

    /// <summary>
    ///     Sorts all storage-items to the Storage-tab if Storage-extended is loaded
    /// </summary>
    private static void sortStorage()
    {
        if (!TabSortingMod.Instance.Settings.SortStorage)
        {
            return;
        }

        var designationCategory = GetDesignationFromDatabase("LWM_DS_Storage") ??
                                  GetDesignationFromDatabase("FurnitureStorage");

        designationCategory ??= GetDesignationFromDatabase("StorageTab");

        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the StorageTab-def, will not sort storage items.",
                "StorageTab".GetHashCode());
            return;
        }

        var storageInGame = (from storage in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(storage.defName) && !changedDefNames.Contains(storage.defName) &&
                  storage.designationCategory != null &&
                  storage.designationCategory.defName != "FurnitureStorage" &&
                  storage.thingClass?.isDerivedFrom(typeof(Building_Grave)) is false &&
                  (storage.thingClass.isDerivedFrom(typeof(Building_Storage)) || storage.inspectorTabs != null &&
                      storage.inspectorTabs.Contains(typeof(ITab_Storage)))
                  && (storage.placeWorkers == null ||
                      !storage.placeWorkers.Contains(typeof(PlaceWorker_NextToHopperAccepter)))
            select storage).ToList();
        foreach (var storage in storageInGame)
        {
            LogMessage(
                $"Changing designation for storage {storage.defName} from {storage.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(storage.defName);
            storage.designationCategory = designationCategory;
        }

        //var LWMTab = GetDesignationFromDatabase("LWM_DS_Storage");
        //if (LWMTab != null)
        //{
        //    LogMessage(
        //        "Forcebly removing the LWM-storage tab since it causes issues otherwise. This will cause an error.");
        //    RemoveEmptyDesignationCategoryDef(LWMTab);
        //}

        LogMessage($"Moved {storageInGame.Count} storage-items to the Storage tab.", true);
    }

    /// <summary>
    ///     Sorts all tables and sitting-furniture to the Table/Chairs-tab
    /// </summary>
    private static void sortTablesAndChairs()
    {
        var designationCategory = GetDesignationFromDatabase("TableChairsTab");
        if (designationCategory == null)
        {
            Log.ErrorOnce("[TabSorting]: Cannot find the TableChairsTab-def, will not sort tables and chairs.",
                "TableChairsTab".GetHashCode());
            return;
        }

        if (!TabSortingMod.Instance.Settings.SortTablesAndChairs)
        {
            return;
        }

        var tableChairsInGame = (from table in DefDatabase<ThingDef>.AllDefsListForReading
            where !DefsToIgnore.Contains(table.defName) && !changedDefNames.Contains(table.defName) &&
                  table.designationCategory != null &&
                  (table.IsTable ||
                   table.surfaceType == SurfaceType.Eat && table.label.ToLower().Contains("table") ||
                   table.building is { isSittable: true })
            select table).ToList();
        foreach (var tableOrChair in tableChairsInGame)
        {
            LogMessage(
                $"Changing designation for tableOrChair {tableOrChair.defName} from {tableOrChair.designationCategory} to {designationCategory.defName}");
            changedDefNames.Add(tableOrChair.defName);
            tableOrChair.designationCategory = designationCategory;
        }

        LogMessage($"Moved {tableChairsInGame.Count} tables and chairs to the Table/Chairs tab.", true);
    }

    public static void LogMessage(string message, bool force = false)
    {
        if (TabSortingMod.Instance.Settings.VerboseLogging || force)
        {
            Log.Message($"[TabSorting]: {message}");
        }
    }

    public static string ValidateTabName(string tabName, bool justTheLabel = false)
    {
        var cleanTabName = Regex.Replace(tabName, @"[^a-zA-Z\u4e00-\u9fa5]*", string.Empty);
        var currentDesignations = DefDatabase<DesignationCategoryDef>.AllDefsListForReading;
        return currentDesignations.Any(def => justTheLabel && def.defName == cleanTabName || def.label == tabName)
            ? null
            : cleanTabName;
    }

    private static void architectIconsPrefix(ref string categoryDefName)
    {
        categoryDefName = GetCustomTabIcon(categoryDefName);
    }
}