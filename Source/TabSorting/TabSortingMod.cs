﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting;

[StaticConstructorOnStartup]
internal class TabSortingMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static TabSortingMod instance;

    private static readonly Vector2 buttonSize = new Vector2(120f, 25f);

    private static readonly int buttonSpacer = 300;

    private static readonly float columnSpacer = 0.1f;

    private static readonly float iconSize = 20f;

    private static float leftSideWidth;

    private static Listing_Standard listing_Standard;

    private static Dictionary<string, string> noneCategoryMembers;

    private static Vector2 optionsScrollPosition;

    private static float rightSideWidth;

    private static string selectedDef = "Settings";

    private static Vector2 tabsScrollPosition;

    private static string currentVersion;

    private static string newTabName;

    public static Texture2D plusTexture;

    private static Dictionary<DesignatorDropdownGroupDef, List<BuildableDef>> designatorGroups;

    /// <summary>
    ///     The private settings
    /// </summary>
    private TabSortingModSettings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public TabSortingMod(ModContentPack content)
        : base(content)
    {
        instance = this;
        tabsScrollPosition = new Vector2(0, 0);
        optionsScrollPosition = new Vector2(0, 0);
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(ModLister.GetActiveModWithIdentifier("Mlie.TabSorting"));
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal TabSortingModSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = GetSettings<TabSortingModSettings>();
            }

            if (settings.SortGarden && DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) == null)
            {
                settings.SortGarden = false;
            }

            if (settings.SortFences && DefDatabase<DesignationCategoryDef>.GetNamed("Fences", false) == null)
            {
                settings.SortFences = false;
            }

            return settings;
        }

        set => settings = value;
    }

    /// <summary>
    ///     The settings-window
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        base.DoSettingsWindowContents(rect);
        if (instance.Settings.ManualSorting == null)
        {
            instance.Settings.ManualSorting = new Dictionary<string, string>();
        }

        if (noneCategoryMembers == null)
        {
            noneCategoryMembers = new Dictionary<string, string>();
            var thingsToRemove = new List<string>();
            foreach (var item in from item in instance.Settings.ManualSorting
                     where item.Value == "TabSorting.None".Translate()
                     select item)
            {
                var hiddenThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(item.Key);
                var hiddenTerrainDef = DefDatabase<TerrainDef>.GetNamedSilentFail(item.Key);
                if (hiddenThingDef == null && hiddenTerrainDef == null)
                {
                    thingsToRemove.Add(item.Key);
                    continue;
                }

                var label = hiddenThingDef != null ? hiddenThingDef.label : hiddenTerrainDef.label;

                noneCategoryMembers.Add(item.Key, label);
            }

            foreach (var defName in thingsToRemove)
            {
                instance.Settings.ManualSorting.Remove(defName);
            }
        }

        var rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 3;
        rightSideWidth = rect2.width - leftSideWidth;

        listing_Standard = new Listing_Standard();

        DrawOptions(rect2);
        DrawTabsList(rect2);
        Settings.Write();
    }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Tab-sorting";
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        var dontSort = false;
        if (ModLister.GetActiveModWithIdentifier("com.github.alandariva.moreplanning") != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "TabSorting.MorePlanning".Translate(),
                "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                delegate
                {
                    noneCategoryMembers = null;
                    TabSorting.DoTheSorting();
                }));
            dontSort = true;
        }

        if (settings.SortStorage && ModLister.GetActiveModWithIdentifier("LWM.DeepStorage") != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "TabSorting.DeepStorage".Translate(),
                "TabSorting.Ok".Translate()));
        }

        if (dontSort)
        {
            return;
        }

        noneCategoryMembers = null;
        TabSorting.DoTheSorting();
    }

    private static void DrawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }

    private static void SetManualSortTarget(Def def)
    {
        var defNames = new List<string> { def.defName };
        if (instance.Settings.GroupSameDesignator && designatorGroups.Any(pair => pair.Value.Contains(def)))
        {
            foreach (var designatorDef in designatorGroups.First(pair => pair.Value.Contains(def)).Value)
            {
                defNames.Add(designatorDef.defName);
            }
        }

        void defaultAction()
        {
            foreach (var defName in defNames)
            {
                if (instance.Settings.ManualSorting == null || !instance.Settings.ManualSorting.ContainsKey(defName))
                {
                    continue;
                }

                instance.Settings.ManualSorting.Remove(defName);
            }
        }

        var list = new List<FloatMenuOption> { new FloatMenuOption("TabSorting.Default".Translate(), defaultAction) };

        foreach (var sortOption in from vanillaCategory in instance.Settings.VanillaCategoryMemory
                 orderby vanillaCategory.label
                 select vanillaCategory)
        {
            void action()
            {
                foreach (var defName in defNames)
                {
                    instance.Settings.ManualSorting[defName] = sortOption.defName;
                }
            }

            list.Add(new FloatMenuOption($"{sortOption.label.CapitalizeFirst()} ({sortOption.defName})", action));
        }

        foreach (var sortOption in from manualCategory in instance.Settings.ManualCategoryMemory
                 orderby manualCategory.label
                 select manualCategory)
        {
            void action()
            {
                foreach (var defName in defNames)
                {
                    instance.Settings.ManualSorting[defName] = sortOption.defName;
                }
            }

            list.Add(new FloatMenuOption($"{sortOption.label.CapitalizeFirst()} ({sortOption.defName})", action));
        }

        void noneAction()
        {
            foreach (var defName in defNames)
            {
                instance.Settings.ManualSorting[defName] = "TabSorting.None".Translate();
            }
        }

        list.Add(new FloatMenuOption("TabSorting.NoneHidden".Translate(), noneAction));
        Find.WindowStack.Add(new FloatMenu(list));
    }

    private void drawIcon(BuildableDef thing, Rect rect)
    {
        if (thing == null || thing.uiIcon == BaseContent.BadTex)
        {
            return;
        }

        var texture2D = thing.uiIcon;
        var textureColor = thing.uiIconColor;
        if (texture2D.width != texture2D.height)
        {
            var ratio = (float)texture2D.width / texture2D.height;

            if (ratio < 1)
            {
                rect.x += (rect.width - (rect.width * ratio)) / 2;
                rect.width *= ratio;
            }
            else
            {
                rect.y += (rect.height - (rect.height / ratio)) / 2;
                rect.height /= ratio;
            }
        }

        var beforeColor = GUI.color;
        GUI.color = new Color(textureColor.r, textureColor.g, textureColor.b, GUI.color.a);
        GUI.DrawTexture(rect, texture2D);
        GUI.color = beforeColor;
        if (!instance.Settings.GroupSameDesignator || thing.designatorDropdown == null ||
            !designatorGroups[thing.designatorDropdown].Any())
        {
            return;
        }

        var newRect = rect;
        newRect.x -= newRect.width;
        GUI.DrawTexture(newRect, plusTexture);
    }

    private void DrawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + columnSpacer;
        optionsOuterContainer.width -= leftSideWidth + columnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(5);
        frameRect.x = leftSideWidth + columnSpacer + 15;
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        var listing_Options = new Listing_Standard();
        switch (selectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                listing_Standard.Begin(frameRect);
                listing_Standard.CheckboxLabeled("TabSorting.SortLights.Label".Translate(), ref Settings.SortLights,
                    "TabSorting.SortLights.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortFloors.Label".Translate(), ref Settings.SortFloors,
                    "TabSorting.SortFloors.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortWallsDoors.Label".Translate(),
                    ref Settings.SortDoorsAndWalls,
                    "TabSorting.SortWallsDoors.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortTablesChairs.Label".Translate(),
                    ref Settings.SortTablesAndChairs,
                    "TabSorting.SortTablesChairs.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortBedroom.Label".Translate(),
                    ref Settings.SortBedroomFurniture,
                    "TabSorting.SortBedroom.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortKitchen.Label".Translate(),
                    ref Settings.SortKitchenFurniture,
                    "TabSorting.SortKitchen.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortHospital.Label".Translate(),
                    ref Settings.SortHospitalFurniture,
                    "TabSorting.SortHospital.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortResearch.Label".Translate(),
                    ref Settings.SortResearchFurniture,
                    "TabSorting.SortResearch.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortDecoration.Label".Translate(),
                    ref Settings.SortDecorations,
                    "TabSorting.SortDecoration.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.SortStorage.Label".Translate(), ref Settings.SortStorage,
                    "TabSorting.SortStorage.Tooltip".Translate());

                if (DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) != null)
                {
                    listing_Standard.CheckboxLabeled("TabSorting.SortGarden.Label".Translate(), ref Settings.SortGarden,
                        "TabSorting.SortGarden.Tooltip".Translate());
                }

                if (DefDatabase<DesignationCategoryDef>.GetNamed("Fences", false) != null)
                {
                    listing_Standard.CheckboxLabeled("TabSorting.SortFences.Label".Translate(), ref Settings.SortFences,
                        "TabSorting.SortFences.Tooltip".Translate());
                }

                if (DefDatabase<DesignationCategoryDef>.GetNamed("IdeologyTab", false) != null)
                {
                    listing_Standard.CheckboxLabeled("TabSorting.SortIdeology.Label".Translate(),
                        ref Settings.SortIdeologyFurniture,
                        "TabSorting.SortIdeology.Tooltip".Translate());
                }

                listing_Standard.Gap();
                listing_Standard.CheckboxLabeled("TabSorting.RemoveEmpty.Label".Translate(),
                    ref Settings.RemoveEmptyTabs,
                    "TabSorting.RemoveEmpty.Tooltip".Translate());
                listing_Standard.CheckboxLabeled("TabSorting.GroupThings.Label".Translate(),
                    ref Settings.GroupSameDesignator,
                    "TabSorting.GroupThings.Tooltip".Translate());
                listing_Standard.Gap();
                listing_Standard.CheckboxLabeled("TabSorting.SortTabs.Label".Translate(), ref Settings.SortTabs,
                    "TabSorting.SortTabs.Tooltip".Translate());
                if (Settings.SortTabs)
                {
                    listing_Standard.CheckboxLabeled("TabSorting.SkipZoneOrders.Label".Translate(),
                        ref Settings.SkipBuiltIn,
                        "TabSorting.SkipZoneOrders.Tooltip".Translate());
                }
                else
                {
                    Settings.SkipBuiltIn = false;
                }

                listing_Standard.Gap();
                if (instance.Settings.ManualSorting != null && instance.Settings.ManualSorting.Any())
                {
                    var labelPoint = listing_Standard.Label("TabSorting.ManualReset.Label".Translate(), -1F,
                        "TabSorting.ManualReset.Tooltip".Translate());
                    DrawButton(instance.Settings.ResetManualValues, "TabSorting.ResetAll".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                }

                if (Current.ProgramState == ProgramState.Playing)
                {
                    listing_Standard.Label(
                        "TabSorting.MapRunning.Label".Translate());
                }

                listing_Standard.CheckboxLabeled("TabSorting.VerboseLogging.Label".Translate(),
                    ref Settings.VerboseLogging,
                    "TabSorting.VerboseLogging.Tooltip".Translate());
                if (currentVersion != null)
                {
                    listing_Standard.Gap();
                    GUI.contentColor = Color.gray;
                    listing_Standard.Label("TabSorting.ModVersion".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listing_Standard.End();
                break;
            }

            case "Hidden":
            {
                contentRect.width -= 20;

                contentRect.height = (noneCategoryMembers.Count * 24f) + 40f;
                Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
                listing_Options.Begin(contentRect);
                GUI.contentColor = Color.green;
                listing_Options.Label("TabSorting.NoneHidden".Translate());
                GUI.contentColor = Color.white;
                foreach (var hiddenItem in noneCategoryMembers)
                {
                    var item = DefDatabase<BuildableDef>.GetNamedSilentFail(hiddenItem.Key);
                    var currentPosition = listing_Options.Label(item.label.CapitalizeFirst());
                    var buttonText = "TabSorting.Default".Translate();
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(item.defName))
                    {
                        buttonText = Settings.ManualSorting[item.defName];
                    }

                    DrawButton(delegate { SetManualSortTarget(item); }, buttonText,
                        new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                    drawIcon(item,
                        new Rect(
                            new Vector2(currentPosition.position.x + buttonSpacer - iconSize,
                                currentPosition.position.y), new Vector2(iconSize, iconSize)));
                }

                listing_Options.GapLine();
                listing_Options.End();
                Widgets.EndScrollView();
                break;
            }

            case "CreateNew":
            {
                listing_Standard.Begin(frameRect);
                listing_Standard.Gap();

                listing_Standard.Label("TabSorting.NewTab".Translate());
                newTabName = listing_Standard.TextEntry(newTabName);
                var cleanTabName = Regex.Replace(newTabName, @"[^a-zA-Z]*", string.Empty);
                if (cleanTabName.Length > 0 && listing_Standard.ButtonText("TabSorting.Create".Translate()))
                {
                    if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(cleanTabName) != null)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "TabSorting.Exists".Translate(newTabName),
                            "TabSorting.Ok".Translate()));
                        break;
                    }

                    var newTab = new DesignationCategoryDef
                    {
                        defName = cleanTabName,
                        label = newTabName,
                        generated = true
                    };
                    DefGenerator.AddImpliedDef(newTab);
                    instance.Settings.ManualTabs[cleanTabName] = newTabName;
                    instance.Settings.ManualCategoryMemory.Add(
                        DefDatabase<DesignationCategoryDef>.GetNamed(cleanTabName));
                    selectedDef = "Settings";
                    newTabName = string.Empty;
                    TabSorting.LogMessage(
                        "TabSorting.CurrentCustom".Translate(string.Join(",", instance.Settings.ManualCategoryMemory)));
                }

                listing_Standard.End();
                break;
            }

            default:
            {
                var sortCategory = TabSorting.GetDesignationFromDatabase(selectedDef);
                if (sortCategory == null)
                {
                    Log.ErrorOnce(
                        $"TabSorter: Could not find category called {selectedDef}, this should not happen.",
                        selectedDef.GetHashCode());
                    return;
                }

                var allDefsInCategory = (from thing in DefDatabase<ThingDef>.AllDefsListForReading
                    where thing.designationCategory != null && thing.designationCategory == sortCategory
                    orderby thing.label
                    select thing).ToList();

                var allTerrainInCategory = (from terrain in DefDatabase<TerrainDef>.AllDefsListForReading
                    where terrain.designationCategory != null && terrain.designationCategory == sortCategory
                    orderby terrain.label
                    select terrain).ToList();

                designatorGroups = new Dictionary<DesignatorDropdownGroupDef, List<BuildableDef>>();
                if (instance.Settings.GroupSameDesignator)
                {
                    var tempAllDefsInCategory = new List<ThingDef>();

                    foreach (var thingDef in allDefsInCategory)
                    {
                        if (thingDef.designatorDropdown == null)
                        {
                            tempAllDefsInCategory.Add(thingDef);
                            continue;
                        }

                        if (!designatorGroups.ContainsKey(thingDef.designatorDropdown))
                        {
                            designatorGroups.Add(thingDef.designatorDropdown, new List<BuildableDef>());
                            tempAllDefsInCategory.Add(thingDef);
                        }

                        designatorGroups[thingDef.designatorDropdown].Add(thingDef);
                    }

                    var tempAllTerrainDefsInCategory = new List<TerrainDef>();

                    foreach (var terrainDef in allTerrainInCategory)
                    {
                        if (terrainDef.designatorDropdown == null)
                        {
                            tempAllTerrainDefsInCategory.Add(terrainDef);
                            continue;
                        }

                        if (!designatorGroups.ContainsKey(terrainDef.designatorDropdown))
                        {
                            designatorGroups.Add(terrainDef.designatorDropdown, new List<BuildableDef>());
                            tempAllTerrainDefsInCategory.Add(terrainDef);
                        }

                        designatorGroups[terrainDef.designatorDropdown].Add(terrainDef);
                    }

                    allDefsInCategory = tempAllDefsInCategory;
                    allTerrainInCategory = tempAllTerrainDefsInCategory;
                }

                contentRect.width -= 20;
                contentRect.height = ((allDefsInCategory.Count() + allTerrainInCategory.Count()) * 24f) + 40f;
                Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
                listing_Options.Begin(contentRect);

                GUI.contentColor = Color.green;
                var contentPack = "TabSorting.UnloadedMod".Translate();
                var manualTab = instance.Settings.ManualCategoryMemory.Contains(sortCategory);
                if (sortCategory.modContentPack?.Name != null)
                {
                    contentPack = sortCategory.modContentPack.Name;
                }

                if (manualTab)
                {
                    contentPack = "TabSorting.CustomTab".Translate();
                }

                var headerRect = listing_Options.Label(
                    $"{sortCategory.label.CapitalizeFirst()} ({sortCategory.defName}) - {contentPack}");
                GUI.contentColor = Color.white;

                if (manualTab)
                {
                    if (Widgets.ButtonText(
                            new Rect(headerRect.position + new Vector2(headerRect.width - buttonSize.x, 0), buttonSize),
                            "TabSorting.Delete".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "TabSorting.ResetOne".Translate(),
                            "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                            delegate
                            {
                                selectedDef = "Settings";
                                TabSorting.RemoveManualTab(sortCategory);
                            }));
                    }
                }

                foreach (var thing in allDefsInCategory)
                {
                    var toolTip = string.Empty;
                    if (instance.Settings.GroupSameDesignator && thing.designatorDropdown != null &&
                        designatorGroups.ContainsKey(thing.designatorDropdown) &&
                        designatorGroups[thing.designatorDropdown].Any())
                    {
                        toolTip = "TabSorting.GroupContaining".Translate(string.Join("\n",
                            designatorGroups[thing.designatorDropdown].Select(def => def.LabelCap)));
                    }

                    var currentPosition = listing_Options.Label(thing.LabelCap, -1f, toolTip);
                    var buttonText = "TabSorting.Default".Translate();
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(thing.defName))
                    {
                        buttonText = Settings.ManualSorting[thing.defName];
                    }

                    DrawButton(delegate { SetManualSortTarget(thing); }, buttonText,
                        new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                    drawIcon(thing,
                        new Rect(
                            new Vector2(currentPosition.position.x + buttonSpacer - iconSize,
                                currentPosition.position.y), new Vector2(iconSize, iconSize)));
                }

                foreach (var terrain in allTerrainInCategory)
                {
                    var toolTip = string.Empty;
                    if (instance.Settings.GroupSameDesignator && terrain.designatorDropdown != null &&
                        designatorGroups.ContainsKey(terrain.designatorDropdown) &&
                        designatorGroups[terrain.designatorDropdown].Any())
                    {
                        toolTip = "TabSorting.GroupContaining".Translate(string.Join("\n",
                            designatorGroups[terrain.designatorDropdown].Select(def => def.LabelCap)));
                    }

                    var currentPosition = listing_Options.Label(terrain.LabelCap, -1f, toolTip);
                    var buttonText = "TabSorting.Default".Translate();
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(terrain.defName))
                    {
                        buttonText = Settings.ManualSorting[terrain.defName];
                    }

                    DrawButton(delegate { SetManualSortTarget(terrain); }, buttonText,
                        new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                    drawIcon(terrain,
                        new Rect(
                            new Vector2(currentPosition.position.x + buttonSpacer - iconSize,
                                currentPosition.position.y), new Vector2(iconSize, iconSize)));
                }

                listing_Options.GapLine();
                listing_Options.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private void DrawTabsList(Rect rect)
    {
        var scrollContainer = rect.ContractedBy(10);
        scrollContainer.width = leftSideWidth;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        var innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        var tabFrameRect = innerContainer.ContractedBy(5);
        tabFrameRect.y += 15;
        tabFrameRect.height -= 15;
        var tabContentRect = tabFrameRect;
        tabContentRect.x = 0;
        tabContentRect.y = 0;
        tabContentRect.width -= 20;

        var categoryDefs = instance.Settings.VanillaCategoryMemory;
        var manualDefs = instance.Settings.ManualCategoryMemory;

        tabContentRect.height = (categoryDefs.Count + manualDefs.Count + 5) * 22f;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listing_Standard.Begin(tabContentRect);
        if (listing_Standard.ListItemSelectable("TabSorting.Settings".Translate(), Color.yellow,
                selectedDef == "Settings"))
        {
            selectedDef = selectedDef == "Settings" ? null : "Settings";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow);
        foreach (var categoryDef in categoryDefs)
        {
            if (!listing_Standard.ListItemSelectable(
                    $"{categoryDef.label.CapitalizeFirst()} ({categoryDef.defName})", Color.yellow,
                    selectedDef == categoryDef.defName))
            {
                continue;
            }

            selectedDef = selectedDef == categoryDef.defName ? null : categoryDef.defName;
        }

        listing_Standard.ListItemSelectable(null, Color.yellow);
        if (manualDefs.Any())
        {
            foreach (var categoryDef in manualDefs)
            {
                if (!listing_Standard.ListItemSelectable(
                        $"{categoryDef.label.CapitalizeFirst()} ({categoryDef.defName})", Color.yellow,
                        selectedDef == categoryDef.defName))
                {
                    continue;
                }

                selectedDef = selectedDef == categoryDef.defName ? null : categoryDef.defName;
            }

            listing_Standard.ListItemSelectable(null, Color.yellow);
        }

        if (listing_Standard.ListItemSelectable("TabSorting.NoneHidden".Translate(), Color.yellow,
                selectedDef == "Hidden"))
        {
            selectedDef = selectedDef == "Hidden" ? null : "Hidden";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow);

        if (listing_Standard.ListItemSelectable("TabSorting.CreateNew".Translate(), Color.yellow,
                selectedDef == "CreateNew"))
        {
            newTabName = string.Empty;
            selectedDef = selectedDef == "CreateNew" ? null : "CreateNew";
        }

        listing_Standard.End();
        Widgets.EndScrollView();
    }
}