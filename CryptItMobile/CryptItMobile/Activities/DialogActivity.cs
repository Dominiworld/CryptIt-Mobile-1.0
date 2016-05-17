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
        private Message _myMessage=new Message();
        private Toast toast;
        private FileWorker _fileWorker;
        static readonly int READ_REQUEST_CODE = 1337;
        private string _file;
        private bool _fileUpload;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Dialog);
            // Create your application here
            _fileWorker=new FileWorker(this);
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

            var selectFileButton = FindViewById<ImageButton>(Resource.Id.select_file);
            selectFileButton.Click += (sender, e) =>
            {
                //var intent = new Intent(this, typeof (FileDialogActivity));
                //StartActivityForResult(intent, READ_REQUEST_CODE);
                _fileWorker.SendKeyRequest(_friendId);
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
            Intent intent = null;
            switch (item.ItemId)
            {
                case Resource.Id.exitMainButton:
                    intent = new Intent(this, typeof (StartActivity));
                    break;
                case Android.Resource.Id.Home:
                    intent = new Intent(this, typeof(MainActivity));
                    break;              
                default:
                    return base.OnOptionsItemSelected(item);
            }

            LongPollServerService.Instance.GotNewMessageEvent -= NewMessage;
            intent.AddFlags(ActivityFlags.ClearTop).AddFlags(ActivityFlags.SingleTop);
            StartActivity(intent);            
            return base.OnOptionsItemSelected(item);
        }

        private async void SendMessage(int friendId)
        {
            _myMessage.Body = _messageText.Text;
            //добавляем полные имена файлов для расшифровки (#имя:ключ,имя:ключ)
            if (_myMessage.Attachments != null && _myMessage.Attachments.Any())
            {
                _myMessage.Body += '#' + string.Join(",", _myMessage.Attachments.Select(a => a.Document.FileName + ":" + a.EncryptedSymmetricKey).ToList());
            }
            Message cryptedMessage=new Message
            {
                Body = CryptingTool.CryptTool.Instance.MakingEnvelope(_myMessage.Body),
                UserId = _myMessage.UserId,
                Attachments = _myMessage.Attachments
            };


            if (_fileUpload)
            {
                await _messageService.SendMessage(friendId, cryptedMessage);
            }
            else
            {
                toast=Toast.MakeText(this, "File uploading", ToastLength.Short);
                toast.Show();
                Log.Debug("1", "toast!!!");
            }
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
            _fileWorker.ParseMessages(messages);
        }

        private async void GetFriend(int friendId)
        {
            _friend = await _userService.GetUser(friendId);
            FindViewById<TextView>(Resource.Id.dialogFriendTextView).Text = _friend.FullName;
            Bitmap imageBitmap = null;

            using (var webClient = new WebClient()) //todo Возможно сделать async
            {

                var imageBytes = webClient.DownloadData(_friend.PhotoUrl);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    FindViewById<ImageView>(Resource.Id.dialogFriendImageView).SetImageBitmap(imageBitmap);
                }
            }
        }

        public async void NewMessage(Message message)
        {
            //Был баг с полученем своих сообщений из другого одноврменно открытого клиента ВК 
            //(Я в браузере пишу сообщение собеседнику 1, в приложении я в диалоге с собеседником 2. 
            //Получаю в приложении свои сообщения, адресованные собеседнику 1 в диалоге с собеседником 2)
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

                    await _fileWorker.FindKeyRequestAndReply(message);
                    await _fileWorker.GetKeyFileFromMessage(message);
                }
                else
                {
                    message.IsNotRead = true;
                    if (_myMessage!=null)
                    {
                        message.Body = _myMessage.Body;
                        _myMessage = new Message();
                    }
                }
                _dialogAdapter.NewMessage(message);
                _dialogListView.SetSelection(_dialogAdapter.Count);
            }
            

        }

        public override void Finish()
        {
            base.Finish();
            Window.SetSoftInputMode(SoftInput.StateHidden);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
           if (requestCode == READ_REQUEST_CODE && resultCode == Result.Ok)
            {
                 _file = data.GetStringExtra("file");
                AddAttachment();
            }
        }

        private async void AddAttachment()
        {
            var attachment = new Attachment
            {
                Document = new Document(),
                Type = "doc"
            };
            if (_myMessage.Attachments == null)
            {
                _myMessage.Attachments = new List<Attachment>();
            }


            var fileNameHash = CryptingTool.CryptTool.Instance.CreateHash(_file) + ".txt";

            var key = CryptingTool.CryptTool.Instance.EncryptFile(_file, fileNameHash);
            _fileUpload = false;
            _sendButton.Enabled = false;

            var uploadedFile = await _fileWorker.UploadFile(_file, _friendId, attachment);
            File.Delete(fileNameHash);
            if (uploadedFile == null)
            {
                toast = Toast.MakeText(this, "Upload error!", ToastLength.Short);
                toast.Show();
                _fileUpload = true;
                _sendButton.Enabled = true;

                return;
            }


            attachment.Document.Id = uploadedFile.Id;
            attachment.Document.OwnerId = uploadedFile.OwnerId;
            attachment.Document.Url = uploadedFile.Url;
            attachment.Document.FileName = uploadedFile.FileName;
            attachment.EncryptedSymmetricKey = key;
            _myMessage.Attachments.Add(attachment);
            _fileUpload = true;
            _sendButton.Enabled = true;



        }
    }
}

