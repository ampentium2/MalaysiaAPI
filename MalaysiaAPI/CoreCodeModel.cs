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
	public class CoreCodeModel
	{
		public string stateSel { get; set; }

		private static List<string> regionEntry = new List<string> ();
		private static List<string> latestAPI = new List<string> ();
		private static string errorString = string.Empty;


		public static List<string> regionsList {
			get { return regionEntry; }
			set { regionEntry = value; }
		}
		public static List<string> APIList {
			get { return latestAPI; }
			set { latestAPI = value; }
		}
		public static string errorMessage {
			get { return errorString; }
			set { errorString = value; }
		}

		static readonly string TAG = "X:" + typeof(CoreCodeModel).Name;

		public async Task<int> HTMLDownload (string stateRequested)
		{
			Log.Debug (TAG, "HTMLDownloadRunning");
			HtmlWeb htmlWeb = new HtmlWeb ();
			HtmlDocument htmlDoc = new HtmlDocument ();
			DateTime currentDateTime = DateTime.Now;
			TimeSpan currentTime = currentDateTime.TimeOfDay;
			int hourIndex = 3;
			int completeStatus = 0;

			if (regionsList.Count != 0) {
				regionsList.Clear ();
				APIList.Clear ();
			}

//			regionEntry.Clear ();
//			latestAPI.Clear();

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
			Log.Debug (TAG, "HTMLDownloadFinish");
			return completeStatus;
		}
	}
}

