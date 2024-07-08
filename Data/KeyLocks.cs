using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDTools.Data {

	public static class KeyLockObject {

		// Based on code at http://www.rosscode.com/blog/index.php?title=performance_tweaks_for_your_cache&more=1&c=1&tb=1&pb=1
		private static object _LockObj = new object();
		private static Dictionary<string, object> _LockObjList = new Dictionary<string, object>();
	
		public static object Get( string key ) {
			// Check if available unlocked
			object obj = null;
			if(_LockObjList.TryGetValue(key, out obj))
				return obj;
			lock( _LockObj ) {
				// Check if available now locked
				if( _LockObjList.TryGetValue( key, out obj ) )
					return obj;
				// Create while locked
				obj = new object();
				_LockObjList.Add( key, obj );
			}
			return obj;
		}

		public static void Remove( string key ) {
			lock( _LockObj ) {
				_LockObjList.Remove( key );
			}
		}

	}

}
