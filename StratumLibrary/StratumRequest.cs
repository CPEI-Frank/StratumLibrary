using System;
using Newtonsoft.Json;

namespace Stratum
{
    /// <summary>
    /// Represents a JsonRpc request
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class StratumRequest
    {
        public StratumRequest()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Unique request id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; private set; }

        /// <summary>
        /// Stratum method name
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// Method params
        /// </summary>
        [JsonProperty("params")]
        public object Params { get; set; }

    }
}
