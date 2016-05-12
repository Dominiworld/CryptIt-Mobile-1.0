using System.Collections.Generic;
using System.IO;
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
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using CryptItMobile.Activities;
using Felipecsl.GifImageViewLibrary;
using Java.IO;
using Model;
using File = System.IO.File;

namespace CryptItMobile.Activities
{
    [Activity(Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class DialogActivity : Activity
    {

        private ListView _dialogListView;
        private DialogAdapter _dialogAdapter;
        private Button _sendButton;
        private EditText _messageText;
        private MessageService _messageService = new MessageService();
        private UserService _userService = new UserService();
        private User _friend;
        private int _friendId;
        private string _myMessage;
        private Toast toast;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Dialog);
            // Create your application here
            Window.SetSoftInputMode(SoftInput.StateHidden);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);

            _friendId = (int) Intent.GetLongExtra("FriendId", 0);
            GetFriend(_friendId);

            StartLoader();

            _dialogListView = FindViewById<ListView>(Resource.Id.dialogListView);
            _dialogAdapter = new DialogAdapter(this, _friendId);

            
            LongPollServerService.Instance.GotNewMessageEvent += NewMessage;
            LongPollServerService.Instance.OutMessageStateChangedToReadEvent += _dialogAdapter.MessageStateChangedToRead;

            _dialogListView.Adapter = _dialogAdapter;

            _sendButton = FindViewById<Button>(Resource.Id.enterButton);
            toast = Toast.MakeText(this, Resource.String.NoPublicKey, ToastLength.Long);
            if (CryptingTool.CryptTool.Instance.keyRSARemote == null)
            {
                _sendButton.Enabled = false;
                toast.Show();
            }

            _messageText = FindViewById<EditText>(Resource.Id.messageEditText);

            _sendButton.Click += (sender, e) =>
            {
                SendMessage(_friendId);
                _messageText.Text = string.Empty;
            };

            //todo сделать loader

            bool isReady = true;

            _dialogListView.Scroll += async (sender, e) =>
            {
                if (_dialogListView.FirstVisiblePosition != 0 || _dialogAdapter.Count == 0 || !isReady) return;
                isReady = false;
                var oldCount = _dialogAdapter.Count;
                await GetMessagesAsync(_friendId);
                if (_dialogAdapter.Count - oldCount != 0)
                {
                    _dialogListView.SetSelection(_dialogAdapter.Count - oldCount);
                    isReady = true;
                }
            };

        }

        private void StartLoader()
        {
            var view = FindViewById<GifImageView>(Resource.Id.dialogLoaderImageView);

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
            var view = FindViewById<GifImageView>(Resource.Id.dialogLoaderImageView);
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
            toast.Cancel();
            var intent = item.ItemId == Resource.Id.exitMainButton ?
                new Intent(this, typeof (StartActivity)) :
                new Intent(this, typeof(MainActivity));
            LongPollServerService.Instance.GotNewMessageEvent -= NewMessage;
            intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
            StartActivity(intent);
            return base.OnOptionsItemSelected(item);
        }

        private async void SendMessage(int friendId)
        {
            _myMessage = _messageText.Text;
            var cryptedMessage = CryptingTool.CryptTool.Instance.MakingEnvelope(_myMessage);
            await _messageService.SendMessage(friendId, cryptedMessage);
        }

        //ƒл€ остальных
        //todo ≈сть одно лишнее обращение дл€ тех, у кого сообщений в диалоге<20. ‘иксить можно доп условием в активити, но надо ли?
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

            using (var webClient = new WebClient()) //todo ¬озможно сделать async
            {

                var imageBytes = webClient.DownloadData(_friend.PhotoUrl);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    FindViewById<ImageView>(Resource.Id.dialogFriendImageView).SetImageBitmap(imageBitmap);
                }
            }
        }

        public void NewMessage(Message message)
        {
            //Ѕыл баг с полученем своих сообщений из другого одноврменно открытого клиента ¬  
            //(я в браузере пишу сообщение собеседнику 1, в приложении € в диалоге с собеседником 2. 
            //ѕолучаю в приложении свои сообщени€, адресованные собеседнику 1 в диалоге с собеседником 2)
            if(message.ChatId!=0)
                return;
            
            if (message.UserId == _friendId || message.UserId == AuthorizeService.Instance.CurrentUserId)
            {
                if (!message.Out)
                {
                    message.IsNotRead = false;
                    List<int> messageList = new List<int> {message.Id};
                    _messageService.MarkMessagesAsRead(messageList, message.UserId);
                    message.Body = CryptingTool.CryptTool.Instance.SplitAndUnpackReceivedMessage(message.Body);
                }
                else
                {
                    message.IsNotRead = true;
                    if (_myMessage!=null)
                    {
                        message.Body = _myMessage;
                        _myMessage = null;
                    }
                }
                _dialogAdapter.NewMessage(message);
            }
            _dialogListView.SetSelection(_dialogAdapter.Count);

        }

        public override void Finish()
        {
            base.Finish();
            Window.SetSoftInputMode(SoftInput.StateHidden);
        }
    }
}

