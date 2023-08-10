using YAFC.Blueprints;
using YAFC.Parser;
using YAFC.Model;
using System.Text.Json;

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

        string interestingRecipe = "creature-chamber-mk02";

        var localBp = MallGenerator.GenerateMallBlueprintForRecipe(interestingRecipe);
        
        Console.WriteLine(JsonSerializer.Serialize(localBp, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(localBp.ToBpString());
    }
}