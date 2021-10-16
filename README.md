# TabSorting

![Image](https://i.imgur.com/buuPQel.png)

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
  If https://steamcommunity.com/sharedfiles/filedetails/?id=731732064]Extended Storage or https://steamcommunity.com/sharedfiles/filedetails/?id=1617282896]LWM's Deep Storage is loaded their Storage-tab will be used instead.
  

Options dependent of other mods:
- All garden-tools to the Garden Tools-tab of https://steamcommunity.com/sharedfiles/filedetails/?id=2007063961]VGP Garden Tools
- All fences the Fences-tab of https://steamcommunity.com/sharedfiles/filedetails/?id=2012420113]Fences and Floors
  
Other options:
- If a tab is empty after the sorting the mod can remove it
- You can sort all tabs alphabetically, the Zones and Orders-tab can be skipped
- Manual sorting - if some items gets in the wrong place, move them around.
- Create new tabs to sort to


If you find any item that don't get sorted or gets sorted in the wrong tab, please leave a report of what item and from what mod it comes from. Either here in the comments or at the support-channel via the Discord-link below.
Also, if you have any ideas on more stuff that can be categorized, just let me know and Ill look into it.


- Added Korean translation, thanks isty2e 

Big thanks to https://steamcommunity.com/id/smashphil/myworkshopfiles/?appid=294100]SmashPhil for the help and coding examples when I made a better (good) version of the settings menu! 
Also thanks to https://steamcommunity.com/id/no-sry/myworkshopfiles/?appid=294100]AUTOMATIC for the well structured code for the miniature icons from the Recipe Icons mod!

![Image](https://i.imgur.com/O0IIlYj.png)

Since modding is just a hobby for me I expect no donations to keep modding. If you still want to show your support you can gift me anything from my https://store.steampowered.com/wishlist/id/Mlie]Wishlist or buy me a cup of tea.

https://ko-fi.com/G2G55DDYD]![Image](https://i.ibb.co/VWJJb3w/Support-Me-dark-2x.png)


![Image](https://i.imgur.com/PwoNOj4.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Post your error-log using https://steamcommunity.com/workshop/filedetails/?id=818773962]HugsLib and command Ctrl+F12
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.



