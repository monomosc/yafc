
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using YAFC.Blueprints;
using YAFC.Model;

namespace YAFC.Blueprints.Generators;

public class RowGenerator {
    

    public RowGenerator(RecipeRow page) {
        this.Row = page;
    }

    public RecipeRow Row { get; init; }

    public const float HALF_BELT_CAPACITY_PER_SECOND = 15;

    public Blueprint GenerateRow(string labelHeader = "") {
        var warnings = new List<string>();
        int ___cur_num = 1;
        var entityNumber = () => ___cur_num++;

        //calculate belts we need
        var numInputHalfBelts = CountInputHalfBelts(Row);
        var numOutputHalfBelts = CountOutputHalfBelts(Row);
        var fluidInputs = GetFluidInputs(Row);
        var fluidOutputs = GetFluidOutputs(Row);
        
        
        if (fluidInputs.Length > 1 || fluidOutputs.Count() > 1) {
            throw new NotImplementedException("Fluid Input/Output > 1 not implemented yet. (" + Row.recipe.name + ")");
        }
        if (fluidInputs.Any(x => (x.amount > 1000))) {
            warnings.Add("one of the fluid inputs is > 1000/s. check throughput");
        }
        if (fluidOutputs.Any(x => (x.amount > 1000))) {
            warnings.Add("one of the fluid outputs is > 1000/s. check throughput");
        }
        if (fluidInputs.Any() || fluidOutputs.Any()) {
            throw new NotImplementedException("Fluid Input/Output not implemented yet. (" + Row.recipe.name + ")");
        }
        if (numInputHalfBelts.Any(x => (x.Value > 1))) {
            warnings.Add("one of the input items needs more than one half belt. check throughput");
        }
        var bp = new Blueprint();
        var add = (BlueprintEntity e) => {
            e.index = entityNumber();
            bp.entities.Add(e);
        };
#region Single Factory
        var fac = new BlueprintEntity
        {
            name = Row.entity!.name,
            recipe = Row.recipe.name,
            position = BlueprintPosition.FromXY(0, 0),
            items = new Dictionary<string, int>()
        };
        add(fac);
        //TODO Module fac.items.Add(row.modules.Aggregate((a,b) => a.module.name + b.module.name), 1);

        var crafterDescriptor = Row.entity!;
        var entityDimensions = crafterDescriptor.size;


        
        var positionTopLeftOutside = fac.position - BlueprintPosition.FromXY((entityDimensions/2.0) + 0.5, ((entityDimensions/2.0) - 0.5));
        var positionTopRightOutside = fac.position + BlueprintPosition.FromXY((entityDimensions/2.0) + 0.5, -((entityDimensions/2.0) - 0.5));
        //TODO Implement half-belts ?!


        //input inserters
        int num = 0;
        foreach (var belt in numInputHalfBelts) {
            if (num == 0) {
                add(new BlueprintEntity() {
                    name = "stack-inserter",
                    position = positionTopLeftOutside,
                    direction = BlueprintDirection.RIGHT,
                });
            } else {
                add(new BlueprintEntity() {
                    name = "long-handed-inserter",
                    position = positionTopLeftOutside + BlueprintPosition.FromXY(0.0, num),
                    direction = BlueprintDirection.RIGHT,
                });
            }
            num++;
        }
        num = 0;
        //output inserters
        foreach (var belt in numOutputHalfBelts) {
            if (num == 0) {
                add(new BlueprintEntity() {
                    name = "stack-inserter",
                    position = positionTopRightOutside,
                    direction = BlueprintDirection.RIGHT,
                });
            } else {
                add(new BlueprintEntity() {
                    name = "long-handed-inserter",
                    position = positionTopRightOutside + BlueprintPosition.FromXY(0.0, num),
                    direction = BlueprintDirection.RIGHT,
                });
            }
            num++;
        }
        //place input belts, TODO Half belts
        var beltNum = 0;
        foreach (var belt in numInputHalfBelts) {
            for (int i = 0; i < entityDimensions; i++) {
                add(new BlueprintEntity() {
                    name = "transport-belt",
                    position = positionTopLeftOutside + BlueprintPosition.FromXY(-1.0, i) - BlueprintPosition.FromXY(beltNum*1.0, 0.0),
                    direction = BlueprintDirection.UP
                });
            }
            beltNum++;
        }
        beltNum = 0;
        //place output belts, TODO Half belts
        foreach (var belt in numOutputHalfBelts) {
            for (int i = 0; i < entityDimensions; i++) {
                add(new BlueprintEntity() {
                    name = "transport-belt",
                    position = positionTopRightOutside + BlueprintPosition.FromXY(1.0,i)  + BlueprintPosition.FromXY(1.0 * beltNum, 0.0),
                    direction = BlueprintDirection.UP
                });
            }
            beltNum++;
        }
#endregion

        //copy factory
        var bpCopy = JsonSerializer.Deserialize<Blueprint>(JsonSerializer.Serialize(bp));

        // attach factory n-1 times where n is the number of factories
        for (int i = 1; i < Math.Ceiling(Row.buildingCount); i++) {
            bp = bp.AttachBlueprint(bpCopy, BlueprintUtilities.BlueprintAttachDirection.Down);
        }


        //add warnings to bp
        if (warnings.Any()) {
            var warningText = warnings.Aggregate((a, b) => a + "\n" + b);
            bp.description = warningText;
        }
        bp.label = labelHeader + Row.recipe.name + " " + Row.recipesPerSecond + " Recipes/s";
        
        return bp;
    }

    private dynamic[] GetFluidOutputs(RecipeRow row)
    {
        return row.recipe.products.Where(p => p.goods.type == "Fluid").Select(x => {
            return new {
                name = x.goods.name,
                amount = x.amount * row.recipesPerSecond
            };
        }).ToArray();
    }

    private dynamic[] GetFluidInputs(RecipeRow row)
    {
        return row.recipe.ingredients.Where(p => p.goods.type == "Fluid").Select(x => {
            return new {
                name = x.goods.name,
                amount = x.amount * row.recipesPerSecond
            };
        }).ToArray();
    }

    private Dictionary<Product, int> CountOutputHalfBelts(RecipeRow row)
    {
        var result = new Dictionary<Product, int>();
        foreach (var product in row.recipe.products.Where(p => p.goods.type == "Item")) {
            var numHalfBeltsPerIngredient = product.amount / HALF_BELT_CAPACITY_PER_SECOND;
            result.Add(product, (int)Math.Ceiling(numHalfBeltsPerIngredient));
        }
        return result;
    }

    private Dictionary<Ingredient, int> CountInputHalfBelts(RecipeRow row) {
        var result = new Dictionary<Ingredient, int>();
        foreach (var ingredient in row.recipe.ingredients.Where(p => p.goods.type == "Item")) {
            var numHalfBeltsPerIngredient = ingredient.amount / HALF_BELT_CAPACITY_PER_SECOND;
            result.Add(ingredient, (int)Math.Ceiling(numHalfBeltsPerIngredient));
        }
        return result;
    }

}

