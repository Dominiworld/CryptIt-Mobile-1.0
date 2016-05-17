
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Util;
using Java.IO;
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
        private FileService _fileService=new FileService();

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

            StreamReader reader=new StreamReader(sdFile.AbsolutePath);
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
                //client.UploadProgressChanged += (sender, args) =>
                //{
                //    attachment.Progress = 100 * (float)args.BytesSent / args.TotalBytesToSend;
                //};
                //client.UploadFileCompleted += (sender, args) =>
                //{

                //    attachment.IsNotCompleted = false;
                //};

                //_cancelFileUploadEvent += attachment1 =>
                //{
                //    if (attachment.File == attachment1.File)
                //    {
                //        client.CancelAsync();
                //        client.Dispose();
                //    }
                //};

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
    }
}