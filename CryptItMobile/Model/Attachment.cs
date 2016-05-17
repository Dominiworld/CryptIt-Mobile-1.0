using Newtonsoft.Json;

namespace Model
{
    public class Attachment
    {

        /// <summary>
        /// "doc"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("doc")]
        public Document Document { get; set; }


        public bool IsEncrypted { get; set; }

        public string EncryptedSymmetricKey { get; set; }


    }
}
