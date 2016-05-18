using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.Views;
using Android.Widget;
using CryptItMobile.Activities;
using CryptItMobile.Model;
using Model;
using vkAPI;

namespace CryptItMobile.Adapters
{
    public class FriendsAdapter: BaseAdapter
    {
        private LayoutInflater lInflater;
        private UserService _userService = new UserService();
        private List<AndroidUser> _friends;//Друзья отображаемые при поиске
        private List<AndroidUser> _allFriends;//список всех друзей, чтобы не грузить по несколько раз
        private FileWorker _fileWorker;
        private MessageService _messageService = new MessageService();
        private Context _ctx;

        public FriendsAdapter(Context context)
        {
            _fileWorker = new FileWorker(context);
            _ctx = context;
            GetImageBitmapFromUrl();
            lInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
        }

        public override long GetItemId(int position)
        {
            return _friends[position].User.Id;
        }

        public void SetFriendKey(int position)
        {
            _fileWorker.SetFriendKey(_friends[position]);
            CryptingTool.CryptTool.Instance.keyRSARemote = _friends[position].PublicKey;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = lInflater.Inflate(Resource.Layout.Friend, null, false);

            if (_friends[position].Avatar!=null)
            {
                view.FindViewById<ImageView>(Resource.Id.friendImageView).SetImageBitmap(_friends[position].Avatar);
            }
            else
            {
                view.FindViewById<ImageView>(Resource.Id.friendImageView).SetImageResource(Resource.Drawable.Camera);
            }

            view.FindViewById<TextView>(Resource.Id.friendTextView).Text = _friends[position].User.FullName;
            if (_friends[position].User.Status == "Online")
            {
                view.FindViewById<ImageView>(Resource.Id.onlineImageView).Visibility= ViewStates.Visible;
            }
            else
            {
                view.FindViewById<ImageView>(Resource.Id.onlineImageView).Visibility = ViewStates.Invisible;
            }

            var numberOfNewMessagesText = view.FindViewById<TextView>(Resource.Id.newMessageTextView);
            numberOfNewMessagesText.Text =
                _friends[position].User.NumberOfNewMessages.ToString();

            if (_friends[position].User.NumberOfNewMessages==0)
            {
                numberOfNewMessagesText.Visibility = ViewStates.Invisible;
            }
            return view;
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }


        public override int Count
        {
            get
            {
                if (_friends!=null)
                {
                    return _friends.Count();
                }
                
                return 0;
            }
        }

        private async Task GetFriends()
        {
            List<User> users;
            try
            {
                users = (await _userService.GetFriends(AuthorizeService.Instance.CurrentUserId)).ToList();
            }
            catch (Exception)
            {
                Toast.MakeText(_ctx, "Ошибка соединения. Попробуйте перезапустить приложение.", ToastLength.Short);
                return;
            }
            _allFriends = new List<AndroidUser>();

            foreach (var friend in users)
            {
                _allFriends.Add(new AndroidUser
                {
                    Avatar = null,
                    User = friend
                });

            }
           
            _allFriends =_allFriends.OrderBy(f => f.User.LastName).ToList();
            _friends = _allFriends.OrderBy(f => f.User.LastName).ToList();
            ((MainActivity)_ctx).FinishLoader();
            NotifyDataSetChanged();
        }

        public void Search(string searchString)
        {
            if (_allFriends != null)
            {
                _friends = string.IsNullOrEmpty(searchString)
                    ? _allFriends
                    : _allFriends.Where(f => f.User.FullName.ToLower().Contains(searchString.ToLower())).ToList();
                NotifyDataSetChanged();
            }
            //else Вставить чего для торопыг, которые ищут, когда друзей еще не подгрузили
        }

        private async void GetImageBitmapFromUrl()
        {
            await GetFriends();
            Bitmap imageBitmap = null;

            using (var webClient = new WebClient())
            {
                foreach (var friend in _allFriends)
                {
                    var imageBytes = await webClient.DownloadDataTaskAsync(friend.User.PhotoUrl);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                        friend.Avatar = imageBitmap;
                        NotifyDataSetChanged();
                    }
                }    
            }
            GetDialogsInfo();
        }

        public async void NewMessage(Message message)
        {
            if (message.ChatId != 0)
                return;
            var friend = _friends.FirstOrDefault(fr => fr.User.Id == message.UserId && !message.Out);
            if (friend != null)
            {
                friend.User.NumberOfNewMessages++;
                _friends.Remove(friend);
                _friends.Insert(0, friend);
                NotifyDataSetChanged();
                await _fileWorker.FindKeyRequestAndReply(message);
                await _fileWorker.GetKeyFileFromMessage(message);
            }
        }

        public void MessageStateChangedToRead(int lastReadId, int peerId)
        {
            var friend = _friends.FirstOrDefault(fr => fr.User.Id == peerId);
            if (friend != null)
            {
                friend.User.NumberOfNewMessages = 0;
                var readDialogs = _friends.Where(f => f.User.NumberOfNewMessages == 0).OrderBy(f => f.User.LastName);
                _friends = _allFriends.Where(f => f.User.NumberOfNewMessages > 0).ToList();
                _friends.AddRange(readDialogs);
            }
            NotifyDataSetChanged();
        }

        public void UserBecameOnlineOrOffline(int userId, bool online) //online = 1, если стал онлайн
                                                                       //=0, если стал оффлайн
        {
            var friend = _friends.FirstOrDefault(fr => fr.User.Id == userId);
            if (friend != null)
            {
                if (online)
                {
                    friend.User.Online = 1;
                }
                else
                {
                    friend.User.Online = 0;
                }
                NotifyDataSetChanged();
            }
            
        }

        private async void GetDialogsInfo()
        {
            try
            {
                var unreadDialogs = await _messageService.GetDialogs(true);
                foreach (var dialog in unreadDialogs)
                {
                    if (dialog.Message.ChatId != 0)
                        continue;
                    var friend = _allFriends.FirstOrDefault(f => f.User.Id == dialog.Message.UserId);
                    if (friend != null)
                    {
                        friend.User.NumberOfNewMessages = dialog.UnreadMessagesAmount;
                    }
                }
                var newDialogFriends = _friends.Where(f => f.User.NumberOfNewMessages > 0).ToList();
                _friends = _friends.Where(f => f.User.NumberOfNewMessages == 0).ToList();
                _friends.InsertRange(0, newDialogFriends);
                NotifyDataSetChanged();

            }
            catch (WebException)
            {

            }

        }
    }
}
