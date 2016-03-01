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
using Android.Gms;

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
//		int lowLvl = 25;
//		int medLvl = 50;

		static readonly string TAG = "X:" + typeof(MainActivity).Name;
		TextView _addressTxt;
		Location _currentLocation;
		LocationManager _locationManager;
		TextView state;
		TextView region;
		Address address;
		TextView lvlIndicator;
		GridLayout mainLayout;
		List<string> regionEntry = new List<string> ();
		List<string> latestAPI = new List<string> ();
		int lastId;

		string _locationProvider;
		TextView _locationTxt;



		public async void OnLocationChanged(Location location)
		{
			_currentLocation = location;

			if (_currentLocation == null) {
				_locationTxt.Text = "Unable to determine your location";
			} else {
				_locationTxt.Text = string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude);
				Log.Debug (TAG, string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude));
				address = await ReverseGeocodeCurrentLocation ();

				if (address == null) {
					state.Text = "String: No Address";
				} else {
					state.Text = address.GetAddressLine (address.MaxAddressLineIndex - 1);
				}

				DisplayAddress (address);

				//Remove textviews first and create new textviews
				HTMLDownload(address.GetAddressLine (address.MaxAddressLineIndex - 1)); 
//				HTMLDownload ("Pulau Pinang");	//For debug in emulator only
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

			string stateString = string.Format ("State: ");
			state.Text = stateString;
//			int lvlInt = Convert.ToInt32(lvlIndicator.Text);
//			var blue = Android.Graphics.Color.Argb (255, 0, 0, 153);
//			var green = Android.Graphics.Color.Argb (255, 0, 153, 0);
//			var red = Android.Graphics.Color.Argb (255, 153, 0, 0);
//			if (lvlInt < lowLvl) {
//				lvlIndicator.SetBackgroundColor(blue);
//			}
//			else if (lvlInt < medLvl && lvlInt > lowLvl) {
//				lvlIndicator.SetBackgroundColor(green);
//			}
//			else if (lvlInt > medLvl) {
//				lvlIndicator.SetBackgroundColor (red);
//			}

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
//			Address areaAddress = AddressFromArea("Seberang Jaya 2, Perai");
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



		async void HTMLDownload (string stateRequested)
		{
			HtmlWeb htmlWeb = new HtmlWeb ();
			HtmlDocument htmlDoc = new HtmlDocument ();
			DateTime currentDateTime = DateTime.Now;
			TimeSpan currentTime = currentDateTime.TimeOfDay;
			int hourIndex = 3;

			regionEntry.Clear ();
			latestAPI.Clear();

			string hourRegion = string.Empty;
			//TODO: A problem will occur if it's 1st day of month at 12AM. Date is 0-3-2016
			string currentDay = currentTime.Hours == 0 ? (currentDateTime.Day - 1).ToString ("D2") : currentDateTime.Day.ToString ("D2");
			string date = currentDateTime.Year.ToString () + "-" + currentDateTime.Month.ToString ("D2") + "-" + currentDay;
			int currentHour = currentTime.Hours == 0 ? 24 : currentTime.Hours;

			if (currentHour > 0 && currentHour <= 6) { hourRegion = "hour1"; hourIndex += currentHour - 1; }
			else if (currentHour > 6 && currentHour <= 12) { hourRegion = "hour2"; hourIndex += (currentHour - 6 - 1); }
			else if (currentHour > 12 && currentHour <= 18) { hourRegion = "hour3"; hourIndex += (currentHour - 12 - 1);}
			else if (currentHour > 18 && currentHour <= 24) { hourRegion = "hour4"; hourIndex += (currentHour - 18 - 1);}

			string urlConstruct = "http://apims.doe.gov.my/v2/" + hourRegion + "_" + date + ".html";

			//TODO: Try-Catch LoadFromWeb so that it doesn't crash if web service is unavailable
			htmlDoc = await htmlWeb.LoadFromWebAsync(urlConstruct);

			var div = htmlDoc.GetElementbyId ("content");
			var table = div.Descendants ("table").ToList()[0].ChildNodes.ToList();
//			var rowEntry = table [3].ChildNodes.ToList ();
//			var stateEntry = rowEntry [0].InnerText.ToString ();

			foreach (var tableEntry in table)
			{
				if (tableEntry.HasChildNodes) {
					var rowEntry = tableEntry.ChildNodes.ToList ();
					var stateEntry = rowEntry [0].InnerText.ToString ();
					if (stateEntry == stateRequested) {
						regionEntry.Add (rowEntry [2].InnerText.ToString ());
						latestAPI.Add(rowEntry [hourIndex].InnerText.ToString ());
					}
				}
			}

			//TODO: Get distance from current location to all areas, select the nearest one
//			List<Single> distanceList = new List<Single> ();
			List<float> distanceList = new List<float> ();
			Locale myLocale = new Locale ("ms");
//			Location distanceLoc = new Location ();
			float[] distanceResult = new float[] {0};
			foreach (string region in regionEntry) {
				Address areaAddress = AddressFromArea (region);
				Location.DistanceBetween (_currentLocation.Latitude, _currentLocation.Longitude, areaAddress.Latitude, areaAddress.Longitude, distanceResult);
				distanceList.Add (distanceResult.FirstOrDefault ());
			}
			int a = 5;

			//TODO: Remove This???
//			int id = 1000;
//			int y = 4;
//			int x = 1;
//
//			for (int index = 1000; index <= lastId; index++) {
//				TextView removeTxtView = FindViewById<TextView> (index);
//				mainLayout.RemoveView (removeTxtView);
//			}
//
//			foreach (var regionGrabbed in regionEntry) {
//				TextView valueText = new TextView (this);
//
//				valueText.Id = id + regionEntry.IndexOf (regionGrabbed);
//				int regionIndex = regionEntry.IndexOf (regionGrabbed);
//				string regionText = regionGrabbed.ToString ();
//				string value = latestAPI [regionEntry.IndexOf (regionGrabbed)];
//				string legendString = string.Empty;
//				string valueString = value.Remove (value.Length - 1);
//
//				char legends = value [value.Length - 1];
//				if (legends == '*') {
//					legendString = "PM10";
//				} else if (legends == 'a') {
//					legendString = "SO2";
//				} else if (legends == 'b') {
//					legendString = "NO2";
//				} else if (legends == 'c') {
//					legendString = "Ozone";
//				} else if (legends == 'd') {
//					legendString = "CO";
//				} else if (legends == '&') {
//					legendString = "Multiple";
//				} else { legendString = "Unknown"; }
//
//				valueText.Text = regionText + ": " + valueString + " " + legendString;
//
//				//Set column and row appropriately instead of append
//				GridLayout.LayoutParams layoutParam = new GridLayout.LayoutParams ();
//				layoutParam.RowSpec = GridLayout.InvokeSpec (y + regionIndex, GridLayout.BaselineAlighment);
//				layoutParam.ColumnSpec = GridLayout.InvokeSpec (x, GridLayout.LeftAlighment);
//
//
//				mainLayout.AddView (valueText,layoutParam);
//			}
//			lastId = id + regionEntry.Count();

			//TODO: Use Google Place API to check for nearest location?

		}
	}
}


