# GitHub Copilot Instructions for RimWorld `Tab-sorting` Mod

## Mod Overview and Purpose

**Mod Name**: Tab-sorting

**Description**: The `Tab-sorting` mod is designed to improve the organization of buildable items within RimWorld by categorizing furniture and structures into appropriate tabs. Originally inspired by the LigtsTab mod, this version reduces complexity by eliminating specific patch-files for each mod or item, instead utilizing C# code to sort items post-loading.

## Key Features and Systems

- **Automated Sorting**: Automatically sorts items into designated tabs post-launch, which is a one-time process at startup.
- **Categorization**:
  - Lights are sorted into a separate Lights-tab.
  - Walls and doors move to Structure-tab.
  - Floors are organized under Floors-tab.
  - Beds and linkable furniture to Bedroom-tab.
  - Medical furniture to Hospital-tab.
  - Tables and chairs to Tables/Chairs-tab.
  - Decorative items to Decorations-tab.
  - Kitchen-related furniture to Kitchen-tab.
  - Research facilities to Research-tab.
  - Ideology-related furniture to Ideology-tab.
- **Special Case Handling**: Storage containers adapt to the Storage-tab, accounting for compatibility with other mods' tabs if loaded.
- **Mod-dependent Options**:
  - Garden tools integrate with VGP Garden Tools' tab.
  - Fencing seamlessly combines with Fences and Floors.

- **Functional Enhancements**:
  - Empty tabs can be auto-removed or hidden based on research visibility.
  - Full manual and alphabetical sorting supported, with features to create new tabs.
  - Compatibility with Architect Icons by marcin212.
  - Performance improvements and batch "Move all" feature.

## Coding Patterns and Conventions

- **Version Management**: Uses .NET Framework 4.7.2, 4.8.1, and 4.8.
- **Class Declarations**: Static utility classes like `ArchitectCategoryTab_InfoRect`, and instanced classes such as `Dialog_ChooseTabIcon`, `Dialog_RenameTab` for UI handling.
- **UI Integration**: Derives from `Window` class for dialog management in `Dialog_ChooseTabIcon` and `Dialog_RenameTab`.
- **Settings Management**: Use `TabSortingModSettings` to reset manual values, maintaining data organization.

## XML Integration

- XML files are not directly used for sorting in this mod. Instead, C# code dynamically sorts and arranges items post-load, enhancing flexibility and reducing maintenance overhead.

## Harmony Patching

- **Usage**: The mod does not heavily rely on Harmony for patching; instead, it employs direct C# code execution after item loading to perform sorting, deviating from traditional Harmony patching methods.
- **Strategic Locations**: Post-def loading hooks can be a point for any additional sorting logic if necessary.

## Suggestions for Copilot

- **Pattern Recognition**: Assist with code completion in `Dialog_RenameTab` for string validation and UI updates.
- **Class Interface Suggestions**: Help with identifying potential interfaces for UI classes, such as `Window`, to improve dialog functionality.
- **Sorting Logic and Methods**: Support refactoring and optimization of sorting methods within `TabSorting` and `ListingExtension`.
- **Method Completion**: Recommend efficient collection operations when dealing with large sets of categorized items.
- **Debugging Assistance**: Provide tips on performance tuning when initializing and sorting tabs, particularly with the `drawOptions` method in `TabSortingMod`.
- **Localization Support**: Facilitate integration of translations with potential for further language support and maintenance.

For any items that don't correctly sort or are misplaced, users are encouraged to report the item and its originating mod either via comments or on the support Discord channel provided in the mod documentation.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
