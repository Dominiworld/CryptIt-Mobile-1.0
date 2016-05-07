using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptingTool
{
    public class CryptTool
    {
        public byte[] keyRSAPublic;
        public byte[] keyRSAPrivate;
        public byte[] keyRSARemote;

        private byte[] senderPubKeyBlob;

        public string _isCryptedFlag = "ъйьz";
        private string _mobileFlag = "h1m1";
        private string _desktopFlag = "h2m2";



        public static CryptTool Instance = new CryptTool();

        protected CryptTool()
        {
            keyRSARemote = null;
            keyRSAPublic = null;
            keyRSAPrivate = null;

        }
        public void CreateRSAKey()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            keyRSAPrivate = rsa.ExportCspBlob(true);
            keyRSAPublic = rsa.ExportCspBlob(false);
            keyRSARemote = keyRSAPublic;
        }

        public string SendRSAKey(byte[] keyRSA)
        {
            return Encoding.Default.GetString(keyRSA);
        }

        public void SetRSAKey(string RSAKey)
        {
            keyRSARemote = Encoding.Default.GetBytes(RSAKey);
        }
        private string EncryptString(string inputString)
        {
            int dwKeySize = 2048;
            byte[] xmlString = keyRSARemote;
            RSACryptoServiceProvider rsaCryptoServiceProvider =
                                          new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.ImportCspBlob(xmlString);
            int keySize = dwKeySize / 8;
            byte[] bytes = Encoding.UTF32.GetBytes(inputString);

            int maxLength = keySize - 42;
            int dataLength = bytes.Length;
            int iterations = dataLength / maxLength;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i <= iterations; i++)
            {
                byte[] tempBytes = new byte[
                        (dataLength - maxLength * i > maxLength) ? maxLength :
                                                      dataLength - maxLength * i];
                Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                  tempBytes.Length);
                byte[] encryptedBytes = rsaCryptoServiceProvider.Encrypt(tempBytes,
                                                                          true);

                Array.Reverse(encryptedBytes);

                stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
            }
            return stringBuilder.ToString();
        }

        private string DecryptString(string inputString)
        {
            int dwKeySize = 2048;
            byte[] xmlString = keyRSAPrivate;
            RSACryptoServiceProvider rsaCryptoServiceProvider
                                     = new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.ImportCspBlob(xmlString);
            int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ?
              (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;
            int iterations = inputString.Length / base64BlockSize;
            ArrayList arrayList = new ArrayList();

            try
            {


                for (int i = 0; i < iterations; i++)
                {
                    byte[] encryptedBytes = Convert.FromBase64String(
                        inputString.Substring(base64BlockSize * i, base64BlockSize));

                    Array.Reverse(encryptedBytes);
                    arrayList.AddRange(rsaCryptoServiceProvider.Decrypt(
                        encryptedBytes, true));
                }

            }
            catch (CryptographicException)
            {

                return inputString;
            }
            return Encoding.UTF32.GetString(arrayList.ToArray(Type.GetType("System.Byte")) as byte[]);
        }
        private string GetRandomString(int length)
        {
            var result = new char[length];
            var r = new Random();
            for (int i = 0; i < result.Length; i++)
            {
                do
                    result[i] = (char)r.Next(127);
                while (result[i] < '!');
            }
            return new string(result);
        }


        #region Шифруем данные(Алгоритм AES)


        private byte[] Encrypt(byte[] key, string value)
        {
            SymmetricAlgorithm Sa = Rijndael.Create();
            var Ct = Sa.CreateEncryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);
            MemoryStream Ms = new MemoryStream();//?
            CryptoStream Cs = new CryptoStream(Ms, Ct, CryptoStreamMode.Write);
            Cs.Write(key, 0, key.Length);
            Cs.FlushFinalBlock();
            byte[] Result = Ms.ToArray();
            Ms.Close();
            Ms.Dispose();
            Cs.Close();
            Cs.Dispose();
            Ct.Dispose();
            return Result;
        }

        private string Decrypt(byte[] str, string keyCrypt)
        {
            string Result;
            try
            {
                CryptoStream Cs = InternalDecrypt(str, keyCrypt);
                StreamReader Sr = new StreamReader(Cs);
                Result = Sr.ReadToEnd();
                Cs.Close();
                Cs.Dispose();
                Sr.Close();
                Sr.Dispose();
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Содержимое сообщения неизвестно");
                return null;
            }

            return Result;
        }


        private CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            ICryptoTransform ct = sa.CreateDecryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);
            MemoryStream ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }



        #endregion

        #region Вспомогательные функции распаковки и упаковки сообщений

        /// <summary>
        /// Расшифровка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string SplitAndUnpackReceivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || message.Length < _isCryptedFlag.Length)
            {
                return message;
            }
            string des = message.Substring(0, _isCryptedFlag.Length);

            if (des != _isCryptedFlag)
                return message;
            string des2 = message.Substring(_isCryptedFlag.Length, _mobileFlag.Length);

            var enc = Encoding.GetEncoding(1252);

            if (des2 == _mobileFlag)
            {
                message = message.Substring(_desktopFlag.Length + _isCryptedFlag.Length).FromBase64();

                string encryptedSymmetricKey = message.Substring(0, 344);
                byte[] receivedData = enc.GetBytes(message.Substring(344));
                string symmetricKey = DecryptString(encryptedSymmetricKey);
                if (symmetricKey == encryptedSymmetricKey)
                {
                    return message;
                }
                return Decrypt(receivedData, symmetricKey);
            }

            if (des2 == _desktopFlag)
            {
                message = message.Substring(_desktopFlag.Length + _isCryptedFlag.Length).FromBase64();
                string encryptedSymmetricKey = message.Substring(136, 344);
                byte[] receivedData = enc.GetBytes(message.Substring(480));
                string symmetricKey = DecryptString(encryptedSymmetricKey);
                if (symmetricKey == encryptedSymmetricKey)
                {
                    return message;
                }
                return Decrypt(receivedData, symmetricKey);
            }

            return message;
        }
        /// <summary>
        /// Шифровка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns>
        /// </returns>
        public string MakingEnvelope(string message)
        {

            var enc = Encoding.GetEncoding(1252);
            string symmetricKey = GetRandomString(8);
            string encryptedSymmetricKey = EncryptString(symmetricKey);
            byte[] senderData = Encrypt(Encoding.UTF8.GetBytes(message), symmetricKey);
            var envelope = encryptedSymmetricKey + enc.GetString(senderData);
            return _isCryptedFlag + _mobileFlag + envelope.ToBase64();
        }

        #endregion

    }
}
