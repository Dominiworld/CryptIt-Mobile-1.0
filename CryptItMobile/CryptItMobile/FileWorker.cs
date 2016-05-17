
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
        private const string PrivateKeyFile = "my_private.txt";
        private static readonly string PublicKeyFile = $"{AuthorizeService.Instance.CurrentUserId}_public.txt";
        private const string FriendsPublicKeysFile = "keys.txt";
        private const string Directory = "CryptIt Keys";
        private readonly string _requestKeyString = "Key request";
        private readonly Context ctx;

        private FileService _fileService=new FileService();
        private MessageService _messageService = new MessageService();

        public FileWorker(Context context)
        {
            ctx = context;
        }

        public bool FillKeys()//���������� false ���� ����� ���. ��������� ��������� � ��������� ����
        {
            try
            {
                // �������� ���� � SD
                File sdPath = Environment.ExternalStorageDirectory;
                // ��������� ���� ������� � ����
                sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
                // ��������� ������ File, ������� �������� ���� � �����
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
            //todo ������� ������� ��� ���������� sd �����
            // �������� ���� � SD
            File sdPath = Environment.ExternalStorageDirectory;
            // ��������� ���� ������� � ����
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            // ������� �������
            sdPath.Mkdirs();
            // ��������� ������ File, ������� �������� ���� � �����
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
            // �������� ���� � SD
            File sdPath = Environment.ExternalStorageDirectory;
            // ��������� ���� ������� � ����
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            //todo ������� ��������� �����
            // ��������� ������ File, ������� �������� ���� � �����
            File sdFile = new File(sdPath, FriendsPublicKeysFile);

            string[] files = System.IO.Directory.GetFiles(sdPath.AbsolutePath);

            if (files==null)
                return;

            //todo ������ ��� ��������� �� ��������� �����
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
                System.IO.File.Delete(file);//todo ��������� ��������
            }
            writer.Close();
        }

        public void SetFriendKey(AndroidUser friend)
        {
            //todo �� �����������!
            // �������� ���� � SD
            File sdPath = Environment.ExternalStorageDirectory;
            // ��������� ���� ������� � ����
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            //todo ������� ��������� �����
            // ��������� ������ File, ������� �������� ���� � �����
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
                //������ ����� ������ (�����#���_�����1,���_�����2)
                var probablyFiles = message.Body.Split('#').Last();
                var cryptedfileNamesWithKeys = probablyFiles.Split(',').ToList();
                //todo ������� ����� ����������!!!
                if (!message.Body.Contains('#') || cryptedfileNamesWithKeys.Count != message.Attachments.Count ||
                    (string.IsNullOrEmpty(cryptedfileNamesWithKeys[0]) && cryptedfileNamesWithKeys.Count == 1))
                    //��������� �� ����������� ��� ������
                    return;
                message.Body = message.Body.Substring(0, message.Body.Length - probablyFiles.Length - 1);
                foreach (var attachment in message.Attachments) //��������������� ����� ������������� �� message.body
                {
                    var items = cryptedfileNamesWithKeys[message.Attachments.IndexOf(attachment)].Split(':');
                    attachment.Document.FileName = items[0];
                    attachment.IsEncrypted = true;
                    attachment.EncryptedSymmetricKey = items[1];
                }
            }
        }

        #region keys
        public async void SendKeyRequest(int friendId)//todo �������� ���������
        {
            var message = new Message { Body = _requestKeyString };
            await _messageService.SendMessage(friendId, message);
        }

        //����� ������� ����� � ����� �� ���� - �������� ��� ���� "�����" ���������
        public async Task<bool> FindKeyRequestAndReply(Message message)
        {
            if (message.Body == _requestKeyString && !message.Out)
            {
                await SendPublicKey(message.UserId, message.Id);
                return true;
            }
            return false;
        }

        //�������� ������ ����� � ���������
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

        //��������� ���� ���� ����� - ���������
        private async Task SendPublicKey(int userId, int messageToRemove)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(ctx);
            var docId = prefs.GetInt("file_id", 0);

            var id = AuthorizeService.Instance.CurrentUserId;
 
            //����� ���� � ���������� �� 
            Document doc = null;

            File sdPath = Environment.ExternalStorageDirectory;
            sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
            // ��������� ������ File, ������� �������� ���� � �����
            File sdFile = new File(sdPath, PublicKeyFile);

            if (docId != 0)
            {
                doc = await _fileService.GetDocumentById(id + "_" + docId);
            }
            if (doc == null)
            {
                //���� � ���������� ���, ��������� 
                if ((doc = await SavePublicKeyInVkDocs(sdFile.AbsolutePath)) == null)
                    return;

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
            await _messageService.SendMessage(userId, message);
            await _messageService.RemoveMessage(messageToRemove);

        }

        //����� ����� � ������ ����� ��������� - �������� ��� ���� "�����" ���������
        public async Task GetKeyFileFromMessage(Message message)
        {
            if (message.Attachments == null)
                return;
            foreach (var attachment in message.Attachments)
            {
                File sdPath = Environment.ExternalStorageDirectory;
                // ��������� ���� ������� � ����
                sdPath = new File(sdPath.AbsolutePath + "/" + Directory);
                // ��������� ������ File, ������� �������� ���� � �����
                var fileName = attachment.Document.FileName;
                File sdFile = new File(sdPath, fileName);
                
                if (fileName == message.UserId +"_" + PublicKeyFile)
                {
                    using (var client = new WebClient())
                    {
                        await client.DownloadFileTaskAsync(attachment.Document.Url, sdFile.AbsolutePath);
                        AddFriendKeys();
                        var user = new AndroidUser {User = new User {Id = message.UserId } };
                        SetFriendKey(user);
                        CryptTool.Instance.keyRSARemote = user.PublicKey;
                    }
                }
            }
        }

        //���� � ���������� ������ ����� � ��� ����
        public async void ParseMessages(List<Message> messages)
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
                    await GetKeyFileFromMessage(message);
                }
                if (keySend && CryptTool.Instance.keyRSARemote != null)
                    break;
            }

        }
        #endregion keys

    }
}