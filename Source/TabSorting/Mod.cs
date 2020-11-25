using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting
{
    [StaticConstructorOnStartup]
    internal class TabSortingMod : Mod
    {
        /// <summary>
        /// Cunstructor
        /// </summary>
        /// <param name="content"></param>
        public TabSortingMod(ModContentPack content) : base(content)
        {
            instance = this;
        }

        /// <summary>
        /// The instance-settings for the mod
        /// </summary>
        internal TabSortingModSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = GetSettings<TabSortingModSettings>();
                }
                return settings;
            }
            set => settings = value;
        }

        /// <summary>
        /// The title for the mod-settings
        /// </summary>
        /// <returns></returns>
        public override string SettingsCategory()
        {
            return "Tab-sorting";
        }

        /// <summary>
        /// The settings-window
        /// </summary>
        /// <param name="rect"></param>
        public override void DoSettingsWindowContents(Rect rect)
        {
            var rect1 = new Rect(rect.x, rect.y, rect.width, rect.height);
            var totalItems = (from thing in DefDatabase<ThingDef>.AllDefsListForReading where thing.designationCategory != null select thing).Count();
            totalItems += (from terrain in DefDatabase<TerrainDef>.AllDefsListForReading where terrain.designationCategory != null select terrain).Count();
            var rect2 = new Rect(0f, 0f, rect1.width - 30f, yStartPoint + (totalItems * rowHeight));

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
                    var label = string.Empty;
                    if (hiddenThingDef != null)
                    {
                        label = hiddenThingDef.label;
                    }
                    else
                    {
                        label = hiddenTerrainDef.label;
                    }
                    noneCategoryMembers.Add(item.Key, label);
                }
                foreach (var defName in thingsToRemove)
                {
                    instance.Settings.ManualSorting.Remove(defName);
                }
            }

            Widgets.BeginScrollView(rect1, ref scrollPosition, rect2, true);
            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect2);
            listing_Standard.Gap();
            listing_Standard.Label("NOTICE: Any change here will only be activated on the next restart of RimWorld");
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("Sort lights", ref Settings.SortLights, "Moves all lights to the Lights-tab");
            listing_Standard.CheckboxLabeled("Sort floors", ref Settings.SortFloors, "Moves all floors to the Floors-tab");
            listing_Standard.CheckboxLabeled("Sort walls & doors", ref Settings.SortDoorsAndWalls, "Moves all doors and walls to the Structure-tab");
            listing_Standard.CheckboxLabeled("Sort tables & chairs", ref Settings.SortTablesAndChairs, "Moves all tables and chairs the Table/Chairs-tab");
            listing_Standard.CheckboxLabeled("Sort bedroom furniture", ref Settings.SortBedroomFurniture, "Moves all bedroom-furniture to the Bedroom-tab");
            listing_Standard.CheckboxLabeled("Sort hospital furniture", ref Settings.SortHospitalFurniture, "Moves all hospital-furniture to the Hospital-tab");
            listing_Standard.CheckboxLabeled("Sort decorations", ref Settings.SortDecorations, "Moves all rugs, plantpots and other cosmetic items to the Decorations-tab");
            if (DefDatabase<DesignationCategoryDef>.GetNamed("FurnitureStorage", false) != null)
            {
                listing_Standard.CheckboxLabeled("Sort storage", ref Settings.SortStorage, "Moves all storage to the Storage-tab from Extended storage");
            }

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
            listing_Standard.GapLine();
            listing_Standard.GapLine();
            var labelPoint = listing_Standard.Label("Manual sorting");
            DrawButton(delegate { ResetManualSorting(); }, "Reset all", new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
            //listing_Standard.Label(labelPoint.position.y.ToString());
            var categories = from designationCategory in DefDatabase<DesignationCategoryDef>.AllDefsListForReading orderby designationCategory.label select designationCategory;
            foreach (var sortCategory in categories)
            {
                GUI.contentColor = Color.green;
                listing_Standard.Label($"{GenText.CapitalizeFirst(sortCategory.label)} ({sortCategory.defName}) - {sortCategory.modContentPack.Name}");
                GUI.contentColor = Color.white;
                var allDefsInCategory = from thing in DefDatabase<ThingDef>.AllDefsListForReading where thing.designationCategory != null && thing.designationCategory == sortCategory orderby thing.label select thing;
                foreach (var thing in allDefsInCategory)
                {
                    var currentPosition = listing_Standard.Label(GenText.CapitalizeFirst(thing.label));
                    var buttonText = "Default";
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(thing.defName))
                    {
                        buttonText = Settings.ManualSorting[thing.defName];
                    }
                    DrawButton(delegate { SetManualSortTarget(thing.defName); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                }
                var allTerrainInCategory = from terrain in DefDatabase<TerrainDef>.AllDefsListForReading where terrain.designationCategory != null && terrain.designationCategory == sortCategory orderby terrain.label select terrain;
                foreach (var terrain in allTerrainInCategory)
                {
                    var currentPosition = listing_Standard.Label(GenText.CapitalizeFirst(terrain.label));
                    var buttonText = "Default";
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(terrain.defName))
                    {
                        buttonText = Settings.ManualSorting[terrain.defName];
                    }
                    DrawButton(delegate { SetManualSortTarget(terrain.defName); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                }
                listing_Standard.GapLine();
            }
            if (noneCategoryMembers.Count() > 0)
            {
                GUI.contentColor = Color.green;
                listing_Standard.Label("None (Hidden)");
                GUI.contentColor = Color.white;
                foreach (var hiddenItem in noneCategoryMembers)
                {
                    var currentPosition = listing_Standard.Label(GenText.CapitalizeFirst(hiddenItem.Value)); 
                    var buttonText = "Default";
                    if (Settings.ManualSorting != null && Settings.ManualSorting.ContainsKey(hiddenItem.Key))
                    {
                        buttonText = Settings.ManualSorting[hiddenItem.Key];
                    }
                    DrawButton(delegate { SetManualSortTarget(hiddenItem.Key); }, buttonText, new Vector2(currentPosition.position.x + buttonSpacer, currentPosition.position.y));
                }
            }
            labelPoint = listing_Standard.Label("End of setting");
            //listing_Standard.Label(labelPoint.position.y.ToString());
            listing_Standard.End();
            Widgets.EndScrollView();
            Settings.Write();
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
            var list = new List<FloatMenuOption>
            {
                new FloatMenuOption("Default", defaultAction)
            };

            foreach (var sortOption in from vanillaCategory in instance.Settings.VanillaMemory orderby vanillaCategory.Value select vanillaCategory)
            {
                void action()
                {
                    instance.Settings.ManualSorting[defName] = sortOption.Key;
                }
                list.Add(new FloatMenuOption($"{GenText.CapitalizeFirst(sortOption.Value)} ({sortOption.Key})", action));
            }
            void noneAction()
            {
                instance.Settings.ManualSorting[defName] = "None";
            }
            list.Add(new FloatMenuOption("None (Hidden)", noneAction));
            Find.WindowStack.Add(new FloatMenu(list));
        }

        private static void DrawButton(Action action, string text, Vector2 pos)
        {
            var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
            if (Widgets.ButtonText(rect, text, true, false, Color.white))
            {
                SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                action();
            }
        }

        private static void ResetManualSorting()
        {
            instance.Settings.ManualSorting = new Dictionary<string, string>();
        }

        /// <summary>
        /// The instance of the settings to be read by the mod
        /// </summary>
        public static TabSortingMod instance;

        /// <summary>
        /// The private settings
        /// </summary>
        private TabSortingModSettings settings;

        private static Dictionary<string, string> noneCategoryMembers;

        private static Vector2 scrollPosition = Vector2.zero;

        private static readonly int yStartPoint = 324;

        private static readonly int rowHeight = 28;

        private static readonly int buttonSpacer = 300;

        protected static readonly Vector2 buttonSize = new Vector2(120f, 25f);
    }
}
