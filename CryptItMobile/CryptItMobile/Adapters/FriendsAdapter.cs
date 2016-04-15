using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Javax.Crypto.Interfaces;
using Model;
using vkAPI;

namespace CryptItMobile.Adapters
{
    public class FriendsAdapter: BaseAdapter
    {
        private LayoutInflater lInflater;
        private UserService _userService = new UserService();
        public List<User> _friends;//todo Подумать, как сделать приватным (нужен для интента в main)
        private List<User> _allFriends;

       
        public FriendsAdapter(Context context)
        {
            GetFriends();
            
            lInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
        }

       
        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = lInflater.Inflate(Resource.Layout.Friend, null, false);

            var imageBitmap = GetImageBitmapFromUrl(_friends[position].PhotoUrl);
            view.FindViewById<ImageView>(Resource.Id.friendImageView).SetImageBitmap(imageBitmap);


            view.FindViewById<TextView>(Resource.Id.friendTextView).Text = _friends[position].FullName;
            //todo Сделать точку для друзей в онлайне
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

        private async void GetFriends()
        {
            _allFriends = (await _userService.GetFriends(AuthorizeService.Instance.CurrentUserId)).ToList();
            _allFriends=_allFriends.OrderBy(f => f.LastName).ToList();
            _friends = _allFriends.OrderBy(f => f.LastName).ToList();
            NotifyDataSetChanged();
        }

        private Bitmap GetImageBitmapFromUrl(string url)//todo Сделать async, перенести в отдельный класс
        {
            Bitmap imageBitmap = null;

            using (var webClient = new WebClient())
            {
                var imageBytes = webClient.DownloadData(url);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                }
            }

            return imageBitmap;
        }

        public void Search(string searchString)
        {
            if (_allFriends != null)
            {
                _friends = string.IsNullOrEmpty(searchString)
                    ? _allFriends
                    : _allFriends.Where(f => f.FullName.ToLower().Contains(searchString.ToLower())).ToList();
                NotifyDataSetChanged();
            }
            //else Вставить чего для торопыг
        }
    }
}