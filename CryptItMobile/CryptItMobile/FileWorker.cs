
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Android.Preferences;
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
        private static readonly string PrivateKeyFile = $"{AuthorizeService.Instance.CurrentUserId}_private.txt";
        private static readonly string PublicKeyFile = $"{AuthorizeService.Instance.CurrentUserId}_public.txt";
        private const string FriendsPublicKeysFile = "keys.txt";
        private const string Directory = "CryptIt Keys";
        public readonly string _requestKeyString = "Key request";
        private readonly Context ctx;

        private FileService _fileService=new FileService();
        private MessageService _messageService = new MessageService();

        public FileWorker(Context context)
        {
            ctx = context;
        }

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
                            return true;
                        }
                    }
                    return false;
                }
                return false;
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
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(ctx);
            var editor = prefs.Edit();
            editor.PutInt("file_id", 0);
            editor.Commit();

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

        public void AddFriendKeys(int userId = 0)
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
            List<string> keysFile = new List<string>();
            try
            {
                reader = new StreamReader(sdFile.AbsolutePath);
                string line = null;
                
                while ((line = reader.ReadLine()) != null)
                {
                    if (int.Parse(line.Split(' ')[0]) != userId)
                    {
                        keysFile.Add(line);
                    }
                }
                reader.Close();
            }
            catch (Exception)
            {
                //ignored             
            }
           
            StreamWriter writer = new StreamWriter(sdFile.AbsolutePath,false);
            foreach (var file in files)
            {
                reader = new StreamReader(file);
                writer.WriteLine(reader.ReadLine());
                System.IO.File.Delete(file);//todo проверить удаление
            }
            foreach (var l in keysFile)
            {
                writer.WriteLine(l);
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

        /// <summary>
        /// поиск запроса ключа и ответ на него - вызывать для всех "новых" сообщений
        /// </summary>
        /// <param name="message"></param>
        /// <returns>
        /// 1 - найден request, отправлен ответ
        /// 0 - не найден request
        /// -1 - не загрузился файл с ключом
        /// -2 - не отправилось сообщение
        /// </returns>
        public async Task<int> FindKeyRequestAndReply(Message message)
        {
            if (message.Body == _requestKeyString && !message.Out)
            {
                return await SendPublicKey(message.UserId, message.Id);               
            }
            return 0;
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

        /// <summary>
        /// отправить свой ключ другу - автоматом
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="messageToRemove"></param>
        /// <returns>
        /// 1 - послан ответ на request
        /// -1 - не загрузился файл с ключом
        /// -2 - не отправилось сообщение
        /// </returns>
        private async Task<int> SendPublicKey(int userId, int messageToRemove)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(ctx);
            var docId = prefs.GetInt("file_id", 0);

            var id = AuthorizeService.Instance.CurrentUserId;
 
            //берем ключ с документов вк 
            Document doc = null;

            File sdPath = Environment.ExternalStorageDirectory;
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            // формируем объект File, который содержит путь к файлу
            File sdFile = new File(sdPath, PublicKeyFile);

            if (docId != 0)
            {
                doc = await _fileService.GetDocumentById(id + "_" + docId);
            }
            if (doc == null)
            {
                //если в документах нет, загружаем 
                if ((doc = await SavePublicKeyInVkDocs(sdFile.AbsolutePath)) == null)
                    return -1;

                var editor = prefs.Edit();
                editor.PutInt("file_id", doc.Id);
                editor.Commit();
            }

            var message = new Message
            {
                Attachments = new List<Attachment>
                {
                    new Attachment {Document = doc, Type = "doc"}
                },
                Body = "key"
            };
            
            var messageId = await _messageService.SendMessage(userId, message);
            if (messageId != 0)
            {
                await _messageService.RemoveMessage(messageToRemove);
                return 1;
            }
            return -2;
        }

        //поиск файла с ключом среди сообщений - вызывать для всех "новых" сообщений
        public async Task GetKeyFileFromMessage(Message message)
        {
            if (message.Attachments == null)
                return;
            foreach (var attachment in message.Attachments)
            {
                File sdPath = Environment.ExternalStorageDirectory;
                // добавляем свой каталог к пути
                sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
                // формируем объект File, который содержит путь к файлу
                if (attachment.Document == null)
                    continue;
                
                var fileName = attachment.Document.FileName;
                File sdFile = new File(sdPath, fileName);

                if (fileName == message.UserId + "_public.txt")
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            await client.DownloadFileTaskAsync(attachment.Document.Url, sdFile.AbsolutePath);
                        }
                        catch (Exception)
                        {
                            //не получилось скачать ключ
                            Log.Debug("KEY DOWNLOAD ERROR", "KEY DOWNLOAD ERROR");
                            return;
                        }
                        AddFriendKeys();
                        var user = new AndroidUser {User = new User {Id = message.UserId } };
                        SetFriendKey(user);
                        CryptTool.Instance.keyRSARemote = user.PublicKey;
                    }
                }
            }
        }

        /// <summary>
        ///ищем в сообщениях запрос ключа и сам ключ
        /// </summary>
        /// <param name="messages"></param>
        /// <returns>
        /// 1 - послан ответ на request
        /// -1 - не загрузился файл с ключом
        /// -2 - не отправилось сообщение
        /// </returns>
        public async Task<int> ParseMessages(List<Message> messages)
        {
            int keySend = 0;
            foreach (var message in messages)
            {
                if (keySend==0)
                {
                    keySend = await FindKeyRequestAndReply(message);
                }                
                if (CryptTool.Instance.keyRSARemote == null)
                {
                    await GetKeyFileFromMessage(message);
                }
                if (keySend!=0 && CryptTool.Instance.keyRSARemote != null)
                    break;
            }
            return keySend;
        }
        #endregion keys

    }
}