using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics.Drawables;
using Android.Content;
using InApp.Droid.Services;

namespace InApp.Droid
{
    [Activity(Label = "InApp", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        public static MainActivity Instance;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            MainActivity.Instance = this;

            ActionBar.SetIcon(new ColorDrawable(Resources.GetColor(Android.Resource.Color.Transparent)));

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // Ask the in-app purchasing service connection's billing handler to process this request
            InAppService inAppService = App.ViewModel.TheInAppService as InAppService;
            inAppService.HandleActivityResult(requestCode, resultCode, data);
        }

        protected override void OnDestroy()
        {
            // Disconnect from the in-app purchasing service
            InAppService inAppService = App.ViewModel.TheInAppService as InAppService;
            inAppService.OnDestroy();

            base.OnDestroy();
        }
    }
}

