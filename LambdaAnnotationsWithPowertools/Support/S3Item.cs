using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LambdaAnnotationsWithPowertools.Support
{
    public class S3Item
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
