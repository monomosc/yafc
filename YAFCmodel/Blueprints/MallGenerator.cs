
using System.Collections.Generic;
using System.Linq;
using YAFC.Model;

namespace YAFC.Blueprints;

public static class MallGenerator
{
    static string somerandomprodstring = "0eNqVk+tuqzAQhN/Fv3EVSAgtr3IUVY7Z0lV9O75EjSrevWtIIW1Jq/4Cy7PfDB7zxo4qgfNoImvfWDDC8Wh577HL61fWPhTszNpmKBhKawJr/5EMeyNUFsSzA9YyjKBZwYzQeSVStFpE6PiTkNH6M9cvm4plhOmAmOVwKBiYiBFhIo6L86NJ+gieBL+xCuZsoHFrLjHru3oMur2rsw/lCXnHedslGfFEdK7pXRG0Gobim2U1W4Yo5AtHE8BH2vlmVV6s6tGqQw9y2q1WsNsZq2yPIaLk8hlC5E6EgCfglPCE3ZrP5pPPURCtXnHY3XLw8D/R8yf0fkRfhI9PqEg9FfLR1AwHRZ/pCa7RoOl551EpIkub8t2pNznaZar6MmVNDoVeJozLSLW5ntlenT+A4k5R64u4/CRevhlMjwZ4MtdkCnNYOan6zxXvv1a8X8HuZ6yGDpPm81E5q+A2vCH4Cq5ZUmqhFFdCu9sNjpDDdONpZPmbC3aiLqd7eV/umoeqaXa7uqzKYXgHRBRPTQ==";

    public static BlueprintString GenerateMallBlueprintForRecipe(string interestingRecipe)
    {
        var allRecipes = Database.recipes.all
           .Where(r => r.crafters.Any(x => x.name.Contains("automated-factory")))
           .Where(r => r.mainProduct != null && r.mainProduct is Item resultItem)
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
        var fac = localBpParsed.blueprint.entities[0];
        fac.items = new Dictionary<string, int>();
        var prodMod = Database.items.all.First(i => i.name == "productivity-module");
        if (Database.recipes.all.First(r => r.name == interestingRecipe).CanAcceptModule(prodMod)) {
            var facData = Database.entities.all.First(e => e.name == fac.name) as EntityWithModules;
            fac.items.Add(prodMod.name, facData.moduleSlots);
        }
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