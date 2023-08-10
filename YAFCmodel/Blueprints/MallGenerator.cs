
using System.Linq;
using YAFC.Model;

namespace YAFC.Blueprints;

public static class MallGenerator
{
    static string somerandomprodstring = "0eNqVk9FOwzAMRf8lzw1au3WF/gpCKGu9YpE4IXEnJrR/x+22bkAH4imKcn3ujZ18qI3tIUQkVvWHSmSCZq+7iO2wf1f1Q6b2qq4OmcLGU1L1o8iwI2MHAe8DqFohg1OZIuOGnenZO8PQ6q1p2Me9dq+LXA0IakGY+eEpU0CMjHAkjpv9M/VuA1EEf7EyFXySck+nmOVdOQZd3pXiE6HBMVgHnl8gOmN1sIb4nOSHYzE5JjbNq0ZKEFlOfjjlJ6dydGpRvI6nxQx2OWGt7zAxNrp5gcQ6mJRwBzpEv8N2zmfxxWdjhFbOOKxuOUR462X9Db0+NWsUPm/Rivo4j/OgJjhYuWYUuENC6nQb0VohN74fnk65GKKdqopvVZ6GUBibHvlSUiyua5ZX/QcYp8VwEedfxJc7A3VIoHu6JkuYp5lOlf8e8fr7iNcz2PUFK8/MamtcuN3ySpAzkGqCOGixd3rqd/AWbicccfKZxg9YX33mTO1klsd3eZ+vqoeiqlarMi/yw+ETXMJOLg==";

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