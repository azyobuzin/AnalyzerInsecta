using System;
using Android.App;
using Android.OS;

namespace SampleXamarinApp
{
    [Activity(Label = "SampleXamarinApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            // SetContentView (Resource.Layout.Main);

            Console.WriteLine($"This class name is {nameof(MainActivity)}.");
            Console.WriteLine($"The namespace name is {nameof(SampleXamarinApp)}.");
        }
    }
}
