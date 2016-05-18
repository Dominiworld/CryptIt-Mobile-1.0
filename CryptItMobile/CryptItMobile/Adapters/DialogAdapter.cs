using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using CryptItMobile.Activities;
using Felipecsl.GifImageViewLibrary;
using vkAPI;
using Message = Model.Message;

namespace CryptItMobile.Adapters//todo Решить баг при прокрутке до конца списка сообщений
{
    public class DialogAdapter : BaseAdapter
    {
        private List<Message> _messages;
        private MessageService _messageService = new MessageService();
        private LayoutInflater lInflater;
        private FileWorker _fileWorker;

        private int _friendId;//todo подумать над этим
        private Context _ctx;//todo подумать над этим

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

        public override View GetView(int position, View convertView, ViewGroup parent)//todo припилить convertView
        {
            View view;
            position = Count-1 - position;
            if (!_messages[position].Out)//Заполнение сообщения от друга
            {
                view = lInflater.Inflate(Resource.Layout.FriendMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.friendMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.friendMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                
            }
            else//заполнение своего сообщения
            {
                view = lInflater.Inflate(Resource.Layout.MyMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.myMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.myMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                if (!_messages[position].IsNotRead)
                {
                    view.FindViewById<TextView>(Resource.Id.myMessageIsReadTextView).Visibility=ViewStates.Invisible;//todo Здесь прочитанность
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

        public void MessageStateChangedToRead(int lastReadId, int peerId)//todo сильно подумать
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

        //Для первой пачки сообщений
        private async void GetMessages(int friendId) //todo попробовать вынести в отдельный класс
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
            var sentKey = await _fileWorker.ParseMessages(messages);
            if (sentKey)
            {
                ((DialogActivity)_ctx)._myMessage.Body = _fileWorker._requestKeyString;
            }
            if (CryptingTool.CryptTool.Instance.keyRSARemote != null)
            {
                ((DialogActivity) _ctx)._sendButton.Enabled = true;
            }
        }

    }
}