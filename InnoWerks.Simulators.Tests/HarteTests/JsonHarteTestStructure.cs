using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

#pragma warning disable CA1002, CA2227

namespace InnoWerks.Simulators.Tests
{
    public enum CycleType
    {
        Read,

        Write,

        None
    }

    public class JsonHarteRamEntry
    {
        public ushort Address { get; set; }

        public byte Value { get; set; }
    }

    public class JsonHarteTestState
    {
        [JsonPropertyName("pc")]
        public ushort ProgramCounter { get; set; }

        [JsonPropertyName("s")]
        public byte S { get; set; }

        [JsonPropertyName("a")]
        public byte A { get; set; }

        [JsonPropertyName("x")]
        public byte X { get; set; }

        [JsonPropertyName("y")]
        public byte Y { get; set; }

        [JsonPropertyName("p")]
        public byte P { get; set; }

        [JsonPropertyName("ram")]
        public List<JsonHarteRamEntry> Ram { get; set; }

        public JsonHarteTestState Clone()
        {
            var clone = (JsonHarteTestState)MemberwiseClone();
            clone.Ram = [.. Ram.OrderBy(r => r.Address)];
            return clone;
        }
    }

    [DebuggerDisplay("{Address} {Value} {Type} [{Address.ToString(\"X4\")} {Value.ToString(\"X2\")}]")]
    public struct JsonHarteTestBusAccess : IEquatable<JsonHarteTestBusAccess>
    {
        public static JsonHarteTestBusAccess Dummy { get; } =
            new()
            {
                Address = 0x0000,
                Value = 0x00,
                Type = CycleType.None
            };

        public ushort Address { get; set; }

        public int Value { get; set; }

        public CycleType Type { get; set; }

        public override readonly string ToString()
        {
            return $"${Address:X4}:${Value:X2} ({Address}:{Value}) {Type.ToString().ToLowerInvariant()}";
        }

        public override readonly bool Equals(object obj)
        {
            var other = (JsonHarteTestBusAccess)obj;

            return other.Address == Address &&
                   other.Value == Value &&
                   other.Type == Type;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Address, Value, Type);
        }

        public static bool operator ==(JsonHarteTestBusAccess left, JsonHarteTestBusAccess right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(JsonHarteTestBusAccess left, JsonHarteTestBusAccess right)
        {
            return !(left == right);
        }

        public readonly bool Equals(JsonHarteTestBusAccess other)
        {
            return other.Address == Address &&
                   other.Value == Value &&
                   other.Type == Type;
        }
    }

    [DebuggerDisplay("{Name}")]
    public class JsonHarteTestStructure
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("initial")]
        public JsonHarteTestState Initial { get; set; }

        [JsonPropertyName("final")]
        public JsonHarteTestState Final { get; set; }

        [JsonPropertyName("cycles")]
        public IEnumerable<JsonHarteTestBusAccess> BusAccesses { get; set; }

        public JsonHarteTestStructure Clone()
        {
            return new JsonHarteTestStructure
            {
                Name = Name,
                Initial = Initial.Clone(),
                Final = Final.Clone(),
                BusAccesses = BusAccesses
            };
        }
    }
}
