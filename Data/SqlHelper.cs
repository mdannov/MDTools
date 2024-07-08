using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using MDTools.Extension;

namespace MDTools.Data {
	/// <summary>
	/// Summary description for SqlHelper
	/// </summary>
	public static class SqlHelper {

		public static readonly char[] AttackCharacters = {'%','[',']','\\','_','&',';'};

		/// <summary>
		/// Creates a simple coma-separated list of values from the list which can be used in SQL Server IN clause
		/// eg. SELECT * FROM table WHERE value IN ( + ComaSet<int>( new List<int>() { 1, 5, 10 } ) + )
		/// </summary>
		public static string ComaSet(IList list) {
			return string.Join( ",", list.ToStringArray() );
#if originalimplement
			int leni = list.Count;
			StringBuilder res = new StringBuilder(leni*8);
			for( int i = 0; i < leni; i++ ) {
				var item = list[i];
				if( i > 0 )
					res.Append( ',' );
				res.Append( item.ToString() );
			}
			return res.ToString();
#endif
		}

		public const string injectionchars = "";
		/// <summary>
		/// Creates a simple coma separated list of strings with apostrophe modifiers from the list which can be used in SQL Server IN clause
		/// eg. SELECT * FROM table WHERE value IN ( + ComaSetString( new List<string>() { "Test", "This", "Out" } ) + )
		/// </summary>
		public static string ComaSetString( IList<string> list, bool injectionattacktest = true ) {
			if( list == null )
				return string.Empty;
			int leni = list.Count;
			if( leni == 0 )
				return string.Empty;
			string res = @"'";
			for( int i = 0; i < leni; i++ ) {
				var item = list[i];
				if( i > 0 )
					res += @"','";
				var str = item.ToString();
				if( injectionattacktest )
					str = str.DoubleApostrophes(); // prevents sql from breaking out of string
				res += str;
			}
			res += '\'';
			return res;
		}

		/// <summary>
		/// Converts list into a string that can used in a SQL WHERE clause when values are converted to columns in a pivot table.
		/// Resulting string contains AND'd values of the list 
		/// Example output of PivotAnd<int>( new List<int>() { 1, 2 } } } );  would result in: ([1]>0 AND [2]>0)
		/// </summary>
		public static string PivotAnd<BASETYPE>( List<BASETYPE> list ) {
			return _Pivot1st2nd<BASETYPE>( list, " AND " );
		}
		/// <summary>
		/// Converts list into a string that can used in a SQL WHERE clause when values are converted to columns in a pivot table.
		/// Resulting string contains OR'd values of the list 
		/// Example output of PivotOr<int>( new List<int>() { 1, 2 } } } );  would result in: ([1]>0 OR [2]>0)
		/// </summary>
		public static string PivotOr<BASETYPE>( List<BASETYPE> list ) {
			return _Pivot1st2nd<BASETYPE>( list, " OR " );
		}

		//!! Warning: string BASETYPE can be used in sql injection attack
		private static string _Pivot1st2nd<BASETYPE>( List<BASETYPE> list, string andor ) {
			StringBuilder res = new StringBuilder( list.Count * 8 );
			int lenj = list.Count;
			if( lenj > 0 ) {
				res.Append( '(' );
				for( int j = 0; j < lenj; j++ ) {
					if( j > 0 )
						res.Append( andor );
					var tagid = list[j];
					res.Append( '[' );
					res.Append( tagid );
					res.Append( "]>0" );
				}
				res.Append( ')' );
			}
			return res.ToString();
		}

		/// <summary>
		/// Converts list of list into a string that can used in a SQL WHERE clause when values are converted to 
		/// columns in a pivot table.
		/// Resulting string contains AND'd values of the outter list and OR'd values in the inner list
		/// Example output of PivotAndOr<int>( new List() { new List<int>() { 1, 2 }, new List<int>() { 5 } } ); 
		///   would result in: ([1]>0 OR [2]) AND ([5]>0)
		/// </summary>
		public static string PivotAndOr<BASETYPE>( List<List<BASETYPE>> listoflist ) {
			return _Pivot1st2ndAnd1st<BASETYPE>( listoflist, " AND ", " OR " );
		}

		/// <summary>
		/// Converts list of list into a string that can used in a SQL WHERE clause when values are converted to 
		/// columns in a pivot table.
		/// Resulting string contains AND'd values of the outter list and OR'd values in the inner list, then OR'd values at the outter level
		/// Example output of PivotAndOrPlusOr<int>( new List() { new List<int>() { 1, 2 }, new List<int>() { 5 } }, new List<int>() { 6 } } ); 
		///   would result in: ([1]>0 OR [2]) AND ([5]>0) OR ([6]>0)
		/// </summary>
		public static string PivotAndOrPlusOr<BASETYPE>( List<List<BASETYPE>> listoflist, List<BASETYPE> list2nd ) {
			return _Pivot1st2ndAnd1st<BASETYPE>( listoflist, " AND ", " OR ", list2nd, " OR " );
		}

		/// <summary>
		/// Converts list of list into a string that can used in a SQL WHERE clause when values are converted to 
		/// columns in a pivot table.
		/// Resulting string contains AND'd values of the outter list and OR'd values in the inner list
		/// Example output of PivotAndOr<int>( new List() { new List<int>() { 1, 2 }, new List<int>() { 5 } } ); 
		///   would result in: ([1]>0 OR [2]) AND ([5]>0)
		/// </summary>
		public static string PivotOrAnd<BASETYPE>( List<List<BASETYPE>> listoflist) {
			return _Pivot1st2ndAnd1st<BASETYPE>( listoflist, " OR ", " AND " );
		}

		//!! Warning: string BASETYPE can be used in sql injection attack
		//!! Warning: string BASETYPE can be used in sql injection attack
		private static string _Pivot1st2ndAnd1st<BASETYPE>( List<List<BASETYPE>> listoflist, string andor1, string andor2, List<BASETYPE> list2nd=null, string andor3=null  ) {
			if( listoflist == null )
				return null;
			int leni = listoflist.Count;
			int len2 = list2nd != null ? list2nd.Count : 0;
			StringBuilder res = new StringBuilder(  leni * 128 + len2 * 64 );
			// Run through list of list and add items at the 2nd level
			for( int i = 0; i < leni; i++ ) {
				var list = listoflist[i];
				if( i != 0 )
					res.Append( andor1 );
				int lenj = list.Count;
				if( lenj > 0 ) {
					res.Append( '(' );
					for( int j = 0; j < lenj; j++ ) {
						if( j > 0 )
							res.Append( andor2 );
						var val = list[j];
						res.Append( '[' );
						res.Append( val );
						res.Append( "]>0" );
					}
					res.Append( ')' );
				}
			}
			// Run through second list and add items at first level
			for( int j = 0; j < len2; j++ ) {
				if( leni > 0 || j > 0 )
					res.Append( andor3 );
				var val = list2nd[j];
				res.Append( '[' );
				res.Append( val );
				res.Append( "]>0" );
			}
			return res.ToString();
		}
		/// Converts list into a string that can be used in a SQL SELECT clause as columns when values are converted 
		/// to columns in a pivot table.
		/// Resulting string contains a list of values contained within brackets [ ]
		/// Example output of PivotColumns<int>( new List() { 1, 2 }  );   would result in: [1],[2]
		public static string PivotColumns<BASETYPE>( List<BASETYPE> list ) {
			StringBuilder res = new StringBuilder( list.Count * 8 );
			res.Append( '[' );
			int lenj = list.Count;
			for( int j = 0; j < lenj; j++ ) {
				var val = list[j];
				if( j != 0 )
					res.Append( "],[" );
				res.Append( val );
			}
			res.Append( ']' );
			return res.ToString();
		}

		/// Converts list of list into a string that can be used in a SQL SELECT clause as columns when values 
		/// are converted to columns in a pivot table.
		/// Resulting string contains a list of values contained within brackets [ ]
		/// Example output of PivotColumns<int>( new List() { new List<int>() { 1, 2 }, new List<int>() { 5 } } ); 
		///   would result in: [1],[2],[5]
		public static string PivotColumns<BASETYPE>( List<List<BASETYPE>> listoflist, List<BASETYPE> list2nd = null ) {
			int leni = listoflist.Count;
			int len2 = list2nd != null ? list2nd.Count : 0;
			StringBuilder res = new StringBuilder( ( leni + len2 ) * 10 );
			for( int i = 0; i < leni; i++ ) {
				if( i > 0 )
					res.Append( ',' );
				res.Append( '[' );
				var list = listoflist[i];
				for( int j = 0, lenj = list.Count; j < lenj; j++ ) {
					var val = list[j];
					if( j > 0 )
						res.Append( "],[" );
					res.Append( val );
				}
				res.Append( ']' );
			}
			for( int j = 0; j < len2; j++ ) {
				if( leni > 0 || j > 0 )
					res.Append( ',' );
				res.Append( '[' );
				var val = list2nd[j];
				res.Append( val );
				res.Append( ']' );
			}

			return res.ToString();
		}

		public static List<Int32> ToInt32List( string[] list ) {
			int len=list.Length;
			var res = new List<Int32>( len );
			for( int i = 0; i < len; i++ ) {
				var t = default( Int32 );
				if( !Int32.TryParse( list[i], out t ) )
					return null;
				res.Add( t );
			}
			return res;
		}
		public static List<Int64> ToInt64List( string[] list ) {
			int len = list.Length;
			var res = new List<Int64>( len );
			for( int i = 0; i < len; i++ ) {
				var t = default( Int64 );
				if( !Int64.TryParse( list[i], out t ) )
					return null;
				res.Add( t );
			}
			return res;
		}

	}

}