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
using System.Net;
using Android.Graphics;
using CryptItMobile.Activities;
using Model;

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
        private UserService _userService=new UserService();
        private User _friend;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Dialog);
            // Create your application here

            int friendId = (int)Intent.GetLongExtra("FriendId", 0);
            GetFriend(friendId);

            _dialogListView = FindViewById<ListView>(Resource.Id.dialogListView);
            _dialogAdapter = new DialogAdapter(this, friendId);

            LongPollServerService.Instance.GotNewMessageEvent += _dialogAdapter.NewMessage;
            LongPollServerService.Instance.OutMessageStateChangedToReadEvent += _dialogAdapter.MessageStateChangedToRead;

            _dialogListView.Adapter = _dialogAdapter;

            _sendButton = FindViewById<Button>(Resource.Id.enterButton);
            string text = "Добавьте публичный ключ пользователя в папку CryptIt Keys";
            Toast toast= Toast.MakeText(this, text, ToastLength.Long);
            if (CryptingTool.CryptTool.Instance.keyRSARemote == null)
            {
                _sendButton.Enabled = false;
                
                toast.Show();
            }

            _messageText = FindViewById<EditText>(Resource.Id.messageEditText);

            _sendButton.Click += (sender, e) =>
            {
                SendMessage(friendId);
                _messageText.Text = string.Empty;
            };

            //todo сделать loader

            bool isReady=true;

            _dialogListView.Scroll += async (sender, e) =>
            {
                if (_dialogListView.FirstVisiblePosition != 0 || _dialogAdapter.Count == 0 || !isReady) return;
                isReady = false;
                var oldCount = _dialogAdapter.Count;
                await GetMessagesAsync(friendId);
                if (_dialogAdapter.Count - oldCount!=0)
                {
                    _dialogListView.SetSelection(_dialogAdapter.Count - oldCount);
                    isReady = true;
                }
            };

            FindViewById<Button>(Resource.Id.exitDialogButton).Click += (sender, e) =>
            {
                toast.Cancel();
                var intent = new Intent(this, typeof(StartActivity));
                LongPollServerService.Instance.GotNewMessageEvent -= _dialogAdapter.NewMessage;
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
                StartActivity(intent);
            };

            FindViewById<Button>(Resource.Id.friendsDialogButton).Click += (sender, e) =>//todo Попробовать переделать с помощью ActionBar.SetDisplayHomeAsUpEnabled(true);
            {
                toast.Cancel();
                var intent = new Intent(this, typeof(MainActivity));
                LongPollServerService.Instance.GotNewMessageEvent -= _dialogAdapter.NewMessage;
                intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);            
                StartActivity(intent);
            };
        }


        private async void SendMessage(int friendId)
        {
            var cryptedMessage = CryptingTool.CryptTool.Instance.MakingEnvelope(_messageText.Text);
            await _messageService.SendMessage(friendId, cryptedMessage);
        }

        //Для остальных
        //todo Есть одно лишнее обращение для тех, у кого сообщений в диалоге<20. Фиксить можно доп условием в активити, но надо ли?
        public async Task GetMessagesAsync(int friendId) //todo попробовать вынести в отдельный класс
        {
            var messages = (await _messageService.GetDialog(friendId, _dialogAdapter.Count)).ToList();
            foreach (var message in messages)
            {
                message.Body = CryptingTool.CryptTool.Instance.SplitAndUnpackReceivedMessage(message.Body);
            }
            _dialogAdapter.AddMessages(messages);
        }

        private async void GetFriend(int friendId)
        {
            _friend = await _userService.GetUser(friendId);
            FindViewById<TextView>(Resource.Id.dialogFriendTextView).Text = _friend.FullName;
            Bitmap imageBitmap = null;

            using (var webClient = new WebClient())//todo Возможно сделать async
            {

                var imageBytes = webClient.DownloadData(_friend.PhotoUrl);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    FindViewById<ImageView>(Resource.Id.dialogFriendImageView).SetImageBitmap(imageBitmap);

                }
            }
        }

    }
}

