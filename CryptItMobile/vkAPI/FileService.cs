﻿using System;
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

        public async Task<Document> GetDocumentById(string fullId)
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = $"https://api.vk.com/method/docs.getById?docs={fullId}&access_token={token}&v=5.45";
            var objs = await GetUrl(url);
            if (objs?["response"] == null)
            {
                return null;
            }
            var docs = JsonConvert.DeserializeObject<List<Document>>(objs["response"].ToString());
            return docs?.FirstOrDefault();
        }

        public async Task<List<Document>> GetDocuments(List<string> fullIds)
        {
            if (!fullIds.Any() || (fullIds.Count == 1 && fullIds[0] == string.Empty))
            {
                return new List<Document>();
            }
            var token = AuthorizeService.Instance.AccessToken;
            var ids = string.Join(",", fullIds);
            var url = $"https://api.vk.com/method/docs.getById?docs={ids}&access_token={token}&v=5.45";
            var objs = await GetUrl(url);
            if (objs["response"] == null)
            {
                return new List<Document>();
            }
            var docs = JsonConvert.DeserializeObject<List<Document>>(objs["response"].ToString());
            if (docs.Count == 0)
            {
                //для тех докуметов, которые не получены по причине недостатка прав - надо для подгрузки этого сообщения другим методом
                docs.Add(new Document { Id = -1 });
            }

            return docs;
        }
    }


}
