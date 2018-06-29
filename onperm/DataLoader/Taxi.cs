namespace taxi
{
    using System;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Taxi
    {
        public Taxi()
        {
        }

        [JsonProperty]
        public long Medallion { get; set; }

        [JsonProperty]
        public long HackLicense { get; set; }

        [JsonProperty]
        public string VendorId { get; set; }

        [JsonIgnore]
        public string PartitionKey
        {
            get => $"{Medallion}_{HackLicense}_{VendorId}";
        }

        [JsonIgnore]
        public string CsvString { get; set; }

        public virtual string GetJsonString()
        {
            return "";
        }
    }
}