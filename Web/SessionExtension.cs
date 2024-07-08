using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;

namespace MDTools.Web {

	public static class SessionExtended {
		/// <summary>
		/// Get a session variable that included an expiration
		/// </summary>
		public static object ExpiringItem( this HttpSessionState Session, string Key, bool RemoveIfExpired = true ) {
			// Check if expiring object exists
			var ExpKey = Key + "_Expires";
			var exp = Session[ExpKey];
			if( exp == null )
				// Item does not exist
				return null;
			// Check if item already expired
			var obj = ExpiringItem( Session, Key, (DateTime)exp, RemoveIfExpired );
			if(obj==null)
				Session.Remove( ExpKey );
			return obj;
		}
		/// <summary>
		/// Get a typed session variable that included an expiration
		/// </summary>
		public static TYPE ExpiringItem<TYPE>( this HttpSessionState Session, string Key, bool RemoveIfExpired = true ) {
			var obj = ExpiringItem( Session, Key, RemoveIfExpired );
			return obj == null ? default( TYPE ) : (TYPE)obj;
		}
		/// <summary>
		/// Get a session variable and test with expiration time
		/// </summary>
		public static object ExpiringItem( this HttpSessionState Session, string Key, DateTime ExpiredTime, bool RemoveIfExpired = true ) {
			// Check if item already expired
			if( DateTime.Now > ExpiredTime ) {
				// Item expired and remove turned on, remove from session
				if( RemoveIfExpired )
					Session.Remove( Key );
				return null;
			}
			return Session[Key];
		}
		/// <summary>
		/// Set a session variable that includes an expiration
		/// </summary>
		public static void ExpiringItem( this HttpSessionState Session, string Key, object item, int ExpireSecs ) {
			Session[Key + "_Expires"] = DateTime.Now.AddSeconds( ExpireSecs );
			Session[Key] = item;
		}
		/// <summary>
		/// Set a typed session variable that includes an expiration, and return the original item
		/// </summary>
		public static TYPE ExpiringItem<TYPE>( this HttpSessionState Session, string Key, TYPE item, int ExpireSecs ) {
			Session[Key + "_Expires"] = DateTime.Now.AddSeconds( ExpireSecs );
			Session[Key] = item;
			return item;
		}

		/// <summary>
		/// Get a session variable that includes a unique key
		/// </summary>
		public static object KeyedItem( this HttpSessionState Session, string Key, object UniqueKey ) {
			return Session[Key + UniqueKey.ToString()];
		}
		/// <summary>
		/// Get a typed session variable that includes a unique key
		/// </summary>
		public static TYPE KeyedItem<TYPE>( this HttpSessionState Session, string Key, object UniqueKey ) {
			var obj = KeyedItem( Session, Key, UniqueKey );
			return obj == null ? default( TYPE ) : (TYPE)obj;
		}
		/// <summary>
		/// Set a session variable that includes a unique key
		/// </summary>
		public static void KeyedItem( this HttpSessionState Session, string Key, object UniqueKey, object item ) {
			Session[Key + UniqueKey.ToString()] = item;
		}
		/// <summary>
		/// Set a typed session variable that includes a unique key, and return the original item
		/// </summary>
		public static TYPE KeyedItem<TYPE>( this HttpSessionState Session, string Key, object UniqueKey, TYPE item ) {
			Session[Key + UniqueKey.ToString()] = item;
			return item;
		}

	}

}
