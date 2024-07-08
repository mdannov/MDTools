using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MDTools.Extension;

namespace MDTools.Web {

	/// <summary>
	/// NQuerySequence breaks url into sequential value parts per each appearance of specified key
	/// </summary>
	public class QuerySeq : NameValueCollection {

		/* Please note that this string tool does not use text encoding. 
		 * At some point in the future when this becomes more of a pressing issue, this
		 * class should probably be updated to use UTF-8 or any encoding standard since 
		 * it is used to construct URL and parameters */


		/* Using this object: 
		 *   Use Add only if you know the key could not already exist 
		 *   Use AddReplace to replace or Add if you can't guarantee the key already exists
		 *   Use AddVal if you want to append values; if value already exists + will separate
		 *   Use RemoveVal if you want to remove values that may have been appended
		 *   Use Remove if the key exists or is not known to exist
		 *   */

		#region initialization/construction

		public QuerySeq() : base() { }
		public QuerySeq( string parstr ) { Add( parstr ); }
		public QuerySeq( NameValueCollection qs ) { base.Add( qs ); }
		public QuerySeq( IDictionary<string, string> qs ) { this.Add( qs ); }
		public QuerySeq( StringDictionary qs ) { this.Add( qs ); }
//		public QuerySeq( Hashtable qs ) { this.Add( qs ); }
//		public QuerySeq( IDictionary qs ) { this.Add( qs ); }

		#endregion

		#region Add / Update Values

		public void Add( string parstr ) {
			//!! Performance and testing of HttpUtility.ParseQueryString
			if( String.IsNullOrEmpty( parstr ) )
				return;
			// Clear nonquerystring portion if present
			int pos = parstr.IndexOf( '?' );
			if( pos > 0 )
				parstr = parstr.Substring( pos + 1 );
			// Process parameters only
			string[] strs = parstr.Split( '&' );
			for( int i = 0, len = strs.Length; i < len; i++ ) {
				string s = strs[i];
				string[] parms = s.Split( '=' );
				switch( parms.Length ) {
					case 2:
						base.Add( parms[0], parms[1] );
						break;
					case 1:
						// In the special case the url is just the file portion
						if( pos <= 0 && len == 1 )
							return;
						base.Add( parms[0], string.Empty );
						break;
					default:
						break;
				}
			}
		}

#if methodalreadyexistsinbase
		public void Add( NameValueCollection qs ) {
			base.Add( qs );
			/*
			var Keys = qs.AllKeys;
			for( int i = 0, len = qs.Count; i < len; i++ )
				base.Add( Keys[i], qs[i] );*/
		}
#endif
		private void Add( IDictionary<string, string> dict ) {
			foreach( var kvp in dict )
				base.Add( kvp.Key, kvp.Value );
		}
		private void Add( StringDictionary dict ) {
			var Keys = dict.Keys;
			foreach( string key in Keys )
				base.Add( key, dict[key] );
		}

		public void Replace( string parstr ) {
			//!! If performance becomes a problem, consider revising to a single-pass implementation
			if( String.IsNullOrEmpty( parstr ) )
				return;
			// Clear nonquerystring portion if present
			int pos = parstr.IndexOf( '?' );
			if( pos > 0 )
				parstr = parstr.Substring( pos );
			// Process parameters only
			string[] strs = parstr.Split( '&' );
			for( int i = 0, len = strs.Length; i < len; i++ ) {
				string s = strs[i];
				string[] parms = s.Split( '=' );
				switch( parms.Length ) {
				case 2:
					base[parms[0]] = parms[1];
					break;
				case 1:
					base[parms[0]] = string.Empty;
					break;
				default:
					break;
				}
			}
		}
		public void Replace( NameValueCollection qs ) {
			var Keys = qs.AllKeys;	
			for( int i = 0, len = qs.Count; i < len; i++ )
				base[Keys[i]] = qs[i];//!! or would qs.GetKey(i) be faster?
		}
		private void Replace( IDictionary<string, string> dict ) {
			foreach( var kvp in dict )
				base[kvp.Key] = kvp.Value;
		}
		private void Replace( StringDictionary dict ) {
			var Keys = dict.Keys;
			foreach( string key in Keys )
				base[key] = dict[key];
		}
		public void Replace( string key, string value ) {
			base[key] = value;
		}

		public void AddValue( string key, string value ) {
			// If already exists, add + separated between values
			var val = base[key];
			if(string.IsNullOrEmpty(val)) 
				base.Add( key, value );
			else
				base[key] = val + "+" + value;
		}

		public void RemoveVal( string key, string value ) {
			// Clears key if value matches last value
			// Clears one value off the line if key has multiple
			var val = base[key];
			if( string.IsNullOrEmpty( val ) )
				return;
			// if total match, remove key
			if( val.Equals( value ) ) {
				base.Remove( key );
				return;
			}
			//!! poor performing solution
			val = val.Replace( '+' + value, string.Empty );//!! need RemoveAll
			val = val.Replace( value + '+', string.Empty );//!! need RemoveAll
			this[key] = val;
		}

		public new string[] GetValues( int index ) {
			return base[index].Split( '+' );
		}
		public new string[] GetValues( string key ) {
			return base[key].Split( '+' );
		}

		#endregion

		#region Formatted as Strings

		public string ToUrl() {
			// Send Back to Current page with parameters
			return ToUrl( HttpContext.Current.Request.Path );
		}
		public string ToUrl( string path ) {
			return ( Count > 0 ) ? path + "?" + this.ToString() : path;
		}

		public override string ToString() {
			if( base.Count <= 0 )
				return string.Empty;
			string str = string.Empty;
			bool bAmp = false;
			var keys = base.AllKeys;
			for( int i = 0, len = base.Count; i < len; i++ ) {
				if( bAmp )
					str += '&';
				else
					bAmp = true;
				str += Keys[i] + "=" + base[i];
			}
			return str;
		}

		#endregion

		#region extra support

		public int IndexOf( string key ) {
			return base.AllKeys.IndexOf(key);
/*			for( int i = 0, len = this.Count; i < len; i++ ) {
				var kv = base[i];
				if( kv.Key.Equals( key ) )
					return i;
			}
			return -1;// not found
 */
		}

		public bool Contains( string key ) {
			return AllKeys.Contains(key );
		}

		#endregion

	}

}