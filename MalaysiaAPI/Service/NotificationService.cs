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
using Android.Support.V4.App;
namespace MalaysiaAPI
{
	[Service]
	public class NotificationService : Service, ILocationListener
	{
		static readonly string TAG = "X:" + typeof(NotificationService).Name;
		private static readonly int notiId = 1000;
		int sec = 0;
		LocationManager _locationManager;
		string _locationProvider;
		Location _currentLocation;

		static List<string> regionEntry = CoreCodeModel.regionsList;
		static List<string> latestAPI = CoreCodeModel.APIList;
		static string errorString = CoreCodeModel.errorMessage;
		Address address;
		string notiTitle;
		string notiContent;
		string notiMinContent;

		CoreCodeModel ccm = new CoreCodeModel();

//		IBinder binder;

		public override IBinder OnBind (Intent intent)
		{
			return null;
		}

		public override void OnCreate ()
		{
			base.OnCreate ();
			Log.Debug (TAG, "Service Started");
			InitLocationManager ();

			//time = hour * minute * second * 1000ms
			int locationRequestFrequency = 60*60*1000;

			_locationManager.RequestLocationUpdates (_locationProvider, locationRequestFrequency, 0, this);

//			SendNotification ("Malaysia API", "Application Started");
		}

		public override void OnDestroy ()
		{
			base.OnDestroy ();
			_locationManager.RemoveUpdates (this);
			Log.Debug (TAG, "Stop listening");
		}

		private void SendNotification (string titles, string texts, string minimizedContent)
		{
			PendingIntent contentIntent = PendingIntent.GetActivity(this, 0,
				new Intent(this, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent);

			NotificationCompat.Builder notiBuild = new NotificationCompat.Builder (this)
				.SetAutoCancel (false)
				.SetContentTitle (titles)
				.SetContentText (minimizedContent)
				.SetSmallIcon(Resource.Mipmap.ic_launcher)
				.SetTicker ("API Update")
				.SetStyle(new NotificationCompat.BigTextStyle().BigText(texts))
				.SetOngoing(true)
				.SetContentIntent(contentIntent);

			NotificationManager notiManager = (NotificationManager)GetSystemService (Context.NotificationService);
			notiManager.Notify (notiId, notiBuild.Build());
		}

		public async void OnLocationChanged(Location location) {
			_currentLocation = location;
			sec++;
			if (_currentLocation != null) {
				Log.Debug (TAG, string.Format ("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude));
				address = await ReverseGeocodeCurrentLocation();
				int complete = 0;
				if (address != null) {
					string state = address.GetAddressLine (address.MaxAddressLineIndex - 1);
					while (complete != 1) {
						complete = await ccm.HTMLDownload (state);
					}
					if (complete == 1) {
						int locationIndex = GetNearestLocationIndex ();
						string currentRegion = regionEntry [locationIndex].ToString ();
						string APIString = latestAPI [locationIndex].ToString ();
						string currentAPI = APIString.Remove (APIString.Length - 1);
						string currentLegend = string.Empty;
						char legends = APIString [APIString.Length - 1];
						if (legends == '*') {
							currentLegend = "PM10";
						} else if (legends == 'a') {
							currentLegend = "SO2";
						} else if (legends == 'b') {
							currentLegend = "NO2";
						} else if (legends == 'c') {
							currentLegend = "Ozone";
						} else if (legends == 'd') {
							currentLegend = "CO";
						} else if (legends == '&') {
							currentLegend = "Multiple";
						} else {
							currentLegend = "Unknown";
						}
						notiTitle = "Malaysia API";
						notiContent = String.Format ("API: {0}\nHighest Concentration: {1}", currentAPI, currentLegend);
						notiMinContent = String.Format ("API: {0}", APIString);
					} else {
						notiTitle = "Malaysia API";
						notiContent = String.Format ("Can't get HTMLCompletion");
						notiMinContent = String.Format ("Can't get HTMLCompletion");

					}
				} else {
					notiTitle = "Malaysia API";
					notiContent = String.Format ("Can't get address");
					notiMinContent = String.Format ("Can't get address");

				}
			} else {
				notiTitle = "Malaysia API";
				notiContent = String.Format ("Can't get location");
				notiMinContent = String.Format ("Can't get HTMLCompletion");
			}

			SendNotification (notiTitle, notiContent, notiMinContent);
		}

		public void OnProviderDisabled(string provider) {
		}

		public void OnProviderEnabled(string provider) {
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug (TAG, "{0},{1}", provider, status);
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

		async Task<Address> ReverseGeocodeCurrentLocation()
		{
			Locale setLocale = new Locale ("ms");
			Geocoder geocoder = new Geocoder (this, setLocale);
			IList<Address> addressList = await geocoder.GetFromLocationAsync (_currentLocation.Latitude, _currentLocation.Longitude, 10);
			Address addressReversed = addressList.FirstOrDefault ();
			return addressReversed;
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

		Address AddressFromArea(string locationName)
		{
			Locale setLocale = new Locale ("ms");
			Geocoder geocoder = new Geocoder (this, setLocale);
			IList<Address> addressList = geocoder.GetFromLocationName (locationName, 50);
			Address addressArea = addressList.SingleOrDefault (loc => loc.CountryName == "Malaysia" && loc.AdminArea == address.GetAddressLine (address.MaxAddressLineIndex - 1));
			return addressArea;
		}
	}
}

