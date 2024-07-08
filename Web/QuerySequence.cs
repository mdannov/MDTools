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

#if obsolete
namespace MDTools.Web {

	/// <summary>
	/// NQuerySequence breaks url into sequential value parts per each appearance of specified key
	/// </summary>
	public class QuerySequence : List<KeyValuePair<string, string>> {

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

		public QuerySequence() : base() { }
		public QuerySequence( string parstr ) { Add( parstr ); }
		public QuerySequence( NameValueCollection qs ) { this.Add( qs ); }
		public QuerySequence( MDTools.Web.QueryString qs ) : base( qs ) { }
		public QuerySequence( MDTools.Web.QuerySequence qs ) : base( qs ) { }

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
						base.Add( new KeyValuePair<string, string>( parms[0], parms[1] ) );
						break;
					case 1:
						// In the special case the url is just the file portion
						if( pos <= 0 && len == 1 )
							return;
						base.Add( new KeyValuePair<string, string>( parms[0], string.Empty ) );
						break;
					default:
						break;
				}
			}
		}

		public void Add( NameValueCollection qs ) {
			var Keys = qs.AllKeys;
			for( int i = 0, len = qs.Count; i < len; i++ )
				base.Add( new KeyValuePair<string, string>( Keys[i], qs[i] ) );
		}
		private void Add( IDictionary<string, string> dict ) {
			foreach( var kvp in dict )
				base.Add( new KeyValuePair<string, string>( kvp.Key, kvp.Value ) );
		}

		public string ToUrl() {
			// Send Back to Current page with parameters
			return ToUrl( HttpContext.Current.Request.Path );
		}
		public string ToUrl( string path ) {
			return ( Count > 0 ) ? path + "?" + this.ToString() : path;
		}

		public override string ToString() {
			string str = string.Empty;
			bool bAmp = false;
			for( int i = 0, len = base.Count; i < len; i++ ) {
				var kv = base[i];
				if( bAmp )
					str += '&';
				else
					bAmp = true;
				str += kv.Key + "=" + kv.Value;
			}
			return str;
		}

		public int IndexOf( string key ) {
			for( int i = 0, len = this.Count; i < len; i++ ) {
				var kv = base[i];
				if( kv.Key.Equals( key ) )
					return i;
			}
			return -1;// not found
		}
		public int IndexOf( string key, int startAt ) {
			for( int i = startAt, len = this.Count; i < len; i++ ) {
				var kv = base[i];
				if( kv.Key.Equals( key ) )
					return i;
			}
			return -1;// not found
		}
		public string IndexOf( int index ) {
			return base[index].Value;
		}

		public new string this[int index] {
			get { return base[index].Value; }
			set { base[index] = new KeyValuePair<string,string>(base[index].Key, value); }
		}

		public void Add( string key, string val ) {
			base.Add( new KeyValuePair<string, string>( key, val ) );
		}

		public bool Contains( string key ) {
			return IndexOf( key ) != -1;
		}

	}

}
#endif
