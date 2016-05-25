using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using CryptItMobile.Activities;
using Felipecsl.GifImageViewLibrary;
using vkAPI;
using Message = Model.Message;

namespace CryptItMobile.Adapters//todo ������ ��� ��� ��������� �� ����� ������ ���������
{
    public class DialogAdapter : BaseAdapter
    {
        private List<Message> _messages;
        private MessageService _messageService = new MessageService();
        private LayoutInflater lInflater;
        private FileWorker _fileWorker;

        private int _friendId;//todo �������� ��� ����
        private Context _ctx;//todo �������� ��� ����

        public DialogAdapter(Context context, int friendId)
        {
            _ctx = context;
            _friendId = friendId;
            _messages=new List<Message>();
            GetMessages(_friendId);
            lInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            _fileWorker = new FileWorker(_ctx);
        }

        public override int Count
        {
            get {
                if (_messages != null)
                {
                    return _messages.Count();
                }

                return 0;
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)//todo ��������� convertView
        {
            View view;
            position = Count-1 - position;
            if (!_messages[position].Out)//���������� ��������� �� �����
            {
                view = lInflater.Inflate(Resource.Layout.FriendMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.friendMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.friendMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                
            }
            else//���������� ������ ���������
            {
                view = lInflater.Inflate(Resource.Layout.MyMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.myMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.myMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                if (_messages[position].IsNotRead)
                {
                    view.FindViewById<LinearLayout>(Resource.Id.myMessageIsRead).SetBackgroundColor(Color.AliceBlue);
                    //view.FindViewById<LinearLayout>(Resource.Id.myMessageIsRead).SetBackgroundResource(Resource.Color);//todo ����� �������������
                }
                else
                {
                    view.FindViewById<LinearLayout>(Resource.Id.myMessageIsRead).SetBackgroundColor(Color.White);
                }
            }
            return view;
        }

        public void AddMessages(List<Message> messages)
        {
            _messages.AddRange(messages);
            NotifyDataSetChanged();
        }

        public void NewMessage(Message message)
        {
            _messages.Insert(0, message);
            NotifyDataSetChanged();

        }

        public void MessageStateChangedToRead(int lastReadId, int peerId)//todo ������ ��������
        {
            if (_friendId==peerId)
            {
                foreach (var message in _messages)
                {
                    if (message.Id <= lastReadId)
                    {
                        message.IsNotRead = false;
                    }
                }
                NotifyDataSetChanged();
            }
        }

        //��� ������ ����� ���������
        private async void GetMessages(int friendId) //todo ����������� ������� � ��������� �����
        {
            var messages = (await _messageService.GetDialog(friendId)).ToList();
            foreach (var message in messages)
            {
                message.Body = CryptingTool.CryptTool.Instance.SplitAndUnpackReceivedMessage(message.Body);
            }
            List<int> messageList = messages.Where(m => !m.Out).Select(m => m.Id).ToList();
            _messageService.MarkMessagesAsRead(messageList, friendId);
            ((DialogActivity) _ctx).FinishLoader();
            AddMessages(messages);
            var sentKeyResult = await _fileWorker.ParseMessages(messages);

            switch (sentKeyResult)
            {
                case 1: //��������� ����� �� request
                    ((DialogActivity)_ctx)._myMessage.Body = "key";
                    break;
                case 0: //�� ������ request
                    break;
                case -1: //�� ������� ���������(��������) ���� � ������ ��� ��������
                    Toast.MakeText(_ctx, Resource.String.KeyFileUploadError, ToastLength.Short).Show();
                    break;
                case -2: //�� ������� ��������� ���������
                    Toast.MakeText(_ctx, Resource.String.KeyMessageSendError, ToastLength.Short).Show();
                    break;
            }
            if (CryptingTool.CryptTool.Instance.keyRSARemote != null)
            {
                ((DialogActivity) _ctx)._sendButton.Enabled = true;
            }
        }

    }
}