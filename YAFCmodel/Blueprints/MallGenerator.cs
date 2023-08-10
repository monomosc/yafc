
using System.Linq;
using YAFC.Model;

namespace YAFC.Blueprints;

public static class MallGenerator
{
    static string somerandomprodstring = "0eNqVVOmOmzAQfhf/xqtAQuiivkkVIQcmZLS+apuo0Yp379gkhN0lPf6ALH/XHPI7O8oBrEMdWP3OvBaWB8N7h108/2L1a8aurK7GjGFrtGf1D4Jhr4WMgHC1wGqGARTLmBYqnsQQjBIBOn4SbTDuytXbJmdRQndAmvl4yBjogAFhUkyHa6MHdQRHgL9pZcwaT3SjbzHLlzIF3b6U5OOgxRSsBxPO4JSQ3Eqhwz3JF8didpSmRx+w5e0ZfOBWeI8X4NaZC3YE/WK9uVmXyZqapKGN1z7e5/HjoFtWGXu7HQ8joY+CvMuVPNs5jw+ifeOoPbiwZp9/sO/QTe6pJAoTnJHNEc7igsZFSouuHTA0dNfNOnG0QcQlKDeRpqxwIkQC+x4Hdycp00FjTo2xQPeJvP33ootY9Eqxu2fNd/BzoP+fur6/DTwBmxNKQk87dV+2WRwkZXQkrlCj7nnnUEoW0w9T5THajVV8YhkdQ01deFCKzZKznBlA2rgAD3D+AfyoGXSPGvigl8oU5rDSqfJhQTstuRTKPu9NRb1ZEdn/927tP+/WfkW2mmUVdDgoPvfbGgnPxVNKehDSI1IvHqSMXWiW0y5/y3fVa1FVu12ZF/k4/gbew5OT";

    public static BlueprintString GenerateMallBlueprintForRecipe(string interestingRecipe)
    {
        var allRecipes = Database.recipes.all
           .Where(r => r.crafters.Any(x => x.name.Contains("automated-factory")))
           .Where(r => r.mainProduct != null && r.mainProduct is Item resultItem && resultItem.placeResult != null)
           .ToDictionary(r => r.name, recipe =>
           {
               var ingredients = recipe.ingredients;
               if (ingredients == null) return null;
               return new
               {
                   Recipe = recipe.name,
                   ingredients = ingredients.Select(i =>
                   {
                       if (i.goods is not Item item) return null;
                       return item.name;
                   }).Where(x => x != null).ToList(),
                   Product = recipe.mainProduct.name
               };
           });
        var localBpParsed = BlueprintString.FromBpString(somerandomprodstring);
        localBpParsed.blueprint.entities[0].recipe = interestingRecipe;
        localBpParsed.blueprint.label = interestingRecipe;
        var requester = localBpParsed.blueprint.entities.Where(e => e.name == "logistic-chest-requester").First();
        requester.requestFilters.Clear();
        var i = 1;
        foreach (var ingredient in allRecipes[interestingRecipe]?.ingredients)
        {
            var item = Database.items.all.First(i => i.name == ingredient);
            requester.requestFilters.Add(new BlueprintRequestFilter
            {
                name = ingredient,
                count = item.stackSize,
                index = i++
            });
        }


        return localBpParsed;
    }
}