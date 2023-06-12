using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Localization;
using Sirenix.OdinInspector;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using PixelCrushers.QuestMachine;
using Debug = UnityEngine.Debug;
using Microsoft.Data.Analysis;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VersionControl;
using Sirenix.Utilities.Editor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.VersionControl;
#endif


public class GlobalLocalizationDatabase : ScriptableObject
{
    [System.Serializable]
    public struct OneClickLocalizationData
    {
        public enum ItemType { StringTable, DialogueDatabase, Misc }
        public ItemType Type;

        [ShowIf("Type", ItemType.StringTable)]
#if UNITY_EDITOR
        public StringTableCollection TargetTable;
#endif

        [ShowIf("Type", ItemType.DialogueDatabase)]
        public DialogueDatabase TargetDialogueDatabase;

        [ShowIf("Type", ItemType.Misc)]
        public List<TextTable> TextTables;

        //[PropertySpace(10)]
        //[PropertyOrder(3)]
        //[FolderPath(AbsolutePath = true)]
        //[InfoBox("The path of the CSV file to export localization data to.")]
        //public string OutputCSVPath;

        [Button("EXPORT CSV")]
        [PropertyOrder(2)]
        public void Export()
        {
#if UNITY_EDITOR
            bool result = ExportToCSV();

            if (result)
            {
                EditorUtility.DisplayDialog("It's Done", $"Successfully exported CSV", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failure", $"FAILED to export CSV", "OK");
            }
#endif
        }


        [Button("IMPORT CSV")]
        [PropertyOrder(3)]
        public void Import()
        {
#if UNITY_EDITOR
            bool result = ImportFromCSV();

            if (result)
            {
                EditorUtility.DisplayDialog("It's Done", $"Successfully imported CSV", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failure", $"FAILED to import CSV", "OK");
            }
#endif
        }


        [PropertyOrder(4)]
        public bool ApplyToNodes;

        [PropertyOrder(4)]
        [ShowIf("ApplyToNodes")]
        public BaseSkillDatabase SkillDatabase;

        [PropertySpace(10)]
        [PropertyOrder(5)]
        [FolderPath(AbsolutePath = true)]
        [InfoBox("ONLY IF MANUAL OVERRIDE (no quotes):")]
        public string InputCSVPath;

        public string Name { get => GetName(); }

        public string GetName()
        {
#if UNITY_EDITOR
            if (Type == ItemType.StringTable && TargetTable != null)
            {
                return TargetTable.name;
            }
#endif
            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                return TargetDialogueDatabase.name;
            }

            if (Type == ItemType.Misc)
            {
                return "Text Table List";
            }

            return "New Localized Entry";
        }

#if UNITY_EDITOR

        

        public bool ExportToCSV(string InFolderPath = "")
        {
            if (string.IsNullOrEmpty(InFolderPath))
            {
                if (TargetTable)
                {
                    InFolderPath = Application.dataPath + "/Data Assets/LocalizationSettings/FromUnity";
                }
                else
                {
                    //InFolderPath = OutputCSVPath;
                }
            }

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(InFolderPath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Invalid path. Cannot export.", "OK");
                return false;
            }

            if (Type == ItemType.StringTable && TargetTable != null)
            {
                var file = Path.Combine(InFolderPath, "Localization_" + Name + ".csv");
                if (File.Exists(file))
                {
                    AssetList assets = new AssetList();
                    assets.Add(Provider.GetAssetByPath(Path.Combine("Assets/Data Assets/LocalizationSettings/FromUnity", "Localization_" + Name + ".csv")));

                    // Perforce stuff
                    if (Provider.GetLatestIsValid(assets))
                    {
                        Task GetLatestTask = Provider.GetLatest(assets);
                        GetLatestTask.Wait();
                    }
                    if (Provider.ResolveIsValid(assets))
                    {
                        Task ResolveTask = Provider.Resolve(assets, ResolveMethod.UseTheirs);
                        ResolveTask.Wait();
                    }
                    if (!Provider.CheckoutIsValid(assets) && !Provider.IsOpenForEdit(assets[0]))
                    {
                        Debug.LogError($"ERROR: Could not check out");
                        return false;
                    }
                    Task checkoutOperation = Provider.Checkout(assets, CheckoutMode.Both);
                    checkoutOperation.Wait();
                }
                // Write the CSV, just to get the columns
                var stream = new StreamWriter(file, false, System.Text.Encoding.UTF8);
                Csv.Export(stream, TargetTable);
                stream.Dispose();

                // WIPE the CSV but keep the column names (numRows = 0 doesn't work wtf)
                var originaldf = DataFrame.LoadCsv(filename: file, numRows: 1);
                var df = originaldf.Head(0);

                // Write the new CSV by reading from the nodes
                foreach (BaseSkillNode SkillNode in SkillDatabase.SkillList)
                {
                    if (SkillNode)
                    {
                        List<KeyValuePair<string, object>> newName = new()
                            {
                                new KeyValuePair<string, object>("Key", SkillNode.name + "_name"),
                                new KeyValuePair<string, object>("English(en)", SkillNode.SkillName),
                            };
                        df.Append(newName, inPlace: true);
                        List<KeyValuePair<string, object>> newDescription = new()
                            {
                                new KeyValuePair<string, object>("Key", SkillNode.name + "_description"),
                                new KeyValuePair<string, object>("English(en)", SkillNode.SkillDescription),
                            };
                        df.Append(newDescription, inPlace: true);
                    }
                }

                // REWRITE the CSV
                DataFrame.SaveCsv(df, file);

                // Import back into the string table (after editing the CSV)
                var stream3 = new StreamReader(file, System.Text.Encoding.UTF8);
                Csv.ImportInto(stream3, TargetTable);
                stream3.Dispose();

                // Open the explorer to folder location (for convenience!)
                Process.Start("explorer.exe", @"" + InFolderPath.Replace('/', '\\'));  
            }
#endif

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                ExportDialogueDatabaseCSV(InFolderPath, Name);
            }

            if (Type == ItemType.Misc && TextTables != null)
            {
                foreach (var Table in TextTables)
                {
                    // All in one file:
                    var content = new List<List<string>>();
                    var languageIDs = new List<int>();

                    // Heading rows:
                    var row = new List<string>();
                    content.Add(row);
                    row.Add("Field");
                    foreach (var kvp in Table.languages)
                    {
                        var language = kvp.Key;
                        var languageID = kvp.Value;
                        languageIDs.Add(languageID);
                        row.Add(language);
                    }

                    // One row per field:
                    foreach (var kvp in Table.fields)
                    {
                        var field = kvp.Value;
                        row = new List<string>();
                        content.Add(row);
                        row.Add(field.fieldName);
                        for (int i = 0; i < languageIDs.Count; i++)
                        {
                            var languageID = languageIDs[i];
                            var value = field.GetTextForLanguage(languageID);
                            row.Add(value);
                        }
                    }

                    CSVUtility.WriteCSVFile(content, Path.Combine(InFolderPath, Table.name + ".csv"), PixelCrushers.EncodingType.UTF8);
                }
            }

            return true;
        }


        public bool ImportFromCSV(string InFolderPath = "")
        {
            string InFilePath = "";

            if (string.IsNullOrEmpty(InFolderPath))
            {
                if (TargetTable)
                {
                    InFolderPath = Application.dataPath + "/Data Assets/LocalizationSettings/ToUnity";
                }
                else
                {
                    //InFolderPath = InputCSVPath;
                }
            }

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(InFolderPath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Invalid path. Cannot Import.", "OK");
                return false;
            }

            if (Type == ItemType.StringTable && TargetTable != null)
            {
                InFilePath = Path.Combine(InFolderPath, "Localization_" + Name + " - Localization_" + Name + ".csv");

                if (!string.IsNullOrEmpty(InputCSVPath))
                {
                    InFilePath = InputCSVPath;
                }

                if (!File.Exists(InFilePath))
                {
                    return false;
                }

                var stream = new StreamReader(InFilePath, System.Text.Encoding.UTF8);
                Csv.ImportInto(stream, TargetTable);

                foreach (var Table in TargetTable.Tables)
                {
                    EditorUtility.SetDirty(Table.asset);
                }

                stream.Close();

                // Assign the nodes in Master Skill DB
                if (ApplyToNodes && SkillDatabase)
                {
                    var df = DataFrame.LoadCsv(InFilePath);
                    // Find column indexes of "Key" column and "English(en)" column
                    int keyIdx = df.Columns.IndexOf("Key");
                    int englishIdx = df.Columns.IndexOf("English(en)");
                    if (keyIdx >= 0 && englishIdx >= 0)
                    {
                        AssetList assets = new AssetList();
                        assets.Add(Provider.GetAssetByPath(AssetDatabase.GetAssetPath(SkillDatabase)));

                        foreach (BaseSkillNode SkillNode in SkillDatabase.SkillList)
                        {
                            if (SkillNode)
                            {
                                assets.Add(Provider.GetAssetByPath(AssetDatabase.GetAssetPath(SkillNode)));

                                // Find row with correct keys (name and description)
                                foreach (DataFrameRow r in df.Rows)
                                {
                                    if (r[keyIdx].ToString() == SkillNode.name + "_name")
                                    {
                                        SkillNode.SkillName = r[englishIdx].ToString();
                                    }
                                    else if (r[keyIdx].ToString() == SkillNode.name + "_description")
                                    {
                                        SkillNode.SkillDescription = r[englishIdx].ToString();
                                    }
                                }
                            }
                        }

                        // Perforce stuff
                        if (Provider.GetLatestIsValid(assets))
                        {
                            Task GetLatestTask = Provider.GetLatest(assets);
                            GetLatestTask.Wait();
                        }
                        if (Provider.ResolveIsValid(assets))
                        {
                            Task ResolveTask = Provider.Resolve(assets, ResolveMethod.UseTheirs);
                            ResolveTask.Wait();
                        }
                        if (!Provider.CheckoutIsValid(assets) && !Provider.IsOpenForEdit(assets[0]))
                        {
                            Debug.LogError($"ERROR: Could not check out");
                            return false;
                        }
                        Task checkoutOperation = Provider.Checkout(assets, CheckoutMode.Both);
                        checkoutOperation.Wait();
                    }
                    else
                    {
                        Debug.Log("There must be a column called Key and a column called English(en)");
                        return false;
                    }
                }

                return true;
            }
#endif

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                if (CheckAssetState(TargetDialogueDatabase))
                {
                    ImportDialogueDatabaseCSV(InFolderPath, Name);
                    return true;
                }

                return false;
            }

            if (Type == ItemType.Misc && TextTables != null)
            {
                bool bResult = false;

                foreach (var Table in TextTables)
                {
                    if (CheckAssetState(Table))
                    {
                        string filename = Path.Combine(InFolderPath, Table.name + ".csv");
                        if (File.Exists(filename))
                        {
                            var data = CSVUtility.ReadCSVFile(filename, PixelCrushers.EncodingType.UTF8);

                            if (data == null || data.Count < 1 || data[0].Count < 2) continue;
                            var fieldList = new List<string>();
                            var firstCell = data[0][0];

                            // All-in-one file:
                            for (int x = 1; x < data[0].Count; x++)
                            {
                                var language = data[0][x];
                                if (string.IsNullOrEmpty(language)) continue;
                                if (!Table.HasLanguage(language)) Table.AddLanguage(language);
                                for (int y = 1; y < data.Count; y++)
                                {
                                    var field = data[y][0];
                                    if (string.IsNullOrEmpty(field)) continue;
                                    if (x == 1) fieldList.Add(field);
                                    if (!Table.HasField(field)) Table.AddField(field);
                                    if ((0 <= y && y < data.Count) && (0 <= x && x < data[y].Count))
                                    {

                                        Table.SetFieldTextForLanguage(field, language, data[y][x]);
                                    }
                                }
                            }

                            Table.ReorderFields(fieldList);
                            Table.OnBeforeSerialize();
                            EditorUtility.SetDirty(Table);
                            bResult = true;
                        }
                    }
                }

                return bResult;
            }

            return false;
        }


        public void CorrectLanguages(List<Locale> AvailableLocales, Template template)
        {
            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                if (CheckAssetState(TargetDialogueDatabase))
                {
                    ApplyLocaleTemplate(TargetDialogueDatabase.conversations, template.dialogueEntryFields, AvailableLocales);
                    EditorUtility.SetDirty(TargetDialogueDatabase);
                }
            }

            if (Type == ItemType.Misc && TextTables != null)
            {
                foreach (var Table in TextTables)
                {
                    if (CheckAssetState(Table))
                    {
                        bool bDirty = false;

                        foreach (var Locale in AvailableLocales)
                        {
                            if (!Table.HasLanguage(Locale.Identifier.Code))
                            {
                                Table.AddLanguage(Locale.Identifier.Code);
                                bDirty = true;
                            }
                        }

                        if (bDirty)
                            EditorUtility.SetDirty(Table);
                    }
                }
            }
        }


        private void ExportDialogueDatabaseCSV(string path, string name)
        {
            try
            {
                List<string> Locales = new List<string>();
                Template template = TemplateTools.LoadFromEditorPrefs();

                for (int i = template.dialogueEntryFields.Count - 1; i >= 0; --i)
                {
                    if (template.dialogueEntryFields[i].type == FieldType.Localization)
                    {
                        Locales.Add(template.dialogueEntryFields[i].title);
                    }
                }

                var numLanguages = Locales.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;

                    EditorUtility.DisplayProgressBar("Exporting Localization CSV", "Exporting CSV files for " + TargetDialogueDatabase.name, progress);

                    // Write Dialogue_LN.csv file:
                    var filename = path + "/" + name + "_" + Locales[i] + ".csv";

                    using (var file = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                    {
                        var orderedFields = new string[] { "Dialogue Text", Locales[i], "Menu Text", "Menu Text " + Locales[i], "Description" };
                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                            "Conversation ID",
                            "Entry ID",
                            "Original Text",
                            "Translated Text [" + Locales[i] + "]",
                            "Original Menu",
                            "Translated Menu [" + Locales[i] + "]",
                            "Description");

                        foreach (var c in TargetDialogueDatabase.conversations)
                        {
                            foreach (var de in c.dialogueEntries)
                            {
                                var fields = new List<string>();
                                foreach (string s in orderedFields)
                                {
                                    var f = de.fields.Find(x => x.title == s);
                                    fields.Add((f != null) ? f.value : string.Empty);
                                }

                                var sb = new StringBuilder();
                                sb.AppendFormat("{0},{1}", c.id, de.id);
                                foreach (string value in fields)
                                    sb.AppendFormat(",{0}", WrapCSVValue(value));
                                file.WriteLine(sb.ToString());
                            }
                        }
                        file.Close();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private void ImportDialogueDatabaseCSV(string path, string name)
        {
            try
            {
                List<string> Locales = new List<string>();
                Template template = TemplateTools.LoadFromEditorPrefs();

                for (int i = template.dialogueEntryFields.Count - 1; i >= 0; --i)
                {
                    if (template.dialogueEntryFields[i].type == FieldType.Localization)
                    {
                        Locales.Add(template.dialogueEntryFields[i].title);
                    }
                }

                var numLanguages = Locales.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;

                    EditorUtility.DisplayProgressBar("Importing Localization CSV", "Importing CSV files for " + TargetDialogueDatabase.name, progress);

                    // Read Dialogue_LN.csv file:
                    var filename = path + "/" + name + "_" + Locales[i] + ".csv";

                    if (File.Exists(filename))
                    {
                        var lines = ReadDialogueCSV(filename);
                        CombineMultilineCSVSourceLines(lines);

                        for (int j = 2; j < lines.Count; j++)
                        {
                            var columns = GetCSVColumnsFromLine(lines[j]);
                            if (columns.Length < 6)
                            {
                                Debug.LogError(filename + ":" + (j + 1) + " Invalid line: " + lines[j]);
                            }
                            else
                            {
                                var conversationID = PixelCrushers.DialogueSystem.Tools.StringToInt(columns[0]);
                                var entryID = PixelCrushers.DialogueSystem.Tools.StringToInt(columns[1]);
                                var entry = TargetDialogueDatabase.GetDialogueEntry(conversationID, entryID);

                                if (entry == null)
                                {
                                    Debug.LogError(filename + ":" + (j + 1) + " Database doesn't contain conversation " + conversationID + " dialogue entry " + entryID);
                                }
                                else
                                {
                                    Field.SetValue(entry.fields, Locales[i], columns[3], FieldType.Localization);
                                    Field.SetValue(entry.fields, "Menu Text " + Locales[i], columns[5], FieldType.Localization);

                                    // Check if we also need to import updated main text.
                                    if (Locales[i] == "en")
                                    {
                                        entry.DialogueText = columns[2];
                                        entry.MenuText = columns[4];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private string WrapCSVValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            string s2 = s.Contains("\n") ? s.Replace("\n", "\\n") : s;
            if (s2.Contains("\r")) s2 = s2.Replace("\r", "\\r");
            if (s2.Contains(",") || s2.Contains("\""))
            {
                return "\"" + s2.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return s2;
            }
        }


        private List<string> ReadDialogueCSV(string filename)
        {
            var lines = new List<string>();
            StreamReader sr = new StreamReader(filename, new UTF8Encoding(true));
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                lines.Add(line.TrimEnd());
            }
            sr.Close();
            return lines;
        }


        private void CombineMultilineCSVSourceLines(List<string> sourceLines)
        {
            int lineNum = 0;
            int safeguard = 0;
            int MaxIterations = 999999;
            while ((lineNum < sourceLines.Count) && (safeguard < MaxIterations))
            {
                safeguard++;
                string line = sourceLines[lineNum];
                if (line == null)
                {
                    sourceLines.RemoveAt(lineNum);
                }
                else
                {
                    bool terminated = true;
                    char previousChar = (char)0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        char currentChar = line[i];
                        bool isQuote = (currentChar == '"') && (previousChar != '\\');
                        if (isQuote) terminated = !terminated;
                        previousChar = currentChar;
                    }
                    if (terminated || (lineNum + 1) >= sourceLines.Count)
                    {
                        if (!terminated) sourceLines[lineNum] = line + '"';
                        lineNum++;
                    }
                    else
                    {
                        sourceLines[lineNum] = line + "\\n" + sourceLines[lineNum + 1];
                        sourceLines.RemoveAt(lineNum + 1);
                    }
                }
            }
        }


        private string[] GetCSVColumnsFromLine(string line)
        {
            Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)");
            List<string> values = new List<string>();
            foreach (Match match in csvSplit.Matches(line))
            {
                values.Add(UnwrapCSVValue(match.Value.TrimStart(',')));
            }
            return values.ToArray();
        }

        private string UnwrapCSVValue(string s)
        {
            string s2 = s.Replace("\\n", "\n").Replace("\\r", "\r");
            if (s2.StartsWith("\"") && s2.EndsWith("\""))
            {
                s2 = s2.Substring(1, s2.Length - 2).Replace("\"\"", "\"");
            }
            return s2;
        }


        private void ApplyLocaleTemplate(List<Conversation> assets, List<Field> templateFields, List<Locale> AvailableLocales)
        {
            foreach (var conversation in TargetDialogueDatabase.conversations)
            {
                foreach (var entry in conversation.dialogueEntries)
                {
                    if (entry.fields == null || templateFields == null) continue;

                    for (int i = entry.fields.Count - 1; i >= 0; --i)
                    {
                        if (entry.fields[i].type == FieldType.Localization &&
                            AvailableLocales.Find((l) => l.Identifier.Code == entry.fields[i].title) == null)
                        {
                            entry.fields.RemoveAt(i);
                        }
                    }

                    foreach (Field templateField in templateFields)
                    {
                        if (!string.IsNullOrEmpty(templateField.title))
                        {
                            var field = Field.Lookup(entry.fields, templateField.title);
                            if (field != null)
                            {
                                // Variables' Initial Value should never be applied from template.
                                var shouldApplyTemplateFieldType = (field.type != templateField.type) && !string.Equals(field.title, "Initial Value");
                                if (shouldApplyTemplateFieldType)
                                {
                                    field.type = templateField.type;
                                    field.typeString = string.Empty;
                                }
                            }
                            else
                            {
                                entry.fields.Add(new Field(templateField));
                            }
                        }
                    }
                }
            }
        }


        public void IsAvailableForEdit()
        {
            if (Type == ItemType.StringTable && TargetTable != null)
            {
                foreach (var Table in TargetTable.Tables)
                {
                    CheckAssetState(Table.asset);
                }
            }

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                CheckAssetState(TargetDialogueDatabase);
            }
        }


        private bool CheckAssetState(Object Item)
        {
            if (!Provider.isActive || Provider.onlineState == OnlineState.Offline || !Provider.hasCheckoutSupport) return true;

            UnityEditor.VersionControl.Asset asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(Item));

            UnityEditor.VersionControl.Task statusTask = UnityEditor.VersionControl.Provider.Status(asset);
            statusTask.Wait();

            if (statusTask.assetList[0].state.HasFlag(UnityEditor.VersionControl.Asset.States.CheckedOutLocal))
            {
                return true;
            }
            else if (Provider.CheckoutIsValid(statusTask.assetList[0]))
            {
                Provider.Checkout(statusTask.assetList[0], CheckoutMode.Asset);
                return true;
            }
            else
            {
                return false;
            }
        }

#endif
    }

    [System.Serializable]
    public struct LocalizationData
    {
        public enum ItemType { StringTable, DialogueDatabase, Misc }
        public ItemType Type;

        [ShowIf("Type", ItemType.StringTable)]
#if UNITY_EDITOR
        public StringTableCollection TargetTable;
#endif

        [ShowIf("Type", ItemType.DialogueDatabase)]
        public DialogueDatabase TargetDialogueDatabase;

        [ShowIf("Type", ItemType.Misc)]
        public List<TextTable> TextTables;

        [PropertySpace(10)]
        [PropertyOrder(2)]
        [FolderPath(AbsolutePath = true)]
        [InfoBox("The path of the CSV file to import localization data from.")]
        public string InputCSVPath;

        [Button("Import From CSV")]
        [PropertyOrder(2)]
        public void Import()
        {
#if UNITY_EDITOR
            bool result = ImportFromCSV(InputCSVPath);

            if (result)
            {
                EditorUtility.DisplayDialog("It's Done", $"Imported CSV from {InputCSVPath}", "OK");
            }
#endif
        }

        [PropertySpace(10)]
        [PropertyOrder(3)]
        [FolderPath(AbsolutePath = true)]
        [InfoBox("The path of the CSV file to export localization data to.")]
        public string OutputCSVPath;

        [Button("Export To CSV")]
        [PropertyOrder(3)]
        public void Export()
        {
#if UNITY_EDITOR
            bool result = ExportToCSV();

            if (result)
            {
                EditorUtility.DisplayDialog("It's Done", $"Exported CSV to {OutputCSVPath}", "OK");
            }
#endif
        }

        public string Name { get => GetName(); }

        public string GetName()
        {
#if UNITY_EDITOR
            if (Type == ItemType.StringTable && TargetTable != null)
            {
                return TargetTable.name;
            }
#endif
            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                return TargetDialogueDatabase.name;
            }

            if (Type == ItemType.Misc)
            {
                return "Text Table List";
            }

            return "New Localized Entry";
        }

#if UNITY_EDITOR

        public bool ImportFromCSV(string InFolderPath = "")
        {
            string InFilePath = "";

            if (string.IsNullOrEmpty(InFolderPath))
            {
                if (TargetTable)
                {
                    InFolderPath = Application.dataPath + "/Data Assets/LocalizationSettings/CSV";
                }
                else
                {
                    InFolderPath = InputCSVPath;
                }
            }

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(InFolderPath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Invalid path. Cannot Import.", "OK");
                return false;
            }

            if (Type == ItemType.StringTable && TargetTable != null)
            {
                InFilePath = Path.Combine(InFolderPath, "Localization_" + Name + ".csv");

                if (!File.Exists(InFilePath))
                {
                    return false;
                }

                var stream = new StreamReader(InFilePath, System.Text.Encoding.UTF8);
                Csv.ImportInto(stream, TargetTable);

                foreach (var Table in TargetTable.Tables)
                {
                    EditorUtility.SetDirty(Table.asset);
                }

                stream.Close();
                return true;
            }
#endif

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                if (CheckAssetState(TargetDialogueDatabase))
                {
                    ImportDialogueDatabaseCSV(InFolderPath, Name);
                    return true;
                }

                return false;
            }

            if (Type == ItemType.Misc && TextTables != null)
            {
                bool bResult = false;

                foreach (var Table in TextTables)
                {
                    if (CheckAssetState(Table))
                    {
                        string filename = Path.Combine(InFolderPath, Table.name + ".csv");
                        if (File.Exists(filename))
                        {
                            var data = CSVUtility.ReadCSVFile(filename, PixelCrushers.EncodingType.UTF8);

                            if (data == null || data.Count < 1 || data[0].Count < 2) continue;
                            var fieldList = new List<string>();
                            var firstCell = data[0][0];

                            // All-in-one file:
                            for (int x = 1; x < data[0].Count; x++)
                            {
                                var language = data[0][x];
                                if (string.IsNullOrEmpty(language)) continue;
                                if (!Table.HasLanguage(language)) Table.AddLanguage(language);
                                for (int y = 1; y < data.Count; y++)
                                {
                                    var field = data[y][0];
                                    if (string.IsNullOrEmpty(field)) continue;
                                    if (x == 1) fieldList.Add(field);
                                    if (!Table.HasField(field)) Table.AddField(field);
                                    if ((0 <= y && y < data.Count) && (0 <= x && x < data[y].Count))
                                    {

                                        Table.SetFieldTextForLanguage(field, language, data[y][x]);
                                    }
                                }
                            }

                            Table.ReorderFields(fieldList);
                            Table.OnBeforeSerialize();
                            EditorUtility.SetDirty(Table);
                            bResult = true;
                        }
                    }
                }

                return bResult;
            }

            return false;
        }


        public bool ExportToCSV(string InFolderPath = "")
        {
            if(string.IsNullOrEmpty(InFolderPath))
            {
                if (TargetTable)
                {
                    InFolderPath = Application.dataPath + "/Data Assets/LocalizationSettings/CSV";
                }
                else
                {
                    InFolderPath = OutputCSVPath;
                }
            }

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(InFolderPath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Invalid path. Cannot export.", "OK");
                return false;
            }

            if (Type == ItemType.StringTable && TargetTable != null)
            {
                var file = Path.Combine(InFolderPath, "Localization_" + Name + ".csv");
                var stream = new StreamWriter(file, false, System.Text.Encoding.UTF8);
                Csv.Export(stream, TargetTable);
                stream.Close();
            }
#endif

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                ExportDialogueDatabaseCSV(InFolderPath, Name);
            }

            if(Type == ItemType.Misc && TextTables != null)
            {
                foreach(var Table in TextTables)
                {
                    // All in one file:
                    var content = new List<List<string>>();
                    var languageIDs = new List<int>();

                    // Heading rows:
                    var row = new List<string>();
                    content.Add(row);
                    row.Add("Field");
                    foreach (var kvp in Table.languages)
                    {
                        var language = kvp.Key;
                        var languageID = kvp.Value;
                        languageIDs.Add(languageID);
                        row.Add(language);
                    }

                    // One row per field:
                    foreach (var kvp in Table.fields)
                    {
                        var field = kvp.Value;
                        row = new List<string>();
                        content.Add(row);
                        row.Add(field.fieldName);
                        for (int i = 0; i < languageIDs.Count; i++)
                        {
                            var languageID = languageIDs[i];
                            var value = field.GetTextForLanguage(languageID);
                            row.Add(value);
                        }
                    }

                    CSVUtility.WriteCSVFile(content, Path.Combine(InFolderPath, Table.name + ".csv"), PixelCrushers.EncodingType.UTF8);
                }
            }

            return true;
        }


        public void CorrectLanguages(List<Locale> AvailableLocales, Template template)
        {
            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                if (CheckAssetState(TargetDialogueDatabase))
                {
                    ApplyLocaleTemplate(TargetDialogueDatabase.conversations, template.dialogueEntryFields, AvailableLocales);
                    EditorUtility.SetDirty(TargetDialogueDatabase);
                }
            }

            if(Type == ItemType.Misc && TextTables != null)
            {
                foreach(var Table in TextTables)
                {
                    if (CheckAssetState(Table))
                    {
                        bool bDirty = false;

                        foreach(var Locale in AvailableLocales)
                        {
                            if (!Table.HasLanguage(Locale.Identifier.Code))
                            {
                                Table.AddLanguage(Locale.Identifier.Code);
                                bDirty = true;
                            }
                        }

                        if(bDirty)
                            EditorUtility.SetDirty(Table);
                    }
                }
            }
        }


        private void ExportDialogueDatabaseCSV(string path, string name)
        {
            try
            {
                List<string> Locales = new List<string>();
                Template template = TemplateTools.LoadFromEditorPrefs();

                for (int i = template.dialogueEntryFields.Count - 1; i >= 0; --i)
                {
                    if (template.dialogueEntryFields[i].type == FieldType.Localization)
                    {
                        Locales.Add(template.dialogueEntryFields[i].title);
                    }
                }

                var numLanguages = Locales.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;

                    EditorUtility.DisplayProgressBar("Exporting Localization CSV", "Exporting CSV files for " + TargetDialogueDatabase.name, progress);

                    // Write Dialogue_LN.csv file:
                    var filename = path + "/" + name + "_" + Locales[i] + ".csv";

                    using (var file = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                    {
                        var orderedFields = new string[] { "Dialogue Text", Locales[i], "Menu Text", "Menu Text " + Locales[i], "Description" };
                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                            "Conversation ID",
                            "Entry ID",
                            "Original Text",
                            "Translated Text [" + Locales[i] + "]",
                            "Original Menu",
                            "Translated Menu [" + Locales[i] + "]",
                            "Description");

                        foreach (var c in TargetDialogueDatabase.conversations)
                        {
                            foreach (var de in c.dialogueEntries)
                            {
                                var fields = new List<string>();
                                foreach (string s in orderedFields)
                                {
                                    var f = de.fields.Find(x => x.title == s);
                                    fields.Add((f != null) ? f.value : string.Empty);
                                }

                                var sb = new StringBuilder();
                                sb.AppendFormat("{0},{1}", c.id, de.id);
                                foreach (string value in fields)
                                    sb.AppendFormat(",{0}", WrapCSVValue(value));
                                file.WriteLine(sb.ToString());
                            }
                        }
                        file.Close();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private void ImportDialogueDatabaseCSV(string path, string name)
        {
            try
            {
                List<string> Locales = new List<string>();
                Template template = TemplateTools.LoadFromEditorPrefs();

                for (int i = template.dialogueEntryFields.Count - 1; i >= 0; --i)
                {
                    if (template.dialogueEntryFields[i].type == FieldType.Localization)
                    {
                        Locales.Add(template.dialogueEntryFields[i].title);
                    }
                }

                var numLanguages = Locales.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;

                    EditorUtility.DisplayProgressBar("Importing Localization CSV", "Importing CSV files for " + TargetDialogueDatabase.name, progress);

                    // Read Dialogue_LN.csv file:
                    var filename = path + "/" + name + "_" + Locales[i] + ".csv";

                    if (File.Exists(filename))
                    {
                        var lines = ReadDialogueCSV(filename);
                        CombineMultilineCSVSourceLines(lines);

                        for (int j = 2; j < lines.Count; j++)
                        {
                            var columns = GetCSVColumnsFromLine(lines[j]);
                            if (columns.Length < 6)
                            {
                                Debug.LogError(filename + ":" + (j + 1) + " Invalid line: " + lines[j]);
                            }
                            else
                            {
                                var conversationID = PixelCrushers.DialogueSystem.Tools.StringToInt(columns[0]);
                                var entryID = PixelCrushers.DialogueSystem.Tools.StringToInt(columns[1]);
                                var entry = TargetDialogueDatabase.GetDialogueEntry(conversationID, entryID);

                                if (entry == null)
                                {
                                    Debug.LogError(filename + ":" + (j + 1) + " Database doesn't contain conversation " + conversationID + " dialogue entry " + entryID);
                                }
                                else
                                {
                                    Field.SetValue(entry.fields, Locales[i], columns[3], FieldType.Localization);
                                    Field.SetValue(entry.fields, "Menu Text " + Locales[i], columns[5], FieldType.Localization);

                                    // Check if we also need to import updated main text.
                                    if (Locales[i] == "en")
                                    {
                                        entry.DialogueText = columns[2];
                                        entry.MenuText = columns[4];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private string WrapCSVValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            string s2 = s.Contains("\n") ? s.Replace("\n", "\\n") : s;
            if (s2.Contains("\r")) s2 = s2.Replace("\r", "\\r");
            if (s2.Contains(",") || s2.Contains("\""))
            {
                return "\"" + s2.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return s2;
            }
        }


        private List<string> ReadDialogueCSV(string filename)
        {
            var lines = new List<string>();
            StreamReader sr = new StreamReader(filename, new UTF8Encoding(true));
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                lines.Add(line.TrimEnd());
            }
            sr.Close();
            return lines;
        }


        private void CombineMultilineCSVSourceLines(List<string> sourceLines)
        {
            int lineNum = 0;
            int safeguard = 0;
            int MaxIterations = 999999;
            while ((lineNum < sourceLines.Count) && (safeguard < MaxIterations))
            {
                safeguard++;
                string line = sourceLines[lineNum];
                if (line == null)
                {
                    sourceLines.RemoveAt(lineNum);
                }
                else
                {
                    bool terminated = true;
                    char previousChar = (char)0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        char currentChar = line[i];
                        bool isQuote = (currentChar == '"') && (previousChar != '\\');
                        if (isQuote) terminated = !terminated;
                        previousChar = currentChar;
                    }
                    if (terminated || (lineNum + 1) >= sourceLines.Count)
                    {
                        if (!terminated) sourceLines[lineNum] = line + '"';
                        lineNum++;
                    }
                    else
                    {
                        sourceLines[lineNum] = line + "\\n" + sourceLines[lineNum + 1];
                        sourceLines.RemoveAt(lineNum + 1);
                    }
                }
            }
        }


        private string[] GetCSVColumnsFromLine(string line)
        {
            Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)");
            List<string> values = new List<string>();
            foreach (Match match in csvSplit.Matches(line))
            {
                values.Add(UnwrapCSVValue(match.Value.TrimStart(',')));
            }
            return values.ToArray();
        }

        private string UnwrapCSVValue(string s)
        {
            string s2 = s.Replace("\\n", "\n").Replace("\\r", "\r");
            if (s2.StartsWith("\"") && s2.EndsWith("\""))
            {
                s2 = s2.Substring(1, s2.Length - 2).Replace("\"\"", "\"");
            }
            return s2;
        }


        private void ApplyLocaleTemplate(List<Conversation> assets, List<Field> templateFields, List<Locale> AvailableLocales)
        {
            foreach (var conversation in TargetDialogueDatabase.conversations)
            {
                foreach (var entry in conversation.dialogueEntries)
                {
                    if (entry.fields == null || templateFields == null) continue;

                    for(int i=entry.fields.Count - 1; i>=0; --i)
                    {
                        if (entry.fields[i].type == FieldType.Localization && 
                            AvailableLocales.Find((l) => l.Identifier.Code == entry.fields[i].title) == null)
                        {
                            entry.fields.RemoveAt(i);
                        }
                    }

                    foreach (Field templateField in templateFields)
                    {
                        if (!string.IsNullOrEmpty(templateField.title))
                        {
                            var field = Field.Lookup(entry.fields, templateField.title);
                            if (field != null)
                            {
                                // Variables' Initial Value should never be applied from template.
                                var shouldApplyTemplateFieldType = (field.type != templateField.type) && !string.Equals(field.title, "Initial Value");
                                if (shouldApplyTemplateFieldType)
                                {
                                    field.type = templateField.type;
                                    field.typeString = string.Empty;
                                }
                            }
                            else
                            {
                                entry.fields.Add(new Field(templateField));
                            } 
                        }
                    }
                }
            }
        }


        public void IsAvailableForEdit()
        {
            if (Type == ItemType.StringTable && TargetTable != null)
            {
                foreach (var Table in TargetTable.Tables)
                {
                    CheckAssetState(Table.asset);
                }
            }

            if (Type == ItemType.DialogueDatabase && TargetDialogueDatabase != null)
            {
                CheckAssetState(TargetDialogueDatabase);
            }
        }


        private bool CheckAssetState(Object Item)
        {
            if (!Provider.isActive || Provider.onlineState == OnlineState.Offline || !Provider.hasCheckoutSupport) return true;

            UnityEditor.VersionControl.Asset asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(Item));

            UnityEditor.VersionControl.Task statusTask = UnityEditor.VersionControl.Provider.Status(asset);
            statusTask.Wait();

            if (statusTask.assetList[0].state.HasFlag(UnityEditor.VersionControl.Asset.States.CheckedOutLocal))
            {
                return true;
            }
            else if (Provider.CheckoutIsValid(statusTask.assetList[0]))
            {
                Provider.Checkout(statusTask.assetList[0], CheckoutMode.Asset);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endif
    }


    [InfoBox("A list of locales available for localized assets.\n\n" +
        "As String Tables automatically update when a new locale is added, this is mostly used for PixelCrushers Assets.")]
    public List<Locale> AvailableLocales;

    [PropertySpace(20)]
    [Title("ONE-CLICK LOCALIZATION")]
    [ListDrawerSettings(DraggableItems = false, NumberOfItemsPerPage = 15,
        Expanded = true, ListElementLabelName = "Name")]
    public List<OneClickLocalizationData> OneClickLocalizedAssets;

    [PropertySpace(20)]
    [Title("LOCALIZED ASSETS")]
    [ListDrawerSettings(DraggableItems = false, NumberOfItemsPerPage = 15, 
        Expanded = true, ListElementLabelName = "Name")]
    public List<LocalizationData> LocalizedAssets;

    public static string FixHTMLTags(string englishText, string targetText)
    {
        string regex = @"(<|>|\{|\})";
        string[] englishSplit = Regex.Split(englishText, regex);
        string[] targetSplit = Regex.Split(targetText, regex);

        StringBuilder SB = new StringBuilder();
        SB.Append(targetSplit[0]);
        // goofy ah algorithm
        int engIndex = 0;
        int tarIndex = 0;
        while (engIndex < englishSplit.Length && tarIndex < targetSplit.Length)
        {
            bool bMatch = false;
            if (englishSplit[engIndex] == "<")
            {
                while (tarIndex < targetSplit.Length)
                {
                    if (targetSplit[tarIndex] == "<")
                    {
                        bMatch = true;
                        break;
                    }
                    SB.Append(targetSplit[tarIndex]);
                    ++tarIndex;
                }
            }
            else if (englishSplit[engIndex] == "{")
            {
                while (tarIndex < targetSplit.Length)
                {
                    if (targetSplit[tarIndex] == "{")
                    {
                        bMatch = true;
                        break;
                    }
                    SB.Append(targetSplit[tarIndex]);
                    ++tarIndex;
                }
            }

            ++engIndex;
            ++tarIndex;

            if (bMatch)
            {
                targetSplit[tarIndex] = englishSplit[engIndex];
            }
            if (tarIndex < targetSplit.Length)
            {
                SB.Append(targetSplit[tarIndex]);
            }
        }

        return SB.ToString();
    }

    #region Editor Functions
#if UNITY_EDITOR
    [PropertySpace(20)]
    [Title("BULK OPERATIONS")]
    [GUIColor(0.44f, 0.85f, 0.48f, 1), Button("Export All to CSV")]
    public void ExportAll()
    {
        var path = EditorUtility.SaveFolderPanel("Select a Target Folder", "", "");

        if(path.Length > 0)
        {
            foreach (var Asset in LocalizedAssets)
            {
                Asset.ExportToCSV(path);
            }

            EditorUtility.DisplayDialog("Export Complete", "All Assets exported to CSV files.", "OK");
        }
    }


    [PropertySpace(10)]
    [GUIColor(0.85f, 0.44f, 0.48f, 1), Button("Import All from CSV")]
    public void ImportAll()
    {
        var path = EditorUtility.SaveFolderPanel("Select a Target Folder", "", "");

        if (path.Length > 0)
        {
            if (EditorUtility.DisplayDialog("Hold Up", "This will attempt to import data into all Localized Assets. " +
                "All data in these files will be overwritten if possible.\n\nAre you sure you wish to continue?", "Proceed", "Cancel"))
            {
                string failed = "Note: The following items were not able to be checked out and were not modified by the import.\n\n";
                bool bFailed = false;

                foreach (var Asset in LocalizedAssets)
                {
                    bool bImported = Asset.ImportFromCSV(path);

                    if (!bImported)
                    {
                        bFailed = true;
                        failed += "\t" + Asset.Name + "\n";
                    }
                }

                if(bFailed)
                    HRItemAssetCheckoutEditor.Init(failed);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Import Complete", "All Assets imported from CSV files.", "OK");
            }
        }
    }


    [PropertySpace(10)]
    [GUIColor(0.85f, 0.44f, 0.48f, 1), Button("Correct PixelCrushers Locales")]
    public void CorrectLanguages()
    {
        Template template = TemplateTools.LoadFromEditorPrefs();

        for (int i = template.dialogueEntryFields.Count - 1; i >= 0; --i)
        {
            if (template.dialogueEntryFields[i].type == FieldType.Localization)
            {
                template.dialogueEntryFields.RemoveAt(i);
            }
        }

        foreach (var Locale in AvailableLocales)
        {
            var DialogueLocale = new Field();
            DialogueLocale.title = Locale.Identifier.Code;
            DialogueLocale.type = FieldType.Localization;

            template.dialogueEntryFields.Add(DialogueLocale);
        }

        foreach (var Asset in LocalizedAssets)
        {
            Asset.CorrectLanguages(AvailableLocales, template);
        }

        TemplateTools.SaveToEditorPrefs(template);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Complete", "Locales have been corrected.", "OK");
    }


    [PropertySpace(20)]
    [Title("LEGACY OPTIONS")]
    [GUIColor(0.85f, 0.44f, 0.48f, 1), Button("Find all Dialogue Databases")]
    public void FindAllDialogueDBs()
    {
        var Databases = FindAssetsByType<DialogueDatabase>();

        foreach(var Asset in Databases)
        {
            LocalizationData LocalizedAsset = new LocalizationData();
            LocalizedAsset.TargetDialogueDatabase = Asset;
            LocalizedAsset.Type = LocalizationData.ItemType.DialogueDatabase;

            LocalizedAssets.Add(LocalizedAsset);
        }

        EditorUtility.DisplayDialog("Complete", "All Dialogue Databases have been added.", "OK");
    }

    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }
        return assets;
    }

#endif
    #endregion
}
