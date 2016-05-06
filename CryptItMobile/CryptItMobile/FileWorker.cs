
using System;
using System.IO;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Util;
using Java.IO;
using CryptingTool;
using CryptItMobile.Model;
using vkAPI;
using Environment = Android.OS.Environment;
using File = Java.IO.File;
using FileNotFoundException = Java.IO.FileNotFoundException;

namespace CryptItMobile
{
    public class FileWorker
    {
        private const string PrivateKeyFile = "my_private.txt";
        private const string PublicKeyFile = "my_public.txt";
        private const string FriendsPublicKeysFile = "keys.txt";
        private const string Directory = "CryptIt Keys";

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
    }
}