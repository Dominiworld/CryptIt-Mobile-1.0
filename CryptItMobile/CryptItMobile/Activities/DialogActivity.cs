using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using CryptItMobile.Adapters;
using vkAPI;

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
        private LongPollServerService _longPollServerService=new LongPollServerService();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Dialog);
            // Create your application here
            
            int friendId = Intent.GetIntExtra("FriendId", 0);

            _longPollServerService.ConnectToLongPollServer();


            _dialogListView = FindViewById<ListView>(Resource.Id.dialogListView);
            _dialogAdapter = new DialogAdapter(this, friendId);

            _longPollServerService.GotNewMessageEvent += _dialogAdapter.NewMessage;

            _dialogListView.Adapter = _dialogAdapter;

            _sendButton = FindViewById<Button>(Resource.Id.enterButton);
            _messageText = FindViewById<EditText>(Resource.Id.messageEditText);

            _sendButton.Click += (sender, e) =>
            {
                SendMessage(friendId);
                _messageText.Text = string.Empty;
            };

            //todo сделать loader
            _dialogListView.Scroll += (sender, e) =>
            {
                if (_dialogListView.LastVisiblePosition == _dialogAdapter.Count-1)//todo менять если сменил навправление листа
                {
                    _dialogAdapter.GetMessages(friendId); 
                }
                
            };
        }


        private async void SendMessage(int friendId)
        {
            await _messageService.SendMessage(friendId, _messageText.Text);
        }

    }
}