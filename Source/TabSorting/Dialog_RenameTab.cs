using RimWorld;
using UnityEngine;
using Verse;

namespace TabSorting;

public class Dialog_RenameTab : Window
{
    private readonly DesignationCategoryDef tab;

    protected string curName;

    private bool focusedRenameField;
    private int startAcceptingInputAtFrame;

    public Dialog_RenameTab(DesignationCategoryDef designationTab)
    {
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnClickedOutside = true;
        tab = designationTab;
        curName = designationTab.label;
    }

    private bool AcceptsInput => startAcceptingInputAtFrame <= Time.frameCount;

    protected int MaxNameLength => 28;

    public override Vector2 InitialSize => new Vector2(280f, 175f);

    public void WasOpenedByHotkey()
    {
        startAcceptingInputAtFrame = Time.frameCount + 1;
    }

    protected AcceptanceReport NameIsValid(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        if (name != tab.label && string.IsNullOrEmpty(TabSorting.ValidateTabName(name, true)))
        {
            return new AcceptanceReport("TabSorting.Exists".Translate(name));
        }

        return true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        var returnPressed = false;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            returnPressed = true;
            Event.current.Use();
        }

        GUI.SetNextControlName("RenameField");
        var text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), curName);
        if (AcceptsInput && text.Length < MaxNameLength)
        {
            curName = text;
        }
        else if (!AcceptsInput)
        {
            ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
        }

        if (!focusedRenameField)
        {
            UI.FocusControl("RenameField", this);
            focusedRenameField = true;
        }

        if (!Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f, inRect.width - 15f - 15f, 35f), "OK") &&
            !returnPressed)
        {
            return;
        }

        var acceptanceReport = NameIsValid(curName);
        if (!acceptanceReport.Accepted)
        {
            if (acceptanceReport.Reason.NullOrEmpty())
            {
                Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
            return;
        }

        SetName(curName);
        Find.WindowStack.TryRemove(this);
    }

    protected void SetName(string name)
    {
        TabSortingMod.instance.Settings.ManualCategoryMemory.RemoveAll(def => def.defName == tab.defName);
        tab.label = name;
        TabSortingMod.instance.Settings.ManualTabs[tab.defName] = name;
        TabSortingMod.instance.Settings.ManualCategoryMemory.Add(tab);

        tab.ClearCachedData();
    }
}