## GitHub Copilot Instructions for Tab-sorting Mod

### Mod Overview and Purpose

The Tab-sorting mod is designed to sort furniture and structures into appropriate tabs in RimWorld. The mod aims to simplify the interface by organizing items into logical categories. It is an improvement over previous versions that relied on patches, making it easier to maintain. The downside is that it requires a rescan of the changed tabs at startup, but this should add no more than a few seconds, depending on the number of tabs that have been changed.

### Key Features and Systems

- Sorting furniture into appropriate tabs: lights, walls, doors, floors, beds, hospital beds, tables, chairs, decorations, kitchen furniture, research furniture, ideology ritual furniture, and storage containers.
- Support for Extended Storage and LWM's Deep Storage if loaded.
- Options for sorting tabs alphabetically and hiding empty tabs. Browsing settings does not restore removed empty tabs; sorting or moving an order can re-register the original category definition when it is needed.
- Manual sorting of tabs.
- Moving all items that use the same button at once.
- Creating new tabs.
- Renaming original and mod-provided tabs without changing their identity or allowing their deletion. Labels persist separately from custom tabs; Default restores an original tab's label.
- Moving order buttons between tabs, individually or with Move everything, and restoring them with Default or Reset sortings. Settings discover live orders after game load, including Harmony additions to the resolved designator lists/enumerations. Orders also present in the Orders tab are managed only from their Orders-tab instance (or its chosen destination); shared copies stay in their original tabs and are suppressed there only while the matching Orders instance is moved in. Matching uses the concrete type, dropdown contents, and occurrence order, so differently configured instances of the same type cannot always be distinguished. Routing preserves existing instances and game-rule filtering without rebuilding designators. Saved identities use the source tab, assembly/type, and same-type registration index (not translated labels); mods changing same-type registration order can require reassignment. Mods drawing buttons outside the standard category enumerations are not intercepted.
- Support for marcin212's Architect Icons.
- Sorting main buttons and buildable items alphabetically.
- Kitchen room role worker fix.

### Coding Patterns and Conventions

- The mod uses C# code to sort things after they have loaded, removing the need for specific patch files for each mod/item.
- Harmony patching is used to modify the behavior of the game.
- The mod is organized into several classes and methods, each responsible for a specific task.

### XML Integration

The mod includes several XML files that define the categories and settings for the sorting system. These files include:

- `DesignationCat.xml`: Defines the categories for designations.
- `ExampleIgnoreSettingDef.xml`: Defines the settings for ignoring certain categories.
- `List.xml`: Defines the list of favorite categories.

### Harmony Patching

The mod uses Harmony patching to modify the behavior of the game. The following Harmony patches are used:

- `MainTabWindow_Architect_TabRectHeight`: Modifies the height of the architect tabs.

### Suggestions for Copilot

- Use Harmony patching to modify the behavior of the game.
- Use XML files to define categories and settings for the sorting system.
- Organize code into several classes and methods, each responsible for a specific task.

## C# Code Examples

csharp
public class ArchitectCategoryTab_InfoRect
{
    public static void Postfix(ArchitectCategoryTab_InfoRect __instance)
    {
        // Add custom code here
    }
}

public class CurrentDefsInCategory
{
    public static List<Def> AllCurrentDefsInCategory(List<Def> defs, DesignationCategoryDef category)
    {
        // Add custom code here
        return defs;
    }

    public static void Reorder(List<Def> defs, DesignationCategoryDef category)
    {
        // Add custom code here
    }
}

public class Dialog_RenameTab : Dialog_Rename
{
    public override AcceptanceReport AcceptanceReport()
    {
        // Add custom code here
        return base.AcceptanceReport();
    }

    public Dialog_RenameTab(ThingDef def)
    {
        // Add custom code here
        base.(def);
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Add custom code here
        base.DoWindowContents(inRect);
    }

    public bool nameIsValid(string name)
    {
        // Add custom code here
        return base.nameIsValid(name);
    }

    public void setName(string name)
    {
        // Add custom code here
        base.setName(name);
    }

    public override void WasOpenedByHotkey()
    {
        // Add custom code here
        base.WasOpenedByHotkey();
    }
}

public class IgnoreSettingDef : Def
{
    public override void ResolveReferences()
    {
        // Add custom code here
        base.ResolveReferences();
    }
}

public class MainTabWindow_Architect_TabRectHeight
{
    public static void Postfix(Rect __result)
    {
        // Add custom code here
    }

    public static void Prefix(ref Rect __result)
    {
        // Add custom code here
    }

    public static List<MethodInfo> TargetMethods()
    {
        // Add custom code here
        return base.TargetMethods();
    }
}


## XML File Examples

xml
<DesignationCatDef>
    <label>Lights</label>
    <defName>Lights</defName>
    <description>Sorts all lights into a separate tab.</description>
    <designations>
        <li>Light</li>
        <li>Lamp</li>
        <li>FluorescentLight</li>
        <!-- Add more designations here -->
    </designations>
</DesignationCatDef>

<IgnoreSettingDef>
    <label>Ignore</label>
    <defName>Ignore</defName>
    <description>Ignore this category.</description>
    <categories>
        <li>Lights</li>
        <li>Floors</li>
        <!-- Add more categories here -->
    </categories>
</IgnoreSettingDef>

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Temporary files

- Do not create temporary files, build output, test artifacts, or Copilot session
  folders inside the mod root.
- Store temporary artifacts under `%USERPROFILE%\CopilotTemp\[modname]`.
- Direct build and test output there as well, without changing project settings.
- Clean up task-specific temporary artifacts when finished.

## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

