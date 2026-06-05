using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TabSorting;

public class IgnoreSettingDef : Def
{
    public List<string> defsToIgnore;
    public List<string> modIDsToIgnore;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        if (modIDsToIgnore?.Any() == true)
        {
            modIDsToIgnore = modIDsToIgnore.Select(id => id.ToLower()).ToList();
        }
    }
}