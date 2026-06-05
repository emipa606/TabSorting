# Tab-sorting Mod: GitHub Copilot Instructions

## Mod Overview and Purpose

**Tab-sorting** is a RimWorld mod designed to enhance the player's organizational experience by sorting furniture and structures into appropriate tabs within the game. Initially inspired by the LightsTab mod by betaALPHAs, Tab-sorting offers a simplified, maintenance-friendly approach by using C# to categorize items dynamically after all content has loaded, rather than relying on numerous patch files.

## Key Features and Systems

- **Automatic Sorting**: Categorizes various structures and items into relevant tabs, such as Lights, Structure, Floors, Bedroom, Hospital, Decorations, Kitchen, Research, and Ideology.
- **Special Cases**: Optimizes sorting for specific items like storage containers, leveraging compatibility with other mods such as Extended Storage or LWM's Deep Storage.
- **Conditional Sorting**: Options to sort items based on available mods, for instance, Garden Tools and Fences.
- **Tab Management**: Users can remove empty tabs, hide tabs based on missing research requirements, and sort tabs alphabetically or manually.
- **Customization**: Create custom tabs, move items manually for precise control, and sort buildable items alphabetically or by priority.
- **Localization Support**: Includes Korean and Russian translations.
- **Performance Enhancements**: Features such as the "Move All" option for grouped item management and specific room role worker fixes.
- **Integration with **marcin212's Architect Icons** for enhanced UI interaction.

## Coding Patterns and Conventions

- **Class and Method Naming**: Use PascalCase for class names (e.g., `TabSortingMod`) and camelCase for method names (e.g., `drawOptions`).
- **XML Integration**: Designation categories are stored in XML format (e.g., `DesignationCat.xml`), allowing straightforward updates and expansions.
- **Harmony Patching**: Harmony is used for non-invasive patches, minimizing conflicts and maintaining compatibility with concurrent mods.

## XML Integration

The XML file `DesignationCat.xml` defines designation categories, which are loaded at runtime to sort structures and furniture automatically. Ensure that if you add new categories, you maintain a consistent and descriptive naming scheme to facilitate future maintenance and readability.

## Harmony Patching

This project minimizes the use of Harmony patches to avoid complexity. However, when necessary, the mod employs Harmony patches to extend or modify existing functionalities without altering the base game code directly. Document any new patches extensively and ensure they are as lightweight as possible to prevent performance issues.

## Suggestions for Copilot

To assist in maintaining and enhancing the Tab-sorting mod, use GitHub Copilot to:
- **Generate Boilerplate Code**: Quickly create new classes or methods to manage additional tab sorting criteria.
- **Suggest Optimization Improvements**: Refactor existing C# code to improve execution performance, especially during the initial game load and tab categorization phases.
- **Facilitate XML Updates**: Auto-suggest XML node patterns when integrating new categories or updating existing ones.
- **Assist with Harmony Patching**: Guide the application of Harmony patches, suggesting potential detour methodologies or prefix/postfix approaches as needed.
- **Localization**: Speed up the addition of new languages by suggesting translation keys and formats based on existing snippets.
  
By following these guidelines and utilizing GitHub Copilot smartly, contributors can efficiently extend the mod’s functionality, ensure high performance, and maintain code readability.


This document provides developers and contributors with a comprehensive understanding of the Tab-sorting mod's structure, purpose, and standard practices, aiding in efficient development and troubleshooting.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).

