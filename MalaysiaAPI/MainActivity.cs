using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Locations;

using HtmlAgilityPack;

namespace MalaysiaAPI
{
	[Activity (Label = "Malaysia Air Pollutant Index", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
	public class MainActivity : Activity, ILocationListener
	{
		/*
		 App starts here, settings button will open settings page and user can config the app's behavior
		 as they wish from that page. The settings will be transfered thru StartActivityForResult and main
		 page's behavior will be configured according to the result transferred from settings page.
		 */

		//Revise these values
		int lowLvl = 25;
		int medLvl = 50;

		static readonly string TAG = "X:" + typeof(MainActivity).Name;
		TextView _addressTxt;
		Location _currentLocation;
		LocationManager _locationManager;
		TextView state;
		Address address;

		string _locationProvider;
		TextView _locationTxt;

		public async void OnLocationChanged(Location location)
		{
			_currentLocation = location;
			if (_currentLocation == null) {
				_locationTxt.Text = "Unable to determine your location";
			} else {
				_locationTxt.Text = string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude);
				Log.Debug(TAG, string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude));
				address = await ReverseGeocodeCurrentLocation ();

				if (address == null) {
					state.Text = "String: No Address";
				} else {
					state.Text = address.GetAddressLine (address.MaxAddressLineIndex - 1);
				}

				DisplayAddress (address);
			}
		}

		public void OnProviderDisabled(string provider) {
		}

		public void OnProviderEnabled(string provider) {
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug (TAG, "{0},{1}", provider, status);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//Our App starts here
			ImageButton setButton = FindViewById<ImageButton> (Resource.Id.set_button);
			TextView lvlIndicator = FindViewById<TextView> (Resource.Id.lvlVal);
			state = FindViewById<TextView> (Resource.Id.stateTxt);
			TextView region = FindViewById<TextView> (Resource.Id.regionTxt);

			string stateString = string.Format ("State: ");
//			state.SetText (stateString, TextView.BufferType.Normal);
			state.Text = stateString;
			int lvlInt = Convert.ToInt32(lvlIndicator.Text);
			var blue = Android.Graphics.Color.Argb (255, 0, 0, 153);
			var green = Android.Graphics.Color.Argb (255, 0, 153, 0);
			var red = Android.Graphics.Color.Argb (255, 153, 0, 0);
			if (lvlInt < lowLvl) {
				lvlIndicator.SetBackgroundColor(blue);
			}
			else if (lvlInt < medLvl && lvlInt > lowLvl) {
				lvlIndicator.SetBackgroundColor(green);
			}
			else if (lvlInt > medLvl) {
				lvlIndicator.SetBackgroundColor (red);
			}

			setButton.Click += delegate {
				var intent = new Intent(this, typeof(SettingsActivity));
				StartActivityForResult(intent, 0); //Request result from settings page to set main page
			};

			//Location Service
			//InitLocationManager will initialize the service (set criteria fine or coarse, getproviders based on criteria)
			//Then the location will always be updated onlocationchange
			_addressTxt = FindViewById<TextView> (Resource.Id.AddressTxt);
			_locationTxt = FindViewById<TextView> (Resource.Id.locationTxt);

			InitLocationManager ();
			HTMLDownload ();
		}

		void InitLocationManager()
		{
			_locationManager = (LocationManager)GetSystemService (LocationService);
			Criteria criteriaForLocation = new Criteria{ Accuracy = Accuracy.Fine };
			IList<string> acceptableLocationProviders = _locationManager.GetProviders (criteriaForLocation, true);

			if (acceptableLocationProviders.Any ()) {
				_locationProvider = acceptableLocationProviders.First ();
			} else {
				_locationProvider = string.Empty;
			}
			Log.Debug (TAG, "Using " + _locationProvider + ".");
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			_locationManager.RemoveUpdates (this);
			Log.Debug (TAG, "Stop listening");
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			_locationManager.RequestLocationUpdates (_locationProvider, 0, 0, this);
			Log.Debug (TAG, "listening to location update via " + _locationProvider);
		}

		protected override void OnRestart ()
		{
			base.OnRestart ();
		}

		protected override void OnStart ()
		{
			base.OnStart ();
		}

		protected override void OnActivityResult(int requestcode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestcode, resultCode, data);
			if (resultCode == Result.Ok) {
				string settingsConfig = Convert.ToString(data.GetStringExtra("settings"));
				string[] settingsConfigSplit = settingsConfig.Split (',');
				string stateString = string.Format("State: {0}",settingsConfigSplit [0]);
				string regionString = string.Format("Region: {0}",settingsConfigSplit [1]);

				state = FindViewById<TextView> (Resource.Id.stateTxt);
				TextView region = FindViewById<TextView> (Resource.Id.regionTxt);
//				state.SetText (stateString, TextView.BufferType.Normal);
				state.Text = stateString;
				region.SetText (regionString, TextView.BufferType.Normal);
			}
		}

		async Task<Address> ReverseGeocodeCurrentLocation()
		{
			Geocoder geocoder = new Geocoder (this);
			IList<Address> addressList = await geocoder.GetFromLocationAsync (_currentLocation.Latitude, _currentLocation.Longitude, 10);
			Address address = addressList.FirstOrDefault ();
			return address;
		}

		void DisplayAddress(Address address)
		{
			if (address != null) {
				StringBuilder deviceAddress = new StringBuilder ();
				for (int i = 0; i < address.MaxAddressLineIndex; i++) {
					deviceAddress.AppendLine (address.GetAddressLine (i));
				}
				_addressTxt.Text = deviceAddress.ToString ();
			} else {
				_addressTxt.Text = "Unable to determine address";
			}
		}

		async void HTMLDownload ()
		{
			HtmlWeb htmlWeb = new HtmlWeb ();
			HtmlDocument htmlDoc = new HtmlDocument ();

			htmlDoc = await htmlWeb.LoadFromWebAsync ("http://apims.doe.gov.my/v2/hour2_2016-02-03.html");

			var div = htmlDoc.GetElementbyId ("content");
			var table = div.Element ("table");
			state.Text = div == null ? "null" : div.InnerHtml.ToString ();
			int a = 5;
		}
	}
}

