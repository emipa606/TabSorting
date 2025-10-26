# GitHub Copilot Instructions for RimWorld 'Tab-sorting' Mod

## Mod Overview and Purpose

The "Tab-sorting" mod enhances the RimWorld gameplay experience by organizing furniture and structures into appropriate category tabs in the architect area. Inspired by the LigtsTab but improved upon by eliminating complexity and maintenance burdens associated with numerous patches, this mod leverages C# code to dynamically sort items after they have loaded. This approach ensures that changes are handled during startup scans, providing seamless integration without requiring individual patch files for every mod or item.

## Key Features and Systems

- **Auto-Sorting System**: Automatically categorizes items into various architect tabs.
  - Lights
  - Structure (walls, doors)
  - Floors
  - Beds and bedroom furniture
  - Medical furniture
  - Tables and chairs
  - Decorative items
  - Kitchen furniture
  - Research equipment
  - Ideology ritual furniture
  - Special cases like storage containers

- **Integrated Support for Other Mods**: 
  - VGP Garden Tools and Fences and Floors are accounted for.
  
- **Customizable Options**:
  - Remove or hide empty tabs.
  - Manual tab and item sorting.
  - Alphabetical sorting, with options to skip certain tabs.
  - New tab creation and architect main button organization.

- **Localization**: Supports multiple languages, including Korean and Russian.

## Coding Patterns and Conventions

- **Static Helpers and Utility Classes**: Encapsulate specific functionalities in static classes to improve code reusability. Example: `ArchitectCategoryTab_InfoRect` and `AllCurrentDefsInCategory`.

- **Dialog and Window Management**: All user interfaces, such as dialogs for choosing icons or renaming tabs, are managed using the `Window` class derivatives (e.g., `Dialog_ChooseTabIcon`, `Dialog_RenameTab`).

- **Settings Management**: Use of `ModSettings` through the `TabSortingModSettings` class to persist user preferences and configurations.

## XML Integration

- The mod utilizes RimWorld's comprehensive XML structure for defining items. Although XML is not directly altered by this mod due to its post-load sorting mechanism, XML definitions remain vital for item identification and categorization. 

## Harmony Patching

- Although the mod minimizes direct patches, it benefits from Harmony for enhanced flexibility. This allows for method interception post-def load, ensuring all categorization logic is contained within C#.

## Suggestions for Copilot

To optimize suggestions provided by GitHub Copilot, consider the following:

1. **Encapsulate Logic**: When writing sorting logic, encapsulate related operations into methods within appropriate static utility classes for better modularity.
   
2. **Use Consistent Naming Conventions**: Maintain clear, intuitive method and class names that reflect the functionality and improve understandability.

3. **Parameterize Tab Names and Categories**: When suggesting new features, Copilot can recommend parameterizing elements to easily expand support for more tabs or mods.

4. **Dialog Enhancements**: When adding new dialogs or functionality, have Copilot suggest standard UI patterns that fit the RimWorld context.

5. **Leverage LINQ**: For operations over collections, such as sorting and filtering, suggest LINQ operations to maintain concise and efficient code.

6. **Localization Considerations**: Propose structures for additional language support, ensuring text strings are easily accessible for translation files.

By adhering to these guidelines, the integration of Copilot can significantly streamline the development process, enhancing both the functionality and maintainability of the "Tab-sorting" mod.
