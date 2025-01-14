using Colossal;
using Game.Modding;
using Newtonsoft.Json;
using Water_Features;
using Water_Features.Settings;

Console.WriteLine("Exporting en-US.json");

WaterFeaturesMod waterFeaturesMod = new WaterFeaturesMod();
WaterFeaturesSettings waterFeaturesSettings = new WaterFeaturesSettings(waterFeaturesMod);
var localeDict = new LocaleEN(waterFeaturesSettings).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
var str = JsonConvert.SerializeObject(localeDict, Newtonsoft.Json.Formatting.Indented);
try
{
    File.WriteAllText("C:\\Users\\TJ\\source\\repos\\Water_Features\\Water_Features\\UI\\src\\lang\\en-US.json", str);
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}