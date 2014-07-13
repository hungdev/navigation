using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Navigation
{
	internal static class CrumbTrailManager
	{
		private static string SEPARATOR = !NavigationSettings.Config.OriginalUrlSeparators ? "_" : "!";
		private static string RET_1_SEP = "1" + SEPARATOR;
		private static string RET_2_SEP = "2" + SEPARATOR;
		private static string RET_3_SEP = "3" + SEPARATOR;
		private static string CRUMB_1_SEP = "4" + SEPARATOR;
		private static string CRUMB_2_SEP = "5" + SEPARATOR;

		internal static List<Crumb> CrumbTrailHrefArray
		{
			get
			{
				List<Crumb> crumbTrailArray = new List<Crumb>();
				int arrayCount = 0;
				string crumbTrail = StateContext.CrumbTrail;
				int crumbTrailSize = GetCrumbTrailSize(crumbTrail);
				string href = null;
				NavigationData navigationData;
				bool last = true;
				State state = null;
				while (arrayCount < crumbTrailSize)
				{
					state = StateContext.GetState(GetCrumbTrailState(crumbTrail));
					navigationData = GetCrumbTrailData(crumbTrail, state);
					crumbTrail = CropCrumbTrail(crumbTrail);
					href = GetHref(state.Id, navigationData, null);
					crumbTrailArray.Add(new Crumb(href, navigationData, state, last));
					last = false;
					arrayCount++;
				}
				crumbTrailArray.Reverse();
				return crumbTrailArray;
			}
		}

		internal static void BuildCrumbTrail()
		{
			string trail = StateContext.CrumbTrail;
			if (StateContext.PreviousStateId != null)
			{
				bool initialState = StateContext.GetDialog(StateContext.StateId).Initial == StateContext.GetState(StateContext.StateId);
				if (initialState)
				{
					trail = null;
				}
				else
				{
					string croppedTrail = trail;
					int crumbTrailSize = GetCrumbTrailSize(trail);
					int count = 0;
					bool repeatedState = false;
					if (StateContext.PreviousStateId == StateContext.StateId)
					{
						repeatedState = true;
					}
					while (!repeatedState && count < crumbTrailSize)
					{
						string trailState = GetCrumbTrailState(croppedTrail);
						croppedTrail = CropCrumbTrail(croppedTrail);
						if (StateContext.GetState(trailState) == StateContext.GetState(StateContext.StateId))
						{
							trail = croppedTrail;
							repeatedState = true;
						}
						count++;
					}

					if (!repeatedState)
					{
						StringBuilder formattedReturnData = new StringBuilder();
						string prefix = string.Empty;
						if (StateContext.ReturnData != null)
						{
							foreach (NavigationDataItem item in StateContext.ReturnData)
							{
								formattedReturnData.Append(prefix);
								formattedReturnData.Append(EncodeURLValue(item.Key));
								formattedReturnData.Append(RET_1_SEP);
								formattedReturnData.Append(FormatURLObject(item.Key, item.Value, StateContext.GetState(StateContext.PreviousStateId)));
								prefix = RET_3_SEP;
							}
						}
						StringBuilder trailBuilder = new StringBuilder();
						trailBuilder.Append(CRUMB_1_SEP);
						trailBuilder.Append(StateContext.GetState(StateContext.PreviousStateId).StateKey);
						trailBuilder.Append(CRUMB_2_SEP);
						trailBuilder.Append(formattedReturnData.ToString());
						trailBuilder.Append(trail);
						trail = trailBuilder.ToString();
					}
				}
			}
			StateContext.GenerateKey(trail);
		}

		internal static string GetHref(string nextState, NavigationData navigationData, NavigationData returnData)
		{
			string previousState = StateContext.StateId;
			string crumbTrail = StateContext.CrumbTrailKey;
			State state = StateContext.GetState(nextState);
			NameValueCollection coll = new NameValueCollection();
			coll[NavigationSettings.Config.StateIdKey] = nextState;
			if (previousState != null && state.TrackCrumbTrail)
			{
				coll[NavigationSettings.Config.PreviousStateIdKey] = previousState;
			}
			if (navigationData != null)
			{
				foreach (NavigationDataItem item in navigationData)
				{
					if (!item.Value.Equals(string.Empty) && !state.DefaultOrDerived(item.Key, item.Value))
						coll[item.Key] = FormatURLObject(item.Key, item.Value, state);
				}
			}
			if (returnData != null && state.TrackCrumbTrail && StateContext.State != null)
			{
				StringBuilder returnDataBuilder = new StringBuilder();
				string prefix = string.Empty;
				foreach (NavigationDataItem item in returnData)
				{
					if (!item.Value.Equals(string.Empty) && !StateContext.State.DefaultOrDerived(item.Key, item.Value))
					{
						returnDataBuilder.Append(prefix);
						returnDataBuilder.Append(EncodeURLValue(item.Key));
						returnDataBuilder.Append(RET_1_SEP);
						returnDataBuilder.Append(FormatURLObject(item.Key, item.Value, StateContext.State));
						prefix = RET_3_SEP;
					}
				}
				if (returnDataBuilder.Length > 0)
					coll[NavigationSettings.Config.ReturnDataKey] = returnDataBuilder.ToString();
			}
			if (crumbTrail != null && state.TrackCrumbTrail)
			{
				coll[NavigationSettings.Config.CrumbTrailKey] = crumbTrail;
			}
#if NET35Plus
			coll = StateContext.ShieldEncode(coll, false, state);
#else
			coll = StateContext.ShieldEncode(coll, state);
#endif
#if NET40Plus
			HttpContextBase context = null;
			if (HttpContext.Current != null)
				context = new HttpContextWrapper(HttpContext.Current);
			else
				context = new MockNavigationContext(null, state);
			return state.StateHandler.GetNavigationLink(state, coll, context);
#else
			return state.StateHandler.GetNavigationLink(state, coll);
#endif
		}

		private static string DecodeURLValue(string urlValue)
		{
			return urlValue.Replace("0" + SEPARATOR, SEPARATOR);
		}

		private static string EncodeURLValue(string urlValue)
		{
			return urlValue.Replace(SEPARATOR, "0" + SEPARATOR);
		}

		internal static string FormatURLObject(string key, object urlObject, State state)
		{
			Type defaultType = state.DefaultTypes[key] ?? typeof(string);
			string converterKey = ConverterFactory.GetKey(urlObject);
			string formattedValue = ConverterFactory.GetConverter(converterKey).ConvertToInvariantString(urlObject);
			formattedValue = EncodeURLValue(formattedValue);
			if (urlObject.GetType() != defaultType)
				formattedValue += RET_2_SEP + converterKey;
			return formattedValue;
		}

		internal static object ParseURLString(string key, string val, State state)
		{
			Type defaultType = state.DefaultTypes[key] ?? typeof(string);
			string urlValue = val;
			string converterKey = ConverterFactory.GetKey(defaultType);
			if (val.IndexOf(RET_2_SEP, StringComparison.Ordinal) > -1)
			{
				string[] arr = Regex.Split(val, RET_2_SEP);
				urlValue = arr[0];
				converterKey = arr[1];
			}
			try
			{
				return ConverterFactory.GetConverter(converterKey).ConvertFromInvariantString(DecodeURLValue(urlValue));
			}
			catch (Exception ex)
			{
				throw new UrlException(Resources.InvalidUrl, ex);
			}
		}

		private static int GetCrumbTrailSize(string trail)
		{
			int crumbTrailSize = trail == null ? 0 : Regex.Split(trail, CRUMB_1_SEP).Length - 1;
			return crumbTrailSize;
		}

		private static string CropCrumbTrail(string trail)
		{
			string croppedTrail;
			int nextTrailStart = trail.IndexOf(CRUMB_1_SEP, 1, StringComparison.Ordinal);
			if (nextTrailStart != -1)
			{
				croppedTrail = trail.Substring(nextTrailStart);
			}
			else
			{
				croppedTrail = "";
			}
			return croppedTrail;
		}

		private static string GetCrumbTrailState(string trail)
		{
			return Regex.Split(trail.Substring(CRUMB_1_SEP.Length), CRUMB_2_SEP)[0];
		}

		private static NavigationData GetCrumbTrailData(string trail, State state)
		{
			NavigationData navData = null;
			string data = Regex.Split(trail.Substring(trail.IndexOf(CRUMB_2_SEP, StringComparison.Ordinal) + CRUMB_2_SEP.Length), CRUMB_1_SEP)[0];
			if (data.Length != 0)
			{
				navData = ParseReturnData(data, state);
			}
			return navData;
		}

		internal static string GetRefreshHref(NavigationData refreshData)
		{
			return GetHref(StateContext.StateId, refreshData, null);
		}

		internal static object Parse(string key, string val, State state)
		{
			object parsedVal;
			if (key == NavigationSettings.Config.ReturnDataKey)
			{
				parsedVal = ParseReturnData(val, state);
			}
			else
			{
				if (key == NavigationSettings.Config.CrumbTrailKey)
				{
					parsedVal = val;
				}
				else
				{
					parsedVal = ParseURLString(key, val, state);
				}
			}
			return parsedVal;
		}

		private static NavigationData ParseReturnData(string returnData, State state)
		{
			NavigationData navData = new NavigationData();
			string[] nameValuePair;
			string[] returnDataArray = Regex.Split(returnData, RET_3_SEP);
			for (int i = 0; i < returnDataArray.Length; i++)
			{
				nameValuePair = Regex.Split(returnDataArray[i], RET_1_SEP);
				navData.Add(DecodeURLValue(nameValuePair[0]), ParseURLString(DecodeURLValue(nameValuePair[0]), nameValuePair[1], state));
			}
			return navData;
		}
	}
}
