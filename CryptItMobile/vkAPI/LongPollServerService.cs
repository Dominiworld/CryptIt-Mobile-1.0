using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;

namespace vkAPI
{
    public class LongPollServerService : BaseService
    {

        private FileService _fileService = new FileService();
        private MessageService _messageService = new MessageService();
        public static readonly LongPollServerService Instance = new LongPollServerService();

        public delegate void GotNewMessage(Message message);

        public delegate void OutMessageStateChangedToRead(int lastReadId, int peerId);
        public delegate void InMessageStateChangedToRead(int lastReadId, int peerId);


        public delegate void UserBecameOnlineOrOffline(int userId, bool online); //online = 1, если стал онлайн
        //=0, если стал оффлайн

        public event GotNewMessage GotNewMessageEvent;

        public event OutMessageStateChangedToRead OutMessageStateChangedToReadEvent;
        public event InMessageStateChangedToRead InMessageStateChangedToReadEvent;

        public event UserBecameOnlineOrOffline UserBecameOnlineOrOfflineEvent;
        public async void ConnectToLongPollServer(bool useSsl = true, bool needPts = true)
        {
            var token = AuthorizeService.Instance.AccessToken;

            var url =
                $"https://api.vk.com/method/messages.getLongPollServer?v=5.45&use_ssl={useSsl}&need_pts={needPts}&access_token={token}";
            var obj = await GetUrl(url);
            var connectionSettings = JsonConvert.DeserializeObject<LongPollConnectionSettings>(obj["response"].ToString());

            while (connectionSettings.TS != 0)
            {
                url =
                    $"http://{connectionSettings.Adress}?act=a_check&key={connectionSettings.Key}&ts={connectionSettings.TS}&wait=25&mode=2";
                obj = await GetUrl(url);

                var updates = JsonConvert.DeserializeObject<LongPoolServerResponse>(obj.ToString());
                connectionSettings.TS = updates.Ts;

                foreach (var update in updates.Updates)
                {
                    switch (int.Parse(update[0].ToString()))
                    {
                        case 4:
                            var message = new Message
                            {
                                Id = int.Parse(update[1].ToString()),
                                Body = update[6].ToString(),
                                UserId = int.Parse(update[3].ToString()),
                                UnixTime = int.Parse(update[4].ToString()),
                                Out = (int.Parse(update[2].ToString()) & 2) != 0, //+2 - OUTBOX   
                                IsNotRead = (int.Parse(update[2].ToString()) & 1) != 0 //+1 - UNREAD
                            };
                            var attachString = update[7].ToString()
                              .Replace("\"", "")
                              .Replace("\r\n", "")
                              .Replace("}", "")
                              .Replace("{", "")
                              .Split(',');

                            if (attachString.ToList().Count == 1 && attachString[0] == string.Empty)
                            {
                                GotNewMessageEvent?.Invoke(message);
                                break;
                            }


                            var attachmentIds = attachString
                                .Where(s => !s.Contains("type")).ToList()
                                .Select(e => e.Split(':').Last().Trim(' ')).ToList();
                            var types = attachString
                               .Where(s => s.Contains("type")).ToList()
                               .Select(e => e.Split(':').Last().Trim(' ')).ToList();

                            if (attachmentIds.Count != types.Count)
                            {
                                GotNewMessageEvent?.Invoke(message);
                                break;
                            }

                            var dict = new Dictionary<string, string>(); //id - type
                            for (int i = 0; i < attachmentIds.Count; i++)
                            {
                                dict.Add(attachmentIds[i], types[i]);
                            }

                            var attachments = await GetFiles(dict);

                            if (attachments.Any(a => a.Document != null && a.Document.Id == -1))
                            {
                                //значит произошла ошибка загрузки документа из-за недостатка прав
                                message = await _messageService.GetMessage(message.Id);
                                GotNewMessageEvent?.Invoke(message);
                                break;
                            }

                            if (attachments != null)
                            {
                                message.Attachments = attachments;
                            }

                            GotNewMessageEvent?.Invoke(message);
                            break;
                        //прочтение входящих сообщений
                        case 6:
                            var userId = int.Parse(update[1].ToString());
                            var lastReadMessageId = int.Parse(update[2].ToString());
                            InMessageStateChangedToReadEvent?.Invoke(lastReadMessageId, userId);
                            break;
                        case 7:
                            //прочтение исходящих сообщений
                            var userId1 = int.Parse(update[1].ToString());
                            var lastReadMessageId1 = int.Parse(update[2].ToString());
                            OutMessageStateChangedToReadEvent?.Invoke(lastReadMessageId1, userId1);
                            break;
                        case 8:
                            UserBecameOnlineOrOfflineEvent?.Invoke(-1 * int.Parse(update[1].ToString()), true);
                            break;
                        case 9:
                            UserBecameOnlineOrOfflineEvent?.Invoke(-1 * int.Parse(update[1].ToString()), false);
                            break;
                        default:
                            break;
                    }



                }
            }
        }

        private async Task<List<Attachment>> GetFiles(Dictionary<string, string> dict)
        {
            var docsIds = dict.Where(i => i.Value == "doc").Select(i => i.Key).ToList();
            var docs = await _fileService.GetDocuments(docsIds);
            var attachments = docs.Select(doc => new Attachment { Document = doc, Type = "doc" }).ToList(); 
            return attachments;
        }
    }
}
