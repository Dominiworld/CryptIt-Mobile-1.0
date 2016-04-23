using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using CryptItMobile.Adapters;
using vkAPI;
using Message = Model.Message;

namespace CryptItMobile.Activities
{
    [Activity(Label = "DialogActivity", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class DialogActivity : Activity
    {
        
        private ListView _dialogListView;
        private DialogAdapter _dialogAdapter;
        private Button _sendButton;
        private EditText _messageText; 
        private MessageService _messageService=new MessageService();
        //private LongPollServerService _longPollServerService=new LongPollServerService();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Dialog);
            // Create your application here

            int friendId = (int)Intent.GetLongExtra("FriendId", 0);

            _dialogListView = FindViewById<ListView>(Resource.Id.dialogListView);
            _dialogAdapter = new DialogAdapter(this, friendId);

            LongPollServerService.Instance.GotNewMessageEvent += _dialogAdapter.NewMessage;
            LongPollServerService.Instance.MessageStateChangedToReadEvent += _dialogAdapter.MessageStateChangedToRead;

            _dialogListView.Adapter = _dialogAdapter;

            _sendButton = FindViewById<Button>(Resource.Id.enterButton);
            _messageText = FindViewById<EditText>(Resource.Id.messageEditText);

            _sendButton.Click += (sender, e) =>
            {
                SendMessage(friendId);
                _messageText.Text = string.Empty;
            };

            //todo сделать loader
            //todo Попробовать перенести всю логику из адаптера в отдельный класс

            bool isReady=true;
            _dialogListView.Scroll += async (sender, e) =>
            {
                if (_dialogListView.LastVisiblePosition == _dialogAdapter.Count - 1
                && _dialogAdapter.Count % 20 == 0 //При одном запросе тягается 20 сообщений
                && _dialogAdapter.Count != 0
                &&isReady)
                {
                    isReady = false;
                    await _dialogAdapter.GetMessagesAsync(friendId);
                    isReady = true;
                }

            };

            FindViewById<Button>(Resource.Id.exitDialogButton).Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(StartActivity));
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
            };

            FindViewById<Button>(Resource.Id.friendsDialogButton).Click += (sender, e) =>//todo Попробовать переделать с помощью ActionBar.SetDisplayHomeAsUpEnabled(true);
            {
                var intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
            };
        }


        private async void SendMessage(int friendId)
        {
            await _messageService.SendMessage(friendId, _messageText.Text);
        }

        
    }
}

