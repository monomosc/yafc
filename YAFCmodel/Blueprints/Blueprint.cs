using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.OrTools.Sat;
using YAFC.Model;
using YAFC.UI;

namespace YAFC.Blueprints
{
    [Serializable]
    public class BlueprintString
    {
        public Blueprint blueprint { get; set; } = new Blueprint();
        private static readonly byte[] header = { 0x78, 0xDA };

        public string ToBpString()
        {
            if (InputSystem.Instance.control)
                return ToJson();
            var sourceBytes = JsonSerializer.SerializeToUtf8Bytes(this, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            using var memory = new MemoryStream();
            memory.Write(header);
            using (var compress = new DeflateStream(memory, CompressionLevel.Optimal, true))
                compress.Write(sourceBytes);
            memory.Write(GetChecksum(sourceBytes, sourceBytes.Length));
            return "0" + Convert.ToBase64String(memory.ToArray());
        }

        private byte[] GetChecksum(byte[] buffer, int length)
        {
            int a = 1, b = 0;
            for (var counter = 0; counter < length; ++counter)
            {
                a = (a + (buffer[counter])) % 65521;
                b = (b + a) % 65521;
            }
            var checksum = (b * 65536) + a;
            var intBytes = BitConverter.GetBytes(checksum);
            Array.Reverse(intBytes);
            return intBytes;
        }

        public string ToJson()
        {
            var sourceBytes = JsonSerializer.SerializeToUtf8Bytes(this, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            using var memory = new MemoryStream(sourceBytes);
            using (var reader = new StreamReader(memory))
                return reader.ReadToEnd();
        }
        public static BlueprintString FromJson(string json)
        {
            return JsonSerializer.Deserialize<BlueprintString>(json);
        }
        public static BlueprintString FromBpString(string bpString)
        {
            if (bpString.StartsWith("0"))
                bpString = bpString.Substring(1);
            var sourceBytes = Convert.FromBase64String(bpString);
            using var memory = new MemoryStream(sourceBytes);
            memory.Seek(2, SeekOrigin.Begin);
            using var decompress = new DeflateStream(memory, CompressionMode.Decompress);
            return JsonSerializer.Deserialize<BlueprintString>(decompress, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
    }

    [Serializable]
    public partial class Blueprint
    {
        public const long VERSION = 0x01000000;

        public string item { get; set; } = "blueprint";
        public string description { get; set; } = null;
        public string label { get; set; }
        public List<BlueprintEntity> entities { get; set; } = new List<BlueprintEntity>();
        public List<BlueprintIcon> icons { get; set; } = new List<BlueprintIcon>();
        public long version { get; set; } = VERSION;
        public static Blueprint Translate(Blueprint old, BlueprintPosition translation)
        {
            // sledgehammer deppcopy
            var newBp = JsonSerializer.Deserialize<Blueprint>(JsonSerializer.Serialize(old));
            foreach (var entity in old.entities)
            {
                entity.position = entity.position + translation;
            }
            return newBp!;
        }
    }

    [Serializable]
    public class BlueprintIcon
    {
        public int index { get; set; }
        public BlueprintSignal signal { get; set; } = new BlueprintSignal();
    }

    [Serializable]
    public class BlueprintSignal
    {
        public string name { get; set; }
        public string type { get; set; }

        public void Set(Goods goods)
        {
            if (goods is Special sp)
            {
                type = "virtual";
                name = sp.virtualSignal;
            }
            else if (goods is Fluid fluid)
            {
                type = "fluid";
                name = fluid.originalName;
            }
            else
            {
                type = "item";
                name = goods.name;
            }
        }
    }

    [Serializable]
    public record BlueprintEntity
    {
        [JsonPropertyName("entity_number")] public int index { get; set; }
        public string name { get; set; }
        public BlueprintPosition position { get; set; } = new BlueprintPosition();
        public int direction { get; set; }
        public int oritentation { get; set;}
        public string recipe { get; set; }
        [JsonPropertyName("control_behavior")] public BlueprintControlBehaviour controlBehavior { get; set; }
        public BlueprintConnection connections { get; set; }
        public List<long> neighbours { get; set;}
        [JsonPropertyName("request_filters")] public List<BlueprintRequestFilter> requestFilters { get; set; } = new List<BlueprintRequestFilter>();
        public Dictionary<string, int> items { get; set; }
        public ushort? bar { get; set; }
        public List<Filter> filters {get;set;}
        public string type { get; set; }
        public string station { get; set; }

        public void Connect(BlueprintEntity other, bool red = true, bool secondPort = false, bool targetSecond = false)
        {
            ConnectSingle(other, red, secondPort, targetSecond);
            other.ConnectSingle(this, red, targetSecond, secondPort);
        }

        private void ConnectSingle(BlueprintEntity other, bool red = true, bool secondPort = false, bool targetSecond = false)
        {
            connections ??= new BlueprintConnection();
            BlueprintConnectionPoint port;
            if (secondPort)
                port = connections.p2 ?? (connections.p2 = new BlueprintConnectionPoint());
            else port = connections.p1 ?? (connections.p1 = new BlueprintConnectionPoint());
            var list = red ? port.red : port.green;
            list.Add(new BlueprintConnectionData { entityId = other.index, circuitId = targetSecond ? 2 : 1 });
        }
    }

    [Serializable]
    public class Filter
    {
        public int index { get; set; }
        public string name { get; set; }
    }

    [Serializable]
    public class BlueprintRequestFilter
    {
        public string name { get; set; }
        public int index { get; set; }
        public int count { get; set; }
    }

    [Serializable]
    public class BlueprintConnection
    {
        [JsonPropertyName("1")] public BlueprintConnectionPoint p1 { get; set; }
        [JsonPropertyName("2")] public BlueprintConnectionPoint p2 { get; set; }
    }

    [Serializable]
    public class BlueprintConnectionPoint
    {
        public List<BlueprintConnectionData> red { get; set; } = new List<BlueprintConnectionData>();
        public List<BlueprintConnectionData> green { get; set; } = new List<BlueprintConnectionData>();
    }

    [Serializable]
    public class BlueprintConnectionData
    {
        [JsonPropertyName("entity_id")] public int entityId { get; set; }
        [JsonPropertyName("circuit_id")] public int circuitId { get; set; } = 1;
    }

    [Serializable]
    public record BlueprintPosition
    {
        public static BlueprintPosition FromXY(double x, double y)
        {
            return new BlueprintPosition { x = x, y = y };
        }
        //Addition Operator
        public static BlueprintPosition operator +(BlueprintPosition a, BlueprintPosition b)
        {
            return new BlueprintPosition { x = a.x + b.x, y = a.y + b.y };
        }
        public static BlueprintPosition operator -(BlueprintPosition a, BlueprintPosition b)
        {
            return new BlueprintPosition { x = a.x - b.x, y = a.y - b.y };
        }


        public double x { get; set; }
        public double y { get; set; }
    }

    [Serializable]
    public class BlueprintControlBehaviour
    {
        public List<BlueprintControlFilter> filters { get; set; } = new List<BlueprintControlFilter>();
    }

    [Serializable]
    public class BlueprintControlFilter
    {
        public BlueprintSignal signal { get; set; } = new BlueprintSignal();
        public int index { get; set; }
        public int count { get; set; }
    }
}

public static class BlueprintDirection
{
    public const int DOWN = 4;
    public const int UP = 0;
    public const int LEFT = 2;
    public const int RIGHT = 6;
}