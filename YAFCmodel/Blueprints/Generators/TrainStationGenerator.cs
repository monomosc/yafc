using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YAFC.Model;

namespace YAFC.Blueprints.Generators;


public class ItemTrainStationGenerator
{

    public IEnumerable<Goods> Items { get; init; }

    public ItemTrainStationGenerator(IEnumerable<Goods> items)
    {
        Items = items;
    }

    public Blueprint GenerateTrainStation(string requesterFor)
    {


        var bp = new Blueprint();
        var station = new BlueprintEntity();
        if (Items.First().type == "Fluid")
        {
            if (Items.Count() > 1)
            {
                Project.current.preferences.sourceResources.Clear();
                throw new InvalidOperationException("cannot request more than 1 fluid");
            }
            var file = File.ReadAllBytes("fluid_train_station_blueprint_base.json");
            var bps = JsonSerializer.Deserialize<BlueprintString>(file);
            bp = bps.blueprint;
            var request_combinator = bp.entities.Where(e => e.name == "constant-combinator").Single();
            station = bp.entities.Where(e => e.name == "logistic-train-stop").Single();
            var item = Items.First() as Fluid;
            request_combinator.controlBehavior.filters.Add(new BlueprintControlFilter()
            {
                index = 1,
                signal = new BlueprintSignal()
                {
                    type = "fluid",
                    name = item.name.Split("@")[0],
                },
                count = -150000,
            });
        }
        else
        {
            var file = File.ReadAllBytes("train_station_blueprint_base.json");
            var bps = JsonSerializer.Deserialize<BlueprintString>(file);
            bp = bps.blueprint;
            var request_combinator = bp.entities.Where(e => e.name == "constant-combinator").Single();
            station = bp.entities.Where(e => e.name == "logistic-train-stop").Single();
            var loaders = bp.entities.Where(e => e.name.Contains("loader")).ToList();
            //loaders 0,3 are request #1, loaders 1,2 are request #2


            foreach (var (item, itemIndex) in Items.Enumerate())
            {
                if (itemIndex > 6)
                {
                    throw new InvalidOperationException("Too many items");
                }
                if (itemIndex == 0)
                {
                    loaders[0].filters[0].name = item.name;
                    loaders[3].filters[0].name = item.name;
                }
                if (itemIndex == 1)
                {
                    loaders[1].filters[0].name = item.name;
                    loaders[2].filters[0].name = item.name;
                }
                if (request_combinator.controlBehavior.filters.Count < 6 + itemIndex)
                {
                    request_combinator.controlBehavior.filters.Add(new BlueprintControlFilter()
                    {
                        index = 6 + itemIndex,
                        signal = new BlueprintSignal()
                        {
                            type = "item",
                            name = item.name,
                        },
                        count = -1 * ((Item)item).stackSize * 100,
                    });
                }
                else
                {
                    throw new InvalidOperationException("Unknown Origin Blueprint State");
                }
            }
        }

        var stationName = new StringBuilder();
        foreach (var item in Items)
        {
            if (item is Item)
            {
                stationName.Append($"[item={item.name}]");
            }
            else
            {
                stationName.Append($"[fluid={item.name.Split("@")[0]}]");

            }
        }
        stationName.Append($" Requester for {requesterFor}");
        station.station = stationName.ToString();
        bp.label = stationName.ToString();
        //clear source items
        Project.current.preferences.sourceResources.Clear();

        return bp;
    }
}