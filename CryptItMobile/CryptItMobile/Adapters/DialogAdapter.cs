using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Views;
using Android.Widget;
using vkAPI;
using Message = Model.Message;

namespace CryptItMobile.Adapters//todo –ешить баг при прокрутке до конца списка сообщений
{
    public class DialogAdapter : BaseAdapter
    {
        private List<Message> _messages;
        private MessageService _messageService = new MessageService();
        private LayoutInflater lInflater;
        
        private int _friendId;//todo подумать над этим

        public DialogAdapter(Context context, int friendId)
        {
            _friendId = friendId;
            _messages=new List<Message>();
            GetMessages(_friendId);
            lInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
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
            if (!_messages[position].Out)//«аполнение сообщени€ от друга
            {
                view = lInflater.Inflate(Resource.Layout.FriendMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.friendMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.friendMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                
            }
            else//заполнение своего сообщени€
            {
                view = lInflater.Inflate(Resource.Layout.MyMessage, null, false);
                view.FindViewById<TextView>(Resource.Id.myMessageTextView).Text = _messages[position].Body;
                view.FindViewById<TextView>(Resource.Id.myMessageTimeTextView).Text =
                    _messages[position].Date.ToString();
                if (!_messages[position].IsNotRead)
                {
                    view.FindViewById<TextView>(Resource.Id.myMessageIsReadTextView).Visibility=ViewStates.Invisible;//todo «десь прочитанность
                }
                
            }
            return view;
        }

        public void AddMessages(List<Message> messages)
        {
            _messages.AddRange(messages);
            NotifyDataSetChanged();
        }

        public void NewMessage(Message message) //todo на рефакторинг 
        {
            //Ѕыл баг с полученем своих сообщений из другого одноврменно открытого клиента ¬  
            //(я в браузере пишу сообщение собеседнику 1, в приложении € в диалоге с собеседником 2. 
            //ѕолучаю в приложении свои сообщени€, адресованные собеседнику 1 в диалоге с собеседником 2)
            if (message.UserId==_friendId||message.UserId==AuthorizeService.Instance.CurrentUserId)
            {
                if (!message.Out)
                {
                    message.IsNotRead = false;
                    List<int> messageList = new List<int> {message.Id};
                    _messageService.MarkMessagesAsRead(messageList, message.UserId);
                }
                else
                {
                    message.IsNotRead = true;
                }
                _messages.Insert(0, message);
                //_messages.Add(message);
                NotifyDataSetChanged();
            }          
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

        //ƒл€ первой пачки сообщений
        private async void GetMessages(int friendId) //todo попробовать вынести в отдельный класс
        {
            
            _messages.AddRange((await _messageService.GetDialog(friendId)).ToList());
            List<int> messageList = _messages.Where(m => !m.Out).Select(m => m.Id).ToList();
            _messageService.MarkMessagesAsRead(messageList, friendId);
            NotifyDataSetChanged();
        }

        //ƒл€ остальных
        //todo ≈сть одно лишнее обращение дл€ тех, у кого сообщений в диалоге<20. ‘иксить можно доп условием в активити, но надо ли?
        public async Task GetMessagesAsync(int friendId) //todo попробовать вынести в отдельный класс
        {
            _messages.AddRange((await _messageService.GetDialog(friendId, Count)).ToList());
            NotifyDataSetChanged();
        }

    }
}