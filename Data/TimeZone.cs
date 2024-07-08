using System;
using System.Collections.Generic;
using System.Web;

namespace MDTools.Data {

	/// <summary>
	/// Summary description for TimeZone
	/// </summary>
	public static class USTimeZone {

		private static readonly Dictionary<short, string> IntToTZ = new Dictionary<short, string>( 7 ) { 
			{-10, "HAST"},
			{ -9, "AKST"},
			{ -8, "PST"},
			{ -7, "MST"},
			{ -6, "CST"},
			{ -5, "EST"},
			{ -4, "AST"} 
		};

		public static string GetAbbrev( short utcadj ) {
			return IntToTZ[utcadj];
		}

		public static short GetAbbrev( string tzabbrev ) {
			foreach(var c in IntToTZ) {
				if(c.Value.Equals(tzabbrev))
					return c.Key;
			}
			throw new IndexOutOfRangeException();
		}

		public static bool IsValidUS(short utcadj ) {
			return (utcadj>=-10 && utcadj<=-4);
		}

	}
}