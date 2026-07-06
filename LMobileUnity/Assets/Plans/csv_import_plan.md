# Project Overview
- **Game Title**: LeghMobile (LMobileUnity)
- **High-Level Concept**: A dialog-driven game or system requiring rapid CSV importing to generate structured DialogueData ScriptableObjects.
- **Players**: Single player
- **Inspiration / Reference Games**: Generic dialog-heavy RPGs/Visual Novels
- **Tone / Art Direction**: Stylized / UI-driven
- **Target Platform**: WebGL / Mobile
- **Screen Orientation / Resolution**: Landscape/Portrait
- **Render Pipeline**: Built-in Render Pipeline

---

# Game Mechanics
## Core Gameplay Loop
The CSV dialog import system is an Editor-only tool. Its core loop is:
1. Write/Edit dialogues in external spreadsheet software (Excel, Google Sheets).
2. Export files as `.csv`.
3. Open the **Dialogue CSV Importer** custom Editor Window in Unity.
4. Load multiple CSV files simultaneously from a designated folder path.
5. Review the list of loaded `TextAsset` files.
6. Set the output directory (`savePath`) for the generated ScriptableObjects.
7. Click the dynamically styled **Generate Dialogue** button.
8. Instantly generate matching `DialogueData` assets in the project.

## Controls and Input Methods
- **Mouse / Keyboard**: Unity Editor-only GUI controls (buttons, text fields, foldouts).

---

# UI Mockup (OnGUI Window Layout)

```text
====================================================================================
                        DIALOGUE CSV IMPORTER WINDOW
====================================================================================

  CSV Source Folder:
  [ Assets/CSV_Input                                                  ] [ Browse ]
  
  [ LOAD ALL CSVS FROM FOLDER ]   <-- Fills 'fileCSV' list with TextAssets in path

  ----------------------------------------------------------------------------------
  Loaded CSV Files List (Count: 3)
  ----------------------------------------------------------------------------------
   [1] TextAsset: intro_dialogue.csv
   [2] TextAsset: quest_dialogue.csv
   [3] TextAsset: ending_dialogue.csv
  
  ----------------------------------------------------------------------------------
  Save Destination Path:
  [ Assets/Resources/Dialogues                                        ] [ Browse ]
  
  ==================================================================================
  [                                GENERATE DIALOGUE                               ]
  ==================================================================================
  * Dynamic Color State:
    - INCOMPLETE inputs: Button is GREY, text is white, button is disabled.
    - VALID inputs:      Button is WHITE, text is BLACK, button is active.

  Status Log:
  [ Status: Process completed successfully! Saved 3 Dialogues.                     ]
====================================================================================
```

---

# Key Asset & Context

### 1. `DialogueData.cs` (Assets/Scripts/Dialogue/DialogueData.cs)
This is the existing target data structure we must populate.
```csharp
[Serializable]
public struct Dialogue
{
    public string speakerName;
    [TextArea(5, 10)]
    public string dialogueText;
    public Sprite speakerPortrait;
}

public class DialogueData : ScriptableObject
{
   public List<Dialogue> dialogues;
}
```

### 2. `ImportCSV.cs` (Assets/Editor/ScriptsEditor/ImportCSV.cs)
We will convert this class from a `MonoBehaviour` to an `EditorWindow`. It will contain:
- `List<TextAsset> fileCSV`: The loaded CSV files.
- `string savePath`: Target output folder for `DialogueData` assets.
- `string csvSourcePath`: Input folder containing CSV assets.
- `string statusLog`: Output logging messages shown directly on the GUI.
- `void GenerateDialogue()`: Core processing loop.
- `void OnGUI()`: Editor window rendering.

---

# Implementation Steps

### Step 1: Convert `ImportCSV` to `EditorWindow`
- **Description**: Modify `Assets/Editor/ScriptsEditor/ImportCSV.cs` to inherit from `UnityEditor.EditorWindow`. Create a custom menu item `[MenuItem("Tools/Dialogue CSV Importer")]` to open the window.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Implement Member Variables & Fields
- **Description**: Add fields:
  - `private List<TextAsset> fileCSV = new List<TextAsset>();`
  - `private string csvSourcePath = "Assets/";`
  - `private string savePath = "Assets/";`
  - `private string statusLog = "Ready";`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

### Step 3: Implement CSV Folder Loading Logic
- **Description**: Implement a button and action to search `csvSourcePath` using `Directory.GetFiles` or `AssetDatabase.FindAssets` to load all `.csv` files as `TextAsset` and populate `fileCSV`.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Implement Core Parser & ScriptableObject Creator (`GenerateDialogue`)
- **Description**: Add the `GenerateDialogue` function:
  - For each `TextAsset` in `fileCSV`:
    - Create a new instance of `DialogueData` via `ScriptableObject.CreateInstance<DialogueData>()`.
    - Split the CSV text content by lines (`\n` or `\r\n`).
    - Parse each line to split into columns (Speaker, Dialogue Text).
    - Skip empty lines.
    - Create a `Dialogue` struct for each parsed line and populate `dialogues`.
    - Save the asset to `savePath` with the same name as the `TextAsset` using `AssetDatabase.CreateAsset` and `AssetDatabase.SaveAssets`.
    - **Mark code clearly with a commented developer marker for future column expansion.**
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Implement UI Design with OnGUI
- **Description**: Code the visual interface in `OnGUI()`:
  - Text fields with "Browse" buttons for choosing paths easily.
  - Read-only numbered display showing how many items are in the `fileCSV` list.
  - Dynamically style the "Generate Dialogue" button:
    - Grey color if fields are empty / invalid.
    - White color with black text when inputs are valid.
  - Log console at the bottom showing step-by-step progress updates of the CSV import process.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

---

# Verification & Testing

### Test 1: Window Interface Test
1. Open the tool via `Tools -> Dialogue CSV Importer`.
2. Confirm all fields, layout, and styling resemble the mockup.

### Test 2: Input & Validation Test
1. Verify the "Generate Dialogue" button remains grey and disabled when `fileCSV` or `savePath` are empty.
2. Enter a folder containing CSVs, click "Load CSVs", and check if the list populated and shows the count.
3. Choose a valid output folder for `savePath`.
4. Check if the "Generate Dialogue" button turns white with black text and becomes clickable.

### Test 3: Dialogue Asset Generation Test
1. Place a test CSV file (e.g., `test_dialogue.csv`) with rows:
   ```csv
   Hero,Hello world!
   Guide,Welcome to the game.
   ```
2. Click "Generate Dialogue".
3. Check the target output folder (`savePath`) for `test_dialogue.asset`.
4. Select the created asset and verify it has exactly 2 dialogues with matching speaker names and dialog text.
