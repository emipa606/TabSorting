using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TabSorting;

public class Dialog_RenameTab : Window
{
    private readonly DesignationCategoryDef tab;

    private string curName;

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

    private static int MaxNameLength => 28;

    public override Vector2 InitialSize => new(280f, 175f);

    public void WasOpenedByHotkey()
    {
        startAcceptingInputAtFrame = Time.frameCount + 1;
    }

    private AcceptanceReport nameIsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (DefDatabase<DesignationCategoryDef>.AllDefsListForReading
            .Concat(TabSortingMod.Instance.Settings.VanillaCategoryMemory)
            .Concat(TabSortingMod.Instance.Settings.ManualCategoryMemory)
            .Any(def => def != tab && string.Equals(def.label, name, StringComparison.OrdinalIgnoreCase)))
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
        switch (AcceptsInput)
        {
            case true when text.Length <= MaxNameLength:
                curName = text;
                break;
            case false:
                ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
                break;
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

        curName = curName.Trim();
        var acceptanceReport = nameIsValid(curName);
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

        setName(curName);
        Find.WindowStack.TryRemove(this);
    }

    private void setName(string name)
    {
        var settings = TabSortingMod.Instance.Settings;
        if (settings.ManualTabs.ContainsKey(tab.defName))
        {
            settings.ManualTabs[tab.defName] = name;
        }
        else
        {
            settings.VanillaTabLabelMemory.TryAdd(tab.defName, tab.label);
            settings.RenamedTabs[tab.defName] = name;
        }

        tab.label = name;
        tab.ClearCachedData();
        settings.Write();
        TabSorting.RefreshArchitectMenu();
    }
}