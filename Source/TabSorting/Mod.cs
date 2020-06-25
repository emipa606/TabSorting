using UnityEngine;
using Verse;

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
            set
            {
                settings = value;
            }
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
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.Gap();
            listing_Standard.Label("NOTICE: Any change here will only be activated on the next restart of RimWorld");
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("Sort lights", ref Settings.SortLights, "Moves all lights to the Lights-tab");
            listing_Standard.CheckboxLabeled("Sort floors", ref Settings.SortFloors, "Moves all floors to the Floors-tab");
            listing_Standard.CheckboxLabeled("Sort walls & doors", ref Settings.SortDoorsAndWalls, "Moves all doors and walls to the Structure-tab");
            listing_Standard.CheckboxLabeled("Sort tables & chairs", ref Settings.SortTablesAndChairs, "Moves all tables and chairs the Table/Chairs-tab");
            listing_Standard.CheckboxLabeled("Sort bedroom furniture", ref Settings.SortBedroomFurniture, "Moves all bedroom-furniture to the Bedroom-tab");
            listing_Standard.CheckboxLabeled("Sort decorations", ref Settings.SortDecorations, "Moves all rugs, plantpots and other cosmetic items to the Decorations-tab");
            if (DefDatabase<DesignationCategoryDef>.GetNamed("FurnitureStorage", false) != null)
                listing_Standard.CheckboxLabeled("Sort storage", ref Settings.SortStorage, "Moves all storage to the Storage-tab from Extended storage");
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("Remove empty tabs after sorting", ref Settings.RemoveEmptyTabs, "If a tab has no things left to build after sorting, remove the tab");
            //listing_Standard.Gap();
            //var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefsListForReading;
            //List<bool> designatorStatuses = new List<bool>();
            //foreach (DesignationCategoryDef designationCategoryDef in allDesignators)
            //{
            //    designatorStatuses.Add(Settings.CategoriesToIgnore.Contains(designationCategoryDef.defName));
            //    listing_Standard.che)
            //}
            listing_Standard.End();
            Settings.Write();
        }

        /// <summary>
        /// The instance of the settings to be read by the mod
        /// </summary>
        public static TabSortingMod instance;

        /// <summary>
        /// The private settings
        /// </summary>
        private TabSortingModSettings settings;

    }
}
