using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using CryptItMobile.Adapters;
using Java.IO;
using vkAPI;
using CryptingTool;

namespace CryptItMobile.Activities
{
    [Activity(Label = "CryptItMobile", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        private ListView _friendsListView;
        private FriendsAdapter _friendsAdapter;
        private EditText _searchEditText;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);
            
            var fileWorker=new FileWorker(this);
            if (!fileWorker.FillKeys())
            {
                CryptingTool.CryptTool.Instance.CreateRSAKey();
                fileWorker.SavePrivateAndPublicKey();
            }

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
                var intent = new Intent(this, typeof(DialogActivity));
                intent.PutExtra("FriendId", _friendsAdapter.GetItemId(e.Position));//todo переделать когда перенесу друзей в активити
                StartActivity(intent);
            };


            FindViewById<Button>(Resource.Id.exitMainButton).Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(StartActivity));
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
            };
            
        }

        
    }
}

