using System;
using Newtonsoft.Json;

namespace Stratum
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class StratumException : System.ApplicationException
    {
        /// <summary>
        /// Error code
        /// </summary>
        [JsonProperty]
        public int code { get; set; }

        /// <summary>
        /// Human readable error message
        /// </summary>
        [JsonProperty]
        public string message { get; set; }

        [JsonProperty]
        public object data { get; set; }

        public StratumException(int code, string message, object data)
        {
            this.code = code;
            this.message = message;
            this.data = data;
        }
    }
}
