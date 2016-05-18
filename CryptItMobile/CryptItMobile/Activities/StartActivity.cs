using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Java.Net;
using vkAPI;

namespace CryptItMobile.Activities
{
    [Activity(Label = "CryptIt Mobile",  MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class StartActivity : Activity
    {
        private WebView _webView;
        private static readonly string AuthorizeUrl = AuthorizeService.Instance.GetAuthorizeUrl(5296011);
        private static Context _ctx;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += (sender, args) =>
            {
                Log.Debug("EXCEPTION", args.ExceptionObject.ToString());
            };
           
            RequestWindowFeature(WindowFeatures.NoTitle);

            // Create your application here
            SetContentView(Resource.Layout.Start);
            _ctx = this;
            _webView = FindViewById<WebView>(Resource.Id.webView);

            _webView.SetWebViewClient(new MyWebViewClient());
            _webView.LoadUrl(AuthorizeUrl);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            _webView.LoadUrl(AuthorizeUrl);
        }

        public class MyWebViewClient : WebViewClient
        {

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
              
                var parseRef = new URL(url).Ref;
                if (parseRef != null)
                {
                    var parseFields = parseRef.Split('&');

                    AuthorizeService.Instance.AccessToken = parseFields[0].Split('=')[1];
                    AuthorizeService.Instance.CurrentUserId = int.Parse(parseFields[2].Split('=')[1]);
                    AuthorizeService.Instance.GetCurrentUser();

                    var intent = new Intent(_ctx, typeof(MainActivity));//todo ѕодумать, как сделать по-нормальному
                    _ctx.StartActivity(intent);
                }

                return false;
            }
        }
    }
}