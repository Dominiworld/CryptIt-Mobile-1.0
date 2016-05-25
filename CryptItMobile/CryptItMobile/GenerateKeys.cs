using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CryptingTool;
using Object = Java.Lang.Object;

namespace CryptItMobile
{
    public class GenerateKeys: AsyncTask
    {
        private FileWorker _fileWorker;

        public GenerateKeys(FileWorker fileworker)
        {
            _fileWorker = fileworker;
        }

        protected override Object DoInBackground(params Object[] @params)
        {
            CryptTool.Instance.CreateRSAKey();
            _fileWorker.SavePrivateAndPublicKey();
            return null;
        }
    }
}