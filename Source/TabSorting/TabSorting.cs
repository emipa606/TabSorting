﻿//#define DEBUGGING

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TabSorting
{
    [StaticConstructorOnStartup]
    public static class TabSorting
    {
        private static readonly List<string> defsToIgnore = new();

        private static readonly HashSet<DesignationCategoryDef> changedCategories =
            new();

        private static readonly List<string> changedDefNames = new();

        /// <summary>
        ///     The main sorting function
        /// </summary>
        static TabSorting()
        {
            foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                TabSortingMod.instance.Settings.VanillaMemory.Add(categoryDef);
            }

            TabSortingMod.instance.Settings.VanillaMemory.SortBy(def => def.label);
            var ignoreMods = (from mod in LoadedModManager.RunningModsListForReading
                where
                    mod.PackageId == "atlas.androidtiers" ||
                    mod.PackageId == "dubwise.dubsbadhygiene" ||
                    mod.PackageId == "vanillaexpanded.vfepower" ||
                    mod.PackageId == "vanillaexpanded.vfepropsanddecor" ||
                    mod.PackageId == "kentington.saveourship2" ||
                    mod.PackageId == "flashpoint55.poweredfloorpanelmod"
                select mod).ToList();
            if (ignoreMods.Count > 0)
            {
                foreach (var mod in ignoreMods)
                {
#if DEBUGGING
                    Log.Message("TabSorting: " + mod.Name + " has " + mod.AllDefs.Count() + " definitions, adding to ignore.");
#endif
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

            if (TabSortingMod.instance.Settings == null)
            {
                TabSortingMod.instance.Settings = new TabSortingModSettings
                {
                    SortLights = true,
                    SortFloors = false,
                    SortDoorsAndWalls = false,
                    SortBedroomFurniture = false,
                    SortHospitalFurniture = false,
                    SortTablesAndChairs = false,
                    SortDecorations = false,
                    SortStorage = false,
                    SortGarden = false,
                    SortFences = false,
                    RemoveEmptyTabs = true,
                    SortTabs = false,
                    SkipBuiltIn = false
                };
            }

            SortManually();

            SortLights();

            SortFloors();

            SortDoorsAndWalls();

            SortTablesAndChairs();

            SortBedroomFurniture();

            SortHospitalFurniture();

            SortDecorations();

            SortStorage();

            SortGarden();

            SortFences();

            var designationsNotToTestForRemoval = new List<string>
            {
                "LightsTab",
                "TableChairsTab",
                "BedroomTab"
            };

            if (changedCategories.Count > 0)
            {
#if DEBUGGING
                Log.Message(changedCategories.Count + " DesignationCategoryDefs changed, resolving references. " + string.Join(",", changedCategories));
#endif
                foreach (var designationCategoryDef in changedCategories)
                {
                    designationCategoryDef.ResolveReferences();
                    if (designationsNotToTestForRemoval.Contains(designationCategoryDef.defName))
                    {
                        continue;
                    }

                    if (designationCategoryDef.defName.StartsWith("LWM_DS"))
                    {
                        continue;
                    }

                    if (TabSortingMod.instance.Settings.RemoveEmptyTabs)
                    {
                        CheckEmptyDesignationCategoryDef(designationCategoryDef);
                    }
                }
            }

            if (!TabSortingMod.instance.Settings.SortTabs)
            {
                return;
            }

            var topValue = 800;
            var designationCategoryDefs = from dd in DefDatabase<DesignationCategoryDef>.AllDefs
                orderby dd.label
                select dd;
            var steps = (int) Math.Floor((decimal) (topValue / designationCategoryDefs.Count()));
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
        }

        /// <summary>
        ///     Goes through all items and checks if there are any references to the selected category
        ///     Removes the category if there are none
        /// </summary>
        /// <param name="currentCategory">The category to check</param>
        private static void CheckEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
        {
#if DEBUGGING
            Log.Message("TabSorting: Checking current defs in " + currentCategory.defName);
#endif
            var thingsLeft = from td in DefDatabase<ThingDef>.AllDefsListForReading
                where td.designationCategory == currentCategory
                select td;
            if (thingsLeft.Count() != 0)
            {
                return;
            }
#if DEBUGGING
            Log.Message("TabSorting: Checking current terrainDefs in " + currentCategory.defName);
#endif
            var moreThingsLeft = from tr in DefDatabase<TerrainDef>.AllDefsListForReading
                where tr.designationCategory == currentCategory
                select tr;
            if (moreThingsLeft.Count() != 0)
            {
                return;
            }

            Log.Message("TabSorting: Removing " + currentCategory.defName + " since its empty now.");
            RemoveEmptyDesignationCategoryDef(currentCategory);
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
                var categoryToSortTo = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(itemToSort.Value);
                if (categoryToSortTo == null && itemToSort.Value != "None")
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
                    changedCategories.Add(thingDefToSort.designationCategory);
                    thingDefToSort.designationCategory = itemToSort.Value != "None" ? categoryToSortTo : null;
                }
                else
                {
                    changedCategories.Add(terrainDefToSort.designationCategory);
                    terrainDefToSort.designationCategory = itemToSort.Value != "None" ? categoryToSortTo : null;
                }

                changedDefNames.Add(itemToSort.Key);
                if (itemToSort.Value != "None")
                {
                    changedCategories.Add(categoryToSortTo);
                }
            }

            foreach (var defName in thingsToRemove)
            {
                TabSortingMod.instance.Settings.ManualSorting.Remove(defName);
            }

            Log.Message("TabSorting: Moved " + TabSortingMod.instance.Settings.ManualSorting.Count +
                        " items to manually designated categories.");
        }

        /// <summary>
        ///     Sorts all lights to the Lights-tab
        /// </summary>
        private static void SortLights()
        {
            var lightsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("LightsTab");
            if (TabSortingMod.instance.Settings.SortLights)
            {
                var gardenToolsExists = DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false) != null;

                var lightsInGame = (from furniture in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(furniture.defName) &&
                        !changedDefNames.Contains(furniture.defName) &&
                        furniture.designationCategory != null &&
                        (furniture.category == ThingCategory.Building &&
                         (furniture.GetCompProperties<CompProperties_Power>() == null ||
                          furniture.GetCompProperties<CompProperties_Power>().compClass != typeof(CompPowerPlant) &&
                          (furniture.GetCompProperties<CompProperties_Power>().basePowerConsumption < 2000 ||
                           furniture.thingClass.Name == "Building_SunLamp")) &&
                         furniture.recipes == null &&
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
                         furniture.thingClass.Name != "Building_TurretGun" &&
                         furniture.thingClass.Name != "Building_PlantGrower" &&
                         furniture.thingClass.Name != "Building_Heater" &&
                         (furniture.thingClass.Name != "Building_SunLamp" || !gardenToolsExists) &&
                         (furniture.inspectorTabs == null || !furniture.inspectorTabs.Contains(typeof(ITab_Storage))) &&
                         !furniture.hasInteractionCell ||
                         furniture.label != null &&
                         (furniture.label.ToLower().Contains("wall") || furniture.label.ToLower().Contains("floor")) &&
                         (furniture.label.ToLower().Contains("light") || furniture.label.ToLower().Contains("lamp")))
                    select furniture).ToList();
                changedCategories.Add(lightsDesignationCategory);
                foreach (var furniture in lightsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for furniture " + furniture.defName + " from " + furniture.designationCategory + " to " + lightsDesignationCategory.defName);
#endif
                    changedDefNames.Add(furniture.defName);
                    changedCategories.Add(furniture.designationCategory);
                    furniture.designationCategory = lightsDesignationCategory;
                }

                Log.Message("TabSorting: Moved " + lightsInGame.Count + " lights to the Lights tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(lightsDesignationCategory);
            }
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

            var floorsInGame = (from floor in DefDatabase<TerrainDef>.AllDefsListForReading
                where
                    !defsToIgnore.Contains(floor.defName) &&
                    !changedDefNames.Contains(floor.defName) &&
                    floor.designationCategory != null &&
                    floor.designationCategory.defName != "Floors" &&
                    floor.fertility == 0 &&
                    !floor.destroyBuildingsOnDestroyed
                select floor).ToList();
            var floorsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Floors");
            changedCategories.Add(floorsDesignationCategory);
            foreach (var floor in floorsInGame)
            {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for floor " + floor.defName + " from " + floor.designationCategory + " to " + floorsDesignationCategory.defName);
#endif
                changedDefNames.Add(floor.defName);
                changedCategories.Add(floor.designationCategory);
                floor.designationCategory = floorsDesignationCategory;
            }

            Log.Message("TabSorting: Moved " + floorsInGame.Count + " floors to the Floors tab.");
        }

        /// <summary>
        ///     Sorts all tables and sitting-furniture to the Table/Chairs-tab
        /// </summary>
        private static void SortTablesAndChairs()
        {
            var tableChairsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("TableChairsTab");
            if (TabSortingMod.instance.Settings.SortTablesAndChairs)
            {
                var tableChairsInGame = (from table in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(table.defName) &&
                        !changedDefNames.Contains(table.defName) &&
                        table.designationCategory != null &&
                        (table.IsTable || table.surfaceType == SurfaceType.Eat &&
                         table.label.ToLower().Contains("table") ||
                         table.building != null && table.building.isSittable)
                    select table).ToList();
                changedCategories.Add(tableChairsDesignationCategory);
                foreach (var tableOrChair in tableChairsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for tableOrChair " + tableOrChair.defName + " from " + tableOrChair.designationCategory + " to " + tableChairsDesignationCategory.defName);
#endif
                    changedDefNames.Add(tableOrChair.defName);
                    changedCategories.Add(tableOrChair.designationCategory);
                    tableOrChair.designationCategory = tableChairsDesignationCategory;
                }

                Log.Message("TabSorting: Moved " + tableChairsInGame.Count +
                            " tables and chairs to the Table/Chairs tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(tableChairsDesignationCategory);
            }
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

            var staticStructureDefs = new List<string>
            {
                "GL_DoorFrame"
            };

            var doorsAndWallsInGame = (from doorOrWall in DefDatabase<ThingDef>.AllDefsListForReading
                where
                    !defsToIgnore.Contains(doorOrWall.defName) &&
                    !changedDefNames.Contains(doorOrWall.defName) &&
                    (doorOrWall.designationCategory != null &&
                     doorOrWall.designationCategory.defName != "Structure" &&
                     (doorOrWall.fillPercent == 1f || doorOrWall.label.ToLower().Contains("column")) &&
                     (doorOrWall.holdsRoof || doorOrWall.IsDoor)
                     || staticStructureDefs.Contains(doorOrWall.defName))
                select doorOrWall).ToList();
            var bridgesInGame = (from bridge in DefDatabase<TerrainDef>.AllDefsListForReading
                where
                    !defsToIgnore.Contains(bridge.defName) &&
                    !changedDefNames.Contains(bridge.defName) &&
                    bridge.designationCategory != null &&
                    bridge.designationCategory.defName != "Structure" &&
                    bridge.destroyEffect != null && bridge.destroyEffect.defName.ToLower().Contains("bridge")
                select bridge).ToList();
            var structureDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Structure");
            changedCategories.Add(structureDesignationCategory);
            foreach (var doorOrWall in doorsAndWallsInGame)
            {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for doorOrWall " + doorOrWall.defName + " from " + doorOrWall.designationCategory + " to " + structureDesignationCategory.defName);
#endif
                changedDefNames.Add(doorOrWall.defName);
                changedCategories.Add(doorOrWall.designationCategory);
                doorOrWall.designationCategory = structureDesignationCategory;
            }

            foreach (var bridge in bridgesInGame)
            {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for bridge " + bridge.defName + " from " + bridge.designationCategory + " to " + structureDesignationCategory.defName);
#endif
                changedDefNames.Add(bridge.defName);
                changedCategories.Add(bridge.designationCategory);
                bridge.designationCategory = structureDesignationCategory;
            }

            Log.Message("TabSorting: Moved " + doorsAndWallsInGame.Count +
                        " bridges, doors and walls to the Structure tab.");
        }

        /// <summary>
        ///     Sort bedroom furniture to the Bedroom-tab
        /// </summary>
        private static void SortBedroomFurniture()
        {
            var bedroomFurnitureDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("BedroomTab");
            if (TabSortingMod.instance.Settings.SortBedroomFurniture)
            {
                var bedsInGame = (from bed in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(bed.defName) &&
                        !changedDefNames.Contains(bed.defName) &&
                        bed.designationCategory != null &&
                        bed.IsBed &&
                        (bed.building == null || !bed.building.bed_defaultMedical && bed.building.bed_humanlike)
                    select bed).ToList();
                changedCategories.Add(bedroomFurnitureDesignationCategory);
                var affectedByFacilities = new HashSet<ThingDef>();
                foreach (var bed in bedsInGame)
                {
                    if (bed.comps.Count == 0)
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

                        if ((from offset in facility.GetCompProperties<CompProperties_Facility>().statOffsets
                            where offset.stat == StatDefOf.SurgerySuccessChanceFactor ||
                                  offset.stat == StatDefOf.MedicalTendQualityOffset ||
                                  offset.stat == StatDefOf.ImmunityGainSpeedFactor
                            select offset).ToList().Count > 0)
                        {
                            continue;
                        }

                        affectedByFacilities.Add(facility);
                    }
                }

                foreach (var bed in bedsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for bed " + bed.defName + " from " + bed.designationCategory + " to " + bedroomFurnitureDesignationCategory.defName);
#endif
                    changedDefNames.Add(bed.defName);
                    changedCategories.Add(bed.designationCategory);
                    bed.designationCategory = bedroomFurnitureDesignationCategory;
                }

                foreach (var facility in affectedByFacilities)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for facility " + facility.defName + " from " + facility.designationCategory + " to " + bedroomFurnitureDesignationCategory.defName);
#endif
                    changedDefNames.Add(facility.defName);
                    changedCategories.Add(facility.designationCategory);
                    facility.designationCategory = bedroomFurnitureDesignationCategory;
                }

                Log.Message("TabSorting: Moved " + (affectedByFacilities.Count + bedsInGame.Count) +
                            " bedroom furniture to the Bedroom tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(bedroomFurnitureDesignationCategory);
            }
        }

        /// <summary>
        ///     Sort hospital furniture to the Hospital-tab
        /// </summary>
        private static void SortHospitalFurniture()
        {
            var hospitalFurnitureDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("HospitalTab");
            if (TabSortingMod.instance.Settings.SortHospitalFurniture)
            {
                var hospitalBedsInGame = (from hoispitalBed in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(hoispitalBed.defName) &&
                        !changedDefNames.Contains(hoispitalBed.defName) &&
                        hoispitalBed.designationCategory != null &&
                        hoispitalBed.IsBed &&
                        hoispitalBed.building != null && hoispitalBed.building.bed_defaultMedical
                    select hoispitalBed).ToList();
                changedCategories.Add(hospitalFurnitureDesignationCategory);
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
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for hospitalBed " + hospitalBed.defName + " from " + hospitalBed.designationCategory + " to " + hospitalFurnitureDesignationCategory.defName);
#endif
                    changedDefNames.Add(hospitalBed.defName);
                    changedCategories.Add(hospitalBed.designationCategory);
                    hospitalBed.designationCategory = hospitalFurnitureDesignationCategory;
                }

                foreach (var facility in affectedByFacilities)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for facility " + facility.defName + " from " + facility.designationCategory + " to " + hospitalFurnitureDesignationCategory.defName);
#endif
                    changedDefNames.Add(facility.defName);
                    changedCategories.Add(facility.designationCategory);
                    facility.designationCategory = hospitalFurnitureDesignationCategory;
                }

                Log.Message("TabSorting: Moved " + (affectedByFacilities.Count + hospitalBedsInGame.Count) +
                            " hospital furniture to the Hospital tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(hospitalFurnitureDesignationCategory);
            }
        }

        /// <summary>
        ///     Sort decorative items to the Decorations-tab
        /// </summary>
        private static void SortDecorations()
        {
            var decorationsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("DecorationTab");
            if (TabSortingMod.instance.Settings.SortDecorations)
            {
                var rugsInGame = (from rug in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(rug.defName) &&
                        !changedDefNames.Contains(rug.defName) &&
                        rug.designationCategory != null &&
                        rug.altitudeLayer == AltitudeLayer.FloorEmplacement &&
                        !rug.clearBuildingArea &&
                        rug.passability == Traversability.Standable &&
                        rug.StatBaseDefined(StatDefOf.Beauty) &&
                        rug.GetStatValueAbstract(StatDefOf.Beauty) > 0
                    select rug).ToList();
                var decorativePlantsInGame = (from decorativePlant in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(decorativePlant.defName) &&
                        !changedDefNames.Contains(decorativePlant.defName) &&
                        decorativePlant.designationCategory != null &&
                        decorativePlant.building != null && decorativePlant.building.sowTag == "Decorative"
                    select decorativePlant).ToList();
                var decorativeFurnitureInGame = (from decorativeFurniture in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(decorativeFurniture.defName) &&
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

                changedCategories.Add(decorationsDesignationCategory);
                foreach (var rug in rugsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for rug " + rug.defName + " from " + rug.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedDefNames.Add(rug.defName);
                    changedCategories.Add(rug.designationCategory);
                    rug.designationCategory = decorationsDesignationCategory;
                }

                foreach (var planter in decorativePlantsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for planter " + planter.defName + " from " + planter.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedDefNames.Add(planter.defName);
                    changedCategories.Add(planter.designationCategory);
                    planter.designationCategory = decorationsDesignationCategory;
                }

                foreach (var furniture in decorativeFurnitureInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for furniture " + furniture.defName + " from " + furniture.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedDefNames.Add(furniture.defName);
                    changedCategories.Add(furniture.designationCategory);
                    furniture.designationCategory = decorationsDesignationCategory;
                }

                Log.Message("TabSorting: Moved " +
                            (rugsInGame.Count + decorativePlantsInGame.Count + decorativeFurnitureInGame.Count) +
                            " decorative items to the Decorations tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(decorationsDesignationCategory);
            }
        }

        /// <summary>
        ///     Sorts all storage-items to the Storage-tab if Storage-extended is loaded
        /// </summary>
        private static void SortStorage()
        {
            var storageDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("LWM_DS_Storage", false);
            if (storageDesignationCategory == null)
            {
                storageDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("FurnitureStorage", false);
            }

            if (storageDesignationCategory == null)
            {
                storageDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("StorageTab", false);
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(DefDatabase<DesignationCategoryDef>.GetNamed("StorageTab", false));
            }

            if (TabSortingMod.instance.Settings.SortStorage)
            {
                var storageInGame = (from storage in DefDatabase<ThingDef>.AllDefsListForReading
                    where
                        !defsToIgnore.Contains(storage.defName) &&
                        !changedDefNames.Contains(storage.defName) &&
                        storage.designationCategory != null &&
                        storage.designationCategory.defName != "FurnitureStorage" &&
                        storage.thingClass.Name != "Building_Grave" &&
                        (storage.thingClass.Name == "Building_Storage" || storage.inspectorTabs != null &&
                            storage.inspectorTabs.Contains(typeof(ITab_Storage))) &&
                        (storage.placeWorkers == null ||
                         !storage.placeWorkers.Contains(typeof(PlaceWorker_NextToHopperAccepter)))
                    select storage).ToList();
                changedCategories.Add(storageDesignationCategory);
                foreach (var storage in storageInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for storage " + storage.defName + " from " + storage.designationCategory + " to " + storageDesignationCategory.defName);
#endif
                    changedDefNames.Add(storage.defName);
                    changedCategories.Add(storage.designationCategory);
                    storage.designationCategory = storageDesignationCategory;
                }

                Log.Message("TabSorting: Moved " + storageInGame.Count + " storage-items to the Storage tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(storageDesignationCategory);
            }
        }

        /// <summary>
        ///     Sorts all garden-items to the Garden-tab if VGP Garden tools is loaded
        /// </summary>
        private static void SortGarden()
        {
            var gardenDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("GardenTools", false);
            if (gardenDesignationCategory == null || !TabSortingMod.instance.Settings.SortGarden)
            {
                return;
            }

            var gardenThingsInGame = (from gardenThing in DefDatabase<ThingDef>.AllDefsListForReading
                where
                    !defsToIgnore.Contains(gardenThing.defName) &&
                    !changedDefNames.Contains(gardenThing.defName) &&
                    gardenThing.designationCategory != null &&
                    gardenThing.designationCategory.defName != "GardenTools" &&
                    (gardenThing.thingClass.Name == "Building_SunLamp" ||
                     gardenThing.thingClass.Name == "Building_PlantGrower" && (gardenThing.building == null ||
                                                                               gardenThing.building.sowTag !=
                                                                               "Decorative") ||
                     gardenThing.label.ToLower().Contains("sprinkler") && !gardenThing.label.ToLower().Contains("fire"))
                select gardenThing).ToList();
            changedCategories.Add(gardenDesignationCategory);
            foreach (var gardenTool in gardenThingsInGame)
            {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for gardenTool " + gardenTool.defName + " from " + gardenTool.designationCategory + " to " + gardenDesignationCategory.defName);
#endif
                changedDefNames.Add(gardenTool.defName);
                changedCategories.Add(gardenTool.designationCategory);
                gardenTool.designationCategory = gardenDesignationCategory;
            }

            Log.Message("TabSorting: Moved " + gardenThingsInGame.Count + " garden-items to the Garden tab.");
        }

        /// <summary>
        ///     Sorts all fences-items to the Fences-tab if Fences and Floors is loaded
        /// </summary>
        private static void SortFences()
        {
            var fencesDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Fences", false);
            if (fencesDesignationCategory == null || !TabSortingMod.instance.Settings.SortFences)
            {
                return;
            }

            var fencesInGame = (from fence in DefDatabase<ThingDef>.AllDefsListForReading
                where
                    !defsToIgnore.Contains(fence.defName) &&
                    !changedDefNames.Contains(fence.defName) &&
                    fence.designationCategory != null &&
                    fence.designationCategory.defName != "Fences" &&
                    ((fence.thingClass.Name == "Building_Door" ||
                      fence.thingClass.Name == "Building" && fence.graphicData != null &&
                      fence.graphicData.linkType == LinkDrawerType.Basic &&
                      fence.passability == Traversability.Impassable) &&
                     fence.fillPercent < 1f &&
                     fence.fillPercent > 0 ||
                     fence.label.ToLower().Contains("fence"))
                select fence).ToList();
            changedCategories.Add(fencesDesignationCategory);
            foreach (var fence in fencesInGame)
            {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for fence " + fence.defName + " from " + fence.designationCategory + " to " + fencesDesignationCategory.defName + " passability: " + fence.passability);
#endif
                changedDefNames.Add(fence.defName);
                changedCategories.Add(fence.designationCategory);
                fence.designationCategory = fencesDesignationCategory;
            }

            Log.Message("TabSorting: Moved " + fencesInGame.Count + " fences to the Fences-tab.");
        }
    }
}