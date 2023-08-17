using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OperationsResearch.Pdlp;
using YAFC.Model;

namespace YAFC.Blueprints.Generators;


public class TrainStationGenerator
{

    public IList<Goods> Goods { get; init; }

    public TrainStationGenerator(IEnumerable<Goods> goods)
    {
        this.Goods = goods.ToList();
    }
    private static Blueprint GenerateItemTrainStation(IList<Item> items, string requesterFor)
    {
        var file = File.ReadAllBytes("train_station_blueprint_base.json");
        var bps = JsonSerializer.Deserialize<BlueprintString>(file);
        var bp = bps.blueprint;
        var request_combinator = bp.entities.Where(e => e.name == "constant-combinator").Single();
        
        var loaders = bp.entities.Where(e => e.name.Contains("loader")).ToList();
        //loaders 0,3 are request #1, loaders 1,2 are request #2


        foreach (var (item, itemIndex) in items.Enumerate())
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
                    count = -1 * item.stackSize * 100,
                });
            }
            else
            {
                throw new InvalidOperationException("Unknown Origin Blueprint State");
            }
        }
    }
    private static Blueprint GenerateFluidTrainStation(Fluid fluid, string requesterFor)
    {
        var file = File.ReadAllBytes("fluid_train_station_blueprint_base.json");
        var bps = JsonSerializer.Deserialize<BlueprintString>(file);
        var bp = bps.blueprint;
        var request_combinator = bp.entities.Where(e => e.name == "constant-combinator").Single();
        request_combinator.controlBehavior.filters.Add(new BlueprintControlFilter()
        {
            index = 1,
            signal = new BlueprintSignal()
            {
                type = "fluid",
                name = fluid.name.Split("@")[0],
            },
            count = -150000,
        });
        return bp;
    }

    public Blueprint GenerateTrainStation(string requesterFor)
    {
        var bp = new Blueprint();
        if (Goods.Count == 0)
        {
            throw new InvalidOperationException("Cannot generate a station for 0 goods");
        }
        if (Goods.First().GetType() == typeof(Item))
        {
            var items = Goods.Cast<Item>().ToList();
            bp = GenerateItemTrainStation(items, requesterFor);
        }
        else if (Goods.First().GetType() == typeof(Fluid))
        {
            if (Goods.Count > 1)
            {
                Project.current.preferences.sourceResources.Clear();
                throw new InvalidOperationException("cannot request more than 1 fluid");
            }
            bp = GenerateFluidTrainStation(Goods.First() as Fluid, requesterFor);
        }
        else
        {
            throw new InvalidOperationException($"Unknown Requester Goods Type {Goods.First().GetType()}");
        }
        var station = bp.entities.Where(e => e.name == "logistic-train-stop").Single();

        var stationName = new StringBuilder();
        foreach (var goods in Goods)
        {
            if (goods is Item)
            {
                stationName.Append($"[item={goods.name}]");
            }
            else if (goods is Fluid)
            {
                stationName.Append($"[fluid={goods.name.Split("@")[0]}]");
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