using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting
{
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
                foreach (var item in from item in instance.Settings.ManualSorting where item.Value == "None" select item)
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

        private static void SetManualSortTarget(string defName)
        {
            void defaultAction()
            {
                if (instance.Settings.ManualSorting == null || !instance.Settings.ManualSorting.ContainsKey(defName))
                {
                    return;
                }

                instance.Settings.ManualSorting.Remove(defName);
            }

            var list = new List<FloatMenuOption> {new FloatMenuOption("Default", defaultAction)};

            foreach (var sortOption in from vanillaCategory in instance.Settings.VanillaCategoryMemory orderby vanillaCategory.label select vanillaCategory)
            {
                void action()
                {
                    instance.Settings.ManualSorting[defName] = sortOption.defName;
                }

                list.Add(new FloatMenuOption($"{sortOption.label.CapitalizeFirst()} ({sortOption.defName})", action));
            }

            void noneAction()
            {
                instance.Settings.ManualSorting[defName] = "None";
            }

            list.Add(new FloatMenuOption("None (Hidden)", noneAction));
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
                var ratio = (float) texture2D.width / texture2D.height;

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
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("Sort lights", ref Settings.SortLights, "Moves all lights to the Lights-tab");
                    listing_Standard.CheckboxLabeled("Sort floors", ref Settings.SortFloors, "Moves all floors to the Floors-tab");
                    listing_Standard.CheckboxLabeled("Sort walls & doors", ref Settings.SortDoorsAndWalls, "Moves all doors and walls to the Structure-tab");
                    listing_Standard.CheckboxLabeled("Sort tables & chairs", ref Settings.SortTablesAndChairs, "Moves all tables and chairs the Table/Chairs-tab");
                    listing_Standard.CheckboxLabeled("Sort bedroom furniture", ref Settings.SortBedroomFurniture, "Moves all bedroom-furniture to the Bedroom-tab");
                    listing_Standard.CheckboxLabeled("Sort hospital furniture", ref Settings.SortHospitalFurniture, "Moves all hospital-furniture to the Hospital-tab");
                    listing_Standard.CheckboxLabeled("Sort decorations", ref Settings.SortDecorations, "Moves all rugs, plantpots and other cosmetic items to the Decorations-tab");
                    listing_Standard.CheckboxLabeled("Sort storage", ref Settings.SortStorage, "Moves all storage to the Storage-tab");

                    if (DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) != null)
                    {
                        listing_Standard.CheckboxLabeled("Sort garden tools", ref Settings.SortGarden, "Moves all garden items to the Garden-tab from VGP Garden Tools");
                    }

                    if (DefDatabase<DesignationCategoryDef>.GetNamed("Fences", false) != null)
                    {
                        listing_Standard.CheckboxLabeled("Sort fences", ref Settings.SortFences, "Moves all fences to the Fences-tab from Fences and Floors");
                    }

                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("Remove empty tabs after sorting", ref Settings.RemoveEmptyTabs, "If a tab has no things left to build after sorting, remove the tab");
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("Sort all tabs alphabetically", ref Settings.SortTabs, "Puts all tabs in alphabetical order");
                    listing_Standard.CheckboxLabeled("But skip Orders and Zone-tab", ref Settings.SkipBuiltIn, "Orders and Zone-tab will remain in the top if the menu");
                    var labelPoint = listing_Standard.Label("Manual sorting reset", -1F, "Reset all manually defined sortings");
                    DrawButton(instance.Settings.ResetManualValues, "Reset all", new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                    listing_Standard.End();
                    break;
                }

                case "Hidden":
                {
                    contentRect.width -= 20;

                    contentRect.height = (noneCategoryMembers.Count() * 24f) + 40f;
                    listing_Options.BeginScrollView(frameRect, ref optionsScrollPosition, ref contentRect);

                    GUI.contentColor = Color.green;
                    listing_Options.Label("None (Hidden)");
                    GUI.contentColor = Color.white;
                    foreach (var hiddenItem in noneCategoryMembers)
                    {
                        var item = DefDatabase<BuildableDef>.GetNamedSilentFail(hiddenItem.Key);
                        var currentPosition = listing_Options.Label(item.label.CapitalizeFirst());
                        var buttonText = "Default";
                        if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(item.defName))
                        {
                            buttonText = Settings.ManualSorting[item.defName];
                        }

                        DrawButton(delegate { SetManualSortTarget(item.defName); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                        drawIcon(item, new Rect(new Vector2(currentPosition.position.x + buttonSpacer - iconSize, currentPosition.position.y), new Vector2(iconSize, iconSize)));
                    }

                    listing_Options.GapLine();
                    listing_Options.EndScrollView(ref contentRect);
                    break;
                }

                default:
                {
                    var sortCategory = (from DesignationCategoryDef category in instance.Settings.VanillaCategoryMemory where category.defName == selectedDef select category).FirstOrDefault();
                    if (sortCategory == null)
                    {
                        Log.Message("TabSorter: Could not find category, this should not happen.");
                        return;
                    }

                    var allDefsInCategory = from thing in DefDatabase<ThingDef>.AllDefsListForReading where thing.designationCategory != null && thing.designationCategory == sortCategory orderby thing.label select thing;

                    var allTerrainInCategory = from terrain in DefDatabase<TerrainDef>.AllDefsListForReading where terrain.designationCategory != null && terrain.designationCategory == sortCategory orderby terrain.label select terrain;

                    contentRect.width -= 20;

                    contentRect.height = ((allDefsInCategory.Count() + allTerrainInCategory.Count()) * 24f) + 40f;
                    listing_Options.BeginScrollView(frameRect, ref optionsScrollPosition, ref contentRect);

                    // var listing_Standard = new Listing_Standard();
                    // listing_Standard.Begin(scrollView);
                    GUI.contentColor = Color.green;
                    var contentPack = "Unloaded mod";
                    if (sortCategory.modContentPack?.Name != null)
                    {
                        contentPack = sortCategory.modContentPack.Name;
                    }

                    listing_Options.Label($"{sortCategory.label.CapitalizeFirst()} ({sortCategory.defName}) - {contentPack}");
                    GUI.contentColor = Color.white;

                    // Log.Message($"{sortCategory.defName} Fetching defs");
                    foreach (var thing in allDefsInCategory)
                    {
                        // Log.Message($"Sorting {thing.defName}");
                        var currentPosition = listing_Options.Label(thing.label.CapitalizeFirst());
                        var buttonText = "Default";
                        if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(thing.defName))
                        {
                            buttonText = Settings.ManualSorting[thing.defName];
                        }

                        DrawButton(delegate { SetManualSortTarget(thing.defName); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                        drawIcon(thing, new Rect(new Vector2(currentPosition.position.x + buttonSpacer - iconSize, currentPosition.position.y), new Vector2(iconSize, iconSize)));
                    }

                    foreach (var terrain in allTerrainInCategory)
                    {
                        // Log.Message($"Sorting {terrain.defName}");
                        var currentPosition = listing_Options.Label(terrain.label.CapitalizeFirst());
                        var buttonText = "Default";
                        if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(terrain.defName))
                        {
                            buttonText = Settings.ManualSorting[terrain.defName];
                        }

                        DrawButton(delegate { SetManualSortTarget(terrain.defName); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                        drawIcon(terrain, new Rect(new Vector2(currentPosition.position.x + buttonSpacer - iconSize, currentPosition.position.y), new Vector2(iconSize, iconSize)));
                    }

                    listing_Options.GapLine();
                    listing_Options.EndScrollView(ref contentRect);
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

            tabContentRect.height = (categoryDefs.Count * 22f) + 15;
            listing_Standard.BeginScrollView(tabFrameRect, ref tabsScrollPosition, ref tabContentRect);
            if (listing_Standard.ListItemSelectable("Settings", Color.yellow, selectedDef == "Settings"))
            {
                selectedDef = selectedDef == "Settings" ? null : "Settings";
            }

            listing_Standard.ListItemSelectable(null, Color.yellow);
            foreach (var categoryDef in categoryDefs)
            {
                if (!listing_Standard.ListItemSelectable($"{categoryDef.label.CapitalizeFirst()} ({categoryDef.defName})", Color.yellow, selectedDef == categoryDef.defName))
                {
                    continue;
                }

                selectedDef = selectedDef == categoryDef.defName ? null : categoryDef.defName;
            }

            if (listing_Standard.ListItemSelectable("None (Hidden)", Color.yellow, selectedDef == "Hidden"))
            {
                selectedDef = selectedDef == "Hidden" ? null : "Hidden";
            }

            listing_Standard.EndScrollView(ref tabContentRect);
        }
    }
}