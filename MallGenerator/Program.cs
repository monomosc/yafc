using YAFC.Blueprints;
using YAFC.Parser;
using YAFC.Model;
using System.Text.Json;
using System.Text.Json.Serialization;
using YAFC.Blueprints.Generators;

internal class Program
{
       private static void Main(string[] args)
    {

        var settings_file = File.ReadAllBytes("factorio_conf.json");
        var settings = JsonSerializer.Deserialize<IDictionary<string, string>>(settings_file)!;
        var progress = new Progress<(string, string)>();
        var errorCollector = new YAFC.Model.ErrorCollector();
        Project fullProject = YAFC.Parser.FactorioDataSource.Parse(
            settings["factorio_data"],
            settings["mod_folder"],
            settings["project_file"],
            false,
            progress,
            errorCollector,
            "en");
        Project.current = fullProject;

        var items = new [] {"coal", "coke"};

        var station = new TrainStationGenerator(Database.items.all.Where(i => items.Contains(i.name)).ToArray()).GenerateTrainStation("coal");
        var bp = new BlueprintString() { blueprint = station};
        Console.WriteLine(bp.ToBpString());
        
    }
}