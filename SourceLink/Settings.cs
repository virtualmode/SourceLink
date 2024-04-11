using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceLink
{
    internal class Settings
    {
        /// <summary>
        /// The DNS hostname or IP address on which to listen.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ListenerIpAddress { get; set; }

        /// <summary>
        /// The TCP port on which to listen.
        /// </summary>
        [DefaultValue(7080)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ListenerPort { get; set; }

        /// <summary>
        /// Maximum response length in bytes.
        /// </summary>
        [DefaultValue(1024 * 300)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int MaxResponseLength { get; set; }

        /// <summary>
        /// Cookies separated by semicolon.
        /// </summary>
        [DefaultValue("_gitlab_session=")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Cookies { get; set; }

        /// <summary>
        /// Source Link provider address.
        /// </summary>
        [DefaultValue("gitlab.local")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string DestinationAddress { get; set; }

        /// <summary>
        /// Source Link provider port.
        /// </summary>
        [DefaultValue(80)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int DestinationPort { get; set; }
    }
}
