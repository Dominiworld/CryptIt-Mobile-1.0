using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using vkAPI;
using Message = Model.Message;

namespace CryptItMobile.Adapters
{
    public class DialogAdapter : BaseAdapter
    {
        private List<Message> _messages;
        private MessageService _messageService = new MessageService();
        private LayoutInflater lInflater;

        public DialogAdapter(Context context, int friendId)
        {
            GetMessages(friendId);
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
            }
             


            return view;
        }

        public async void GetMessages(int friendId) //todo попробовать убрать public
        {
            if (_messages == null)
            {
                _messages = (await _messageService.GetDialog(friendId)).ToList();
            }
            else
            {
                _messages.AddRange((await _messageService.GetDialog(friendId, Count)).ToList());
            }
            
            NotifyDataSetChanged();
        }

        public void NewMessage(Message message) //todo на рефакторинг 
        {
            _messages.Insert(0,message);
            NotifyDataSetChanged();
        }
       
    }
}