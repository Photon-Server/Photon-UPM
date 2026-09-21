namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;

	internal static class ListUtility
	{
		public static void InsertionSort<T>(List<T> list, Comparison<T> comparison)
		{
			for (int j = 1, count = list.Count; j < count; ++j)
			{
				T key = list[j];

				int i = j - 1;
				while (i >= 0 && comparison(list[i], key) > 0)
				{
					list[i + 1] = list[i];
					 --i;
				}

				list[i + 1] = key;
			}
		}

		public static int CompareHierarchyDescending(string h1, string h2)
		{
			if (h2.Length > h1.Length && h2[h1.Length] == '.' && h2.StartsWith(h1, StringComparison.Ordinal) == true)
				return -1;

			if (h1.Length > h2.Length && h1[h2.Length] == '.' && h1.StartsWith(h2, StringComparison.Ordinal) == true)
				return 1;

			return string.CompareOrdinal(h2, h1);
		}
	}
}
