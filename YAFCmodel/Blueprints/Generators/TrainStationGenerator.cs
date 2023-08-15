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


public class ItemTrainStationGenerator {
    
    public IEnumerable<Item> Items { get; init; }

    public ItemTrainStationGenerator(IEnumerable<Item> items) {
        Items = items;
    }

    public Blueprint GenerateTrainStation(string requesterFor) {
        var file = File.ReadAllBytes("train_station_blueprint_base.json");
        var bps =  JsonSerializer.Deserialize<BlueprintString>(file);
        var bp = bps.blueprint;
        var request_combinator = bp.entities.Where(e => e.name == "constant-combinator").Single();
        var station = bp.entities.Where(e => e.name == "logistic-train-stop").Single();
        var loaders = bp.entities.Where(e => e.name.Contains("loader")).ToList();
        //loaders 0,3 are request #1, loaders 1,2 are request #2


        foreach (var (item, itemIndex) in Items.Enumerate()) {
            if (itemIndex > 6) {
                throw new InvalidOperationException("Too many items");
            }
            if (itemIndex == 0) {
                loaders[0].filters[0].name = item.name;
                loaders[3].filters[0].name = item.name;
            }
            if (itemIndex == 1) {
                loaders[1].filters[0].name = item.name;
                loaders[2].filters[0].name = item.name;
            }
            if (request_combinator.controlBehavior.filters.Count < 6 + itemIndex) {
                request_combinator.controlBehavior.filters.Add(new BlueprintControlFilter() {
                    index = 6 + itemIndex,
                    signal = new BlueprintSignal() {
                        type = "item",
                        name = item.name,
                    },
                    count = -1 * item.stackSize * 100,
                });
            } else { 
                throw new InvalidOperationException("Unknown Origin Blueprint State");
            }
        }
        var stationName = new StringBuilder();
        foreach (var item in Items) {
            stationName.Append($"[item={item.name}]");
        }
        stationName.Append($" Requester for {requesterFor}");
        station.station = stationName.ToString();
        //clear source items
        Project.current.preferences.sourceResources.Clear();

        return bp;
    }
}