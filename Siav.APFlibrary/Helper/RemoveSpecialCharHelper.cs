using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Helper
{
	
	public static class RemoveSpecialCharHelper
	{
		private static bool[] _lookup;

		static RemoveSpecialCharHelper()
		{
			_lookup = new bool[65536];
			for (char c = '0'; c <= '9'; c++) _lookup[c] = true;
			for (char c = 'A'; c <= 'Z'; c++) _lookup[c] = true;
			for (char c = 'a'; c <= 'z'; c++) _lookup[c] = true;
			_lookup['.'] = true;
			_lookup['_'] = true;
		}
		public static string RemoveSpecialCharacters(string str)
		{
			char[] buffer = new char[str.Length];
			int index = 0;
			foreach (char c in str)
			{
				if (_lookup[c])
				{
					buffer[index] = c;
					index++;
				}
			}
			return new string(buffer, 0, index);
		}
	}
}
