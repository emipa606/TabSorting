using System.Reflection;
using HarmonyLib;
using Verse;

namespace TabSorting;

[StaticConstructorOnStartup]
public class ArchitectIcons_Patch
{
    static ArchitectIcons_Patch()
    {
        var harmony = new Harmony("Mlie.TabSorting.ArchitectIcons_Patch");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}