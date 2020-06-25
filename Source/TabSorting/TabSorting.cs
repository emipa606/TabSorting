//#define DEBUGGING

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TabSorting
{
    [StaticConstructorOnStartup]
    public static class TabSorting
    {
        public static List<string> defsToIgnore = new List<string>();

        /// <summary>
        /// Goes through all items and checks if there are any references to the selected category
        /// Removes the category if there are none
        /// </summary>
        /// <param name="currentCategory">The category to check</param>
        static void CheckEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
        {
#if DEBUGGING
            Log.Message("TabSorting: Checking current defs in " + currentCategory.defName);
#endif
            var thingsLeft = from td in DefDatabase<ThingDef>.AllDefsListForReading where td.designationCategory == currentCategory select td;
            if (thingsLeft.Count() != 0)
            {
                return;
            }
#if DEBUGGING
            Log.Message("TabSorting: Checking current terrainDefs in " + currentCategory.defName);
#endif
            var moreThingsLeft = from tr in DefDatabase<TerrainDef>.AllDefsListForReading where tr.designationCategory == currentCategory select tr;
            if (moreThingsLeft.Count() != 0)
            {
                return;
            }
            Log.Message("TabSorting: Removing " + currentCategory.defName + " since its empty now.");
            RemoveEmptyDesignationCategoryDef(currentCategory);
        }

        /// <summary>
        /// Removes a (hopefully) empty category
        /// </summary>
        /// <param name="currentCategory"></param>
        static void RemoveEmptyDesignationCategoryDef(DesignationCategoryDef currentCategory)
        {
            GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), typeof(DesignationCategoryDef), "Remove", new object[]
            {
                currentCategory
            });
        }

        /// <summary>
        /// Sorts all lights to the Lights-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortLights(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            var lightsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("LightsTab");
            if (TabSortingMod.instance.Settings.SortLights)
            {
                var lightsInGame = (from td in DefDatabase<ThingDef>.AllDefsListForReading
                                    where
                                    !defsToIgnore.Contains(td.defName) &&
                                    (td.designationCategory != null &&
                                    ((td.category == ThingCategory.Building &&
                                    (td.GetCompProperties<CompProperties_Power>() == null || td.GetCompProperties<CompProperties_Power>().compClass != typeof(CompPowerPlant)) &&
                                    (td.placeWorkers == null || !td.placeWorkers.Contains(typeof(PlaceWorker_ShowFacilitiesConnections))) &&
                                    td.GetCompProperties<CompProperties_ShipLandingBeacon>() == null &&
                                    td.GetCompProperties<CompProperties_Glower>() != null &&
                                    td.GetCompProperties<CompProperties_TempControl>() == null &&
                                    td.surfaceType != SurfaceType.Eat &&
                                    td.thingClass.Name != "Building_TurretGun" &&
                                    td.thingClass.Name != "Building_SunLamp" &&
                                    td.thingClass.Name != "Building_Heater" &&
                                    td.thingClass.Name != "Building_PlantGrower" &&
                                    (td.inspectorTabs == null || !td.inspectorTabs.Contains(typeof(ITab_Storage))) &&
                                    !td.hasInteractionCell) ||
                                    (td.label != null && (td.label.ToLower().Contains("wall lamp") || td.label.ToLower().Contains("wall light")))))
                                    select td).ToList();
                changedCategories.Add(lightsDesignationCategory);
                foreach (ThingDef furniture in lightsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + furniture.defName + " from " + furniture.designationCategory + " to " + lightsDesignationCategory.defName);
#endif
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
        /// Sorts all floors to the Floors-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortFloors(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            if (TabSortingMod.instance.Settings.SortFloors)
            {
                var floorsInGame = (from td in DefDatabase<TerrainDef>.AllDefsListForReading
                                    where
                                    !defsToIgnore.Contains(td.defName) &&
                                    (td.designationCategory != null &&
                                    td.designationCategory.defName != "Floors" &&
                                    td.fertility == 0 &&
                                    !td.destroyBuildingsOnDestroyed)
                                    select td).ToList();
                var floorsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Floors");
                changedCategories.Add(floorsDesignationCategory);
                foreach (TerrainDef floor in floorsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + floor.defName + " from " + floor.designationCategory + " to " + floorsDesignationCategory.defName);
#endif
                    changedCategories.Add(floor.designationCategory);
                    floor.designationCategory = floorsDesignationCategory;
                }
                Log.Message("TabSorting: Moved " + floorsInGame.Count + " floors to the Floors tab.");

            }
        }

        /// <summary>
        /// Sorts all tables and sitting-furniture to the Table/Chairs-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortTablesAndChairs(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            var tableChairsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("TableChairsTab");
            if (TabSortingMod.instance.Settings.SortTablesAndChairs)
            {
                var tableChairsInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                         where
                                          !defsToIgnore.Contains(dd.defName) &&
                                         (dd.designationCategory != null &&
                                         ((dd.IsTable || (dd.surfaceType == SurfaceType.Eat && dd.label.ToLower().Contains("table"))) ||
                                         (dd.building != null && dd.building.isSittable)))
                                         select dd).ToList();
                changedCategories.Add(tableChairsDesignationCategory);
                foreach (ThingDef tableOrChair in tableChairsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + tableOrChair.defName + " from " + tableOrChair.designationCategory + " to " + tableChairsDesignationCategory.defName);
#endif
                    changedCategories.Add(tableOrChair.designationCategory);
                    tableOrChair.designationCategory = tableChairsDesignationCategory;
                }
                Log.Message("TabSorting: Moved " + tableChairsInGame.Count + " tables and chairs to the Table/Chairs tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(tableChairsDesignationCategory);
            }
        }

        /// <summary>
        /// Sorts all walls and doors to the Structure-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortDoorsAndWalls(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            if (TabSortingMod.instance.Settings.SortDoorsAndWalls)
            {
                var doorsAndWallsInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                           where
                                        !defsToIgnore.Contains(dd.defName) &&
                                           (dd.designationCategory != null &&
                                           dd.designationCategory.defName != "Structure" &&
                                           dd.fillPercent == 1f &&
                                           (dd.holdsRoof ||
                                           dd.IsDoor))
                                           select dd).ToList();
                var structureDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Structure");
                changedCategories.Add(structureDesignationCategory);
                foreach (ThingDef doorOrWall in doorsAndWallsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + doorOrWall.defName + " from " + doorOrWall.designationCategory + " to " + structureDesignationCategory.defName);
#endif
                    changedCategories.Add(doorOrWall.designationCategory);
                    doorOrWall.designationCategory = structureDesignationCategory;
                }
                Log.Message("TabSorting: Moved " + doorsAndWallsInGame.Count + " doors and walls to the Structure tab.");

            }
        }

        /// <summary>
        /// Sort bedroom furniture to the Bedroom-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortBedroomFurniture(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            var bedroomFurnitureDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("BedroomTab");
            if (TabSortingMod.instance.Settings.SortBedroomFurniture)
            {
                var bedsInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                  where
                                  !defsToIgnore.Contains(dd.defName) &&
                                  (dd.designationCategory != null &&
                                  dd.IsBed &&
                                  (dd.building == null || !dd.building.bed_defaultMedical))
                                  select dd).ToList();
                changedCategories.Add(bedroomFurnitureDesignationCategory);
                HashSet<ThingDef> affectedByFacilities = new HashSet<ThingDef>();
                foreach (ThingDef bed in bedsInGame)
                {
                    if (bed.comps.Count == 0)
                        continue;
                    var affections = bed.GetCompProperties<CompProperties_AffectedByFacilities>();
                    if (affections == null || affections.linkableFacilities == null)
                        continue;
                    foreach (ThingDef facility in affections.linkableFacilities)
                    {
                        if (facility.designationCategory == null)
                            continue;
                        if ((from offset in facility.GetCompProperties<CompProperties_Facility>().statOffsets where offset.stat == StatDefOf.SurgerySuccessChanceFactor select offset).ToList().Count > 0)
                            continue;
                        affectedByFacilities.Add(facility);
                    }
                }
                foreach (ThingDef bed in bedsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + bed.defName + " from " + bed.designationCategory + " to " + bedroomFurnitureDesignationCategory.defName);
#endif
                    changedCategories.Add(bed.designationCategory);
                    bed.designationCategory = bedroomFurnitureDesignationCategory;

                }
                foreach (ThingDef facility in affectedByFacilities)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + facility.defName + " from " + facility.designationCategory + " to " + bedroomFurnitureDesignationCategory.defName);
#endif
                    changedCategories.Add(facility.designationCategory);
                    facility.designationCategory = bedroomFurnitureDesignationCategory;

                }
                Log.Message("TabSorting: Moved " + (affectedByFacilities.Count + bedsInGame.Count) + " bedroom furniture to the Bedroom tab.");
            }
            else
            {
                RemoveEmptyDesignationCategoryDef(bedroomFurnitureDesignationCategory);
            }
        }

        /// <summary>
        /// Sort decorative items to the Decorations-tab
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortDecorations(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            var decorationsDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("DecorationTab");
            if (TabSortingMod.instance.Settings.SortDecorations)
            {
                var rugsInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                  where
                                   !defsToIgnore.Contains(dd.defName) &&
                                  (dd.designationCategory != null &&
                                  dd.altitudeLayer == AltitudeLayer.FloorEmplacement &&
                                  !dd.clearBuildingArea &&
                                  dd.passability == Traversability.Standable &&
                                  dd.StatBaseDefined(StatDefOf.Beauty))
                                  select dd).ToList();
                var decorativePlantsInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                              where
                                               !defsToIgnore.Contains(dd.defName) &&
                                              (dd.designationCategory != null &&
                                              dd.building != null && dd.building.sowTag == "Decorative")
                                              select dd).ToList();
                var decorativeFurnitureInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                                 where
                                                  !defsToIgnore.Contains(dd.defName) &&
                                                 (dd.designationCategory != null &&
                                                 dd.altitudeLayer == AltitudeLayer.BuildingOnTop &&
                                                 dd.StatBaseDefined(StatDefOf.Beauty) &&
                                                 dd.GetCompProperties<CompProperties_Glower>() == null &&
                                                 !dd.neverMultiSelect &&
                                                 (dd.PlaceWorkers == null || !dd.placeWorkers.Contains(typeof(PlaceWorker_ShowFacilitiesConnections))) &&
                                                 !dd.IsBed && !dd.IsTable)
                                                 select dd).ToList();

                changedCategories.Add(decorationsDesignationCategory);
                foreach (ThingDef rug in rugsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + rug.defName + " from " + rug.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedCategories.Add(rug.designationCategory);
                    rug.designationCategory = decorationsDesignationCategory;
                }
                foreach (ThingDef planter in decorativePlantsInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + planter.defName + " from " + planter.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedCategories.Add(planter.designationCategory);
                    planter.designationCategory = decorationsDesignationCategory;
                }
                foreach (ThingDef furniture in decorativeFurnitureInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + furniture.defName + " from " + furniture.designationCategory + " to " + decorationsDesignationCategory.defName);
#endif
                    changedCategories.Add(furniture.designationCategory);
                    furniture.designationCategory = decorationsDesignationCategory;
                }
                Log.Message("TabSorting: Moved " + (rugsInGame.Count + decorativePlantsInGame.Count + decorativeFurnitureInGame.Count) + " decorative items to the Decorations tab.");

            }
            else
            {
                RemoveEmptyDesignationCategoryDef(decorationsDesignationCategory);
            }
        }

        /// <summary>
        /// Sorts all storage-items to the Storage-tab if Storage-extended is loaded
        /// </summary>
        /// <param name="changedCategories">A variable to save each category that has been changed</param>
        static void SortStorage(ref HashSet<DesignationCategoryDef> changedCategories)
        {
            var storageDesignationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("FurnitureStorage", false);
            if (storageDesignationCategory != null && TabSortingMod.instance.Settings.SortStorage)
            {
                var storageInGame = (from dd in DefDatabase<ThingDef>.AllDefsListForReading
                                     where
                                     !defsToIgnore.Contains(dd.defName) &&
                                     (dd.designationCategory != null &&
                                     dd.designationCategory.defName != "FurnitureStorage" &&
                                     dd.thingClass.Name != "Building_Grave" &&
                                     (dd.thingClass.Name == "Building_Storage" || (dd.inspectorTabs != null && dd.inspectorTabs.Contains(typeof(ITab_Storage)))) &&
                                     (dd.placeWorkers == null || !dd.placeWorkers.Contains(typeof(PlaceWorker_NextToHopperAccepter))))
                                     select dd).ToList();
                changedCategories.Add(storageDesignationCategory);
                foreach (ThingDef storage in storageInGame)
                {
#if DEBUGGING
                    Log.Message("TabSorting: Changing designation for " + storage.defName + " from " + storage.designationCategory + " to " + storageDesignationCategory.defName);
#endif
                    changedCategories.Add(storage.designationCategory);
                    storage.designationCategory = storageDesignationCategory;
                }
                Log.Message("TabSorting: Moved " + storageInGame.Count + " storage-items to the Storage tab.");
            }
        }

        /// <summary>
        /// The main sorting function
        /// </summary>
        static TabSorting()
        {
            var ignoreMods = (from mod in LoadedModManager.RunningModsListForReading
                              where
                                    mod.PackageId == "atlas.androidtiers" ||
                                    mod.PackageId == "dubwise.dubsbadhygiene" ||
                                    mod.PackageId == "vanillaexpanded.vfepower" ||
                                    mod.PackageId == "vanillaexpanded.vfepropsanddecor"
                              select mod).ToList();
            if (ignoreMods.Count > 0)
            {
                foreach (var mod in ignoreMods)
                {
#if DEBUGGING
                Log.Message("TabSorting: " + mod.Name + " has " + mod.AllDefs.Count() + " definitions, adding to ignore.");
#endif
                    foreach (Def def in mod.AllDefs)
                    {
                        defsToIgnore.Add(def.defName);
                    }

                }
            }

            if (TabSortingMod.instance.Settings == null)
            {
                TabSortingMod.instance.Settings.SortLights = true;
                TabSortingMod.instance.Settings.SortFloors = false;
                TabSortingMod.instance.Settings.SortDoorsAndWalls = false;
                TabSortingMod.instance.Settings.SortBedroomFurniture = false;
                TabSortingMod.instance.Settings.SortTablesAndChairs = false;
                TabSortingMod.instance.Settings.SortDecorations = false;
                TabSortingMod.instance.Settings.SortStorage = false;

                TabSortingMod.instance.Settings.RemoveEmptyTabs = true;
            }

            var changedCategories = new HashSet<DesignationCategoryDef>();

            SortLights(ref changedCategories);

            SortFloors(ref changedCategories);

            SortDoorsAndWalls(ref changedCategories);

            SortTablesAndChairs(ref changedCategories);

            SortBedroomFurniture(ref changedCategories);

            SortDecorations(ref changedCategories);

            SortStorage(ref changedCategories);

            var designationsNotToTestForRemoval = new List<string>()
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
                foreach (DesignationCategoryDef designationCategoryDef in changedCategories)
                {

                    designationCategoryDef.ResolveReferences();
                    if (designationsNotToTestForRemoval.Contains(designationCategoryDef.defName))
                        continue;

                    if (TabSortingMod.instance.Settings.RemoveEmptyTabs)
                        CheckEmptyDesignationCategoryDef(designationCategoryDef);
                }
            }
        }
    }
}
