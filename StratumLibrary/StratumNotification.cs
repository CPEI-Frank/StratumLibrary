using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Stratum
{
    /// <summary>
    /// Represents a Stratum notification
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class StratumNotification
    {
        /// <summary>
        /// Response id, should be null
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }

        /// <summary>
        /// Method name
        /// </summary>
        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        /// <summary>
        /// Notification data
        /// </summary>
        [JsonProperty(PropertyName = "params")]
        public JArray Params { get; set; }
    }
}
