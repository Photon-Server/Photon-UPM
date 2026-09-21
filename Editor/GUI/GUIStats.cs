namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Globalization;
	using UnityEngine;

	internal class GUIStat
	{
		public readonly GUIStyle   Style;
		public readonly GUIContent Content;

		public float Width;

		public GUIStat(GUIStyle style, string text, string tooltip, float width = 0.0f)
		{
			Style   = style;
			Content = new GUIContent(text, tooltip);
			Width   = width;

			if (width <= 0.0f && style != null)
			{
				Width = style.CalcSize(Content).x;
			}
		}
	}

	internal sealed class GUIIconStat : GUIStat
	{
		public readonly GUIContent IconContent;

		public GUIIconStat(GUIStyle style, string text, string iconText, string tooltip, float width = 0.0f) : base(style, text, tooltip, width)
		{
			IconContent = new GUIContent(iconText, tooltip);
		}

		public GUIIconStat(GUIStyle style, Texture2D icon, string iconText, string tooltip, float width = 0.0f) : base(style, "", tooltip, width)
		{
			Content.image = icon;

			IconContent = new GUIContent(iconText, tooltip);
		}
	}

	internal sealed class GUIIntStat : GUIStat
	{
		private string _format;
		private int    _value;
		private float  _width8;
		private float  _width88;
		private float  _width888;

		public GUIIntStat(GUIStyle style, string format, int value, string tooltip, float width = 0.0f) : base(style, string.Format(format, value), tooltip, width)
		{
			_format = format;
			_value  = value;

			if (style != null)
			{
				_width8   = style.CalcSize(new GUIContent(string.Format(format, 8))).x;
				_width88  = style.CalcSize(new GUIContent(string.Format(format, 88))).x;
				_width888 = style.CalcSize(new GUIContent(string.Format(format, 888))).x;
			}
		}

		// The width is based on the order of the value, it changes only when the value crosses an order.
		public float GetWidth(int value)
		{
			if (value < 10)
				return _width8;
			if (value < 100)
				return _width88;

			return _width888;
		}

		public GUIContent GetContent(int value)
		{
			if (_value != value)
			{
				_value = value;

				Content.text = string.Format(_format, value);
			}

			return Content;
		}
	}

	internal sealed class GUIPlayersStat : GUIStat
	{
		private string    _tooltip;
		private string    _noPlayerTooltip;
		private int       _count = -1;
		private PlayerRef _localPlayer;
		private int       _widthKey = -1;

		public GUIPlayersStat(GUIStyle style, string tooltip, string noPlayerTooltip) : base(style, "", tooltip)
		{
			_tooltip         = tooltip;
			_noPlayerTooltip = noPlayerTooltip;
		}

		public GUIContent GetContent(int count, PlayerRef localPlayer)
		{
			if (_count != count || _localPlayer != localPlayer)
			{
				_count       = count;
				_localPlayer = localPlayer;

				Content.text    = localPlayer.IsRealPlayer == true ? $"{count} (Player {localPlayer.PlayerId})" : $"{count}";
				Content.tooltip = localPlayer.IsRealPlayer == true ? _tooltip : _noPlayerTooltip;

				// The width is based on the orders of the values, it changes only when a value crosses an order.
				int widthKey = GetOrder(count) * 10 + (localPlayer.IsRealPlayer == true ? GetOrder(localPlayer.PlayerId) + 1 : 0);
				if (_widthKey != widthKey)
				{
					_widthKey = widthKey;

					Width = Style.CalcSize(new GUIContent(localPlayer.IsRealPlayer == true ? $"{GetTemplate(count)} (Player {GetTemplate(localPlayer.PlayerId)})" : GetTemplate(count))).x;
				}
			}

			return Content;
		}

		private static int    GetOrder(int value)    => value < 10 ? 0 : (value < 100 ? 1 : (value < 1000 ? 2 : 3));
		private static string GetTemplate(int value) => value < 10 ? "8" : (value < 100 ? "88" : (value < 1000 ? "888" : "8888"));
	}

	internal sealed class GUITimeStat : GUIStat
	{
		private string _format;
		private int    _seconds;
		private int    _minutes;
		private int    _hours;
		private int    _days;

		public GUITimeStat(GUIStyle style, string format, TimeSpan value, string tooltip, float width = 0.0f) : base(style, string.Format(format, GetFormattedValue(value)), tooltip, width)
		{
			_format  = format;
			_seconds = value.Seconds;
			_minutes = value.Minutes;
			_hours   = value.Hours;
			_days    = value.Days;
		}

		public GUIContent GetContent(TimeSpan value)
		{
			int seconds = value.Seconds;
			int minutes = value.Minutes;
			int hours   = value.Hours;
			int days    = value.Days;

			if (_seconds != seconds || _minutes != minutes || _hours != hours || _days != days)
			{
				_seconds = seconds;
				_minutes = minutes;
				_hours   = hours;
				_days    = days;

				Content.text = string.Format(_format, GetFormattedValue(value));
			}

			return Content;
		}

		private static string GetFormattedValue(TimeSpan value)
		{
			return value.Days > 0 ? $"{(int)value.TotalHours}:{value:mm\\:ss}" : $"{value:hh\\:mm\\:ss}";
		}
	}

	internal sealed class GUITrafficStat : GUIStat
	{
		private string _format;
		private int    _value;

		public GUITrafficStat(GUIStyle style, string format, int value, string tooltip, float width = 0.0f) : base(style, GetFormattedValue(format, Mathf.CeilToInt(value / 1024.0f)), tooltip, width)
		{
			_format = format;
			_value  = Mathf.CeilToInt(value / 1024.0f);
		}

		public GUIContent GetContent(float value)
		{
			int valueKB = Mathf.CeilToInt(value / 1024.0f);

			if (_value != valueKB)
			{
				_value = valueKB;

				Content.text = GetFormattedValue(_format, valueKB);
			}

			return Content;
		}

		private static string GetFormattedValue(string format, int valueKB)
		{
			return valueKB < 1024 ? string.Format(format, $"{valueKB} kB") : string.Format(format, string.Format(CultureInfo.InvariantCulture, "{0:F2} MB", valueKB / 1024.0f));
		}
	}

	internal sealed class GUISortStat : GUIStat
	{
		private string _text;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUISortStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public GUIContent GetContent(int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_sortMode == sortMode)
				return Content;

			_sortMode = sortMode;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_UP} {_text}";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_DOWN} {_text}";
			}
			else
			{
				Content.text = _text;
			}

			return Content;
		}
	}

	internal sealed class GUINameStat : GUIStat
	{
		private string _text;
		private int    _total;
		private int    _filtered;
		private int    _selected;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUINameStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public bool IsSorted(int sortMode) => sortMode == _ascendingSortMode || sortMode == _descendingSortMode;

		public GUIContent GetContent(int total, int filtered, int selected, int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_total == total && _filtered == filtered && _selected == selected && _sortMode == sortMode)
				return Content;

			_total    = total;
			_filtered = filtered;
			_selected = selected;
			_sortMode = sortMode;

			string text;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				text = total == filtered ? $"{GUISymbols.ARROW_UP} {_text} ({total})" : $"{GUISymbols.ARROW_UP} {_text} ({filtered} / {total})";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				text = total == filtered ? $"{GUISymbols.ARROW_DOWN} {_text} ({total})" : $"{GUISymbols.ARROW_DOWN} {_text} ({filtered} / {total})";
			}
			else
			{
				text = total == filtered ? $"{_text} ({total})" : $"{_text} ({filtered} / {total})";
			}

			if (selected > 1)
			{
				text += $" - {selected} Selected";
			}

			Content.text = text;

			return Content;
		}
	}
}
