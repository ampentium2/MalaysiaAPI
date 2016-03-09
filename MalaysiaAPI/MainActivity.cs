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
using Java.Util;

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

		//Values are based APIMS guide.
		int lowLvl = 50;
		int medLvl = 100;
		int highLvl = 200;
		int alarmLvl = 300;

		static readonly string TAG = "X:" + typeof(MainActivity).Name;
		Location _currentLocation;
		LocationManager _locationManager;
		TextView state;
		TextView region;
		Address address;
		TextView lvlIndicator;
		TextView loadingText;
		GridLayout mainLayout;
		static List<string> regionEntry;
		static List<string> latestAPI;
		string _locationProvider;
		string errorString;

		//for debug
		int x = 10;

		Android.Graphics.Color blue = Android.Graphics.Color.Argb (255, 0, 0, 153);
		Android.Graphics.Color green = Android.Graphics.Color.Argb (255, 0, 153, 0);
		Android.Graphics.Color red = Android.Graphics.Color.Argb (255, 153, 0, 0);
		Android.Graphics.Color yellow = Android.Graphics.Color.Argb (255, 153, 153, 0);
		Android.Graphics.Color orange = Android.Graphics.Color.Argb (255, 255, 153, 0);

		public async void OnLocationChanged(Location location)
		{
			_currentLocation = location;

			if (_currentLocation == null) {
				state.Text = "Location is not available";

			} else {
				Log.Debug (TAG, string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude));
				//Please use Emulator API23. Geocoder is not available on Emulator API19, got bug
				address = await ReverseGeocodeCurrentLocation ();

				if (address == null) {
					state.Text = "String: No Address";
				} else {
					state.Text = address.GetAddressLine (address.MaxAddressLineIndex - 1);
					int complete = await HTMLDownload (address.GetAddressLine (address.MaxAddressLineIndex - 1));
					if (complete == 1) {
						int locationIndex = GetNearestLocationIndex ();
						UpdateUI (locationIndex);
					} else {
						UpdateUI (999);
					}
				}
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
			lvlIndicator = FindViewById<TextView> (Resource.Id.lvlVal);
			state = FindViewById<TextView> (Resource.Id.stateTxt);
			region = FindViewById<TextView> (Resource.Id.regionTxt);
			mainLayout = FindViewById<GridLayout> (Resource.Id.gridLayout1);
			loadingText = FindViewById<TextView> (Resource.Id.loadText);
			regionEntry = new List<string> ();
			latestAPI = new List<string> ();

			string stateString = string.Format ("State: ");
			state.Text = stateString;

			//TODO: Might Eliminate settings button
			setButton.Click += delegate {
				var intent = new Intent(this, typeof(SettingsActivity));
				StartActivityForResult(intent, 0); //Request result from settings page to set main page
			};

			//Location Service
			//InitLocationManager will initialize the service (set criteria fine or coarse, getproviders based on criteria)
			//Then the location will always be updated onlocationchange
			InitLocationManager ();
		}

		void InitLocationManager()
		{
			_locationManager = (LocationManager)GetSystemService (LocationService);
			Criteria criteriaForLocation = new Criteria{ Accuracy = Accuracy.Coarse, PowerRequirement = Power.Low };
			string acceptableLocationProviders = _locationManager.GetBestProvider (criteriaForLocation, true);

			if (acceptableLocationProviders.Any ()) {
				_locationProvider = acceptableLocationProviders;

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
				region = FindViewById<TextView> (Resource.Id.regionTxt);
				state.Text = stateString;
				region.Text = regionString;
			}
		}

		async Task<Address> ReverseGeocodeCurrentLocation()
		{
			Locale setLocale = new Locale ("ms");
			Geocoder geocoder = new Geocoder (this, setLocale);
			IList<Address> addressList = await geocoder.GetFromLocationAsync (_currentLocation.Latitude, _currentLocation.Longitude, 10);
			Address addressReversed = addressList.FirstOrDefault ();
			return addressReversed;
		}

		Address AddressFromArea(string locationName)
		{
			Locale setLocale = new Locale ("ms");
			Geocoder geocoder = new Geocoder (this, setLocale);
			IList<Address> addressList = geocoder.GetFromLocationName (locationName, 10);
			Address addressArea = addressList.SingleOrDefault (loc => loc.CountryName == "Malaysia");
			return addressArea;
		}

		async Task<int> HTMLDownload (string stateRequested)
		{
			HtmlWeb htmlWeb = new HtmlWeb ();
			HtmlDocument htmlDoc = new HtmlDocument ();
			DateTime currentDateTime = DateTime.Now;
			TimeSpan currentTime = currentDateTime.TimeOfDay;
			int hourIndex = 3;
			int completeStatus = 0;

			regionEntry.Clear ();
			latestAPI.Clear();

			string hourRegion = string.Empty;
			//A problem will occur if it's 1st day of month at 12AM. Date is 0-3-2016
			string currentDay = string.Empty;
			if (currentDateTime.Day == 1) {
				currentDay = currentDateTime.Day.ToString ("D2");
			} else {
				currentDay = currentTime.Hours == 0 ? (currentDateTime.Day - 1).ToString ("D2") : currentDateTime.Day.ToString ("D2");
			}

			string date = currentDateTime.Year.ToString () + "-" + currentDateTime.Month.ToString ("D2") + "-" + currentDay;
			int currentHour = currentTime.Hours == 0 ? 24 : currentTime.Hours;

			if (currentHour > 0 && currentHour <= 6) { hourRegion = "hour1"; hourIndex += currentHour - 1; }
			else if (currentHour > 6 && currentHour <= 12) { hourRegion = "hour2"; hourIndex += (currentHour - 6 - 1); }
			else if (currentHour > 12 && currentHour <= 18) { hourRegion = "hour3"; hourIndex += (currentHour - 12 - 1);}
			else if (currentHour > 18 && currentHour <= 24) { hourRegion = "hour4"; hourIndex += (currentHour - 18 - 1);}

			string urlConstruct = "http://apims.doe.gov.my/v2/" + hourRegion + "_" + date + ".html";

			try {
				htmlDoc = await htmlWeb.LoadFromWebAsync(urlConstruct);
				var div = htmlDoc.GetElementbyId ("content");
				var table = div.Descendants ("table").ToList () [0].ChildNodes.ToList ();

				foreach (var tableEntry in table) {
					if (tableEntry.HasChildNodes) {
						var rowEntry = tableEntry.ChildNodes.ToList ();
						var stateEntry = rowEntry [0].InnerText.ToString ();
						if (stateEntry == stateRequested) {
							regionEntry.Add (rowEntry [2].InnerText.ToString ());
							latestAPI.Add (rowEntry [hourIndex].InnerText.ToString ());
						}
					}
				}
				completeStatus = 1;
			}
			catch (Exception e) {
				completeStatus = 0;
				errorString = e.Message.ToString();
			}
			return completeStatus;
		}

		int GetNearestLocationIndex()
		{
			//Get distance from current location to all areas, select the nearest one
			List<float> distanceList = new List<float> ();
			Locale myLocale = new Locale ("ms");
			float[] distanceResult = new float[] {0};
			foreach (string regionIteration in regionEntry) {
				Address areaAddress = AddressFromArea (regionIteration);
				Location.DistanceBetween (_currentLocation.Latitude, _currentLocation.Longitude, areaAddress.Latitude, areaAddress.Longitude, distanceResult);
				distanceList.Add (distanceResult.FirstOrDefault ());
			}

			//updateUI
			return distanceList.IndexOf(distanceList.Min());
		}

		void UpdateUI(int index)
		{
			if (index == 999) {
				lvlIndicator.Text = errorString;
				lvlIndicator.SetBackgroundColor( Android.Graphics.Color.Transparent);
				lvlIndicator.SetTextColor (Android.Graphics.Color.Black);
				lvlIndicator.SetPadding (0, lvlIndicator.PaddingTop, lvlIndicator.PaddingRight, lvlIndicator.PaddingBottom);
			} else {
				region.Text = regionEntry [index].ToString ();
				loadingText.Visibility = ViewStates.Invisible;
				region.Visibility = ViewStates.Visible;
				state.Visibility = ViewStates.Visible;

				string value = latestAPI [index].ToString ();
				string finalValue = value.Remove (value.Length - 1);
				string legendString = string.Empty;
				char legends = value [value.Length - 1];
				if (legends == '*') {
					legendString = "PM10";
				} else if (legends == 'a') {
					legendString = "SO2";
				} else if (legends == 'b') {
					legendString = "NO2";
				} else if (legends == 'c') {
					legendString = "Ozone";
				} else if (legends == 'd') {
					legendString = "CO";
				} else if (legends == '&') {
					legendString = "Multiple";
				} else {
					legendString = "Unknown";
				}

				lvlIndicator.Text = finalValue + " " + legendString;
				int lvlInt = Convert.ToInt32 (finalValue);

				if (lvlInt <= lowLvl) {
					lvlIndicator.SetBackgroundColor (blue);
				} else if (lvlInt > lowLvl && lvlInt <= medLvl) {
					lvlIndicator.SetBackgroundColor (green);
				} else if (lvlInt > medLvl && lvlInt <= highLvl) {
					lvlIndicator.SetBackgroundColor (yellow);
				} else if (lvlInt > highLvl && lvlInt <= alarmLvl) {
					lvlIndicator.SetBackgroundColor (orange);
				} else if (lvlInt > alarmLvl) {
					lvlIndicator.SetBackgroundColor (red);
				}
				lvlIndicator.SetTextColor (Android.Graphics.Color.White);
				lvlIndicator.Visibility = ViewStates.Visible;
			}
		}
	}
}


