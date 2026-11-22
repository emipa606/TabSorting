using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TabSorting;

internal static class OrderSorting
{
    [ThreadStatic] private static bool collecting;

    private static IEnumerable<DesignationCategoryDef> Categories =>
        DefDatabase<DesignationCategoryDef>.AllDefsListForReading
            .Concat(TabSortingMod.Instance.Settings.VanillaCategoryMemory)
            .Concat(TabSortingMod.Instance.Settings.ManualCategoryMemory).Distinct();

    private static bool IsOrder(Designator designator)
    {
        return designator is not Designator_Build &&
               (designator is not Designator_Dropdown dropdown || !dropdown.Elements.Any(element => !IsOrder(element)));
    }

    private static string Signature(Designator designator)
    {
        var signature = designator.GetType().AssemblyQualifiedName;
        return designator is Designator_Dropdown dropdown
            ? signature + "|" + string.Join("|", dropdown.Elements.Select(Signature))
            : signature;
    }

    private static List<Entry> Entries(DesignationCategoryDef source, List<Designator> available,
        List<Designator> unfiltered = null)
    {
        var occurrences = new Dictionary<Type, int>();
        var signatureOccurrences = new Dictionary<string, int>();
        var entries = new List<Entry>();
        // Use the unfiltered registration order, not translated labels or game-rule visibility, for saved identities.
        foreach (var designator in source.AllResolvedDesignators.Concat(unfiltered ?? []).Concat(available)
                     .Distinct().Where(IsOrder))
        {
            var type = designator.GetType();
            var occurrence = occurrences.GetValueOrDefault(type);
            occurrences[type] = occurrence + 1;
            var signature = Signature(designator);
            var signatureIndex = signatureOccurrences.GetValueOrDefault(signature);
            signatureOccurrences[signature] = signatureIndex + 1;
            if (available.Contains(designator))
            {
                entries.Add(new Entry($"{source.defName}|{type.Assembly.GetName().Name}:{type.FullName}|{occurrence}",
                    source, designator, signature, signatureIndex));
            }
        }

        return entries;
    }

    private static List<Designator> Unfiltered(DesignationCategoryDef source)
    {
        return Current.Game == null
            ? source.AllResolvedDesignators.ToList()
            : source.AllResolvedAndIdeoDesignators.Concat(source.ResolvedAllowedDesignators).Distinct().ToList();
    }

    private static HashSet<(string Signature, int Index)> SharedOrders(IEnumerable<DesignationCategoryDef> categories)
    {
        var source = categories.FirstOrDefault(category => category.defName == "Orders");
        if (source == null)
        {
            return [];
        }

        return Entries(source, Unfiltered(source)).Select(entry => (entry.Signature, entry.SignatureIndex))
            .ToHashSet();
    }

    private static bool IsSharedCopy(Entry entry, HashSet<(string Signature, int Index)> sharedOrders)
    {
        return entry.Source.defName != "Orders" && sharedOrders.Contains((entry.Signature, entry.SignatureIndex));
    }

    private static string Destination(Entry entry)
    {
        if (!TabSortingMod.Instance.Settings.ManualOrderSorting.TryGetValue(entry.Key, out var destination))
        {
            return entry.Source.defName;
        }

        if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(destination) != null)
        {
            return destination;
        }

        Log.ErrorOnce(
            $"[TabSorting]: Order destination {destination} is unavailable; leaving {entry.Key} in its original tab.",
            entry.Key.GetHashCode());
        return entry.Source.defName;
    }

    internal static Action PreserveRuntimeOrders()
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            return () => { };
        }

        var snapshots = Categories.ToDictionary(source => source,
            source => Entries(source, source.AllResolvedDesignators.ToList()));
        return () =>
        {
            foreach (var snapshot in snapshots)
            {
                var resolved = snapshot.Key.AllResolvedDesignators;
                var previous = snapshot.Value.ToDictionary(entry => entry.Key, entry => entry.Designator);
                var rebuilt = Entries(snapshot.Key, resolved.ToList());
                var orders = rebuilt.Select(entry => previous.GetValueOrDefault(entry.Key, entry.Designator)).ToList();
                var rebuiltKeys = rebuilt.Select(entry => entry.Key).ToHashSet();
                orders.AddRange(snapshot.Value.Where(entry => !rebuiltKeys.Contains(entry.Key))
                    .Select(entry => entry.Designator));
                resolved.RemoveAll(IsOrder);
                resolved.InsertRange(0, orders);
            }
        };
    }

    internal static List<Entry> GetOrders(DesignationCategoryDef category)
    {
        var entries = new List<Entry>();
        collecting = true;
        try
        {
            var categories = Categories.ToList();
            var sharedOrders = SharedOrders(categories);
            foreach (var source in categories)
            {
                var available = Unfiltered(source);
                entries.AddRange(Entries(source, available).Where(entry =>
                    !IsSharedCopy(entry, sharedOrders) && Destination(entry) == category.defName));
            }
        }
        finally
        {
            collecting = false;
        }

        return entries;
    }

    internal static IEnumerable<Designator> Route(DesignationCategoryDef category,
        IEnumerable<Designator> original, bool allowedOnly)
    {
        var sorting = TabSortingMod.Instance.Settings.ManualOrderSorting;
        if (collecting || !sorting.Any(pair => pair.Value == category.defName ||
                                               pair.Key.StartsWith(category.defName + "|", StringComparison.Ordinal)))
        {
            return original;
        }

        collecting = true;
        try
        {
            var categories = Categories.ToList();
            var sharedOrders = SharedOrders(categories);
            var current = original.ToList();
            var currentEntries = Entries(category, current, Unfiltered(category));
            var outgoing = currentEntries.Where(entry => !IsSharedCopy(entry, sharedOrders) &&
                                                         Destination(entry) != category.defName)
                .Select(entry => entry.Designator).ToHashSet();
            var result = current.Where(designator => !outgoing.Contains(designator)).ToList();
            foreach (var source in categories.Where(source => source != category))
            {
                if (!sorting.Any(pair => pair.Value == category.defName &&
                                         pair.Key.StartsWith(source.defName + "|", StringComparison.Ordinal)))
                {
                    continue;
                }

                var available = (allowedOnly ? source.ResolvedAllowedDesignators : source.AllResolvedAndIdeoDesignators)
                    .ToList();
                foreach (var entry in Entries(source, available, Unfiltered(source)).Where(entry =>
                             !IsSharedCopy(entry, sharedOrders) && Destination(entry) == category.defName &&
                             !result.Contains(entry.Designator)))
                {
                    if (entry.Source.defName == "Orders")
                    {
                        foreach (var copy in currentEntries.Where(copy => IsSharedCopy(copy, sharedOrders) &&
                                     copy.Signature == entry.Signature && copy.SignatureIndex == entry.SignatureIndex))
                        {
                            result.Remove(copy.Designator);
                        }
                    }

                    result.Add(entry.Designator);
                }
            }

            return result;
        }
        finally
        {
            collecting = false;
        }
    }

    internal sealed record Entry(string Key, DesignationCategoryDef Source, Designator Designator,
        string Signature, int SignatureIndex);
}

[HarmonyPatch(typeof(DesignationCategoryDef), nameof(DesignationCategoryDef.ResolvedAllowedDesignators),
    MethodType.Getter)]
internal static class DesignationCategoryDef_ResolvedAllowedDesignators
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        __result = OrderSorting.Route(__instance, __result, true);
    }
}

[HarmonyPatch(typeof(DesignationCategoryDef), nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators),
    MethodType.Getter)]
internal static class DesignationCategoryDef_AllResolvedAndIdeoDesignators
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        __result = OrderSorting.Route(__instance, __result, false);
    }
}