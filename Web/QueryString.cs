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
using System.Linq;

#if obsolete

namespace MDTools.Web {

	/// <summary>
	/// NQueryString breaks parameterized url into name value parts (unique keys only)
	/// </summary>
	public class QueryString : Dictionary<string, string> {

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

		public QueryString() : base() { }
		public QueryString( string parstr ) { Add( parstr ); }
		public QueryString( NameValueCollection qs ) { this.Add( qs ); }
		public QueryString( MDTools.Web.QueryString qs ) : base( qs ) { }
		public QueryString( MDTools.Web.QuerySequence qs ) { this.Add( qs.ToString() ); }

		public void Add( string parstr ) {
			//!! If performance becomes a problem, consider revising to a single-pass implementation
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
		public void Add( NameValueCollection qs ) {
			var Keys = qs.AllKeys;
			for( int i = 0, len = qs.Count; i < len; i++ )
				base.Add(Keys[i], qs[i]);
		}
		private void Add( IDictionary<string, string> dict ) {
			foreach( var kvp in dict ) 
				base.Add( kvp.Key, kvp.Value );
		}
		public void AddReplace( string parstr ) {
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

		public string ToUrl() {
			// Send Back to Current page with parameters
			return ToUrl( HttpContext.Current.Request.Path );
		}
		public string ToUrl( string path ) {
			return ( base.Count > 0 ) ? path + "?" + this.ToString() : path;
		}

		public override string ToString() {
			string str = string.Empty;
			bool bAmp = false;
			var enumerator = base.GetEnumerator();
			while( enumerator.MoveNext() ) {
				if( bAmp )
					str += '&';
				else
					bAmp = true;
				str += enumerator.Current.Key + "=" + enumerator.Current.Value;
			}
			return str;
		}

		public void AddReplace( string key, string value ) {
			//            if(base.Contains(key))
			this[key] = value;
		}

		public void AddVal( string key, string value ) {
			// If already exists, add + separated between values
			if( !base.ContainsKey( key ) )
				base.Add( key, value );
			else
				base[key] = base[key] + "+" + value;
		}

		public void RemoveVal( string key, string value ) {
			// Clears key if value matches last value
			// Clears one value off the line if key has multiple
			if( !base.ContainsKey( key ) )
				return;
			string val = this[key] as string;
			// if total match, remove key
			if( val.Equals( value ) ) {
				base.Remove( key );
				return;
			}
			val = val.Replace( "+" + value, string.Empty );
			val = val.Replace( value + "+", string.Empty );
			this[key] = val;
		}
		public new string this[string key] {
			get { return ( base.ContainsKey( key ) ) ? base[key] : null; }
			set { base[key] = value; }
		}
		/*
		public string this[int index] {
			get {
				int len = base.Count;
				if( index >= len )
					throw new NullReferenceException();
				var enumerator = base.GetEnumerator();
				for( int i = 0; i<index; i++ )
					enumerator.MoveNext();
				return enumerator.Current.Value;
			}
		}*/

	}

}
#endif
