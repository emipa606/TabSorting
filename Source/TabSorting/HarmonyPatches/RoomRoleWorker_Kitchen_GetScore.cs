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
[HarmonyPatch(typeof(RoomRoleWorker_Kitchen), nameof(RoomRoleWorker_Kitchen.GetScore))]
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
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(Thing), "def"))),
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(BuildableDef), "designationCategory"))),
                new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(DesignationCategoryDefOf), "Production")))
            };

            var toBeAdded = new[]
            {
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "category")),
                new CodeInstruction(OpCodes.Ldc_I4_3)
            };

            return new CodeMatcher(code, generator)
                .Start()
                .MatchStartForward(toBeRemoved)
                .ThrowIfInvalid("finding the designation category check")
                .RemoveInstructions(toBeRemoved.Length)
                .Insert(toBeAdded)
                .InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"[TabSorting]: Failed to patch {original.Name}: {ex}");
            return code;
        }
    }
}