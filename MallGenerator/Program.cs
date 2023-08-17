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

        var progress = new Progress<(string, string)>();
        var errorCollector = new YAFC.Model.ErrorCollector();
        Project fullProject = YAFC.Parser.FactorioDataSource.Parse(
            @"C:\Program Files (x86)\Steam/steamApps\common\Factorio\data",
            @"C:\Users\Hoang\AppData\Roaming\Factorio\mods",
            @"C:\monomo\pyno.yafc",
            false,
            progress,
            errorCollector,
            "en");
        Project.current = fullProject;

        var items = new [] {"coal"};

        var station = new TrainStationGenerator(Database.items.all.Where(i => items.Contains(i.name)).ToArray()).GenerateTrainStation("coal");
        var bp = new BlueprintString() { blueprint = station};
        Console.WriteLine(bp.ToBpString());
        
    }
}