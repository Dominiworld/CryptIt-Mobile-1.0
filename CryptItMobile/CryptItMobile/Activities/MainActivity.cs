using System.IO;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using CryptItMobile.Adapters;
using Java.IO;
using vkAPI;
using CryptingTool;
using Felipecsl.GifImageViewLibrary;

namespace CryptItMobile.Activities
{
    [Activity(Label = "CryptItMobile", Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        private ListView _friendsListView;
        private FriendsAdapter _friendsAdapter;
        private EditText _searchEditText;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

          //  StartLoader();

            var fileWorker = new FileWorker(this);
            if (!fileWorker.FillKeys())
            {
                var toast = Toast.MakeText(this, Resource.String.KeyGeneration, ToastLength.Long);
                toast.Show();
                CryptTool.Instance.CreateRSAKey();
                fileWorker.SavePrivateAndPublicKey();
            }
            fileWorker.AddFriendKeys();



            SetContentView(Resource.Layout.Main);
            Window.SetSoftInputMode(SoftInput.StateHidden);
         

            _friendsListView = FindViewById<ListView>(Resource.Id.friendsListView);
            _friendsAdapter = new FriendsAdapter(this);
            _friendsListView.Adapter = _friendsAdapter;

            LongPollServerService.Instance.ConnectToLongPollServer();

            LongPollServerService.Instance.GotNewMessageEvent += _friendsAdapter.NewMessage;
            LongPollServerService.Instance.InMessageStateChangedToReadEvent += _friendsAdapter.MessageStateChangedToRead;
            LongPollServerService.Instance.UserBecameOnlineOrOfflineEvent += _friendsAdapter.UserBecameOnlineOrOffline;

            _searchEditText = FindViewById<EditText>(Resource.Id.searchEditText);

            _searchEditText.TextChanged += (sender, e) =>
            {
                _friendsAdapter.Search(_searchEditText.Text);
            };

            _friendsListView.ItemClick += (sender, e) =>
            {
                _friendsAdapter.SetFriendKey(e.Position);
                var intent = new Intent(this, typeof (DialogActivity));
                intent.PutExtra("FriendId", _friendsAdapter.GetItemId(e.Position));
                StartActivity(intent);
            };


        }

        private void StartLoader()
        {
            var view = FindViewById<GifImageView>(Resource.Id.mainLoaderImageView);

            var input = Assets.Open("loading.gif");
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                input.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            view.SetBytes(bytes);
            view.StartAnimation();
        }

        public void FinishLoader()
        {
            var view = FindViewById<GifImageView>(Resource.Id.mainLoaderImageView);
            view.StopAnimation();
            view.Visibility = ViewStates.Gone;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Layout.mainMenu, menu);
           
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.exitMainButton)
            {
                var intent = new Intent(this, typeof(StartActivity));
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
            }
            return base.OnOptionsItemSelected(item);
        }


    }
}

