using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TabSorting;

/// <summary>
///     Vanilla only counts facilities that are in the Production-category when determining the kitchen score.
///     This code removes that check. Created by user slippycheeze.
///     https://gist.github.com/slippycheeze/8ee7bdc7e035a3ea4c7ecb2fcb1c4406
/// </summary>
[HarmonyPatch(typeof(RoomRoleWorker_Kitchen), "GetScore")]
public static class RoomRoleWorker_Kitchen_GetScore
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code, ILGenerator generator,
        MethodBase original)
    {
        try
        {
            Log.Message("[TabSorting]: Fixing kitchen room role worker, thanks slippycheeze!");

            var toBeRemoved = new[]
            {
                // IL_0018: ldloc.3      // thing
                new CodeMatch(i => i.IsLdloc()),
                // IL_0019: ldfld        class Verse.ThingDef Verse.Thing::def
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(Thing), "def"))),
                // IL_001e: ldfld        class Verse.DesignationCategoryDef Verse.BuildableDef::designationCategory
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(BuildableDef), "designationCategory"))),
                // IL_0023: ldsfld       class Verse.DesignationCategoryDef RimWorld.DesignationCategoryDefOf::Production
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(DesignationCategoryDefOf), "Production"))),
                // IL_0028: bne.un       IL_00ad
                new CodeMatch(i => i.Branches(out _))
            };

            return new CodeMatcher(code, generator)
                .Start()
                .MatchStartForward(toBeRemoved)
                .ThrowIfInvalid("finding the designation category check")
                .RemoveInstructions(toBeRemoved.Length)
                .InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"[TabSorting]: Failed to patch {original.Name}: {ex}");
            return code;
        }
    }
}