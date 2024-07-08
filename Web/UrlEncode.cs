using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MDTools.Extension;

namespace MDTools.Web {
	/// <summary>
	/// VALID URL CHARACTERS
	/// In general URIs as defined by RFC 3986 may contain any of the following characters: 
	///	A-Z, a-z, 0-9, -, ., _, ~, :, /, ?, #, [, ], @, !, $, &, ', (, ), *, +, ,, ; and =. 
	///	Any other character needs to be encoded with the percent-encoding (%hh). 
	///	Each part of the URI has further restrictions about what characters need to be represented by an percent-encoded word.
	/// </summary>
	public static class UrlEncode {

#if notused
		protected static readonly Dictionary<char, char> enc = new Dictionary<char,char>();
		protected static readonly Dictionary<char, char> dec = new Dictionary<char,char>();

		protected static UrlEncode() {
			// period okay, apostrophe okay
			// demote: space to dash, dash to underscore 
			enc.Add( ' ', '-' );
//			enc.Add( '-', '_' );
			dec.Add( '-', ' ' );
			dec.Add( '_', '-' );
		}

		public static void Encode(this string str) {
			for(int i=0,len=str.Length; i<len; i++) {
				char c = str[i];
				char to;
				if( enc.TryGetValue( c, out to ) ) {
					// Check if remove
					if( to == '\0' ) {
						str.Remove( i, 1 );
						i--;
						len--;
					} else
						str[i] = to;
				}
			}
		}

#endif

		public static string SEOEncode( this string str ) {
			return str.Replace( ' ', '-' );
		}
		public static string SEODecode( this string str ) {
			return str.ReplaceAll( decodeFrom, decodeTo );
		}
		private static char[] decodeFrom = new char[] { '-', '_' };
		private static char[] decodeTo = new char[] { ' ', ' ' };

	}
}
