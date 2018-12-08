using System;
using System.Linq;

namespace oMediaCenter.MetaDatabase
{
	public static class SearchableString
	{
		public static string ToSearchableString(this string input)
		{
			string lowerInput = input.ToLower();
			string searchableString = new string(lowerInput.Where(c => Char.IsLetterOrDigit(c)).ToArray());
			return searchableString;
		}
	}
}
