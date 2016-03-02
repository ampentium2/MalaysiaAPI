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
	public class CoreCodeModel : Activity
	{
//		public string stateSel { get; set; }
		public List<string> regionEntry { get; set; }
		public List<string> latestAPI { get; set; }
		public List<float> distanceList { get; set; }

//		public Dictionary<string, string> regionAPI = new Dictionary<string, string> ();

//		MainActivity MainMethods = new MainActivity ();

		public CoreCodeModel ()
		{

		}

		public async Task<int> HTMLDownload (string stateRequested, Location location)
		{
			HtmlWeb htmlWeb = new HtmlWeb ();
			HtmlDocument htmlDoc = new HtmlDocument ();
			DateTime currentDateTime = DateTime.Now;
			TimeSpan currentTime = currentDateTime.TimeOfDay;
			int hourIndex = 3;

			if (regionEntry != null && latestAPI != null) {
				regionEntry.Clear ();
				latestAPI.Clear ();
			}

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

			foreach (var tableEntry in table)
			{
				if (tableEntry.HasChildNodes) {
					var rowEntry = tableEntry.ChildNodes.ToList ();
					var stateEntry = rowEntry [0].InnerText.ToString ();
					if (stateEntry == stateRequested) {
						regionEntry.Add (rowEntry [2].InnerText.ToString ());
						latestAPI.Add(rowEntry [hourIndex].InnerText.ToString ());

//						regionAPI.Add (rowEntry [2].InnerText.ToString (), rowEntry[hourIndex].InnerText.ToString());
					}
				}
			}


			//Let this portion being done on MainActivity
			//TODO: Get distance from current location to all areas, select the nearest one
			Locale myLocale = new Locale ("ms");
			float[] distanceResult = new float[] {0};
			foreach (string regionIteration in regionEntry) {
				Address areaAddress = AddressFromArea (regionIteration);
				Location.DistanceBetween (location.Latitude, location.Longitude, areaAddress.Latitude, areaAddress.Longitude, distanceResult);
				distanceList.Add (distanceResult.FirstOrDefault ());
			}

			return 1;
//
//			int entryIndex = distanceList.IndexOf(distanceList.Min());
//			region.Text = regionEntry [entryIndex].ToString ();
//
//			string value = latestAPI [entryIndex].ToString ();
//			string finalValue = value.Remove (value.Length - 1);
//			string legendString = string.Empty;
//			char legends = value [value.Length - 1];
//			if (legends == '*') {
//				legendString = "PM10";
//			} else if (legends == 'a') {
//				legendString = "SO2";
//			} else if (legends == 'b') {
//				legendString = "NO2";
//			} else if (legends == 'c') {
//				legendString = "Ozone";
//			} else if (legends == 'd') {
//				legendString = "CO";
//			} else if (legends == '&') {
//				legendString = "Multiple";
//			} else { legendString = "Unknown"; }
//
//
//			lvlIndicator.Text = finalValue + " " + legendString;
//			int lvlInt = Convert.ToInt32(finalValue);
//
//			if (lvlInt <= lowLvl) {
//				lvlIndicator.SetBackgroundColor (blue);
//			} else if (lvlInt > lowLvl && lvlInt <= medLvl) {
//				lvlIndicator.SetBackgroundColor (green);
//			} else if (lvlInt > medLvl && lvlInt <= highLvl) {
//				lvlIndicator.SetBackgroundColor (yellow);
//			} else if (lvlInt > highLvl && lvlInt <= alarmLvl) {
//				lvlIndicator.SetBackgroundColor (orange);
//			} else if (lvlInt > alarmLvl) {
//				lvlIndicator.SetBackgroundColor (red);
//			}
		}

		Address AddressFromArea(string locationName)
		{
			Locale setLocale = new Locale ("ms");
			Geocoder geocoder = new Geocoder (this, setLocale);
			IList<Address> addressList = geocoder.GetFromLocationName (locationName, 10);
			Address addressArea = addressList.SingleOrDefault (loc => loc.CountryName == "Malaysia");
			return addressArea;
		}
	}
}

