﻿using System;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Navigation.Mvc
{
	public static class AjaxExtensions
	{
		public static MvcHtmlString RefreshPanel(this AjaxHelper ajaxHelper, string id, string navigationDataKeys, Func<dynamic, HelperResult> content)
		{
			string html = null;
			if (NavigationDataChanged(ajaxHelper, navigationDataKeys))
			{
				RefreshAjaxInfo info = RefreshAjaxInfo.GetInfo(ajaxHelper.ViewContext.HttpContext);
				if (info.PanelId == null)
					info.PanelId = id;
				html = content(null).ToHtmlString();
				if (info.PanelId == id)
				{
					info.Panels[id] = html;
					info.PanelId = null;
				}
			}
			TagBuilder tagBuilder = new TagBuilder("span");
			tagBuilder.MergeAttribute("id", id);
			tagBuilder.InnerHtml = html ?? content(null).ToHtmlString();
			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
		}

		private static bool NavigationDataChanged(AjaxHelper ajaxHelper, string navigationDataKeys)
		{
			NavigationData data = RefreshAjaxInfo.GetInfo(ajaxHelper.ViewContext.HttpContext).Data;
			if (data != null)
			{
				if (string.IsNullOrEmpty(navigationDataKeys))
					return true;
				foreach (string key in navigationDataKeys.Split(new char[] { ',' }))
				{
					if (!data[key.Trim()].Equals(StateContext.Data[key.Trim()]))
						return true;
				}
			}
			return false;
		}
	}
}
