
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.Util;
using CryptingTool;
using CryptItMobile.Model;
using Model;
using vkAPI;
using Environment = Android.OS.Environment;
using File = Java.IO.File;

namespace CryptItMobile
{
    public class FileWorker
    {
        private const string PrivateKeyFile = "my_private.txt";
        private const string PublicKeyFile = "my_public.txt";
        private const string FriendsPublicKeysFile = "keys.txt";
        private const string Directory = "CryptIt Keys";
        private readonly string _requestKeyString = "Key request";

        private FileService _fileService=new FileService();
        private MessageService _messageService = new MessageService();

        public bool FillKeys()//Возвращает false если файла нет. Заполняет приватный и публичный ключ
        {
            try
            {
                // получаем путь к SD
                File sdPath = Environment.ExternalStorageDirectory;
                // добавляем свой каталог к пути
                sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
                // формируем объект File, который содержит путь к файлу
                File sdFile = new File(sdPath, PrivateKeyFile);

                var reader = new StreamReader(sdFile.AbsolutePath);
                var line = reader.ReadToEnd();
                if (line != string.Empty)
                {
                    CryptTool.Instance.keyRSAPrivate = Convert.FromBase64String(line);
                    sdFile = new File(sdPath, PublicKeyFile);
                    reader = new StreamReader(sdFile.AbsolutePath);
                    line = reader.ReadToEnd();
                    var data = line.Split(' ');
                    if (line != string.Empty && data.Length == 2)
                    {
                        int id;
                        if (int.TryParse(data[0], out id) && id == AuthorizeService.Instance.CurrentUserId)
                        {
                            CryptTool.Instance.keyRSAPublic = Convert.FromBase64String(data[1]);
                            reader.Dispose();
                        }
                    }

                }
                return true;
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return false;
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }
        }

        public void SavePrivateAndPublicKey()
        {
            //todo Сделать вариант при остутствии sd карты
            // получаем путь к SD
            File sdPath = Environment.ExternalStorageDirectory;
            // добавляем свой каталог к пути
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            // создаем каталог
            sdPath.Mkdirs();
            // формируем объект File, который содержит путь к файлу
            File sdFile = new File(sdPath, PublicKeyFile);

            var writer = new StreamWriter(sdFile.AbsolutePath);
            var key = Convert.ToBase64String(CryptTool.Instance.keyRSAPublic);
            writer.Write(AuthorizeService.Instance.CurrentUserId + " " + key);
            writer.Close();
            sdFile = new File(sdPath, PrivateKeyFile);
            writer = new StreamWriter(sdFile.AbsolutePath);
            writer.Write(Convert.ToBase64String(CryptTool.Instance.keyRSAPrivate));
            writer.Close();
        }

        public void AddFriendKeys()
        {
            // получаем путь к SD
            File sdPath = Environment.ExternalStorageDirectory;
            // добавляем свой каталог к пути
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            //todo Сделать отдельную папку
            // формируем объект File, который содержит путь к файлу
            File sdFile = new File(sdPath, FriendsPublicKeysFile);

            string[] files = System.IO.Directory.GetFiles(sdPath.AbsolutePath);

            if (files==null)
                return;

            //todo Убрать при переделке на отдельную папку
            files =
                files.Where(
                    f => !f.EndsWith(PublicKeyFile) && !f.EndsWith(PrivateKeyFile) && !f.EndsWith(FriendsPublicKeysFile))
                    .ToArray();
            StreamReader reader;
            StreamWriter writer = new StreamWriter(sdFile.AbsolutePath);
            foreach (var file in files)
            {
                reader = new StreamReader(file);
                writer.WriteLine(reader.ReadLine());
            }
            writer.Close();
        }

        public void SetFriendKey(AndroidUser friend)
        {
            //todo На рефакторинг!
            // получаем путь к SD
            File sdPath = Environment.ExternalStorageDirectory;
            // добавляем свой каталог к пути
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            //todo Сделать отдельную папку
            // формируем объект File, который содержит путь к файлу
            File sdFile = new File(sdPath, FriendsPublicKeysFile);

            StreamReader reader = new StreamReader(sdFile.AbsolutePath);
            string line;
            int id;
            while ((line=reader.ReadLine())!=null)
            {
                var data = line.Split(' ');
                if (int.TryParse(data[0], out id) && id == friend.User.Id)
                {
                    friend.PublicKey = Convert.FromBase64String(data[1]);
                    return;
                }

            }
        }

        public async Task<Document> UploadFile(string fileName, int userId, Attachment attachment)
        {

            using (var client = new WebClient())
            {
                var url = await _fileService.GetUploadUrl(fileName);
            
                byte[] file = null;
                try
                {
                    file = await client.UploadFileTaskAsync(url, fileName);
                }
                catch (WebException ex)
                {
                    Log.Debug("ex", ex.Message);
                    return null;
                }
                return await _fileService.UploadFile(fileName, file);
            }
        }

        public void TakeFileNamesFromBody(Message message)
        {
            if (message.Attachments != null && message.Attachments.Any())
            {
                //парсим имена файлов (текст#имя_файла1,имя_файла2)
                var probablyFiles = message.Body.Split('#').Last();
                var cryptedfileNamesWithKeys = probablyFiles.Split(',').ToList();
                //todo условие может поломаться!!!
                if (!message.Body.Contains('#') || cryptedfileNamesWithKeys.Count != message.Attachments.Count ||
                    (string.IsNullOrEmpty(cryptedfileNamesWithKeys[0]) && cryptedfileNamesWithKeys.Count == 1))
                    //сообщение не шифрованное или ошибка
                    return;
                message.Body = message.Body.Substring(0, message.Body.Length - probablyFiles.Length - 1);
                foreach (var attachment in message.Attachments) //восстанавливаем имена зашифрованных из message.body
                {
                    var items = cryptedfileNamesWithKeys[message.Attachments.IndexOf(attachment)].Split(':');
                    attachment.Document.FileName = items[0];
                    attachment.IsEncrypted = true;
                    attachment.EncryptedSymmetricKey = items[1];
                }
            }
        }

        #region keys
        private async void SendKeyRequest(int friendId)
        {
            var message = new Message { Body = _requestKeyString };
            await _messageService.SendMessage(friendId, message);
        }

        //поиск запроса ключа и ответ на него - вызывать для всех "новых" сообщений
        private async Task<bool> FindKeyRequestAndReply(Message message)
        {
            if (message.Body == _requestKeyString && !message.Out)
            {
                await SendPublicKey(message.UserId, message.Id);
                return true;
            }
            return false;
        }

        //загрузка своего ключа в документы
        private async Task<Document> SavePublicKeyInVkDocs(string path)
        {
            using (var client = new WebClient())
            {
                var url = await _fileService.GetUploadUrl(path);

                byte[] file = null;
                try
                {
                    file = await client.UploadFileTaskAsync(url, path);
                }
                catch (WebException)
                {
                    return null;
                }
                return await _fileService.UploadFile(path, file);
            }
        }

        //отправить свой ключ другу - автоматом
        private async Task SendPublicKey(int userId, int messageToRemove)
        {
            //todo заполнить id файла!!!
            int docIdPath = 0;

            var id = AuthorizeService.Instance.CurrentUserId;

            //берем ключ с документов вк      
            Document doc = (await _fileService.GetDocumentById(id + "_" + docIdPath));

            if (doc == null)
            {
                //есл в документах нет, загружаем
                //todo полный путь до публичного ключа!!!
                if ((doc = await SavePublicKeyInVkDocs(Path.Combine(_keysPath, id + PublicKeyFile))) == null)
                    return;

                // todo сохраняем id, чтобы потом файл с вк вытащить

            }
            var message = new Message
            {
                Attachments = new List<Attachment>
                {
                    new Attachment {Document = doc, Type = "doc"}
                },
                Body = "key"
            };
            await _messageService.SendMessage(userId, message);
            await _messageService.RemoveMessage(messageToRemove);

        }

        //поиск файла с ключом среди сообщений - вызывать для всех "новых" сообщений
        private async Task GetKeyFileFromMessage(Message message, int userId)
        {
            if (message.Attachments == null)
                return;
            foreach (var attachment in message.Attachments)
            {
                var fileName = attachment.Document.FileName;
                if (fileName == message.UserId + PublicKeyFile)
                {
                    using (var client = new WebClient())
                    {
                        //todo - скачивать в Crypt keys !!!!
                        await client.DownloadFileTaskAsync(attachment.Document.Url, fileName);
                        AddFriendKeys();
                        var user = new AndroidUser {User = new User {Id = userId}};
                        SetFriendKey(user);
                        CryptTool.Instance.keyRSARemote = user.PublicKey;
                        //todo удалить файл
                    }
                }
            }
        }

        //ищем в сообщениях запрос ключа и сам ключ
        private async void ParseMessages(List<Message> messages, int userId)
        {
            bool keySend = false;
            foreach (var message in messages)
            {
                if (!keySend)
                {
                    keySend = await FindKeyRequestAndReply(message);
                }                
                if (CryptTool.Instance.keyRSARemote == null)
                {
                    await GetKeyFileFromMessage(message,userId);
                }
                if (keySend && CryptTool.Instance.keyRSARemote != null)
                    break;
            }

        }
        #endregion keys

    }
}