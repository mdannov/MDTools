using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;
using System.Threading;

// Collection of Primitive Type Extensions from a number of web contributors

namespace MDTools.Extension {

	public static class ObjectExt {
		#region MD
		#endregion

		#region Other Sources

		public static bool IsNumericType( this object o ) {
			if( o == null )
				return false;
			switch( Type.GetTypeCode( o.GetType() ) ) {
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				return true;
			default:
				return false;
			}
		}
		public static bool IsIntType( this object o ) {
			switch( Type.GetTypeCode( o.GetType() ) ) {
			case TypeCode.Boolean:
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
				return true;
			default:
				return false;
			}
		}

		static bool IsSubclassOfRawGeneric( this Type toCheck, Type generic ) {
			while( toCheck != null && toCheck != typeof( object ) ) {
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if( generic == cur ) 
					return true;
				//toCheck = toCheck.BaseType;
			}
			return false;
		}

		#endregion
	}

	public static class NumberExt {
		#region MD

		public static int Min( params int[] vals ) {
			int res = int.MaxValue;
			for( int i = 0, len = vals.Length; i < len; i++ ) {
				var cur = vals[i];
				if( cur < res )
					res = cur;
			}
			return res;
		}
		public static int Max( params int[] vals ) {
			int res = int.MinValue;
			for( int i = 0, len = vals.Length; i < len; i++ ) {
				var cur = vals[i];
				if( cur > res )
					res = cur;
			}
			return res;
		}
		public static uint Min( params uint[] vals ) {
			uint res = uint.MaxValue;
			for( int i = 0, len = vals.Length; i < len; i++ ) {
				var cur = vals[i];
				if( cur < res )
					res = cur;
			}
			return res;
		}
		public static uint Max( params uint[] vals ) {
			uint res = uint.MinValue;
			for( int i = 0, len = vals.Length; i < len; i++ ) {
				var cur = vals[i];
				if( cur > res )
					res = cur;
			}
			return res;
		}
		#endregion

		#region Other Sources
		#endregion
	}
	public static class StringExt {
		#region MD

		public static bool Equals( this string input, string comp, bool ignoreCase ) {
			return ignoreCase ? input.Equals( comp, StringComparison.InvariantCultureIgnoreCase ) : input == comp;
		}
		public static bool NotEquals( this string input, string comp, bool ignoreCase ) {
			return ignoreCase ? !input.Equals( comp, StringComparison.InvariantCultureIgnoreCase ) : input != comp;
		}

		/// <summary>
		/// Limit string to maximum length; ensures its at least empty
		/// </summary>
		public static string Constrain( this string input, int max ) {
			if( input == null )
				return string.Empty;
			if( input.Length <= max )
				return input;
			return input.Substring( 0, max );
		}
		/// <summary>
		/// Protect against sql injection by making sure sql strings are closed
		/// </summary>
		public static string DoubleApostrophes( this string input ) {
			return input.ReplaceAll( '\'', "''" );
		}
		/// <summary>
		/// Protect against javascript injection by making sure strings are closed
		/// </summary>
		public static string EscapeApostrophes( this string input ) {
			if( escAposFrom == null ) {
				escAposFrom = new char[] { '\\', '\'' };
				escAposTo = new string[] { @"\\", @"\'" };
			}
			return input.ReplaceAll( escAposFrom, escAposTo );//Replace( '\\', @"\\" ).Replace( '\'', @"\'" );
		}
		public static string EscapeQuotes( this string input ) {
			if( escQuotFrom == null ) {
				escQuotFrom = new char[] { '\\', '\"' };
				escQuotTo = new string[] { @"\\", "\\\"" };
			}
			return input.ReplaceAll( escQuotFrom, escQuotTo );// Replace('\\', @"\\" ).Replace( '\"', "\\\"" );
		}
		private static char[] escQuotFrom = null;
		private static string[] escQuotTo = null;
		private static char[] escAposFrom = null;
		private static string[] escAposTo = null;

		private static char[] JSUnsafe = null;
		public static bool IsJavaScriptStringSafe( this string input ) {
			if(JSUnsafe==null) JSUnsafe = new char[] { ';', '\\' };
			return ( input.IndexOfAny( JSUnsafe ) < 0 );
		}

#if slower
		private static Regex RegExFNUnsafe = null;
		public static string FixInvalidFilename( this string filename, string replaceBadCharsWith = null ) {
			if( RegExFNUnsafe == null ) {
				RegExFNUnsafe = new Regex( string.Concat( "[", Regex.Escape( Path.GetInvalidFileNameChars().ToString() + Path.GetInvalidPathChars().ToString() ), "]" ) );
			}
			return RegExFNUnsafe.Replace( filename, (replaceBadCharsWith == null) ? string.Empty : replaceBadCharsWith );
		}
#endif
		private static char[] FNUnsafe = null;
		public static string FixInvalidFilename( this string filename, char replaceBadCharsWith = '_' ) {
			if( FNUnsafe == null )
				FNUnsafe = Path.GetInvalidFileNameChars().Expand( Path.GetInvalidPathChars() );
			int pos=0;
			StringBuilder sb = null;
			while( ( pos = filename.IndexOfAny( FNUnsafe, pos ) ) >= 0 ) {
				if( sb == null )
					sb = new StringBuilder( filename );
				sb[pos++] = replaceBadCharsWith;
			}
			return sb==null ? filename : sb.ToString();
		}

		/// <summary>
		/// Convert string to a specific Enum Type
		/// </summary>
		/// <typeparam name="ENUM">Enum Type</typeparam>
		public static ENUM ToEnum<ENUM>( this string val, ENUM defval, bool ignoreCase = true ) where ENUM : struct {
			if( string.IsNullOrEmpty( val ) )
				return defval;
			ENUM e;
			if( !Enum.TryParse<ENUM>( val, ignoreCase, out e ) )
				return defval;
			return e;
		}

		/// <summary>
		/// Converts a java string that contains \x## values to a proper string
		/// </summary>
		public static string ConvertJHex( this string str ) {
			int len = str.Length;
			StringBuilder sb = new StringBuilder( len );
			for( int i = 0; i < len; i++ ) {
				char c = str[i];
				if( c == '\\' && i <= len-4 && str[i + 1] == 'x' ) {// find "\x" within string
					Int32 val = 0;
					try { val = Convert.ToInt32( str.Substring( i + 2, 2 ), 16 ); }// convert from hex
					catch { sb.Append( c ); continue; }	// failure so continue and just copy
					sb.Append( char.ConvertFromUtf32( val ) );
					i += 3;	// on success, move 3 chars after the '\'
				} else
					sb.Append( c );
			}
			return sb.ToString();
		}

		public static Int16 ToInt16( this string val, Int16 def=0 ) {
			Int16 r = def;
			Int16.TryParse( val, out r );
			return r;
		}
		public static UInt16 ToUInt16( this string val, UInt16 def=0 ) {
			UInt16 r = def;
			UInt16.TryParse( val, out r );
			return r;
		}
		public static Int32 ToInt32( this string val, Int32 def=0 ) {
			Int32 r = def;
			Int32.TryParse( val, out r );
			return r;
		}
		public static UInt32 ToUInt32( this string val, UInt32 def=0 ) {
			UInt32 r = def;
			UInt32.TryParse( val, out r );
			return r;
		}
		public static Int64 ToInt64( this string val, Int64 def=0 ) {
			Int64 r = def;
			Int64.TryParse( val, out r );
			return r;
		}
		public static UInt64 ToUInt64( this string val, UInt64 def=0 ) {
			UInt64 r = def;
			UInt64.TryParse( val, out r );
			return r;
		}
		public static Decimal ToDecimal( this string val, Decimal def=0m ) {
			Decimal r = def;
			Decimal.TryParse( val, out r );
			return r;
		}
		public static Double ToDouble( this string val, Double def=0 ) {
			Double r = def;
			Double.TryParse( val, out r );
			return r;
		}
		public static Single ToSingle( this string val, Single def=0 ) {
			Single r = def;
			Single.TryParse( val, out r );
			return r;
		}
		public static byte[] ToByteArray( this string val ) {
		    /*System.Text.UTF8Encoding enc=new System.Text.UTF8Encoding();
			return enc.GetBytes(val);*/
			return Encoding.UTF8.GetBytes( val );
		}
		/// <summary>
		/// Convert Sql Server *varchar/text type string to utf-8 - default page is 1252
		/// </summary>
		public static string ToUtf8FromSql( this string val, int page = 1252 ) {
			byte[] bytes = Encoding.GetEncoding( page ).GetBytes( val );
			return Encoding.UTF8.GetString( bytes );
		}
		/// <summary>
		/// Convert utf-8 string as Sql Server *varchar/text type string - default page is 1252
		/// </summary>
		public static string ToSqlFromUtf8( this string val, int page = 1252 ) {
			return Encoding.GetEncoding( page ).GetString( Encoding.UTF8.GetBytes( val ) );
		}

		public static string EncodeRedirectUrlSafeParam( this string value ) {
			var enc = Convert.ToBase64String( System.Text.ASCIIEncoding.ASCII.GetBytes( value ) ).ReplaceAll( urlSafeT, urlSafeF );
			int len = enc.Length;
			if(len>0 && enc[len-1]=='=')  {
				len--;
				if(len>0 && enc[len-1]=='=')
					len--;
				return enc.Substring( 0, len );
			}
			return enc;
		}
		public static string DecodeRedirectUrlSafeParam( this string value ) {
			value= value.ReplaceAll( urlSafeF, urlSafeT );// Replace( '-', '/' ).Replace( '_', '+' );
			int len = value.Length;
			len = len + ( 4 - len % 4 ) % 4;
			return System.Text.ASCIIEncoding.ASCII.GetString( Convert.FromBase64String( value.PadRight( len, '=' ) ) );
		}
		private static char[] urlSafeF = { '-', '_' };
		private static char[] urlSafeT = { '/', '+' };

		public static string HtmlHardspaces( this string val ) {
			return val.ReplaceAll( ' ', "&nbsp;" );
		}

		#region Substrings

		public static string[] SplitAtFirst( this string input, char delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return new string[] { input };
			return new string[] { input.Substring( 0, pos ), input.Substring( pos + 1 ) };
		}
		public static string[] SplitAtFirst( this string input, string delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return new string[] { input };
			return new string[] { input.Substring( 0, pos ), input.Substring( pos + delim.Length ) };
		}
		public static string[] SplitAtLast( this string input, char delim ) {
			int pos = input.LastIndexOf( delim );
			if( pos < 0 )
				return new string[] { input };
			return new string[] { input.Substring( 0, pos ), input.Substring( pos + 1 ) };
		}
		public static string[] SplitAtLast( this string input, string delim ) {
			int pos = input.LastIndexOf( delim );
			if( pos < 0 )
				return new string[] { input };
			return new string[] { input.Substring( 0, pos ), input.Substring( pos + delim.Length ) };
		}
		public static string RightOfLast( this string input, char ch ) {
			int pos = input.LastIndexOf( ch );
			if( pos < 0 )
				return null;
			return input.Substring( pos + 1 );
		}
		public static string RightOfLast( this string input, string delim ) {
			int pos = input.LastIndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( pos + delim.Length );
		}
		public static string RightOfFirst( this string input, char delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( pos + 1 );
		}
		public static string RightOfFirst( this string input, string delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( pos + delim.Length );
		}
		public static string LeftOfLast( this string input, char delim ) {
			int pos = input.LastIndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( 0, pos );
		}
		public static string LeftOfLast( this string input, string delim ) {
			int pos = input.LastIndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( 0, pos );
		}
		public static string LeftOfFirst( this string input, char delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( 0, pos );
		}
		public static string LeftOfFirst( this string input, string delim ) {
			int pos = input.IndexOf( delim );
			if( pos < 0 )
				return null;
			return input.Substring( 0, pos );
		}
		public static string BetweenLastAndNext( this string input, char delim1, char delim2 ) {
			int p1 = input.LastIndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1++;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenLastAndNext( this string input, string delim1, string delim2 ) {
			int p1 = input.LastIndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1 += delim1.Length;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenLastAndNext( this string input, string delim1, char delim2 ) {
			int p1 = input.LastIndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1 += delim1.Length;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenLastAndNext( this string input, char delim1, string delim2 ) {
			int p1 = input.LastIndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1++;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenFirstAndNext( this string input, char delim1, char delim2 ) {
			int p1 = input.IndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1++;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenFirstAndNext( this string input, string delim1, string delim2 ) {
			int p1 = input.IndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1 += delim1.Length;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenFirstAndNext( this string input, string delim1, char delim2 ) {
			int p1 = input.IndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1 += delim1.Length;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenFirstAndNext( this string input, char delim1, string delim2 ) {
			int p1 = input.IndexOf( delim1 );
			if( p1 < 0 )
				return null;
			p1++;
			int p2 = input.IndexOf( delim2, p1 );
			if( p2 < 0 )
				return null;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenLastAndLast( this string input, char delim1, char delim2 ) {//**Tested
			int p2 = input.LastIndexOf( delim2 );
			if( p2 < 0 )
				return null;
			int p1 = input.LastIndexOf( delim1, p2 );
			if( p1 < 0 )
				return null;
			p1++;
			return input.Substring( p1, p2 - p1 );
		}
		public static string BetweenLastAndLast( this string input, string delim1, string delim2 ) {
			int p2 = input.LastIndexOf( delim2 );
			if( p2 < 0 )
				return null;
			int p1 = input.LastIndexOf( delim1, p2 );
			if( p1 < 0 )
				return null;
			p1+=delim1.Length;
			return input.Substring( p1, p2 - p1 );
		}
		// BetweenLastAndLast
		// BetweenFirstAndLast
		// BetweenLastAndPossibleLast
		// BetweenFirstAndPossibleLast
		// BetweenFirstAndPossibleFirst
		// BetweenLastAndPossibleFirst
		// BetweenPossibleLastAndLast
		// BetweenPossibleFirstAndLast
		// BetweenPossibleFirstAndFirst
		// BetweenPossibleAndFirst
		// BetweenNth...
		// Any...

		#endregion

		#endregion

		#region Other Sources

		/// <summary>
		/// T must be enum type - where cannot be enum cuz enum is abstract, but it can be the same bases
		/// </summary>
		public static bool EnumTryParse<T>( this string strType, out T result ) where T:IComparable, IFormattable, IConvertible {
			var names = Enum.GetNames( typeof( T ) );
			for( int i = 0, len = names.Length; i < len; i++ ) {
				var value = names[i];
				if( value.Equals( strType, StringComparison.OrdinalIgnoreCase ) ) {
					result = (T)Enum.Parse( typeof( T ), value, true );
					return true;
				}
			}
			result = default( T ); //!! known bug if enum doesn't have 0 as firstvalue
			return false;
		}

		public static bool IsNullOrEmpty( this string s ) { return s == null || s.Length == 0; }
		
		public static bool StartsWith( this string s, params string[] candidate ) {
			string match = candidate.FirstOrDefault( t => s.StartsWith( t ) );
			return match != default( string );
		}

		public static bool SubstringIs( this string s, int start, int length, params string[] candidate ) {
			if( start < 0 )
				return false;
			string sub = s.Substring( start, length );
			string match = candidate.FirstOrDefault( t => t == sub );
			return match != default( string );
		}

		public static string RemoveNonNumeric( this string s ) {
			if( !string.IsNullOrEmpty( s ) ) {
				char[] result = new char[s.Length];
				int resultIndex = 0;
				foreach( char c in s ) {
					if( char.IsNumber( c ) )
						result[resultIndex++] = c;
				}
				if( 0 == resultIndex )
					s = string.Empty;
				else if( result.Length != resultIndex )
					s = new string( result, 0, resultIndex );
			}
			return s;
		}

		/// <summary>
		/// Returns the given string truncated to the specified length, suffixed with an elipses (...)
		/// </summary>
		/// <param name="input"></param>
		/// <param name="length">Maximum length of return string</param>
		/// <returns></returns>
		public static string Truncate( this string input, int length ) {
			return Truncate( input, length, "..." );
		}
		/// <summary>
		/// Returns the given string truncated to the specified length, suffixed with the given value
		/// </summary>
		/// <param name="input"></param>
		/// <param name="length">Maximum length of return string</param>
		/// <param name="suffix">The value to suffix the return value with (if truncation is performed)</param>
		/// <returns></returns>
		public static string Truncate( this string input, int length, string suffix ) {
			if( input == null )
				return string.Empty;
			if( input.Length <= length )
				return input;
			if( suffix == null )
				suffix = "...";
			return input.Substring( 0, length - suffix.Length ) + suffix;
		}

		/// <summary>
		/// Splits a given string into an array based on character line breaks
		/// </summary>
		/// <param name="input"></param>
		/// <returns>String array, each containing one line</returns>
		public static string[] ToLineArray( this string input ) {
			if( input == null )
				return null;
			return input.Split( new string[] {"\r\n", "\n"}, StringSplitOptions.None );
		}

		/// <summary>
		/// Splits a given string into a strongly-typed list based on character line breaks
		/// </summary>
		/// <param name="input"></param>
		/// <returns>Strongly-typed string list, each containing one line</returns>
		public static List<string> ToLineList( this string input ) {
			List<string> output = new List<string>( input.ToLineArray() );
			return output;
		}

		/// <summary>
		/// Replaces line breaks with self-closing HTML 'br' tags
		/// </summary>
		public static string ReplaceBreaksWithBR( this string input ) {
			return string.Join( "<br/>", input.ToLineArray() );
		}

		/// <summary>
		/// Replace well-formed html urls in string to html anchors
		/// </summary>
		public static string ReplaceUrlsWithAnchors( this string input ) {
			return Regex.Replace( input,
					@"((http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
					"<a target='_blank' href='$1'>$1</a>" );
		}
		/// <summary>
		/// Encodes the input string as HTML (converts special characters to entities)
		/// </summary>
		public static string HtmlEncode( this string input ) {
			return HttpUtility.HtmlEncode( input );
		}
		/// <summary>
		/// Encodes the input string as a URL (converts special characters to % codes)
		/// </summary>
		public static string UrlEncode( this string input ) {
			return HttpUtility.UrlEncode( input );
		}
		/// <summary>
		/// Decodes any HTML entities in the input string
		/// </summary>
		public static string HtmlDecode( this string input ) {
			return HttpUtility.HtmlDecode( input );
		}
		/// <summary>
		/// Decodes any URL codes (% codes) in the input string
		/// </summary>
		/// <param name="input"></param>
		/// <returns>String</returns>
		public static string UrlDecode( this string input ) {
			return HttpUtility.UrlDecode( input );
		}

		/// <summary>
		/// Removes any HTML tags from the input string
		/// </summary>
		/// <param name="input"></param>
		/// <returns>String</returns>
		public static string StripHTML( this string input ) {
			return Regex.Replace( input, @"<(style|script)[^<>]*>.*?</\1>|</?[a-z][a-z0-9]*[^<>]*>|<!--.*?-->", "" );
		}
		/// <summary>
		/// true, if is valid email address
		/// from http://www.davidhayden.com/blog/dave/
		/// archive/2006/11/30/ExtensionMethodsCSharp.aspx
		/// </summary>
		/// <param name="s">email address to test</param>
		/// <returns>true, if is valid email address</returns>
		public static bool IsValidEmailAddress( this string s ) {
			return new Regex( @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,6}$" ).IsMatch( s );
		}

		/// <summary>
		/// Checks if url is valid. 
		/// from http://www.osix.net/modules/article/?id=586 and changed to match http://localhost
		/// 
		/// complete (not only http) url regex can be found 
		/// at http://internet.ls-la.net/folklore/url-regexpr.html
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static bool IsValidUrl( this string url ) {
			string strRegex = @"^(https?://)?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?(([0-9]{1,3}\.){3}[0-9]{1,3}|([0-9a-z_!~*'()-]+\.)*([0-9a-z][0-9a-z-]{0,61})?[0-9a-z](\.[a-z]{2,6})?)(:[0-9]{1,5})?((/?)|(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$";
			return new Regex( strRegex ).IsMatch( url );
		}

		/// <summary>
		/// Reduce string to shorter preview which is optionally ended by some string (...).
		/// </summary>
		/// <param name="s">string to reduce</param>
		/// <param name="count">Length of returned string including endings.</param>
		/// <param name="endings">optional edings of reduced text</param>
		/// <example>
		/// string description = "This is very long description of something";
		/// string preview = description.Reduce(20,"...");
		/// produce -> "This is very long..."
		/// </example>
		/// <returns></returns>
		public static string Reduce( this string s, int count, string endings ) {
			if( count < endings.Length )
				throw new ArgumentException( "Failed to reduce to less then endings length." );
			int sLength = s.Length;
			int len = sLength;
			if( endings != null )
				len += endings.Length;
			if( count > sLength )
				return s; //it's too short to reduce
			s = s.Substring( 0, sLength - len + count );
			if( endings != null )
				s += endings;
			return s;
		}

		/// <summary>
		/// true, if the string can be parse as Double respective Int32
		/// Spaces are not considred.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatpoint">true, if Double is considered,
		/// otherwhise Int32 is considered.</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
		public static bool IsNumber( this string s, bool floatpoint ) {
			int i;
			double d;
			string withoutWhiteSpace = s.RemoveSpaces();
			if( floatpoint )
				return double.TryParse( withoutWhiteSpace, NumberStyles.Any,
					Thread.CurrentThread.CurrentUICulture, out d );
			else
				return int.TryParse( withoutWhiteSpace, out i );
		}

		/// <summary>
		/// true, if the string contains only digits or float-point.
		/// Spaces are not considred.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatpoint">true, if float-point is considered</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
		public static bool IsNumberOnly( this string s, bool floatpoint ) {
			s = s.Trim();
			if( s.Length == 0 )
				return false;
			for( int i=0, len=s.Length; i<len;i++ ) {
				char c = s[i];
				if( !char.IsDigit( c ) ) {
					if( floatpoint && ( c == '.' || c == ',' ) )
						continue;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Remove accent from strings 
		/// </summary>
		/// <example>
		///  input:  "Příliš žluťoučký kůň úpěl ďábelské ódy."
		///  result: "Prilis zlutoucky kun upel dabelske ody."
		/// </example>
		/// <remarks>founded at http://stackoverflow.com/questions/249087/
		/// how-do-i-remove-diacritics-accents-from-a-string-in-net</remarks>
		/// <returns>string without accents</returns>
		public static string RemoveDiacritics( this string s ) {
			string stFormD = s.Normalize( NormalizationForm.FormD );
			StringBuilder sb = new StringBuilder();
			for( int ich = 0; ich < stFormD.Length; ich++ ) {
				UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory( stFormD[ich] );
				if( uc != UnicodeCategory.NonSpacingMark ) {
					sb.Append( stFormD[ich] );
				}
			}
			return ( sb.ToString().Normalize( NormalizationForm.FormC ) );
		}

		/// <summary>
		/// Replace \r\n or \n by <br />
		/// from http://weblogs.asp.net/gunnarpeipman/archive/2007/11/18/c-extension-methods.aspx
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string NewlineToHtmlBR( this string s ) {
			return s.Replace( "\r\n", "<br />" ).ReplaceAll( '\n', "<br />" );
		}

		/// <summary>
		/// from http://weblogs.asp.net/gunnarpeipman/archive/2007/11/18/c-extension-methods.aspx
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string MD5( this string s ) {
			if( s_md5 == null ) //creating only when needed
				s_md5 = new MD5CryptoServiceProvider();
			Byte[] newdata = Encoding.Default.GetBytes( s );
			Byte[] encrypted = s_md5.ComputeHash( newdata );
			return BitConverter.ToString( encrypted ).RemoveAll( '-' ).ToLower();
		}
		private static MD5CryptoServiceProvider s_md5 = null;

		/// <summary>
		/// Check for Positive Integers with zero inclusive  
		/// </summary>
		/// <param name="strNumber"></param>
		/// <returns></returns>
		public static bool IsWholeNumber( this string strNumber ) {//!! optimize this
			if( string.IsNullOrEmpty( strNumber ) )
				return false;
			Regex objNotWholePattern = new Regex( "[^0-9]" );
			return !objNotWholePattern.IsMatch( strNumber );
		}

		/// <summary>
		///Function to Check for AlphaNumeric. 
		/// </summary>
		/// <param name="strToCheck"> String to check for alphanumeric</param>
		/// <returns>True if it is Alphanumeric</returns>
		public static bool IsAlphaNumeric( this string strToCheck ) {//!! optimize this
			if( string.IsNullOrEmpty( strToCheck ) )
				return false;
			Regex objAlphaNumericPattern = new Regex( "[^a-zA-Z0-9]" );
			return !objAlphaNumericPattern.IsMatch( strToCheck );
		}

		/// <summary>
		///Function to Check for valid alphanumeric input with space chars also
		/// </summary>
		/// <param name="strToCheck"> String to check for alphanumeric</param>
		/// <returns>True if it is Alphanumeric</returns>
		public static bool IsValidAlphaNumericWithSpace( this string strToCheck ) {//!! optimize this
			if( string.IsNullOrEmpty( strToCheck ) )
				return false;
			Regex objAlphaNumericPattern = new Regex( "[^a-zA-Z0-9\\s]" );
			return !objAlphaNumericPattern.IsMatch( strToCheck );
		}

		/// <summary>
		/// Check for valid alphabet input with space chars also
		/// </summary>
		/// <param name="strToCheck"> String to check for alphanumeric</param>
		/// <returns>True if it is Alphanumeric</returns>
		public static bool IsValidAlphabetWithSpace( this string strToCheck ) {//!! optimize this
			if( string.IsNullOrEmpty( strToCheck ) )
				return false;
			Regex objAlphaNumericPattern = new Regex( "[^a-zA-Z\\s]" );
			return !objAlphaNumericPattern.IsMatch( strToCheck );
		}

		/// <summary>
		/// Check for valid alphabet input with space chars also
		/// </summary>
		/// <param name="strToCheck"> String to check for alphanumeric</param>
		/// <returns>True if it is Alphanumeric</returns>
		public static bool IsValidAlphabetWithHyphen( this string strToCheck ) {//!! optimize this
			if( string.IsNullOrEmpty( strToCheck ) )
				return false;
			Regex objAlphaNumericPattern = new Regex( "[^a-zA-Z\\-]" );
			return !objAlphaNumericPattern.IsMatch( strToCheck );
		}

		/// <summary>
		///  Check for Alphabets.
		/// </summary>
		/// <param name="strToCheck">Input string to check for validity</param>
		/// <returns>True if valid alphabetic string, False otherwise</returns>
		public static bool IsAlpha( this string strToCheck ) {//!! optimize this
			if( string.IsNullOrEmpty(strToCheck)  )
				return false;
			Regex objAlphaPattern = new Regex( "[^a-zA-Z]" );
			return !objAlphaPattern.IsMatch( strToCheck );
		}

		/// <summary>
		/// Check whether the string is valid number or not
		/// </summary>
		public static bool IsNumber( this string strNumber ) {
			// Room for improvement
			double dbl;
			return double.TryParse( strNumber, out dbl );
		}
		/// <summary>
		/// Check whether the string is valid integer or not
		/// </summary>
		public static bool IsInteger( this string strNumber ) {
			// Room for improvement
			Int64 i;
			return Int64.TryParse( strNumber, out i );
		}
		/// <summary>
		/// Check whether the string is valid unsigned integer or not
		/// </summary>
		public static bool IsUnsignedInteger( this string strNumber ) {
			// Room for improvement
			UInt64 i;
			return UInt64.TryParse( strNumber, out i );
		}

		/// <summary>
		/// Function to validate given string for HTML Injection
		/// </summary>
		/// <param name="strBuff">String to be validated</param>
		/// <returns>Boolean value indicating if given input string passes HTML Injection validation</returns>
		public static bool IsValidHTMLInjection( this string strBuff ) {
			return ( !Regex.IsMatch( HttpUtility.HtmlDecode( strBuff ), "<(.|\n)+?>" ) );
		}

		/// <summary>
		/// Checks whether a valid Email address was input
		/// </summary>
		/// <param name="inputEmail">Email address to validate</param>
		/// <returns>True if valid, False otherwise</returns>
		public static bool isEmail( this string inputEmail ) {
			if( string.IsNullOrEmpty(inputEmail))
				return false;
			Regex re = new Regex( @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$" );
			return ( re.IsMatch( inputEmail ) );
		}

		public static bool IsGuid( this string value ) {
			if( string.IsNullOrEmpty( value ) )
				return false;
			return Regex.IsMatch( value, @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$" );
		}

		public static bool IsUnicode( this string str ) {
			if( string.IsNullOrEmpty( str ) )
				throw new ArgumentException( "str must have length" );
			return System.Runtime.InteropServices.Marshal.SizeOf( str[0] ) == 2;
		}

		#endregion
	}

	public static class CharExt {
		#region MD
		#endregion

		#region Other Sources
		public static bool IsVowelOrY( this char c ) {
			return "AEIOUY".Contains( Char.ToUpper( c ) );
		}
		#endregion
	}
	public static class DateTimeExt {
		#region MD
		public static bool IsEmpty( this DateTime dt ) { return dt.Equals( DateTime.MinValue ); }
		public static void SetEmpty( this DateTime dt ) { dt = DateTime.MinValue; }
		#endregion

		#region Other Sources
		/// <summary>
		/// returns the number of milliseconds since Jan 1, 1970 (useful for converting C# dates to JS/Unix dates)
		/// </summary>
		public static double UnixTicks( this DateTime dt ) {
			DateTime d1 = new DateTime( 1970, 1, 1 );
			DateTime d2 = dt.ToUniversalTime();
			TimeSpan ts = new TimeSpan( d2.Ticks - d1.Ticks );
			return ts.TotalMilliseconds;
		}
		/// <summary>
		/// Convert a long into a DateTime
		/// </summary>
		public static DateTime FromUnixTime( this long timestamp ) {
			return new DateTime( 1970, 1, 1 ).AddSeconds( timestamp );
		}
		/// <summary>
		///   Convert a DateTime into a long
		/// </summary>
		public static long ToUnixTime( this DateTime self ) {
			if( self == DateTime.MinValue )
				return 0;
			long ts = (long)( self - new DateTime( 1970, 1, 1 ) ).TotalSeconds;
			if( ts < 0 )
				throw new ArgumentOutOfRangeException( "Unix epoc begins January 1, 1970" );
			return ts;
		}
		#endregion
	}
	public static class TimeSpanExt {
		#region MD
		public static bool IsEmpty( this TimeSpan ts ) { return ts.Equals( TimeSpan.MinValue ); }
		public static void SetEmpty( this TimeSpan ts ) { ts = TimeSpan.MinValue; }
		#endregion
	}
	public static class OtherExt {
		#region MD
		#endregion

		#region Other Sources
		/// <summary>
		/// Check if Typed object is set to its default
		/// </summary>
		public static bool IsDefault<T>( this T val ) { return EqualityComparer<T>.Default.Equals( val, default( T ) ); }

		#endregion
	}

}
