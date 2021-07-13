using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace TabSorting
{
    [StaticConstructorOnStartup]
    public static class TabSorting
    {
        private static List<string> changedDefNames;

        private static List<string> tabsToIgnore;

        private static List<string> defsToIgnore;

        private static List<string> namespacesToIgnore;

        static TabSorting()
        {
            DoTheSorting();
        }

        public static void DoTheSorting()
        {
            LogMessage("Starting a new sorting-session");
            defsToIgnore = new List<string>();
            tabsToIgnore = new List<string>();
            changedDefNames = new List<string>();
            namespacesToIgnore = new List<string>();
            if (!TabSortingMod.instance.Settings.VanillaCategoryMemory.Any())
            {
                foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
                {
                    TabSortingMod.instance.Settings.VanillaCategoryMemory.Add(categoryDef);
                    TabSortingMod.instance.Settings.VanillaOrderMemory.Add(categoryDef, categoryDef.order);
                }
            }

            if (!TabSortingMod.instance.Settings.VanillaItemMemory.Any())
            {
                foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    TabSortingMod.instance.Settings.VanillaItemMemory.Add(thingDef, thingDef.designationCategory);
                }

                foreach (var terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
                {
                    TabSortingMod.instance.Settings.VanillaItemMemory.Add(terrainDef, terrainDef.designationCategory);
                }
            }
            else
            {
                RestoreVanillaSorting();
            }

            TabSortingMod.instance.Settings.VanillaCategoryMemory.SortBy(def => def.label);
            var ignoreMods = (from mod in LoadedModManager.RunningModsListForReading
                where mod.PackageId == "atlas.androidtiers" || mod.PackageId == "dubwise.dubsbadhygiene" ||
                      mod.PackageId == "vanillaexpanded.vfepower" ||
                      mod.PackageId == "vanillaexpanded.vfepropsanddecor" ||
                      mod.PackageId == "kentington.saveourship2" || mod.PackageId == "flashpoint55.poweredfloorpanelmod"
                select mod).ToList();
            if (ignoreMods.Count > 0)
            {
                foreach (var mod in ignoreMods)
                {
                    LogMessage($"{mod.Name} has {mod.AllDefs.Count()} definitions, adding to ignore.");
                    foreach (var def in mod.AllDefs)
                    {
                        defsToIgnore.Add(def.defName);
                    }
                }
            }

            // Static defs to ignore (not too many hopefully)
            defsToIgnore.Add("FM_AIManager");
            defsToIgnore.Add("PRF_MiniDroneColumn");
            defsToIgnore.Add("PRF_RecipeDatabase");
            defsToIgnore.Add("PRF_TypeOneAssembler_I");
            defsToIgnore.Add("PRF_TypeTwoAssembler_I");
            defsToIgnore.Add("PRF_TypeTwoAssembler_II");
            defsToIgnore.Add("PRF_TypeTwoAssembler_III");

            tabsToIgnore.Add("Planning");
            tabsToIgnore.Add("Shapes");

            namespacesToIgnore.Add("RimWorld");
            namespacesToIgnore.Add("DubRoss");

            TabSortingMod.instance.Settings = TabSortingMod.instance.Settings ?? new TabSortingModSettings
            {
                SortLights = true,
                SortFloors = false,
                SortDoorsAndWalls = false,
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

            SortManually();

            SortLights();

            SortFloors();

            SortDoorsAndWalls();

            SortTablesAndChairs();

            SortBedroomFurniture();

            SortKitchenFurniture();

            SortResearchFurniture();

            SortHospitalFurniture();

            SortStorage();

            SortGarden();

            SortFences();

            SortDecorations();

            var designationCategoriesToRemove = new List<DesignationCategoryDef>();

            foreach (var designationCategoryDef in from dd in DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                where !tabsToIgnore.Contains(dd.defName)
                select dd)
            {
                designationCategoryDef.ResolveReferences();
                if (CheckEmptyDesignationCategoryDef(designationCategoryDef.defName))
                {
                    designationCategoriesToRemove.Add(designationCategoryDef);
                }
            }

            if (TabSortingMod.instance.Settings.RemoveEmptyTabs)
            {
                for (var i = designationCategoriesToRemove.Count - 1; i >= 0; i--)
                {
                    LogMessage($"Removing {designationCategoriesToRemove[i].defName} since its empty now.", true);
                    RemoveEmptyDesignationCategoryDef(designationCategoriesToRemove[i]);
                }
            }

            if (!TabSortingMod.instance.Settings.SortTabs)
            {
                RefreshArchitectMenu();
                return;
            }

            var topValue = 800;
            var designationCategoryDefs =
                from dd in DefDatabase<DesignationCategoryDef>.AllDefs
                orderby dd.label
                select dd;
            var steps = (int) Math.Floor((decimal) ((float) topValue / designationCategoryDefs.Count()));
            foreach (var designationCategoryDef in designationCategoryDefs)
            {
                topValue -= steps;
                if (TabSortingMod.instance.Settings.SkipBuiltIn && (designationCategoryDef.label == "orders" ||
                                                                    designationCategoryDef.label == "zone"))
                {
                    continue;
                }

                designationCategoryDef.order = topValue;
            }

            RefreshArchitectMenu();
        }

        /// <summary>
        ///     Goes through all items and checks if there are any references to the selected category
        ///     Removes the category if there are none
        /// </summary>
        /// <param name="currentCategoryName">The category to check</param>
        private static bool CheckEmptyDesignationCategoryDef(string currentCategoryName)
        {
            var currentCategory = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(currentCategoryName);
            if (currentCategory.defName == "Orders" || currentCategory.defName == "Zone")
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

        private static DesignationCategoryDef GetDesignationFromDatabase(string categoryString)
        {
            if (!TabSortingMod.instance.Settings.VanillaCategoryMemory.Any(def => def.defName == categoryString))
            {
                return null;
            }

            var returnValue =
                TabSortingMod.instance.Settings.VanillaCategoryMemory.First(def => def.defName == categoryString);
            if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryString) == null)
            {
                DefGenerator.AddImpliedDef(returnValue);
            }

            return returnValue;
        }

        private static void RefreshArchitectMenu()
        {
            LogMessage("Sorting-session done");
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            MainButtonDefOf.Architect.tabWindowClass
                .GetMethod("CacheDesPanels", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(MainButtonDefOf.Architect.TabWindow, null);
        }

        /// <summary>
        ///     Removes a (hopefully) empty category
        /// </summary>
        /// <param name="currentCategory"></param>
        private static void RemoveEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
        {
            GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), typeof(DesignationCategoryDef), "Remove",
                currentCategory);
        }

        private static void RestoreVanillaSorting()
        {
            LogMessage("Restoring all things to vanilla sorting");
            foreach (var designationCategoryDef in TabSortingMod.instance.Settings.VanillaCategoryMemory)
            {
                var designation = GetDesignationFromDatabase(designationCategoryDef.defName);
                if (designation != null)
                {
                    designation.order = TabSortingMod.instance.Settings.VanillaOrderMemory[designationCategoryDef];
                }
            }

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (TabSortingMod.instance.Settings.VanillaItemMemory.ContainsKey(def))
                {
                    def.designationCategory = TabSortingMod.instance.Settings.VanillaItemMemory[def];
                }
            }

            foreach (var def in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                if (TabSortingMod.instance.Settings.VanillaItemMemory.ContainsKey(def))
                {
                    def.designationCategory = TabSortingMod.instance.Settings.VanillaItemMemory[def];
                }
            }

            foreach (var designationCategoryDef in TabSortingMod.instance.Settings.VanillaCategoryMemory)
            {
                designationCategoryDef.ResolveReferences();
            }
        }

        /// <summary>
        ///     Sort kitchen furniture to the Kitchen-tab
        /// </summary>
        private static void SortKitchenFurniture()
        {
            var designationCategory = GetDesignationFromDatabase("KitchenTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the KitchenTab-def, will not sort kitchen items.",
                    "KitchenTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortKitchenFurniture)
            {
                return;
            }

            var foodCatagories = ThingCategoryDefOf.Foods.ThisAndChildCategoryDefs;

            var foods = (from food in DefDatabase<ThingDef>.AllDefsListForReading
                where food.thingCategories != null && food.thingCategories.SharesElementWith(foodCatagories)
                select food).ToList();
            var foodRecipies = (from recipe in DefDatabase<RecipeDef>.AllDefsListForReading
                where recipe.ProducedThingDef != null && foods.Contains(recipe.ProducedThingDef)
                select recipe).ToList();
            var recipeMakers = (from foodMaker in DefDatabase<ThingDef>.AllDefsListForReading
                where foodMaker.recipes != null && foodMaker.recipes.SharesElementWith(foodRecipies)
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

            foreach (var recipeDef in foodRecipies)
            {
                if (recipeDef.recipeUsers == null || !recipeDef.recipeUsers.Any())
                {
                    continue;
                }

                foodMakers.AddRange(recipeDef.recipeUsers);
            }

            LogMessage($"Found {foodMakers.Count} food processing buildings");

            var foodMakersInGame = (from foodMaker in foodMakers
                where !defsToIgnore.Contains(foodMaker.defName) && !changedDefNames.Contains(foodMaker.defName) &&
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
        private static void SortResearchFurniture()
        {
            var designationCategory = GetDesignationFromDatabase("ResearchTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the ResearchTab-def, will not sort research items.",
                    "ResearchTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortResearchFurniture)
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
                                                      building.thingClass.IsInstanceOfType(
                                                          typeof(Building_ResearchBench)))
                select building).ToList();

            LogMessage(
                $"Found {researchBenches.Count} research-benches and {requiredResearchBuildings.Count} researchBuildings");
            researchBuildings.AddRange(researchBenches);
            researchBuildings.AddRange(requiredResearchBuildings);
            var researchBuildingsInGame = (from researchBuilding in researchBuildings
                where !defsToIgnore.Contains(researchBuilding.defName) &&
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
        private static void SortBedroomFurniture()
        {
            var designationCategory = GetDesignationFromDatabase("BedroomTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the BedroomTab-def, will not sort bedroom items.",
                    "BedroomTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortBedroomFurniture)
            {
                return;
            }

            var bedsInGame = (from bed in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(bed.defName) && !changedDefNames.Contains(bed.defName) &&
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
                "Moved " + (affectedByFacilities.Count + bedsInGame.Count) + " bedroom furniture to the Bedroom tab.",
                true);
        }

        /// <summary>
        ///     Sort decorative items to the Decorations-tab
        /// </summary>
        private static void SortDecorations()
        {
            var designationCategory = GetDesignationFromDatabase("DecorationTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the DecorationTab-def, will not sort decoration items.",
                    "DecorationTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortDecorations)
            {
                return;
            }

            var rugsInGame = (from rug in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(rug.defName) && !changedDefNames.Contains(rug.defName) &&
                      rug.designationCategory != null && rug.altitudeLayer == AltitudeLayer.FloorEmplacement &&
                      !rug.clearBuildingArea && rug.passability == Traversability.Standable &&
                      rug.StatBaseDefined(StatDefOf.Beauty) && rug.GetStatValueAbstract(StatDefOf.Beauty) > 0
                select rug).ToList();
            var decorativePlantsInGame = (from decorativePlant in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(decorativePlant.defName) &&
                      !changedDefNames.Contains(decorativePlant.defName) &&
                      decorativePlant.designationCategory != null && decorativePlant.building is {sowTag: "Decorative"}
                select decorativePlant).ToList();
            var decorativeFurnitureInGame = (from decorativeFurniture in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(decorativeFurniture.defName) &&
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
        private static void SortDoorsAndWalls()
        {
            if (!TabSortingMod.instance.Settings.SortDoorsAndWalls)
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

            var staticStructureDefs = new List<string> {"GL_DoorFrame"};

            var doorsAndWallsInGame = (from doorOrWall in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(doorOrWall.defName) && !changedDefNames.Contains(doorOrWall.defName) &&
                      (doorOrWall.designationCategory != null &&
                       doorOrWall.designationCategory.defName != "Structure" &&
                       (doorOrWall.fillPercent == 1f || doorOrWall.label.ToLower().Contains("column")) &&
                       (doorOrWall.holdsRoof || doorOrWall.IsDoor) ||
                       staticStructureDefs.Contains(doorOrWall.defName))
                select doorOrWall).ToList();
            var bridgesInGame = (from bridge in DefDatabase<TerrainDef>.AllDefsListForReading
                where !defsToIgnore.Contains(bridge.defName) && !changedDefNames.Contains(bridge.defName) &&
                      bridge.designationCategory != null && bridge.designationCategory.defName != "Structure" &&
                      bridge.destroyEffect != null && bridge.destroyEffect.defName.ToLower().Contains("bridge")
                select bridge).ToList();
            foreach (var doorOrWall in doorsAndWallsInGame)
            {
                LogMessage(
                    $"Changing designation for doorOrWall {doorOrWall.defName} from {doorOrWall.designationCategory} to {designationCategory.defName}");
                changedDefNames.Add(doorOrWall.defName);
                doorOrWall.designationCategory = designationCategory;
            }

            foreach (var bridge in bridgesInGame)
            {
                LogMessage(
                    $"Changing designation for bridge {bridge.defName} from {bridge.designationCategory} to {designationCategory.defName}");
                changedDefNames.Add(bridge.defName);
                bridge.designationCategory = designationCategory;
            }

            LogMessage($"Moved {doorsAndWallsInGame.Count} bridges, doors and walls to the Structure tab.", true);
        }

        /// <summary>
        ///     Sorts all fences-items to the Fences-tab if Fences and Floors is loaded
        /// </summary>
        private static void SortFences()
        {
            if (!TabSortingMod.instance.Settings.SortFences)
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
                where !defsToIgnore.Contains(fence.defName) && !changedDefNames.Contains(fence.defName) &&
                      fence.designationCategory != null && fence.designationCategory.defName != "Fences" &&
                      ((fence.thingClass?.Name == "Building_Door" || fence.thingClass?.Name == "Building" &&
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

        /// <summary>
        ///     Sorts all floors to the Floors-tab
        /// </summary>
        private static void SortFloors()
        {
            if (!TabSortingMod.instance.Settings.SortFloors)
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
                where !defsToIgnore.Contains(floor.defName) && !changedDefNames.Contains(floor.defName) &&
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
        private static void SortGarden()
        {
            if (!TabSortingMod.instance.Settings.SortGarden)
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
                where !defsToIgnore.Contains(gardenThing.defName) && !changedDefNames.Contains(gardenThing.defName) &&
                      gardenThing.designationCategory?.defName != "GardenTools" &&
                      (gardenThing.thingClass?.Name == "Building_SunLamp" ||
                       gardenThing.thingClass?.Name == "Building_PlantGrower" &&
                       gardenThing.building?.sowTag != "Decorative" ||
                       gardenThing.label.ToLower().Contains("sprinkler") &&
                       !gardenThing.label.ToLower().Contains("fire"))
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
        private static void SortHospitalFurniture()
        {
            var designationCategory = GetDesignationFromDatabase("HospitalTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the HospitalTab-def, will not sort hospital items.",
                    "HospitalTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortHospitalFurniture)
            {
                return;
            }

            var hospitalBedsInGame = (from hoispitalBed in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(hoispitalBed.defName) && !changedDefNames.Contains(hoispitalBed.defName) &&
                      hoispitalBed.designationCategory != null && hoispitalBed.IsBed && hoispitalBed.building is
                          {bed_defaultMedical: true}
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

                    if ((from offset in facility.GetCompProperties<CompProperties_Facility>().statOffsets
                        where offset.stat == StatDefOf.SurgerySuccessChanceFactor ||
                              offset.stat == StatDefOf.MedicalTendQualityOffset ||
                              offset.stat == StatDefOf.ImmunityGainSpeedFactor
                        select offset).ToList().Count > 0)
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
        private static void SortLights()
        {
            var designationCategory = GetDesignationFromDatabase("LightsTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the LightsTab-def, will not sort lights.",
                    "LightsTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortLights)
            {
                return;
            }

            var gardenToolsExists = DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) != null;

            var lightsInGame = (from furniture in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(furniture.defName) && !changedDefNames.Contains(furniture.defName) &&
                      furniture.designationCategory != null &&
                      (furniture.category == ThingCategory.Building &&
                       (furniture.GetCompProperties<CompProperties_Power>() == null ||
                        furniture.GetCompProperties<CompProperties_Power>().compClass != typeof(CompPowerPlant) &&
                        (furniture.GetCompProperties<CompProperties_Power>().basePowerConsumption < 2000 ||
                         furniture.thingClass?.Name == "Building_SunLamp")) && furniture.recipes == null &&
                       (furniture.placeWorkers == null ||
                        !furniture.placeWorkers.Contains(typeof(PlaceWorker_ShowFacilitiesConnections))) &&
                       furniture.GetCompProperties<CompProperties_ShipLandingBeacon>() == null &&
                       furniture.GetCompProperties<CompProperties_Battery>() == null &&
                       furniture.GetCompProperties<CompProperties_Glower>() != null &&
                       furniture.GetCompProperties<CompProperties_Glower>().glowRadius >= 3 &&
                       furniture.GetCompProperties<CompProperties_TempControl>() == null &&
                       (furniture.GetCompProperties<CompProperties_HeatPusher>() == null ||
                        furniture.GetCompProperties<CompProperties_HeatPusher>().heatPerSecond <
                        furniture.GetCompProperties<CompProperties_Glower>().glowRadius) &&
                       furniture.surfaceType != SurfaceType.Eat &&
                       furniture.terrainAffordanceNeeded != TerrainAffordanceDefOf.Heavy &&
                       furniture.thingClass?.Name != "Building_TurretGun" &&
                       furniture.thingClass?.Name != "Building_PlantGrower" &&
                       furniture.thingClass?.Name != "Building_Heater" &&
                       (furniture.thingClass?.Name != "Building_SunLamp" || !gardenToolsExists) &&
                       (furniture.inspectorTabs == null ||
                        !furniture.inspectorTabs.Contains(typeof(ITab_Storage))) && !furniture.hasInteractionCell ||
                       furniture.label != null &&
                       (furniture.label.ToLower().Contains("wall") || furniture.label.ToLower().Contains("floor")) &&
                       (furniture.label.ToLower().Contains("light") || furniture.label.ToLower().Contains("lamp")))
                select furniture).ToList();
            foreach (var furniture in lightsInGame)
            {
                LogMessage(
                    $"Changing designation for furniture {furniture.defName} from {furniture.designationCategory} to {designationCategory.defName}");
                changedDefNames.Add(furniture.defName);
                furniture.designationCategory = designationCategory;
            }

            LogMessage($"Moved {lightsInGame.Count} lights to the Lights tab.", true);
        }

        /// <summary>
        ///     Sorts all manually assigned things
        /// </summary>
        private static void SortManually()
        {
            if (TabSortingMod.instance.Settings.ManualSorting == null ||
                TabSortingMod.instance.Settings.ManualSorting.Count == 0)
            {
                return;
            }

            var thingsToRemove = new List<string>();
            foreach (var itemToSort in TabSortingMod.instance.Settings.ManualSorting)
            {
                var designationCategory = GetDesignationFromDatabase(itemToSort.Value);
                if (designationCategory == null && itemToSort.Value != "None")
                {
                    thingsToRemove.Add(itemToSort.Key);
                    continue;
                }

                var thingDefToSort = DefDatabase<ThingDef>.GetNamedSilentFail(itemToSort.Key);
                var terrainDefToSort = DefDatabase<TerrainDef>.GetNamedSilentFail(itemToSort.Key);
                if (thingDefToSort == null && terrainDefToSort == null)
                {
                    thingsToRemove.Add(itemToSort.Key);
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

            foreach (var defName in thingsToRemove)
            {
                TabSortingMod.instance.Settings.ManualSorting.Remove(defName);
            }

            LogMessage(
                $"Moved {TabSortingMod.instance.Settings.ManualSorting.Count} items to manually designated categories.",
                true);
        }

        /// <summary>
        ///     Sorts all storage-items to the Storage-tab if Storage-extended is loaded
        /// </summary>
        private static void SortStorage()
        {
            if (!TabSortingMod.instance.Settings.SortStorage)
            {
                return;
            }

            var designationCategory = GetDesignationFromDatabase("LWM_DS_Storage");

            if (designationCategory == null)
            {
                designationCategory = GetDesignationFromDatabase("FurnitureStorage");
            }

            if (designationCategory == null)
            {
                designationCategory = GetDesignationFromDatabase("StorageTab");
            }

            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the StorageTab-def, will not sort storage items.",
                    "StorageTab".GetHashCode());
                return;
            }

            var storageInGame = (from storage in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(storage.defName) && !changedDefNames.Contains(storage.defName) &&
                      storage.designationCategory != null &&
                      storage.designationCategory.defName != "FurnitureStorage" && storage.thingClass != null &&
                      storage.thingClass?.Name != "Building_Grave" &&
                      (storage.thingClass.Name == "Building_Storage" || storage.inspectorTabs != null &&
                          storage.inspectorTabs.Contains(typeof(ITab_Storage))) && (storage.placeWorkers == null ||
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
        private static void SortTablesAndChairs()
        {
            var designationCategory = GetDesignationFromDatabase("TableChairsTab");
            if (designationCategory == null)
            {
                Log.ErrorOnce("[TabSorting]: Cannot find the TableChairsTab-def, will not sort tables and chairs.",
                    "TableChairsTab".GetHashCode());
                return;
            }

            if (!TabSortingMod.instance.Settings.SortTablesAndChairs)
            {
                return;
            }

            var tableChairsInGame = (from table in DefDatabase<ThingDef>.AllDefsListForReading
                where !defsToIgnore.Contains(table.defName) && !changedDefNames.Contains(table.defName) &&
                      table.designationCategory != null &&
                      (table.IsTable ||
                       table.surfaceType == SurfaceType.Eat && table.label.ToLower().Contains("table") ||
                       table.building is {isSittable: true})
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

        private static void LogMessage(string message, bool force = false)
        {
            if (TabSortingMod.instance.Settings.VerboseLogging || force)
            {
                Log.Message($"[TabSorting]: {message}");
            }
        }
    }
}