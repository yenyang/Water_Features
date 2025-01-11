using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Game;
using Game.Net;
using Game.UI.InGame;
using Water_Features;
using Water_Features.Localization;
using Water_Features.Settings;
/*
using Colossal;
using Tree_Controller;
using Tree_Controller.Settings;

var setting = new TreeControllerSettings(new TreeControllerMod());

var locale = new LocaleEN(setting);
var e = new Dictionary<string, string>(
    locale.ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()));
var str = JsonSerializer.Serialize(e, new JsonSerializerOptions()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});

File.WriteAllText("C:\\Users\\TJ\\source\\repos\\Tree_Controller\\Tree_Controller\\UI\\src\\mods\\lang\\en-US.json", str);

*/
string[] languages = new string[]
{
    "de-DE",
    "es-ES",
    "fr-FR",
    "it-IT",
    "ko-KR",
    "pl-PL",
    "ru-RU",
    "pt-PT",
    "pt-BR",
    "zh-HANS",
    "zh-HANT",
};
foreach (string lang in languages)
{
    var file = $"C:\\Users\\TJ\\source\\repos\\Water_Features\\Water_Features\\l10n\\{lang}.csv";
    if (File.Exists(file))
    {
        string[] lines = File.ReadAllLines(file);
        WaterFeaturesSettings waterFeaturesSettings = new WaterFeaturesSettings(new WaterFeaturesMod());
        Console.Write("file exists");
        Dictionary<string, string> translations = new();

        // Parsing fields.
        StringBuilder builder = new();
        string key = null;
        bool parsingKey = true;
        bool quoting = false;

        // Iterate through each line of file.
        foreach (string line in lines)
        {
            // Read next line of file, stopping when we've reached the end.
            if (line is null)
            {
                break;
            }

            // Skip empty lines.
            if (string.IsNullOrWhiteSpace(line) || line.Length == 0)
            {
                continue;
            }

            // Iterate through each character in line.
            for (int i = 0; i < line.Length; ++i)
            {
                // Local reference.
                char thisChar = line[i];

                // Are we parsing quoted text?
                if (quoting)
                {
                    // Is this character a quote?
                    if (thisChar == '"')
                    {
                        // Is this a double quote?
                        int j = i + 1;
                        if (j < line.Length && line[j] == '"')
                        {
                            // Yes - append single quote to output and continue.
                            i = j;
                            builder.Append('"');
                            continue;
                        }

                        // It's a single quote - stop quoting here.
                        quoting = false;

                        // If we're parsing a value, this is also the end of parsing this line (discard everything else).
                        if (!parsingKey)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Not a closing quote - just append character to our parsed value.
                        builder.Append(thisChar);
                    }
                }
                else
                {
                    // Not parsing quoted text - is this a tab or comma?
                    if (thisChar == '\t' || thisChar == ',')
                    {
                        // Tab or comma - if we're parsing a value, this is also the end of parsing this line (discard everything else).
                        if (!parsingKey)
                        {
                            break;
                        }

                        // Otherwise, what we've parsed is the key - store value and reset the builder.
                        parsingKey = false;

                        key = Localization.UnpackOptionsKey(builder.ToString(), waterFeaturesSettings);
                        builder.Length = 0;
                    }
                    else if (thisChar == '"' & builder.Length == 0)
                    {
                        // If this is a quotation mark at the start of a field (immediately after comma), then we start parsing this as quoted text.
                        quoting = true;
                    }
                    else
                    {
                        // Otherwise, just append character to our parsed string.
                        builder.Append(thisChar);
                    }
                }
            }

            // Finished looping through chars - are we still parsing quoted text?
            if (quoting)
            {
                // Still quoting; continue, after adding a newline.
                builder.AppendLine();
                continue;
            }

            // If we got here, then we've reached the end of the line - reset parsing status.
            parsingKey = true;

            // Was key empty?
            if (string.IsNullOrWhiteSpace(key))
            {
                Console.WriteLine($" - Invalid key in line {line}");
                continue;
            }

            // Otherwise, did we get two delimited fields (key and value?)
            if (builder.Length == 0)
            {
                Console.WriteLine($" - No value field found in line {line}");
                continue;
            }

            // Otherwise, all good.
            // Convert value to string and reset builder.
            string value = builder.ToString();
            builder.Length = 0;

            // Check for duplicates.
            if (!translations.ContainsKey(key))
            {
                translations.Add(key, value);
                Console.WriteLine($"[LoadTranslations]  Adding key: {key} value: {value} to ");
            }
            else
            {
                Console.WriteLine($" - Ignoring duplicate translation key {key} in embedded file ");
            }
        }
        var str = JsonSerializer.Serialize(translations, new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\Water_Features\\Water_Features\\lang\\{lang}.json", str);
        Console.Write(lang.ToString());
    }
}