# TabSorting

![Image](https://i.imgur.com/WAEzk68.png)

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

Options dependant of other mods:
- All storage-containers to the Storage-tab of the Extended Storage-mod
  https://steamcommunity.com/sharedfiles/filedetails/?id=731732064
- All garden-tools to the Garden Tools-tab of the VGP Garden Tools-mod
  https://steamcommunity.com/sharedfiles/filedetails/?id=2007063961
- All fences the Fences-tab of the Fences and Floors-mod
  https://steamcommunity.com/sharedfiles/filedetails/?id=2012420113
  
Other options:
- If a tab is empty after the sorting the mod can remove it
- You can sort all tabs alphabetically, the Zones and Orders-tab can be skipped
- Manual sorting - if some items gets in the wrong place, move them around.


If you find any item that dont get sorted or gets sorted in the wrong tab, please leave a report of what item and from what mod it comes from. Either here in the comments or at the support-channel via the Discord-link below.
Also, if you have any ideas on more stuff that can be categorised, just let me know and Ill look into it.

[table]
    [tr]
        [td]https://invite.gg/Mlie]![Image](https://i.imgur.com/zdzzBrc.png)
[/td]
        [td]https://github.com/emipa606/TabSorting]![Image](https://i.imgur.com/kTkpTOE.png)
[/td]
    [/tr]
[/table]

Big thanks to https://steamcommunity.com/id/smashphil/myworkshopfiles/?appid=294100]SmashPhil for the help and coding examples when I made a better (good) version of the settings menu! 
Also thanks to https://steamcommunity.com/id/no-sry/myworkshopfiles/?appid=294100]AUTOMATIC for the well structured code for the miniature icons from the Recipe Icons mod!

![Image](https://i.imgur.com/Rs6T6cr.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Post your error-log using https://steamcommunity.com/workshop/filedetails/?id=818773962]HugsLib and command Ctrl+F12
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.




