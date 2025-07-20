# GitHub Copilot Instructions for RimWorld Tab Sorting Mod

## Mod Overview and Purpose
This RimWorld mod enhances the user experience by allowing players to sort and customize the architect tabs within the game. It provides functionalities for renaming tabs, choosing custom icons, and reordering the architect categories, improving the overall gameplay experience by making the user interface more intuitive and personalized.

## Key Features and Systems
- **Tab Renaming:** Modify the names of architect tabs to better fit your play style or thematic preferences.
- **Custom Icons:** Choose from a selection of icons for each tab to enhance visual distinction.
- **Tab Sorting:** Drag and drop functionality to reorder architect tabs according to personal preferences.
- **Integration with Existing Game Systems:** Ensures that modifications are seamlessly integrated without disrupting the vanilla game experience.

## Coding Patterns and Conventions
- **Static Utility Classes:** Many classes like `ListingExtension` and `MainTabWindow_Architect_TabRectHeight` use static methods to provide utility functions.
- **Inheritance:** The `Dialog_ChooseTabIcon` and `Dialog_RenameTab` classes inherit from `Window`, demonstrating polymorphism for creating custom UI windows.
- **Modular Methods:** Functions are compartmentalized for clarity and reuse, such as `drawIcon` and `DrawTabsList` in `TabSortingMod`.

## XML Integration
The mod connects with RimWorld's existing XML framework for defining UI components, game objects, and other settings. Although specific XML content isn't highlighted, integration involves:
- Ensuring Harmony patches and code changes reflect XML configuration when needed.
- Overriding or extending XML attributes in methods where applicable.

## Harmony Patching
Harmony is utilized to patch and extend the functionality of existing RimWorld methods. This allows the mod to:
- Modify game behavior without altering the base game files.
- Intercept and alter method calls tied to tab sorting and UI displays, ensuring smooth user experience enhancements.

## Suggestions for Copilot
- **Feature Implementation:** Use Copilot to generate boilerplate code for new methods that interact with RimWorld's UI, such as extensions to `Dialog_*` classes.
- **Error Handling:** Implement consistent error handling across methods, suggesting try-catch blocks where necessary to maintain user experience.
- **Optimization Suggestions:** Leverage Copilot for code optimization, particularly in loops and static methods used frequently within the mod.
- **Code Commenting:** Encourage Copilot to create descriptive comments, aiding both future development and community contributions.
- **Integration Patterns:** Recommend patterns for integrating with RimWorld's XML systems when extending tab capacities or icon customization.

By following these guidelines and leveraging Copilot, developers can efficiently maintain and expand the functionality of the Tab Sorting Mod, contributing to a more customizable and user-friendly RimWorld experience.


This .github/copilot-instructions.md file is designed to provide a detailed guide for developers using GitHub Copilot in the context of this RimWorld C# modding project, helping streamline development processes and ensure quality outcomes.
