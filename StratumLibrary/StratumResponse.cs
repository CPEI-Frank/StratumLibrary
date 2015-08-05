using Newtonsoft.Json;

namespace Stratum
{
    /// <summary>
    /// Represents a Stratum Response
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class StratumResponse
    {
        /// <summary>
        /// Response id, should be null or identical to request id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Result object
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "result")]
        public object Result { get; set; }

        /// <summary>
        /// Error object
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "error")]
        public StratumException Error { get; set; }
    }

    /// <summary>
    /// Represents a Stratum Response
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class StratumResponse<T>
    {
        /// <summary>
        /// Response id, should be null or identical to request id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Result object
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "result")]
        public T Result { get; set; }

        /// <summary>
        /// Error object
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "error")]
        public StratumException Error { get; set; }
    }
}
