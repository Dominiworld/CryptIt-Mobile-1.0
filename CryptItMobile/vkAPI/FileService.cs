using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class FileService: BaseService
    {
        MessageService _messageService = new MessageService();
        private string _token = AuthorizeService.Instance.AccessToken;

        public async Task<string> GetUploadUrl(string fileName)
        {
            using (var client = new WebClient())
            {
                var u = "https://api.vk.com/method/docs.getUploadServer?access_token=" + _token;
                var r = await client.DownloadStringTaskAsync(u);
                var j = JsonConvert.DeserializeObject(r) as JObject;

                return j["response"]["upload_url"].ToString();
            }
        }



        public async Task<Document> UploadFile(string fileName, byte[] file)
        {
            using (var client = new System.Net.WebClient())
            {
                var r2 = Encoding.UTF8.GetString(file);

                var j2 = JsonConvert.DeserializeObject(r2) as JObject;
                if (j2["file"] == null)
                {
                    return null;
                }

                var u3 = "https://api.vk.com/method/docs.save?v=5.45&access_token=" + _token
                         + "&file=" + j2["file"];
                var docObj = await GetUrl(u3);
                //todo проверка ошибки на капчу
                var doc = JsonConvert.DeserializeObject<Document>(docObj["response"][0].ToString());
                return doc;
            }
        }
    }
}
