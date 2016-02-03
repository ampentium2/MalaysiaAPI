
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MalaysiaAPI
{
	[Activity (Label = "@string/settings_page_label")]			
	public class SettingsActivity : Activity
	{
		string stateSelection = string.Empty;
		string regionSelection = string.Empty;

		private string ConstructedResult (string state, string region)
		{
			return string.Format ("{0},{1}", state, region);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.Settings);

			Spinner stateSpin = FindViewById<Spinner> (Resource.Id.state_spinner);
			Spinner regionSpin = FindViewById<Spinner> (Resource.Id.region_spinner);

			stateSpin.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (stateSpin_ItemSelected);
			var stateAdapter = ArrayAdapter.CreateFromResource (this, Resource.Array.state_array, Android.Resource.Layout.SimpleSpinnerItem);

			regionSpin.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (regionSpin_ItemSelected);
			var regionAdapter = ArrayAdapter.CreateFromResource (this, Resource.Array.region_array, Android.Resource.Layout.SimpleSpinnerItem);

			stateAdapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			stateSpin.Adapter = stateAdapter;

			regionAdapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			regionSpin.Adapter = regionAdapter;
				
		}

		private void regionSpin_ItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;
			var main = new Intent (this, typeof(MainActivity));

			string selectedItem = Convert.ToString (spinner.GetItemAtPosition (e.Position));
			string selRegionString;

			if (selectedItem != "Region") {
				selRegionString = string.Format ("Your region is {0}", spinner.GetItemAtPosition (e.Position));
			} else {
				selRegionString = string.Empty;
			}

			regionSelection = selectedItem;

//			string tempString = ConstructedResult
			main.PutExtra("settings", ConstructedResult(stateSelection, regionSelection));
			SetResult (Result.Ok, main);

		}

		private void stateSpin_ItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			var main = new Intent (this, typeof(MainActivity));

			TextView selectedState = FindViewById<TextView> (Resource.Id.stateSelected);

			string selectedItem = Convert.ToString (spinner.GetItemAtPosition (e.Position));
			string selStateString;

			if (selectedItem != "State") {
				selStateString = string.Format ("Thou Art At {0}", spinner.GetItemAtPosition (e.Position));
			} else {
				selStateString = string.Empty;
			}
				
			selectedState.SetText (selStateString,TextView.BufferType.Normal);

			stateSelection = selectedItem;
			main.PutExtra("settings", ConstructedResult(stateSelection, regionSelection));

//			main.PutExtra ("stateSel", selectedStateToMain);
			SetResult (Result.Ok, main);

		}

		protected override void OnPause ()
		{			
			base.OnPause ();
//			var main = new Intent (this, typeof(MainActivity));
//
//			string sendToMain = string.Format ("{0},{1}", stateSelection, regionSelection);
//			main.PutExtra ("settings", sendToMain);
////			SetResult (Result.Ok, main);
			Finish ();
		}


	}
}

