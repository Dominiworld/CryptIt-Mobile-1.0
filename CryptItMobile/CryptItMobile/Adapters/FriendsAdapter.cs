using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.Views;
using Android.Widget;
using CryptItMobile.Model;
using Model;
using vkAPI;

namespace CryptItMobile.Adapters
{
    public class FriendsAdapter: BaseAdapter//todo Оптимизировать загрузку картинок
    {
        private LayoutInflater lInflater;
        private UserService _userService = new UserService();
        private List<AndroidUser> _friends;//Друзья отображаемые при поиске
        private List<AndroidUser> _allFriends;//список всх друзей, чтобы не грузить по несколько раз

        public FriendsAdapter(Context context)
        {
            GetImageBitmapFromUrl();
            lInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
        }

       
        public override long GetItemId(int position)
        {
            return _friends[position].User.Id;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = lInflater.Inflate(Resource.Layout.Friend, null, false);

            if (_friends[position].Avatar!=null)
            {
                view.FindViewById<ImageView>(Resource.Id.friendImageView).SetImageBitmap(_friends[position].Avatar);
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

            view.FindViewById<TextView>(Resource.Id.newMessageTextView).Text =
                _friends[position].User.NumberOfNewMessages.ToString();
            return view;
        }
        /// <summary>
        /// Возвращает id друга
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
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
            var users = (await _userService.GetFriends(AuthorizeService.Instance.CurrentUserId)).ToList();
            _allFriends = new List<AndroidUser>();

            foreach (var friend in users)
            {
                _allFriends.Add(new AndroidUser
                {
                    Avatar = null,
                    User = friend
                });
            }

            _allFriends=_allFriends.OrderBy(f => f.User.LastName).ToList();
            _friends = _allFriends.OrderBy(f => f.User.LastName).ToList();
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

        private async void GetImageBitmapFromUrl() //todo Перенести в отдельный класс, сделать async
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
                    }
                }    
            }
        }

        public void NewMessage(Message message) 
        {
            var friend = _friends.FirstOrDefault(fr => fr.User.Id == message.UserId&&!message.Out);
            if (friend != null)
            {
                friend.User.NumberOfNewMessages++;
                NotifyDataSetChanged();
            }

            
        }

        public void MessageStateChangedToRead(int lastReadId, int peerId)//todo сильно подумать
        {
            var friend = _friends.FirstOrDefault(fr => fr.User.Id == peerId);
            if (friend != null) friend.User.NumberOfNewMessages=0;
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
    }
}