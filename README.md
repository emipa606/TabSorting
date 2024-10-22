# [Tab-sorting](https://steamcommunity.com/sharedfiles/filedetails/?id=2138635288)

![Image](https://i.imgur.com/iCj5o7O.png)

**NOTICE**

Not sure how much more time I will put on this mod seeing the scope of the [Vanilla UI Expanded]( https://steamcommunity.com/sharedfiles/filedetails/?id=2749945924)
I may add compatibility for the autosort but the general Ui of that mod is more refined and worked on by more than one person (that do not have a few houndred mods to support). 

—

Sort your stuff!
  
This mod can sort furniture/structures into appropriate tabs.
It started as an improved version of the LigtsTab (by betaALPHAs) but since that version relied on patches it was very complex and hard to maintain.

Instead this mod uses C#-code to sort things after they have loaded, removing the need for specific patch-files for each mod/item. The downside is that it requires a rescan of the changed tabs at startup but this should add no more than at most a few seconds depending on how many tabs has been changed.

Currently the mod supports the following sorting:

- All lights to a separate Lights-tab
- All walls and doors to the Structure-tab
- All floors to the Floors-tab
- All beds and linkable furniture to a separate Bedroom-tab
- All hospitalbeds and linkable medical furniture to a separate Hospital-tab
- All tables and sittable furniture to a separate Tables/Chairs-tab
- All decorative items to a separate Decorations-tab
- All kitchen furniture and linkables to a separate Kitchen-tab
- All research furniture to a separate Research-tab
- All Ideology ritual-furniture to a separate Ideology-tab

Special case
- All storage-containers to the Storage-tab
  If [Extended Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=731732064) or [LWM's Deep Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=1617282896) is loaded their Storage-tab will be used instead.
  
Options dependent of other mods:
- All garden-tools to the Garden Tools-tab of [VGP Garden Tools](https://steamcommunity.com/sharedfiles/filedetails/?id=2007063961)
- All fences the Fences-tab of [Fences and Floors](https://steamcommunity.com/sharedfiles/filedetails/?id=2012420113)
  
Other options:
- If a tab is empty after the sorting the mod can remove it
- You can sort all tabs alphabetically, the Zones and Orders-tab can be skipped
- You can also sort the tabs manually
- Items that use the same button can be moved all at once, for example stuffed floors
- Manual sorting - if some items gets in the wrong place, move them around.
- Create new tabs to sort to
- Added support for marcin212's [Architect Icons](https://steamcommunity.com/sharedfiles/filedetails/?id=1195427067)
- Sort the main buttons
- Sort the order of buildable items

If you find any item that don't get sorted or gets sorted in the wrong tab, please leave a report of what item and from what mod it comes from. Either here in the comments or at the support-channel via the Discord-link below.
Also, if you have any ideas on more stuff that can be categorized, just let me know and Ill look into it.

- Added Korean translation, thanks isty2e 
- Added Russian translation, thanks Reiquard
- Thanks to some performace updates and the "Move all"-feature by Taranchuk
- Kitchen room role worker fix by slippycheeze

Big thanks to [SmashPhil](https://steamcommunity.com/id/smashphil/myworkshopfiles/?appid=294100) for the help and coding examples when I made a better (good) version of the settings menu! 
Also thanks to [AUTOMATIC](https://steamcommunity.com/id/no-sry/myworkshopfiles/?appid=294100) for the well structured code for the miniature icons from the Recipe Icons mod!

![Image](https://i.imgur.com/Ds0rBAD.png)

Since modding is just a hobby for me I expect no donations to keep modding. If you still want to show your support you can gift me anything from my [Wishlist](https://store.steampowered.com/wishlist/id/Mlie) or buy me a cup of tea.

[![Image](https://i.imgur.com/VWG0yff.png)](https://ko-fi.com/G2G55DDYD)

![Image](https://i.imgur.com/5xwDG6H.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Post your error-log using [HugsLib](https://steamcommunity.com/workshop/filedetails/?id=818773962) or the standalone [Uploader](https://steamcommunity.com/sharedfiles/filedetails/?id=2873415404) and command Ctrl+F12
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.
-  Use [RimSort](https://github.com/RimSort/RimSort/releases/latest) to sort your mods

 

[![Image](https://img.shields.io/github/v/release/emipa606/TabSorting?label=latest%20version&style=plastic&labelColor=0070cd&color=white)](https://steamcommunity.com/sharedfiles/filedetails/changelog/2138635288) | tags: sorting,  categories
