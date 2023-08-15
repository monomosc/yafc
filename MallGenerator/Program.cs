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
            @"C:\Users\morit\AppData\Roaming\Factorio\mods",
            @"C:\monomo\pyno.yafc",
            false,
            progress,
            errorCollector,
            "en");

        var page = fullProject.pages.Where(p => p.name == "very_simple").Single();
        var table = (ProductionTable)page.content;
        var gen = new RowGenerator(table.recipes[0]);
        var bp = new BlueprintString() { blueprint = gen.GenerateRow() };
        Console.WriteLine(bp.ToJson());
        Console.WriteLine(bp.ToBpString());

        
    }
}