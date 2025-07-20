using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TabSorting;

public class Dialog_ChooseTabIcon : Window
{
    private const float ItemsPerRow = 10f;

    private static Vector2 scrollPosition;

    private static string currentTabDef;

    public Dialog_ChooseTabIcon(string tabDef)
    {
        scrollPosition = Vector2.zero;
        currentTabDef = tabDef;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        TabSortingMod.Instance.Settings.ManualTabIcons ??= new Dictionary<string, string>();

        GUI.contentColor = Color.green;
        Widgets.Label(new Rect(inRect), "TabSorting.ChooseTabIcon".Translate());
        GUI.contentColor = Color.white;
        Widgets.DrawLineHorizontal(inRect.x, inRect.y + 25f, inRect.width);
        if (TabSortingMod.Instance.Settings.ManualTabIcons.ContainsKey(currentTabDef) && Widgets.ButtonText(
                new Rect(inRect.position + new Vector2(inRect.width - TabSortingMod.ButtonSize.x, 0),
                    TabSortingMod.ButtonSize),
                "TabSorting.Reset".Translate()))
        {
            TabSortingMod.Instance.Settings.ManualTabIcons.Remove(currentTabDef);
            Find.WindowStack.TryRemove(this, false);
            return;
        }

        var viewRect = inRect;
        viewRect.y += 40f;
        var contentRect = viewRect;
        contentRect.width -= 20;
        contentRect.x = 0;
        contentRect.y = 0f;
        contentRect.height = TabSorting.IconsCache.Count / ItemsPerRow * 50f;
        Widgets.BeginScrollView(viewRect, ref scrollPosition, contentRect);

        var spacer = contentRect.width / (ItemsPerRow + 1);
        var i = 0;
        var y = -50f;
        var x = 0f;
        foreach (var textureName in TabSorting.IconsCache.Keys.OrderBy(s => s))
        {
            if (i % (ItemsPerRow + 1) == 0)
            {
                y += 50f;
                x = 0;
            }
            else
            {
                x += spacer;
            }

            if (ListingExtension.TabIconSelectable(new Rect(new Vector2(x, y), TabSortingMod.TabIconSize * 2),
                    textureName, null, textureName == TabSorting.GetCustomTabIcon(currentTabDef)))
            {
                if (currentTabDef == textureName)
                {
                    TabSortingMod.Instance.Settings.ManualTabIcons.Remove(textureName);
                }
                else
                {
                    TabSortingMod.Instance.Settings.ManualTabIcons[currentTabDef] = textureName;
                }

                Widgets.EndScrollView();
                TabSortingMod.SelectedDef = currentTabDef;
                Find.WindowStack.TryRemove(this, false);
                return;
            }

            i++;
        }

        Widgets.EndScrollView();
    }
}