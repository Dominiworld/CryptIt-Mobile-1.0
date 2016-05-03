using System.Text;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;
using Java.IO;
using CryptingTool;

namespace CryptItMobile
{
    public class FileWorker
    {
        private const string PrivateKeyFile="my_private";
        private const string PublicKeyFile = "my_public";
        private const string FriendsPublicKeysFile = "keys";
        private Context _context;

        public FileWorker(Context context)
        {
            _context = context;
        }

        public bool FillKeys()//Возвращает false если файла нет. Заполняет приватный и публичный ключ
        {
            try
            {
                BufferedReader br = new BufferedReader(new InputStreamReader(_context.OpenFileInput(PrivateKeyFile)));
                CryptingTool.CryptTool.Instance.keyRSAPrivate = Encoding.Default.GetBytes(br.ReadLine());
                br = new BufferedReader(new InputStreamReader(_context.OpenFileInput(PublicKeyFile)));
                CryptingTool.CryptTool.Instance.keyRSAPublic = Encoding.Default.GetBytes(br.ReadLine());
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        public void SavePrivateAndPublicKey()
        {
            
            BufferedWriter bw=new BufferedWriter(new OutputStreamWriter(_context.OpenFileOutput(PrivateKeyFile, FileCreationMode.WorldReadable)));
            bw.Write(Encoding.Default.GetString(CryptingTool.CryptTool.Instance.keyRSAPrivate));
            bw.Close();
            bw = new BufferedWriter(new OutputStreamWriter(_context.OpenFileOutput(PublicKeyFile, FileCreationMode.WorldReadable)));
            bw.Write(Encoding.Default.GetString(CryptingTool.CryptTool.Instance.keyRSAPublic));
            bw.Close();
        }
    }
}