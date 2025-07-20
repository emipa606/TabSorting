using System;
using System.Collections.Generic;
using System.Linq;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting;

[StaticConstructorOnStartup]
internal class TabSortingMod : Mod
{
    private const int ButtonSpacer = 300;

    private const float ColumnSpacer = 0.1f;

    private const float IconSize = 20f;

    private const float IconSpacer = 2f;

    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static TabSortingMod Instance;

    public static readonly Vector2 ButtonSize = new(120f, 20f);

    public static readonly Vector2 TabIconSize = new(16f, 16f);
    private static readonly Vector2 tabIconContainer = new(20f, 20f);
    private static readonly Vector2 searchSize = new(200f, 25f);

    private static float leftSideWidth;

    private static Listing_Standard listingStandard;

    private static Dictionary<string, string> noneCategoryMembers;

    private static Vector2 optionsScrollPosition;

    public static string SelectedDef = "Settings";

    private static Vector2 tabsScrollPosition;

    private static string currentVersion;

    private static string newTabName;

    public static Texture2D PlusTexture;

    private static string searchText = "";

    private static Dictionary<DesignatorDropdownGroupDef, List<BuildableDef>> designatorGroups;

    private int reorderID = -1;

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
        Instance = this;
        tabsScrollPosition = new Vector2(0, 0);
        optionsScrollPosition = new Vector2(0, 0);
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal TabSortingModSettings Settings
    {
        get
        {
            settings ??= GetSettings<TabSortingModSettings>();

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
        Instance.Settings.ManualSorting ??= new Dictionary<string, string>();

        if (noneCategoryMembers == null)
        {
            noneCategoryMembers = new Dictionary<string, string>();
            var thingsToRemove = new List<string>();
            foreach (var item in from item in Instance.Settings.ManualSorting
                     where item.Value == "TabSorting.None".Translate()
                           || item.Value == "None"
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
                Instance.Settings.ManualSorting.Remove(defName);
                ResetSortOrder(defName);
            }
        }

        var rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 3;

        listingStandard = new Listing_Standard();

        drawOptions(rect2);
        drawTabsList(rect2);
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
        var doNotSort = false;
        if (ModLister.GetActiveModWithIdentifier("com.github.alandariva.moreplanning", true) != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "TabSorting.MorePlanning".Translate(),
                "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                delegate
                {
                    noneCategoryMembers = null;
                    TabSorting.DoTheSorting();
                }));
            doNotSort = true;
        }

        if (settings.SortStorage && ModLister.GetActiveModWithIdentifier("LWM.DeepStorage", true) != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "TabSorting.DeepStorage".Translate(),
                "TabSorting.Ok".Translate()));
        }

        if (doNotSort)
        {
            return;
        }

        noneCategoryMembers = null;
        TabSorting.DoTheSorting();
    }

    private static void drawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, ButtonSize.x, ButtonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }

    private static List<BuildableDef> sortAlphabetically(List<BuildableDef> buildableDefs)
    {
        var skipList = new List<BuildableDef>();
        var buildingsWithDesignatorGroup = buildableDefs.Where(def => def.designatorDropdown != null).ToList();
        var buildingsWithNoDesignatorGroup = buildableDefs.Where(def => def.designatorDropdown == null).ToList();

        if (buildingsWithDesignatorGroup.Any())
        {
            var designatorDropdownGroupDefs = buildableDefs.Select(def => def.designatorDropdown).Distinct();
            foreach (var designatorGroup in designatorDropdownGroupDefs)
            {
                var groupBuildings = buildingsWithDesignatorGroup
                    .Where(def => def.designatorDropdown == designatorGroup).OrderBy(def => def.label).ToArray();
                if (!groupBuildings.Any())
                {
                    continue;
                }

                if (groupBuildings.Count() == 1)
                {
                    buildingsWithNoDesignatorGroup.AddRange(groupBuildings);
                    continue;
                }

                buildingsWithNoDesignatorGroup.Add(groupBuildings.First());
                skipList.AddRange(groupBuildings.Skip(1));
            }
        }

        buildingsWithDesignatorGroup = buildingsWithNoDesignatorGroup.OrderBy(def => def.label).ToList();
        var currentUiValue = buildableDefs.OrderBy(def => def.uiOrder).First().uiOrder;

        foreach (var buildableDef in buildingsWithDesignatorGroup)
        {
            Instance.Settings.ManualThingSorting[buildableDef.defName] = currentUiValue;
            if (buildableDef.designatorDropdown != null)
            {
                var sameDesignator = skipList.Where(def => def.designatorDropdown == buildableDef.designatorDropdown)
                    .ToArray();
                if (sameDesignator.Any())
                {
                    foreach (var def in sameDesignator.OrderBy(def => def.label))
                    {
                        currentUiValue++;
                        Instance.Settings.ManualThingSorting[def.defName] = currentUiValue;
                    }
                }
            }

            currentUiValue++;
        }

        return buildingsWithNoDesignatorGroup.OrderBy(def => def.uiOrder).ToList();
    }

    private static void setManualSortTarget(List<BuildableDef> defs)
    {
        var defNames = defs.Select(x => x.defName).ToList();
        foreach (var def in defs)
        {
            if (!Instance.Settings.GroupSameDesignator || !designatorGroups.Any(pair => pair.Value.Contains(def)))
            {
                continue;
            }

            foreach (var designatorDef in designatorGroups.First(pair => pair.Value.Contains(def)).Value)
            {
                defNames.Add(designatorDef.defName);
            }
        }

        var list = new List<FloatMenuOption> { new("TabSorting.Default".Translate(), defaultAction) };

        foreach (var sortOption in from vanillaCategory in Instance.Settings.VanillaCategoryMemory
                 orderby vanillaCategory.label
                 select vanillaCategory)
        {
            list.Add(new FloatMenuOption($"{sortOption.label.CapitalizeFirst()} ({sortOption.defName})", action));
            continue;

            void action()
            {
                foreach (var defName in defNames)
                {
                    Instance.Settings.ManualSorting[defName] = sortOption.defName;
                    ResetSortOrder(defName);
                }
            }
        }

        foreach (var sortOption in from manualCategory in Instance.Settings.ManualCategoryMemory
                 orderby manualCategory.label
                 select manualCategory)
        {
            list.Add(new FloatMenuOption($"{sortOption.label.CapitalizeFirst()} ({sortOption.defName})", action));
            continue;

            void action()
            {
                foreach (var defName in defNames)
                {
                    Instance.Settings.ManualSorting[defName] = sortOption.defName;
                    ResetSortOrder(defName);
                }
            }
        }

        list.Add(new FloatMenuOption("TabSorting.NoneHidden".Translate(), noneAction));
        Find.WindowStack.Add(new FloatMenu(list));
        return;

        void defaultAction()
        {
            foreach (var defName in defNames)
            {
                if (Instance.Settings.ManualSorting == null || !Instance.Settings.ManualSorting.Remove(defName))
                {
                    continue;
                }

                ResetSortOrder(defName);
            }
        }

        void noneAction()
        {
            foreach (var defName in defNames)
            {
                Instance.Settings.ManualSorting[defName] = "None";
                ResetSortOrder(defName);
            }
        }
    }

    private static void drawIcon(BuildableDef thing, Rect rect, string designatorTooltip = null)
    {
        if (thing == null || thing.uiIcon == null || thing.uiIcon == BaseContent.BadTex)
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
        if (!Instance.Settings.GroupSameDesignator || thing.designatorDropdown == null || designatorGroups == null ||
            !designatorGroups.ContainsKey(thing.designatorDropdown) ||
            !designatorGroups[thing.designatorDropdown].Any())
        {
            return;
        }

        var newRect = rect;
        newRect.x -= newRect.width;
        GUI.DrawTexture(newRect, PlusTexture);
        if (!string.IsNullOrEmpty(designatorTooltip))
        {
            TooltipHandler.TipRegion(newRect, designatorTooltip);
        }
    }

    // copypaste - Stolen from Replace Stuff mod
    private static void defLabelWithIconButNoTooltip(Rect rect, Def def, float iconMargin = 2f,
        float textOffsetX = 6f)
    {
        Widgets.BeginGroup(rect);
        var rect2 = new Rect(0f, 0f, rect.height, rect.height);
        if (iconMargin != 0f)
        {
            rect2 = rect2.ContractedBy(iconMargin);
        }

        Widgets.DefIcon(rect2, def, drawPlaceholder: true);
        var rect3 = new Rect(rect2.xMax + textOffsetX, 0f, rect.width, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        Widgets.Label(rect3, def.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        Widgets.EndGroup();
    }


    private void drawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + ColumnSpacer;
        optionsOuterContainer.width -= leftSideWidth + ColumnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(5);
        frameRect.x = leftSideWidth + ColumnSpacer + 15;
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        var listingOptions = new Listing_Standard();
        switch (SelectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                listingStandard.Begin(frameRect);
                listingStandard.CheckboxLabeled("TabSorting.SortLights.Label".Translate(), ref Settings.SortLights,
                    "TabSorting.SortLights.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortFloors.Label".Translate(), ref Settings.SortFloors,
                    "TabSorting.SortFloors.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortWallsDoors.Label".Translate(),
                    ref Settings.SortDoorsAndWalls,
                    "TabSorting.SortWallsDoors.Tooltip".Translate());
                if (Settings.SortDoorsAndWalls)
                {
                    listingStandard.CheckboxLabeled("TabSorting.SortDoors.Label".Translate(),
                        ref Settings.SortDoors,
                        "TabSorting.SortDoors.Tooltip".Translate());
                }
                else
                {
                    Settings.SortDoors = false;
                }

                listingStandard.CheckboxLabeled("TabSorting.SortTablesChairs.Label".Translate(),
                    ref Settings.SortTablesAndChairs,
                    "TabSorting.SortTablesChairs.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortBedroom.Label".Translate(),
                    ref Settings.SortBedroomFurniture,
                    "TabSorting.SortBedroom.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortKitchen.Label".Translate(),
                    ref Settings.SortKitchenFurniture,
                    "TabSorting.SortKitchen.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortHospital.Label".Translate(),
                    ref Settings.SortHospitalFurniture,
                    "TabSorting.SortHospital.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortResearch.Label".Translate(),
                    ref Settings.SortResearchFurniture,
                    "TabSorting.SortResearch.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortDecoration.Label".Translate(),
                    ref Settings.SortDecorations,
                    "TabSorting.SortDecoration.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.SortStorage.Label".Translate(), ref Settings.SortStorage,
                    "TabSorting.SortStorage.Tooltip".Translate());

                if (TabSorting.GardenToolsLoaded)
                {
                    listingStandard.CheckboxLabeled("TabSorting.SortGarden.Label".Translate(), ref Settings.SortGarden,
                        "TabSorting.SortGarden.Tooltip".Translate());
                }
                else
                {
                    Settings.SortGarden = false;
                }

                if (TabSorting.FencesAndFloorsLoaded)
                {
                    listingStandard.CheckboxLabeled("TabSorting.SortFences.Label".Translate(), ref Settings.SortFences,
                        "TabSorting.SortFences.Tooltip".Translate());
                }
                else
                {
                    Settings.SortFences = false;
                }

                if (ModLister.IdeologyInstalled)
                {
                    listingStandard.CheckboxLabeled("TabSorting.SortIdeology.Label".Translate(),
                        ref Settings.SortIdeologyFurniture,
                        "TabSorting.SortIdeology.Tooltip".Translate());
                }
                else
                {
                    Settings.SortIdeologyFurniture = false;
                }

                listingStandard.Gap();
                listingStandard.CheckboxLabeled("TabSorting.RemoveEmpty.Label".Translate(),
                    ref Settings.RemoveEmptyTabs,
                    "TabSorting.RemoveEmpty.Tooltip".Translate());
                listingStandard.CheckboxLabeled("TabSorting.GroupThings.Label".Translate(),
                    ref Settings.GroupSameDesignator,
                    "TabSorting.GroupThings.Tooltip".Translate());
                listingStandard.Gap();
                listingStandard.CheckboxLabeled("TabSorting.SortTabs.Label".Translate(), ref Settings.SortTabs,
                    "TabSorting.SortTabs.Tooltip".Translate());
                if (Settings.SortTabs)
                {
                    listingStandard.CheckboxLabeled("TabSorting.SkipZoneOrders.Label".Translate(),
                        ref Settings.SkipBuiltIn,
                        "TabSorting.SkipZoneOrders.Tooltip".Translate());
                }
                else
                {
                    Settings.SkipBuiltIn = false;
                }

                listingStandard.CheckboxLabeled("TabSorting.HideEmptyTabs.Label".Translate(),
                    ref Settings.HideEmptyTabs, "TabSorting.HideEmptyTabs.Tooltip".Translate());

                listingStandard.Gap();
                var labelPoint = listingStandard.Label("TabSorting.ManualReset.Label".Translate(), -1F,
                    "TabSorting.ManualReset.Tooltip".Translate());
                drawButton(Instance.Settings.ResetManualValues, "TabSorting.ResetSort".Translate(),
                    new Vector2(labelPoint.position.x + ButtonSpacer, labelPoint.position.y));
                drawButton(Instance.Settings.ResetManualThingSortingValues, "TabSorting.ResetOrder".Translate(),
                    new Vector2(labelPoint.position.x + ButtonSpacer + ButtonSize.x, labelPoint.position.y));

                if (Current.ProgramState == ProgramState.Playing)
                {
                    listingStandard.Label(
                        "TabSorting.MapRunning.Label".Translate());
                }

                listingStandard.CheckboxLabeled("TabSorting.VerboseLogging.Label".Translate(),
                    ref Settings.VerboseLogging,
                    "TabSorting.VerboseLogging.Tooltip".Translate());
                if (currentVersion != null)
                {
                    listingStandard.Gap();
                    GUI.contentColor = Color.gray;
                    listingStandard.Label("TabSorting.ModVersion".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listingStandard.End();
                break;
            }

            case "TabSorting":
            {
                contentRect.width -= 20;

                var sortedTabDefs = DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                    .OrderByDescending(def => def.order)
                    .ToList();
                contentRect.height = (sortedTabDefs.Count + 3) * 25f;
                Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
                listingOptions.Begin(contentRect);
                GUI.contentColor = Color.green;
                listingOptions.Label("TabSorting.TabSorting".Translate());
                GUI.contentColor = Color.white;
                if (Widgets.ButtonText(
                        new Rect(contentRect.position + new Vector2(contentRect.width - ButtonSize.x, 0), ButtonSize),
                        "TabSorting.Reset".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "TabSorting.ResetTabsort".Translate(),
                        "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                        delegate { Instance.Settings.ResetManualTabSortingValues(); }));
                }

                var num = 50f;
                for (var i = 0; i < sortedTabDefs.Count; i++)
                {
                    var currentDef = sortedTabDefs[i];
                    if (i > 0)
                    {
                        var rect2 = new Rect(0f, num, 12f, 12f);
                        if (Widgets.ButtonImage(rect2, TexButton.ReorderUp, Color.white))
                        {
                            if (currentDef.order == sortedTabDefs[i - 1].order)
                            {
                                currentDef.order -= 1;
                            }
                            else
                            {
                                (currentDef.order, sortedTabDefs[i - 1].order) =
                                    (sortedTabDefs[i - 1].order, currentDef.order);
                            }

                            Instance.Settings.ManualTabSorting[currentDef.defName] = currentDef.order;
                            Instance.Settings.ManualTabSorting[sortedTabDefs[i - 1].defName] =
                                sortedTabDefs[i - 1].order;
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                    }

                    if (i < sortedTabDefs.Count - 1)
                    {
                        var rect3 = new Rect(0f, num + 12f, 12f, 12f);
                        if (Widgets.ButtonImage(rect3, TexButton.ReorderDown, Color.white))
                        {
                            if (currentDef.order == sortedTabDefs[i + 1].order)
                            {
                                sortedTabDefs[i + 1].order -= 1;
                            }
                            else
                            {
                                (currentDef.order, sortedTabDefs[i + 1].order) =
                                    (sortedTabDefs[i + 1].order, currentDef.order);
                            }

                            Instance.Settings.ManualTabSorting[currentDef.defName] = currentDef.order;
                            Instance.Settings.ManualTabSorting[sortedTabDefs[i + 1].defName] =
                                sortedTabDefs[i + 1].order;
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                    }

                    Widgets.Label(new Rect(20f, num, contentRect.width - 20f, 25f),
                        $"{currentDef.LabelCap} ({currentDef.defName})");

                    num += 25f;
                }

                Widgets.DrawLineHorizontal(0, num, contentRect.width);
                listingOptions.End();
                Widgets.EndScrollView();
                break;
            }

            case "ButtonSorting":
            {
                contentRect.width -= 20;

                var buttonDefs = DefDatabase<MainButtonDef>.AllDefsListForReading
                    .OrderBy(def => def.order)
                    .ToList();
                contentRect.height = (buttonDefs.Count + 3) * 25f;
                Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
                listingOptions.Begin(contentRect);
                GUI.contentColor = Color.green;
                listingOptions.Label("TabSorting.ButtonSorting".Translate());
                GUI.contentColor = Color.white;
                if (Widgets.ButtonText(
                        new Rect(contentRect.position + new Vector2(contentRect.width - ButtonSize.x, 0), ButtonSize),
                        "TabSorting.Reset".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "TabSorting.ResetButtonsort".Translate(),
                        "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                        delegate { Instance.Settings.ResetManualButtonSortingValues(); }));
                }

                var num = 50f;
                for (var i = 0; i < buttonDefs.Count; i++)
                {
                    var currentDef = buttonDefs[i];
                    if (i > 0)
                    {
                        var rect2 = new Rect(0f, num, 12f, 12f);
                        if (Widgets.ButtonImage(rect2, TexButton.ReorderUp, Color.white))
                        {
                            if (currentDef.order == buttonDefs[i - 1].order)
                            {
                                currentDef.order -= 1;
                            }
                            else
                            {
                                (currentDef.order, buttonDefs[i - 1].order) =
                                    (buttonDefs[i - 1].order, currentDef.order);
                            }

                            Instance.Settings.ManualButtonSorting[currentDef.defName] = currentDef.order;
                            Instance.Settings.ManualButtonSorting[buttonDefs[i - 1].defName] = buttonDefs[i - 1].order;
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                    }

                    if (i < buttonDefs.Count - 1)
                    {
                        var rect3 = new Rect(0f, num + 12f, 12f, 12f);
                        if (Widgets.ButtonImage(rect3, TexButton.ReorderDown, Color.white))
                        {
                            if (currentDef.order == buttonDefs[i + 1].order)
                            {
                                buttonDefs[i + 1].order -= 1;
                            }
                            else
                            {
                                (currentDef.order, buttonDefs[i + 1].order) =
                                    (buttonDefs[i + 1].order, currentDef.order);
                            }

                            Instance.Settings.ManualButtonSorting[currentDef.defName] = currentDef.order;
                            Instance.Settings.ManualButtonSorting[buttonDefs[i + 1].defName] =
                                buttonDefs[i + 1].order;
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                    }

                    Widgets.Label(new Rect(20f, num, contentRect.width - 20f, 25f),
                        $"{currentDef.LabelCap} ({currentDef.defName})");

                    num += 25f;
                }

                Widgets.DrawLineHorizontal(0, num, contentRect.width);
                listingOptions.End();
                Widgets.EndScrollView();
                break;
            }

            case "Hidden":
            {
                contentRect.width -= 20;

                contentRect.height = (noneCategoryMembers.Count * 24f) + 40f;
                Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
                listingOptions.Begin(contentRect);
                GUI.contentColor = Color.green;
                listingOptions.Label("TabSorting.NoneHidden".Translate());
                GUI.contentColor = Color.white;
                foreach (var hiddenItem in noneCategoryMembers)
                {
                    var item = DefDatabase<BuildableDef>.GetNamedSilentFail(hiddenItem.Key);
                    var toolTip = item.defName;
                    if (!string.IsNullOrEmpty(item.modContentPack?.Name))
                    {
                        toolTip += $" ({item.modContentPack.Name})";
                    }

                    var currentPosition =
                        listingOptions.Label((TaggedString)item.label.CapitalizeFirst(), -1f, toolTip);
                    var buttonText = "TabSorting.Default".Translate();
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(item.defName))
                    {
                        buttonText = Settings.ManualSorting[item.defName];
                    }

                    drawButton(delegate { setManualSortTarget([item]); }, buttonText,
                        new Vector2(currentPosition.position.x + ButtonSpacer, currentPosition.position.y));
                    drawIcon(item,
                        new Rect(
                            new Vector2(currentPosition.position.x + ButtonSpacer - IconSize,
                                currentPosition.position.y), new Vector2(IconSize, IconSize)));
                }

                listingOptions.GapLine();
                listingOptions.End();
                Widgets.EndScrollView();
                break;
            }

            case "CreateNew":
            {
                listingStandard.Begin(frameRect);
                listingStandard.Gap();

                listingStandard.Label("TabSorting.NewTab".Translate());
                newTabName = listingStandard.TextEntry(newTabName);
                if (newTabName.Length > 0 && listingStandard.ButtonText("TabSorting.Create".Translate()))
                {
                    var cleanTabName = TabSorting.ValidateTabName(newTabName);
                    if (string.IsNullOrEmpty(cleanTabName))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "TabSorting.Exists".Translate(newTabName),
                            "TabSorting.Ok".Translate()));
                        break;
                    }

                    var order = 1;
                    var orders = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Select(def => def.order)
                        .ToArray();
                    while (orders.Contains(order))
                    {
                        order++;
                    }

                    var newTab = new DesignationCategoryDef
                    {
                        defName = cleanTabName,
                        label = newTabName,
                        generated = true,
                        order = order
                    };
                    DefGenerator.AddImpliedDef(newTab);
                    Instance.Settings.ManualTabs[cleanTabName] = newTabName;
                    Instance.Settings.ManualCategoryMemory.Add(
                        DefDatabase<DesignationCategoryDef>.GetNamed(cleanTabName));
                    SelectedDef = "Settings";
                    newTabName = string.Empty;
                    TabSorting.LogMessage(
                        "TabSorting.CurrentCustom".Translate(string.Join(",", Instance.Settings.ManualCategoryMemory)));
                }

                listingStandard.End();
                break;
            }

            default:
            {
                var sortCategory = TabSorting.GetDesignationFromDatabase(SelectedDef);
                if (sortCategory == null)
                {
                    Log.ErrorOnce(
                        $"TabSorter: Could not find category called {SelectedDef}, this should not happen.",
                        SelectedDef.GetHashCode());
                    return;
                }

                var allDefsInCategory = (from thing in DefDatabase<BuildableDef>.AllDefsListForReading
                    where thing.designationCategory != null && thing.designationCategory == sortCategory
                    select thing).ToList();

                allDefsInCategory.AddRange(from terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading
                    where terrainDef.designationCategory != null && terrainDef.designationCategory == sortCategory &&
                          !allDefsInCategory.Contains(terrainDef)
                    select terrainDef);

                allDefsInCategory =
                    verifyUniqueOrderValues([..allDefsInCategory.OrderBy(def => def.uiOrder)]);
                AllCurrentDefsInCategory.allDefsInCategory = allDefsInCategory;

                designatorGroups = new Dictionary<DesignatorDropdownGroupDef, List<BuildableDef>>();
                if (Instance.Settings.GroupSameDesignator)
                {
                    var tempAllDefsInCategory = new List<BuildableDef>();

                    foreach (var thingDef in allDefsInCategory)
                    {
                        if (thingDef.designatorDropdown == null)
                        {
                            tempAllDefsInCategory.Add(thingDef);
                            continue;
                        }

                        if (!designatorGroups.ContainsKey(thingDef.designatorDropdown))
                        {
                            designatorGroups.Add(thingDef.designatorDropdown, []);
                            tempAllDefsInCategory.Add(thingDef);
                        }

                        designatorGroups[thingDef.designatorDropdown].Add(thingDef);
                    }

                    allDefsInCategory = tempAllDefsInCategory;
                }

                var architectMargin = 0f;
                if (TabSorting.ArchitectIconsLoaded)
                {
                    architectMargin = tabIconContainer.x;
                    var tabIconRect =
                        new Rect(
                            frameRect.position + new Vector2(frameRect.width - tabIconContainer.x, 0),
                            tabIconContainer);
                    if (ListingExtension.TabIconSelectable(tabIconRect, TabSorting.GetCustomTabIcon(SelectedDef),
                            "TabSorting.Edit".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_ChooseTabIcon(SelectedDef));
                    }
                }

                GUI.contentColor = Color.green;
                var contentPack = "TabSorting.UnloadedMod".Translate();
                var manualTab = Instance.Settings.ManualCategoryMemory.Contains(sortCategory);
                if (sortCategory.modContentPack?.Name != null)
                {
                    contentPack = sortCategory.modContentPack.Name;
                }

                if (manualTab)
                {
                    contentPack = "TabSorting.CustomTab".Translate();
                }

                GUI.contentColor = Color.white;
                var frameTitle = $"{sortCategory.label.CapitalizeFirst()} ({sortCategory.defName}) - {contentPack}";
                Widgets.Label(
                    new Rect(frameRect.position,
                        new Vector2(frameRect.width - ButtonSize.x, searchSize.y)),
                    frameTitle);
                Text.Font = GameFont.Small;

                searchText =
                    Widgets.TextField(
                        new Rect(
                            frameRect.position + new Vector2(frameRect.width - searchSize.x, 0) -
                            new Vector2(architectMargin, 0), searchSize),
                        searchText);
                TooltipHandler.TipRegion(new Rect(frameRect.position + new Vector2(frameRect.width - searchSize.x, 0) -
                                                  new Vector2(architectMargin, 0),
                    searchSize), "TabSorting.Search".Translate());
                var extraRowSpace = ButtonSize.y * 1.5f;
                if (manualTab)
                {
                    if (Widgets.ButtonText(
                            new Rect(
                                frameRect.position +
                                new Vector2(0, ButtonSize.y),
                                ButtonSize),
                            "TabSorting.Rename".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_RenameTab(sortCategory));
                    }

                    if (Widgets.ButtonText(
                            new Rect(
                                frameRect.position + new Vector2(ButtonSize.x,
                                    ButtonSize.y),
                                ButtonSize),
                            "TabSorting.Delete".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "TabSorting.ResetOne".Translate(),
                            "TabSorting.No".Translate(), null, "TabSorting.Yes".Translate(),
                            delegate
                            {
                                SelectedDef = "Settings";
                                TabSorting.RemoveManualTab(sortCategory);
                            }));
                    }

                    extraRowSpace += ButtonSize.y;
                }

                Widgets.DrawLineHorizontal(frameRect.position.x,
                    frameRect.position.y + extraRowSpace - (ButtonSize.y / 3), frameRect.width);
                var viewRect = frameRect;
                viewRect.height -= extraRowSpace;
                viewRect.y += extraRowSpace;
                contentRect.width -= 20;
                AllCurrentDefsInCategory.allCurrentDefsInCategory = allDefsInCategory;

                if (!string.IsNullOrEmpty(searchText))
                {
                    AllCurrentDefsInCategory.allCurrentDefsInCategory = allDefsInCategory.Where(def =>
                        def.label.ToLower().Contains(searchText.ToLower()) ||
                        def.modContentPack?.Name.ToLower().Contains(searchText.ToLower()) == true).ToList();
                }

                var itemHeight = Text.LineHeight + 5f;

                var moveEverythingRowHeight = itemHeight + 20f;
                var defListHeight = (AllCurrentDefsInCategory.allCurrentDefsInCategory.Count * itemHeight) + 10f;

                contentRect.height = defListHeight + moveEverythingRowHeight;
                Widgets.BeginScrollView(viewRect, ref optionsScrollPosition, contentRect);
                listingOptions.Begin(contentRect);

                listingOptions.Gap(5f);

                if (AllCurrentDefsInCategory.allCurrentDefsInCategory.Any())
                {
                    var buttonRow = new Rect(contentRect.x, listingOptions.CurHeight, contentRect.width, 24);
                    Widgets.Label(buttonRow.LeftHalf(), "TabSorting.MoveEverything".Translate());
                    if (Widgets.ButtonText(buttonRow.LeftHalf().LeftPart(0.95f).RightPartPixels(ButtonSize.x),
                            "TabSorting.Select".Translate()))
                    {
                        setManualSortTarget(AllCurrentDefsInCategory.allCurrentDefsInCategory);
                    }

                    Widgets.Label(buttonRow.RightHalf(), "TabSorting.SortAlphabetically".Translate());
                    if (Widgets.ButtonText(buttonRow.RightHalf().LeftPart(0.95f).RightPartPixels(ButtonSize.x),
                            "TabSorting.Sort".Translate()))
                    {
                        AllCurrentDefsInCategory.allDefsInCategory =
                            sortAlphabetically(AllCurrentDefsInCategory.allDefsInCategory);
                        TabSorting.DoTheSorting();
                    }

                    listingOptions.Gap(24f);
                }

                listingOptions.GapLine();

                var reorderRect = listingOptions.GetRect(defListHeight);
                Widgets.DrawBox(reorderRect);
                listingOptions.Gap(2f);

                var labelRect = reorderRect.ContractedBy(5).TopPartPixels(itemHeight);
                var globalDragRect = labelRect;
                globalDragRect.position = GUIUtility.GUIToScreenPoint(globalDragRect.position);

                if (Event.current.type == EventType.Repaint)
                {
                    reorderID = ReorderableWidget.NewGroup(
                        AllCurrentDefsInCategory.Reorder,
                        ReorderableDirection.Vertical,
                        reorderRect,
                        extraDraggedItemOnGUI: delegate(int index, Vector2 dragStartPos)
                        {
                            var dragRect = globalDragRect; //copy it in so multiple frames don't edit the same thing
                            dragRect.y += index * itemHeight; //i-th item
                            dragRect.position +=
                                Event.current.mousePosition - dragStartPos; //adjust for mouse vs starting point
                            //Same id 34003428 as GenUI.DrawMouseAttachment
                            Find.WindowStack.ImmediateWindow(34003428, dragRect, WindowLayer.Super, () =>
                                defLabelWithIconButNoTooltip(dragRect.AtZero(),
                                    AllCurrentDefsInCategory.allCurrentDefsInCategory[index], 0)
                            );
                        });
                }

                foreach (var def in AllCurrentDefsInCategory.allCurrentDefsInCategory)
                {
                    var toolTip = def.defName;
                    var iconToolTip = string.Empty;

                    if (Instance.Settings.GroupSameDesignator && def.designatorDropdown != null &&
                        designatorGroups.ContainsKey(def.designatorDropdown) &&
                        designatorGroups[def.designatorDropdown].Any())
                    {
                        iconToolTip = "TabSorting.GroupContaining".Translate(string.Join("\n",
                            designatorGroups[def.designatorDropdown].Select(buildableDef => buildableDef.LabelCap)));
                    }

                    if (!string.IsNullOrEmpty(def.modContentPack?.Name))
                    {
                        toolTip += $" ({def.modContentPack.Name})";
                    }

                    var halfRect = labelRect;
                    halfRect.width /= 2;
                    var rightPart = halfRect.RightPartPixels(halfRect.width - IconSize - IconSpacer);
                    rightPart.y += 2;
                    var leftPart = halfRect.LeftPartPixels(IconSize).TopPartPixels(IconSize).CenteredOnYIn(halfRect);
                    GUI.DrawTexture(leftPart, TexButton.DragHash);
                    Widgets.Label(rightPart, def.LabelCap);
                    TooltipHandler.TipRegion(rightPart, toolTip);
                    var buttonText = "TabSorting.Default".Translate();
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(def.defName))
                    {
                        buttonText = Settings.ManualSorting[def.defName];
                    }

                    var buttonPosition = new Vector2(rightPart.position.x + ButtonSpacer, rightPart.position.y);
                    var copyRect = new Rect(buttonPosition + new Vector2(ButtonSize.x + IconSpacer, 0), TabIconSize);
                    var pasteRect = new Rect(
                        buttonPosition + new Vector2(ButtonSize.x + (IconSpacer * 2) + TabIconSize.x, 0),
                        TabIconSize);
                    if (GUIUtility.systemCopyBuffer?.StartsWith("Designation|") == true)
                    {
                        var designation = GUIUtility.systemCopyBuffer.Split('|').Last();
                        if (designation != buttonText)
                        {
                            TooltipHandler.TipRegion(pasteRect, "TabSorting.Paste".Translate(designation));
                            if (Widgets.ButtonImageFitted(pasteRect, TexButton.Paste))
                            {
                                if (designation != buttonText)
                                {
                                    if (designation == "TabSorting.Default".Translate())
                                    {
                                        Instance.Settings.ManualSorting?.Remove(def.defName);
                                    }
                                    else
                                    {
                                        Settings.ManualSorting[def.defName] = designation;
                                    }

                                    ResetSortOrder(def.defName);
                                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                                    Messages.Message("TabSorting.Pasted".Translate(),
                                        MessageTypeDefOf.SituationResolved,
                                        false);
                                }
                            }
                        }
                    }

                    drawButton(delegate { setManualSortTarget([def]); }, buttonText, buttonPosition);
                    if (Widgets.ButtonImageFitted(copyRect, TexButton.Copy))
                    {
                        GUIUtility.systemCopyBuffer = $"Designation|{buttonText}";
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        Messages.Message("TabSorting.Copied".Translate(), MessageTypeDefOf.SituationResolved, false);
                    }

                    TooltipHandler.TipRegionByKey(copyRect, "TabSorting.Copy");

                    drawIcon(def,
                        new Rect(
                            new Vector2(rightPart.position.x + ButtonSpacer - IconSize,
                                rightPart.position.y), new Vector2(IconSize, IconSize)), iconToolTip);

                    ReorderableWidget.Reorderable(reorderID, labelRect);

                    labelRect.y += itemHeight;
                }

                listingOptions.GapLine();
                listingOptions.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private static List<BuildableDef> verifyUniqueOrderValues(List<BuildableDef> buildableDefs)
    {
        if (!buildableDefs.Any(def => buildableDefs.Count(buildableDef => buildableDef.uiOrder == def.uiOrder) > 1))
        {
            return buildableDefs;
        }

        var currentOrder = buildableDefs.OrderBy(def => def.uiOrder).First().uiOrder;
        var skipList = new List<BuildableDef>();

        foreach (var buildableDef in buildableDefs)
        {
            if (buildableDef.designatorDropdown != null)
            {
                foreach (var def in buildableDefs)
                {
                    if (def.designatorDropdown != buildableDef.designatorDropdown)
                    {
                        continue;
                    }

                    def.uiOrder = currentOrder;
                    Instance.Settings.ManualThingSorting[def.defName] = currentOrder;
                    skipList.Add(def);
                    currentOrder++;
                }

                continue;
            }

            if (!skipList.Any(def => def == buildableDef))
            {
                buildableDef.uiOrder = currentOrder;
                Instance.Settings.ManualThingSorting[buildableDef.defName] = currentOrder;
            }

            currentOrder++;
        }

        return buildableDefs;
    }

    public static void ResetSortOrder(string defName)
    {
        Instance.Settings.ManualThingSorting?.Remove(defName);
    }

    private static void drawTabsList(Rect rect)
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

        var categoryDefs = Instance.Settings.VanillaCategoryMemory;
        var manualDefs = Instance.Settings.ManualCategoryMemory;
        var rows = categoryDefs.Count + manualDefs.Count + 6;
        if (manualDefs.Count == 0)
        {
            rows--;
        }

        tabContentRect.height = rows * 27f;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listingStandard.Begin(tabContentRect);
        if (listingStandard.ListItemSelectable("TabSorting.Settings".Translate(), Color.yellow,
                SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }


        if (listingStandard.ListItemSelectable("TabSorting.CreateNew".Translate(), Color.yellow,
                SelectedDef == "CreateNew"))
        {
            newTabName = string.Empty;
            SelectedDef = SelectedDef == "CreateNew" ? null : "CreateNew";
        }

        if (!Instance.Settings.SortTabs)
        {
            if (listingStandard.ListItemSelectable("TabSorting.TabSorting".Translate(), Color.yellow,
                    SelectedDef == "TabSorting"))
            {
                SelectedDef = SelectedDef == "TabSorting" ? null : "TabSorting";
            }
        }

        if (listingStandard.ListItemSelectable("TabSorting.ButtonSorting".Translate(), Color.yellow,
                SelectedDef == "ButtonSorting"))
        {
            SelectedDef = SelectedDef == "ButtonSorting" ? null : "ButtonSorting";
        }

        listingStandard.ListItemSelectable(null, Color.yellow);
        foreach (var categoryDef in categoryDefs)
        {
            if (!listingStandard.ListItemSelectable(
                    $"{categoryDef.label.CapitalizeFirst()} ({categoryDef.defName})", Color.yellow,
                    SelectedDef == categoryDef.defName, categoryDef.defName))
            {
                continue;
            }

            SelectedDef = SelectedDef == categoryDef.defName ? null : categoryDef.defName;
        }

        listingStandard.ListItemSelectable(null, Color.yellow);
        if (manualDefs.Any())
        {
            foreach (var categoryDef in manualDefs)
            {
                if (!listingStandard.ListItemSelectable(
                        $"{categoryDef.label.CapitalizeFirst()} ({categoryDef.defName})", Color.yellow,
                        SelectedDef == categoryDef.defName, categoryDef.defName))
                {
                    continue;
                }

                SelectedDef = SelectedDef == categoryDef.defName ? null : categoryDef.defName;
            }

            listingStandard.ListItemSelectable(null, Color.yellow);
        }

        if (listingStandard.ListItemSelectable("TabSorting.NoneHidden".Translate(), Color.yellow,
                SelectedDef == "Hidden"))
        {
            SelectedDef = SelectedDef == "Hidden" ? null : "Hidden";
        }

        listingStandard.End();
        Widgets.EndScrollView();
    }
}