using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using com.xamarin.recipes.filepicker;
using CryptItMobile.Adapters;

namespace CryptItMobile.Activities
{
    [Activity(Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]

    public class FileDialogActivity:Activity
    {

        static readonly int READ_REQUEST_CODE = 1337;
        public static readonly String TAG = "StorageClientFragment";

        public static readonly string DefaultInitialDirectory = "/";
        private FileListAdapter _adapter;
        private DirectoryInfo _directory;
        private ListView _listview;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.fileExplorer);

            ActionBar.SetDisplayHomeAsUpEnabled(true);

            _listview = FindViewById<ListView>(Resource.Id.file_explorer);
            _adapter = new FileListAdapter(this, new FileSystemInfo[0]);
            _listview.Adapter = _adapter;

            _listview.ItemClick += (sender, args) =>
            {
                var fileSystemInfo = _adapter.GetItem(args.Position);

                if (fileSystemInfo.IsFile())
                {
                    // Do something with the file.  In this case we just pop some toast.
                    Log.Verbose("FileListFragment", "The file {0} was clicked.", fileSystemInfo.FullName);
                    Toast.MakeText(this, "You selected file " + fileSystemInfo.FullName, ToastLength.Short).Show();
                    var intent = new Intent();
                    intent.PutExtra("file", fileSystemInfo.FullName);
                    SetResult(Result.Ok,intent);
                    Finish();
                }
                else
                {
                    // Dig into this directory, and display it's contents
                    RefreshFilesList(fileSystemInfo.FullName);
                }
            };
        }

        protected override void OnResume()
        {
            base.OnResume();
            RefreshFilesList(DefaultInitialDirectory);
        }

        public void RefreshFilesList(string directory)
        {
            IList<FileSystemInfo> visibleThings = new List<FileSystemInfo>();
            var dir = new DirectoryInfo(directory);

            try
            {
                foreach (var item in dir.GetFileSystemInfos().Where(item => item.IsVisible()))
                {
                    visibleThings.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error("FileListFragment", "Couldn't access the directory " + _directory.FullName + "; " + ex);
                Toast.MakeText(this, "Problem retrieving contents of " + directory, ToastLength.Long).Show();
                return;
            }

            _directory = dir;

            _adapter.AddDirectoryContents(visibleThings);

            // If we don't do this, then the ListView will not update itself when then data set 
            // in the adapter changes. It will appear to the user that nothing has happened.
            _listview.RefreshDrawableState();

            Log.Verbose("FileListFragment", "Displaying the contents of directory {0}.", directory);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:

                    if (_directory?.Parent != null)
                    {
                        RefreshFilesList(_directory.Parent.FullName);
                    }
                    else
                    {
                        Finish();
                    }
                    return true;
                default:
                    return base.OnOptionsItemSelected(item); ;
            }
        }

    
    }
}