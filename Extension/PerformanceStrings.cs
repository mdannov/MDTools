using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Extension {

	public static class PerformanceStrings {

		#region Replace

		/// <summary>
		/// Change single character at location in string: equiv to "mystring[i]=replacement; return mystring;"
		/// Note: Fastest method for single character; not as efficient if changing many characters; use StringBuilder in that case
		/// </summary>
#if !FullTrust
		public static string CharAt( this string source, int index, char replacement ) {
			// Check if nothing to do
			if( source[index] == replacement )
				return source;
			// Fastest way to replace single character at position
			var temp = source.ToCharArray();
			temp[index] = replacement;
			return new String( temp );
		}
#else
		public static unsafe string CharAt( this string source, int index, char replacement ) {
			if( source[index] == replacement )
				return source;
			var target = string.Copy( source );// non-interned copy of source
			fixed( char* p = target ) {
				p[index] = replacement;
			}
			return target;
		}
#endif

		/// <summary>
		/// Replace characters
		/// </summary>
		[Obsolete("Replace() is equivalent/faster")]
		public static string ReplaceAll( this string source, char from, char to ) {
			return source.Replace( from, to );
#if slower
			char[] temp = null;
			for( int i = 0, len = source.Length; i < len; i++ )
				if( source[i] == from ) {
					if(temp==null)
						temp = source.ToCharArray();
					temp[i] = to;
				}
			return temp!=null ? new String( temp ) : source;
#endif
		}
		/// <summary>
		/// Replace all from characters with to at same position; faster than Replace; from/to should preferably be a static array
		/// </summary>
		public static string ReplaceAll( this string source, char[] from, char[] to ) {
			int fromlen = from.Length;
#if DEBUG
			if( fromlen != to.Length )
				throw new ArgumentException( "from list count must match to list count" );
#endif
			if( fromlen == 1 )
				return source.Replace( from[0], to[0] );
			var temp = source.ToCharArray();
			bool changed = false;
			for( int i = 0, len = temp.Length; i < len; i++ ) {
				var ch = temp[i];
				for( int j = 0; j < fromlen; j++ ) {
					if( ch == from[j] ) {
						temp[i] = to[j];
						changed = true;
						break;
					}
				}
			}
			return changed ? new String( temp ) : source;
		}
		/// <summary>
		/// Replace all from characters with to character; faster than Replace; from should preferably be a static array
		/// </summary>
		public static string ReplaceAll( this string source, char[] from, char to ) {
			int fromlen = from.Length;
			if( fromlen == 1 )
				return source.Replace( from[0], to );
			var temp = source.ToCharArray();
			bool changed = false;
			for( int i = 0, len = temp.Length; i < len; i++ ) {
				var ch = temp[i];
				for( int j = 0; j < fromlen; j++ ) {
					if( ch == from[j] ) {
						temp[i] = to;
						changed = true;
						break;
					}
				}
			}
			return changed ? new String( temp ) : source;
		}
		/// <summary>
		/// Replace any from characters with strings at same position; from/to should preferably be a static array
		/// </summary>
		public static string ReplaceAll( this string source, char[] from, string[] to ) {
			int fromlen = from.Length;
#if DEBUG
			if( fromlen != to.Length )
				throw new ArgumentException( "from list count must match to list count" );
#endif
			if( fromlen == 1 )
				return ReplaceAll( source, from[0], to[0] );
			int len = source.Length;
			StringBuilder sb = null;
			for( int i = 0; i < len; i++ ) {
				var ch = source[i];
				for( int j = 0; j < fromlen; j++ ) {
					if( ch == from[j] ) {
						if( sb == null ) {
							sb = new StringBuilder( (int)( len * 1.1 ) );// add 10%
							if( i > 0 )
								sb.Append( source, 0, i );
						}
						sb.Append( to[j] );
						break;
					} else if( sb != null )
						sb.Append( ch );
				}
			}
			return sb == null ? source : sb.ToString();
		}
		/// <summary>
		/// Replace any from characters with to string; from/to should preferably be a static array
		/// </summary>
		public static string ReplaceAll( this string source, char[] from, string to ) {
			int fromlen = from.Length;
			if( fromlen == 1 )
				return ReplaceAll( source, from[0], to );
			int len = source.Length;
			StringBuilder sb = null;
			for( int i = 0; i < len; i++ ) {
				var ch = source[i];
				for( int j = 0; j < fromlen; j++ ) {
					if( ch == from[j] ) {
						if( sb == null ) {
							sb = new StringBuilder( (int)( len * 1.1 ) );// add 10%
							if( i > 0 )
								sb.Append( source, 0, i );
						}
						sb.Append( to );
						break;
					} else if( sb != null )
						sb.Append( ch );
				}
			}
			return sb == null ? source : sb.ToString();
		}
		/// <summary>
		/// Replace character with string
		/// </summary>
		public static string ReplaceAll( this string source, char from, string to ) {
			int len = source.Length;
			StringBuilder sb = null;
			for( int i = 0; i < len; i++ ) {
				var ch = source[i];
				if( ch == from ) {
					if( sb == null ) {
						sb = new StringBuilder( (int)( len * 1.1 ) );// add 10%
						if( i > 0 )
							sb.Append( source, 0, i );
					}
					sb.Append( to );
				} else if( sb != null )
					sb.Append( ch );
			}
			return sb == null ? source : sb.ToString();
		}
		/// <summary>
		/// Replace all occurences of string in string
		/// </summary>
		public static string ReplaceAll( this string source, string from, string to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, string[] from, string[] to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, string[] from, string to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, string from, char to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, string[] from, char[] to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, string[] from, char to ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, IDictionary<char, char> dict ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, IDictionary<char, string> dict ) {
			throw new NotImplementedException();
		}
		public static string ReplaceAll( this string source, IDictionary<string, char> dict ) {
			throw new NotImplementedException();
		}

		#region Replace

		/// <summary>
		/// Replace single occurence of char in string
		/// </summary>
		public static string ReplaceOnce( this string source, char from, char to ) {
			int i = source.IndexOf( from );
			return ( i < 0 ) ? source : source.CharAt( i, to );
		}
		/// <summary>
		/// Replace single occurence of string in string
		/// </summary>
		public static string ReplaceOnce( this string source, string pattern, string with ) {
			int pos = source.IndexOf( pattern );
			if( pos < 0 )
				return source;
			// Handle if char
			int withlen = with.Length;
			if( withlen == 1 && pattern.Length == 1 )
				return CharAt( source, pos, with[0] );
			// Make replacement string
			int patlen = pattern.Length;
			int srclen = source.Length;
			var res = new char[srclen + withlen - patlen];
			source.CopyTo( 0, res, 0, pos );
			with.CopyTo( 0, res, pos, withlen );
			source.CopyTo( pos + patlen, res, pos + withlen, srclen - ( pos + patlen ) );
			return new string( res );
		}
		//!! ReplaceOnce( char, string )
		//!! ReplaceOnce( string, char )

		/// <summary>
		/// Replace single occurence of string in string
		/// </summary>
		public static string ReplaceLast( this string source, string pattern, string with ) {
			int pos = source.LastIndexOf( pattern );
			if( pos < 0 )
				return source;
			// Handle if char
			int withlen = with.Length;
			if( withlen == 1 && pattern.Length == 1)
				return CharAt( source, pos, with[0] );
			// Make replacement string
			int patlen = pattern.Length;
			int srclen = source.Length;
			var res = new char[srclen + withlen - patlen];
			source.CopyTo( 0, res, 0, pos );
			with.CopyTo( 0, res, pos, withlen );
			source.CopyTo( pos + patlen, res, pos + withlen, srclen - ( pos + patlen ) );
			return new string( res );
		}
		public static string ReplaceLast( this string source, char pattern, char with ) {
			int pos = source.LastIndexOf( pattern );
			if( pos < 0 )
				return source;
			// Handle if char
			return CharAt( source, pos, with );
		}
		//!! ReplaceLast( char, string )
		//!! ReplaceLast( string, char )
		#endregion


		/// <summary>
		/// Remove spaces-only
		/// </summary>
		public static string RemoveSpaces( this string s ) {
			return s.RemoveAll( ' ' );// Replace( " ", "" );
		}
		/// <summary>
		/// Remove all white-space characters //** try StringBuilder technique similar to Replace
		/// </summary>
		public static string RemoveWhitespace( this string s ) {
			var res = string.Empty;
			for( int i = 0, len = s.Length; i < len; i++ ) {
				var c = s[i];
				if( !char.IsWhiteSpace( c ) )
					res += c;
			}
			return res;
		}
		/// <summary>
		/// Remove all but characters in list //!! consider the StringBuffer model used in Replace
		/// </summary>
		public static string RemoveAllBut( this string s, string notin ) {
			var res = string.Empty;
			for( int i = 0, len = s.Length; i < len; i++ ) {
				var c = s[i];
				if( notin.Contains( c ) )
					res += c;
			}
			return res;
		}
		/// <summary>
		/// Remove all but characters in char array -- !!use StringBuilder - See Replace
		/// </summary>
		public static string RemoveAllBut( this string s, params char[] notin ) {
			var res = string.Empty;
			for( int i = 0, len = s.Length; i < len; i++ ) {
				var c = s[i];
				if( notin.Contains( c ) )
					res += c;
			}
			return res;
		}
		/// <summary>
		/// Remove characters from string
		/// </summary>
		public static string RemoveAll( this string source, char removeChar ) {
			int len = source.Length;
			StringBuilder sb = null;
			for( int i = 0; i < len; i++ ) {
				var ch = source[i];
				if( ch == removeChar ) {
					if( sb == null ) {
						sb = new StringBuilder( len );// add 10%
						if( i > 0 )
							sb.Append( source, 0, i );
					}
				} else if( sb != null )
					sb.Append( ch );
			}
			return sb == null ? source : sb.ToString();
		}
		/// <summary>
		/// Remove characters from string
		/// </summary>
		public static string RemoveAll( this string source, char[] any ) {
			int fromlen = any.Length;
			int len = source.Length;
			StringBuilder sb = null;
			for( int i = 0; i < len; i++ ) {
				var ch = source[i];
				for( int j = 0; j < fromlen; j++ ) {
					if( ch == any[j] ) {
						if( sb == null ) {
							sb = new StringBuilder( len );
							if( i > 0 )
								sb.Append( source, 0, i );
						}
						break;
					} else if( sb != null )
						sb.Append( ch );
				}
			}
			return sb == null ? source : sb.ToString();
		}
		/// <summary>
		/// Remove all occurences of string in string
		/// </summary>
		public static string RemoveAll( this string source, string pattern ) {
			throw new NotImplementedException();
		}
		public static string RemoveAll( this string source, HashSet<char> any ) {
			throw new NotImplementedException();
		}

		#endregion

		#region Delim, Tokens, and Token Count

		/// <summary>
		/// Only use if items between tokens are guaranteed unique and tokens are not possibly subsets of each other
		/// </summary>
		public static bool FastTokenCheck( this string instr, string find, char token ) {
			if( string.IsNullOrEmpty( instr ) )
				return false;
			var pos = instr.IndexOf( find );// eg: instr.FastTokenCheck(find, ',')
			if( pos < 0 )	// not found
				return false;
			int len = instr.Length, end = pos + find.Length;
			if( pos == 0 )	// at beginning  // find "11" - "11,22,33"(pos=0,len=8,end=1), "1"(pos=0,len=1,end=1) find "11" in "11,22,33"(pos=0,len=8,end=2)
				return ( end == len ) ? true : instr[end] == token;// nextchar is end or token
			if( end == len )// at end (pos>0)// find "33" - "11,22,33"(pos=4,len=5,end=5) find "33" - "11,22,33"(pos=6,len=8,end=8)
				return instr[pos - 1] == token; // prevchar is token
			// between tokens middle
			return instr[pos - 1] == token && instr[end] == token; // prev & nextchar are tokens
		}

		public static void DelimAppend( this StringBuilder sb, string txt, string delim = ", " ) {
			if( sb.Length > 0 )
				sb.Append( delim );
			sb.Append( txt );
		}
		public static void DelimAppend( this StringBuilder sb, string txt, char delim ) {
			if( sb.Length > 0 )
				sb.Append( delim );
			sb.Append( txt );
		}
		public static string DelimAppend( this string str, string txt, string delim = ", " ) {
			if( string.IsNullOrEmpty( str ) )
				return txt;
			if( string.IsNullOrEmpty( txt ) )
				return str;
			return string.Concat( str, delim, txt );
		}
		public static string DelimAppend( this string str, string txt, char delim ) {
			if( string.IsNullOrEmpty( str ) )
				return txt;
			if( string.IsNullOrEmpty( txt ) )
				return str;
			return string.Concat( str, delim, txt );
		}

		/// <summary>
		/// Indicates the number of sections that would be created if splitting by a token; Equivalent to str.Split( c ).Length
		/// Same as Count( str, c ) + 1
		/// (Confirmed fastest)
		/// </summary>
		public static int SplitCount( this string source, char find ) {
			int count = 0;
			for( int i=0, len=source.Length; i<len; i++ )
				if( source[i] == find )
					count++;
			return count+1;
		}
		/// <summary>
		/// Indicates the number of sections that would be created if splitting by a token; Equivalent to str.Split( c ).Length
		/// Same as Count( str, c ) + 1
		/// </summary>
		public static int SplitCount( this string str, string c ) {
			int cnt = 0;
			int lenc = c.Length;
			for( int i = -1; ( i = str.IndexOf( c, i + lenc ) ) > -1; )
				cnt++;
			return cnt+1;
		}

		#endregion

		public static bool Equals( this string from, string to, int fromPos, bool ignoreCase = false ) {
			return string.Compare( to, 0, from, fromPos, from.Length - fromPos, ignoreCase ) == 0;
		}
		public static bool Equals( this string from, string to, int fromPos, int Cnt, bool ignoreCase = false ) {
			return string.Compare( to, 0, from, fromPos, Cnt, ignoreCase ) == 0;
		}
		public static bool Equals( this string from, string to, int fromPos, int toPos, int Cnt, bool ignoreCase = false ) {
			return string.Compare( to, toPos, from, fromPos, Cnt, ignoreCase ) == 0;
		}

		#region Split //!! Test if performance is actually better

		public class SplitString {
			public struct ItemSplit {
				public int start;
				public int end;
				public string str;
			}
			public string String;
			public IList<ItemSplit> items = null;
			public string this[int idx] {
				get {
					var item = items[idx];
					if( item.str != null ) // if already cached
						return item.str;
					if( item.start == item.end )
						return item.str = string.Empty;
					return item.str = String.Substring( item.start, item.end - item.start );
				}
			}
			public int Length { get { return items.Count; } }
		}

		public static SplitString FastSplit( this string str, char delim, int anticipatedSize = 10 ) {
			var ss = new SplitString() { String = str };
			var list = new List<SplitString.ItemSplit>( anticipatedSize );
			int last = 0;
			// Add internal items
			for( int pos = 0; ( pos = str.IndexOf( delim, pos ) ) > -1; last=++pos )
				list.Add( new SplitString.ItemSplit() { start = last, end = pos } );
			// Add last item
			list.Add( new SplitString.ItemSplit() { start = last, end = str.Length } );
			ss.items = list;
			return ss;
		}

		public static SplitString FastSplit( this string str, string delim, int anticipatedSize = 10 ) {
			var ss = new SplitString() { String = str };
			var list = new List<SplitString.ItemSplit>( anticipatedSize );
			int last = 0;
			int delimlen = delim.Length;
			// Add internal items
			for( int pos = 0; ( pos = str.IndexOf( delim, pos ) ) > -1; pos += delimlen, last = pos )
				list.Add( new SplitString.ItemSplit() { start = last, end = pos } );
			// Add last item
			list.Add( new SplitString.ItemSplit() { start = last, end = str.Length } );
			ss.items = list;
			return ss;
		}

		#endregion -- Split

		#region To Number - Use these instead if the odds that the integer value is more likely a low single digit number than a higher one

		private readonly static char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char ToSingleDigit( this Int32 i ) {
			return numbers[i % 10];
		}
		public static char ToSingleDigit( this Int64 i ) {
			return numbers[i % 10];
		}
		private readonly static string[] numberstr = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
		public static string ToDigitString( this Int32 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this UInt32 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this Int16 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this UInt16 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this Int64 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this UInt64 i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
		public static string ToDigitString( this byte i ) {
			return i < 10 ? numberstr[i] : i.ToString();
		}
	
		#endregion

		#region Count Occurence

		/// <summary>
		/// Count the occurance of a character in the string
		/// (Confirmed fastest)
		/// </summary>
		public static int Count( this string source, char find ) {
			int count = 0;
			for( int i = 0, len = source.Length; i < len; i++ )
				if( source[i] == find )
					count++;
			return count;
		}
		/// <summary>
		/// Count the occurance of a character in the string
		/// </summary>
		public static int Count( this string str, string c ) {
			int cnt = 0;
			int lenc = c.Length;
			for( int i = -1; ( i = str.IndexOf( c, i + lenc ) ) > -1; )
				cnt++;
			return cnt;
		}

		#endregion

		/// <summary>
		/// Converts a string to a Title/Sentence case
		/// </summary>
		public static string ToTitleCase( this string String ) {
			if( String.Length > 0 )
				String.CharAt( 0, char.ToUpper( String[0] ) );
			return String;
		}

		/// <summary>
		/// Reverse the string
		/// from http://en.wikipedia.org/wiki/Extension_method
		/// </summary>
		public static string Reverse( this string input ) {
			char[] chars = input.ToCharArray();
			Array.Reverse( chars );
			return new String( chars );
		}

	}

}
