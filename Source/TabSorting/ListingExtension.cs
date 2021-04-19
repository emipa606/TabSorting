using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TabSorting
{
    [StaticConstructorOnStartup]
    public static class ListingExtension
    {
        public static bool ListItemSelectable(this Listing lister, string header, Color hoverColor, bool selected = false)
        {
            var anchor = Text.Anchor;
            var color = GUI.color;
            var rect = lister.GetRect(20f);

            if (selected)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            }

            GUI.color = Color.white;
            if (Mouse.IsOver(rect))
            {
                GUI.color = hoverColor;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            if (header != null)
            {
                Widgets.Label(rect, header);
            }

            Text.Anchor = anchor;
            GUI.color = color;

            if (!Widgets.ButtonInvisible(rect))
            {
                return false;
            }

            SoundDefOf.Click.PlayOneShotOnCamera();
            return true;
        }
    }
}