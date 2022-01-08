using Verse;

namespace TabSorting;

public class Dialog_RenameTab : Dialog_Rename
{
    private readonly DesignationCategoryDef tab;

    public Dialog_RenameTab(DesignationCategoryDef designationTab)
    {
        tab = designationTab;
        curName = designationTab.label;
    }

    protected override AcceptanceReport NameIsValid(string name)
    {
        var result = base.NameIsValid(name);
        if (!result.Accepted)
        {
            return result;
        }

        if (name != tab.label && string.IsNullOrEmpty(TabSorting.ValidateTabName(name, true)))
        {
            return new AcceptanceReport("TabSorting.Exists".Translate(name));
        }

        return true;
    }

    protected override void SetName(string name)
    {
        TabSortingMod.instance.Settings.ManualCategoryMemory.RemoveAll(def => def.defName == tab.defName);
        tab.label = name;
        TabSortingMod.instance.Settings.ManualTabs[tab.defName] = name;
        TabSortingMod.instance.Settings.ManualCategoryMemory.Add(tab);

        tab.ClearCachedData();
    }
}