using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Caching;
using System.Linq;
using System.Web;
using System.Data;

/*	Code Copyright Michael Dannov 2009-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */

/// Version 2.7  - TBD	  - (still need to implement cache and expiration variations)
/// Version 2.6  - TBD	  - Added IDisposable to IDL interfaces to ensure proper closure of objects
/// Version 2.55 - 10/13/13- Added IParameterKey interface for Unique Parameter IDs
/// Version 2.5  - 6/18/13 - Revised Key strategy for final cache strategy; added keyed locks to scope
/// Version 2.4  - 7/10/10 - DLDataCommandCreate include KEY_TYPE, revert DLDataCommandBase to non-static
/// Version 2.3  - 6/1/10  - Auto-keys, Merged & Major Rewrite of Static DataLayer Command Classes 
///	Version 2.2  - 4/13/10 - Overhaul of Static DataLayer Command Classes
///	Version 2.1  - 1/18/10 - Static DataLayer Command Class Updates & Added Interfaces, Added Policy Interface
/// Version 2.0  - 2009    - All New DataLayer Architecture with Generics & Templates

namespace MDTools.Data {

	public struct DLEMPTYIPARAMS {}

	public interface IDLLoader : IDisposable {
		bool Load();	// returns true if it loads something
	}
	public interface IDLUpdater : IDisposable {
		bool Update();	// return true if success
	}
	public interface IDLCreater : IDisposable {
		CREATEDKEY Create<CREATEDKEY>();	// return key of inserted record
	}
	public interface IDLDeleter : IDisposable {
		bool Delete();	// return true if success
	}
	public interface IParameterKey {
		string UniqueID { get; }
	}

	public interface IDLScope {
		object this[string name] { get; set; }
		void Delete( string key );
		bool Locked { get; }
	}

	public interface IDLExpirePolicy {
		bool IsExpired();
		void NotifyCreate();
		void NotifyUpdate();
		void NotifyDelete();
	}

	public delegate void dDLLoader();
	public delegate bool dDLUpdate();
	public delegate bool dDLDelete();
	public delegate CREATEDKEY dDLCreate<CREATEDKEY>();

	#if legacy
	public interface IDLDataCommand<DATA_OBJECT, PARAMETER_OBJECT> {
		IDLExpirePolicy Policy { get; }
		IDLLoader Loader { get; }
		IDLCreater Creater { get; }
		IDLUpdater Updater { get; }
		IDLDeleter Deleter { get; }
		PARAMETER_OBJECT Parameters { set; }
		DATA_OBJECT Data { get; set;  }
	}
	#endif

	public interface IDLDataCommandLoad<DATA_OBJECT, PARAMETER_OBJECT> {
		IDLLoader Loader { get; }
	}

	public interface IDLDataCommandCreate<DATA_OBJECT, PARAMETER_OBJECT> {
		IDLCreater Creater { get; }
	}
	public interface IDLDataCommandUpdate<DATA_OBJECT, PARAMETER_OBJECT> {
		IDLUpdater Updater { get; }
	}
	public interface IDLDataCommandDelete<PARAMETER_OBJECT> {
		IDLDeleter Deleter { get; }
	}

	public class DLCommandBase<PARAMETER_OBJECT> {

		#region Key Creation Support
		public virtual string KeyName { get { return null; } }
		private string finalkey=null;
		public string GetKey( IDLScope scope, object uniqueKey ) {
			// If already rendered, return the finalkey
			if( !string.IsNullOrEmpty( finalkey ) )
				return finalkey;
			// Base keyname is object base name or overridden KeyName
			string key = KeyName;
			if( key == null )
				key = this.ToString();
			// Check if we must combine a unique component to the key
			if( scope != null && scope!=null && !uniqueKey.Equals( default( PARAMETER_OBJECT ) ) ) {
				if( uniqueKey is IParameterKey ) {
					// The parameter object implements IParameterKey so use its uniqueKey
					return key + ( (IParameterKey)uniqueKey ).UniqueID;
				} else {
					// If not using IParameterKey, make sure that the unique key isn't just the name of the type
					var uniqkey = uniqueKey.ToString();
					if( uniqkey != uniqueKey.GetType().ToString() )
						// Return the unique name
						return finalkey = key + uniqkey;
				}
			}
			return finalkey = key;
		}
		#endregion

		#region DLDataCommandBase Interface
		protected PARAMETER_OBJECT param = default( PARAMETER_OBJECT );
		public PARAMETER_OBJECT Parameters { set { param = value; } }

		public virtual IDLScope Scope( HttpContext context ) { return new DLAutoScope( context ); }	// default
		public virtual IDLExpirePolicy Policy { get { return null; } }	// not implemented
		#endregion
	}


	public abstract class DLDataCommandLoad<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT> : 
		DLCommandBase<PARAMETER_OBJECT>, IDLDataCommandLoad<DATA_OBJECT, PARAMETER_OBJECT>
		where DATA_OBJECT: new()
		where DL_WRAPPER : DLDataCommandLoad<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT>, new() {

		#region DLDataCommand... properties
		public abstract IDLLoader Loader { get; }
		protected DATA_OBJECT data;
		public bool IsLoaded = false;
		#endregion

		#region Loader Methods - Data Required, Optional Parameters
		public static DATA_OBJECT GetData( HttpContext context, PARAMETER_OBJECT Params, bool Initialize = true ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			var scope = dldata.Scope( context );
			var data = _GetOnly( scope, Params, ref dldata );
			if( dldata.IsLoaded )
				return data;
			if( !scope.Locked )
				return _Load( scope, Params, Initialize, ref dldata );
			// Try locked method ** KeyName unique portion is based on PARAMETER_TYPE on load
			lock( KeyLockObject.Get( dldata.GetKey( scope, Params ) ) ) {
				// Try to load again post lock
				data = _GetOnly( scope, Params, ref dldata );
				if( dldata.IsLoaded )
					return data;
				// If first to load, load it
				data = _Load( scope, Params, Initialize, ref dldata );
			}
			return data;
		}
		public static DATA_OBJECT GetData( IDLScope scope, PARAMETER_OBJECT Params, bool Initialize = true ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			var data = _GetOnly( scope, Params, ref dldata );
			if( dldata.IsLoaded )
				return data;
			if( !scope.Locked )
				return _Load( scope, Params, Initialize, ref dldata );
			// Try locked method ** KeyName unique portion is based on PARAMETER_TYPE on load
			lock( KeyLockObject.Get( dldata.GetKey( scope, Params ) ) ) {
				// Try to load again post lock
				data = _GetOnly( scope, Params, ref dldata );
				if( dldata.IsLoaded )
					return data;
				// If first to load, load it
				data = _Load( scope, Params, Initialize, ref dldata );
			}
			return data;
		}

		public static DATA_OBJECT GetOnly( HttpContext context, PARAMETER_OBJECT Params ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			return _GetOnly( dldata.Scope(context), Params, ref dldata );
		}
		protected static DATA_OBJECT _GetOnly( IDLScope scope, PARAMETER_OBJECT Params, ref DL_WRAPPER dldata ) {
			dldata.Parameters = Params;// make sure new dlcommandbase has access to params
			// If no scope, nothing to get
			DATA_OBJECT data = default( DATA_OBJECT );
			if( scope == null )
				return data;
			// Get the key and retrieve it from scope ** KeyName unique portion is based on PARAMETER_TYPE on load
			string key = dldata.GetKey( scope, Params );
			var scp = scope[key];
			if( scp != null ) {
				// Now we need to check to see if the data is expired
				data = (DATA_OBJECT)scp;
				IDLExpirePolicy policy = dldata.Policy;
				if( policy != null && policy.IsExpired() ) {
					// Clear the data and attempt to load if possible
					if( scope != null )
						scope.Delete( key );
					data = default( DATA_OBJECT );
				} else
					dldata.IsLoaded = true;
			}
			return data;
		}
		public static DATA_OBJECT Load( HttpContext context, PARAMETER_OBJECT Params, bool Initialize = true ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			return _Load( dldata.Scope(context), Params, Initialize, ref dldata );
		}
		public static DATA_OBJECT Load( IDLScope scope, PARAMETER_OBJECT Params, bool Initialize = true ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			return _Load( scope, Params, Initialize, ref dldata );
		}
		protected static DATA_OBJECT _Load( IDLScope scope, PARAMETER_OBJECT Params, bool Initialize, ref DL_WRAPPER dldata ) {
			// Set up the loader
			dldata.Parameters = Params;// make sure new dlcommandbase has access to params
			using( var loader = dldata.Loader ) {
				if( loader == null )
					throw new ArgumentException( "Loader must be assigned to DLCommand Object" );
				dldata.data = new DATA_OBJECT();
				if( !loader.Load() && !Initialize )
					return default( DATA_OBJECT );
			}
			if( scope != null ) {
				//** KeyName unique portion is based on PARAMETER_TYPE on load
				string key = dldata.GetKey( scope, Params );
				scope[key] = dldata.data;
			}
			return dldata.data;
		}
		#endregion
	}

	public abstract class DLDataCommandCreate<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT, KEY_TYPE> :
		DLCommandBase<PARAMETER_OBJECT>, IDLDataCommandCreate<DATA_OBJECT, PARAMETER_OBJECT>
		where DL_WRAPPER : DLDataCommandCreate<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT, KEY_TYPE>, new() {

		#region DLDataCommand... properties
		public abstract IDLCreater Creater { get; }
		protected DATA_OBJECT data = default( DATA_OBJECT );
		public DATA_OBJECT Data { get { return data; } set { data = value; } }
		#endregion

		#region Create Methods - Data Required, Parameter Optional
		public static KEY_TYPE CreateData( DATA_OBJECT data ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _CreateData( default( IDLScope ), data, default( PARAMETER_OBJECT ), ref dldata );
		}
		public static KEY_TYPE CreateData( DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _CreateData( default(IDLScope), data, Params, ref dldata );
		}
		public static KEY_TYPE CreateData( HttpContext context, DATA_OBJECT data ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			return _CreateData( dldata.Scope( context ), data, default( PARAMETER_OBJECT ), ref dldata );
		}
		public static KEY_TYPE CreateData( HttpContext context, DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			var dldata = new DL_WRAPPER();	// necessary for scope and key
			return _CreateData( dldata.Scope( context ), data, Params, ref dldata );
		}
		public static KEY_TYPE CreateData( IDLScope scope, DATA_OBJECT data ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _CreateData( scope, data, default( PARAMETER_OBJECT ), ref dldata );
		}
		public static KEY_TYPE CreateData( IDLScope scope, DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _CreateData( scope, data, Params, ref dldata );
		}
		protected static KEY_TYPE _CreateData( IDLScope scope, DATA_OBJECT data, PARAMETER_OBJECT Params, ref DL_WRAPPER dldata ) {
			dldata.Data = data;
			dldata.Parameters = Params;// make sure new dlcommandbase has access to params
			KEY_TYPE retkey;
			using(var creater = dldata.Creater) {
				retkey = creater.Create<KEY_TYPE>();
			}
			//!! Deal with Scope storage if scope != null - zero overwrite concern 
			if( scope != null ) {
				//** KeyName unique portion is based on KEY_TYPE on create
				var key = dldata.GetKey( scope, retkey );
				scope[key]=data; // created object may be complete
			}
			var policy = dldata.Policy;
			if( policy != null )
				policy.NotifyCreate();
			return retkey;
		}
		#endregion
	}

	public abstract class DLDataCommandUpdate<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT> :
		DLCommandBase<PARAMETER_OBJECT>, IDLDataCommandUpdate<DATA_OBJECT, PARAMETER_OBJECT>
		where DL_WRAPPER : DLDataCommandUpdate<DL_WRAPPER, DATA_OBJECT, PARAMETER_OBJECT>, new() {

		#region DLDataCommand... properties
		public abstract IDLUpdater Updater { get; }
		protected DATA_OBJECT data = default( DATA_OBJECT );
		public DATA_OBJECT Data { get { return data; } set { data = value; } }
		#endregion

		#region Update Methods - Data Required, Parameter Required

		public static bool UpdateData( HttpContext context, DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _UpdateData( dldata.Scope( context ), data, Params, ref dldata );
		}
		public static bool UpdateData( IDLScope scope, DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return _UpdateData( scope, data, Params, ref dldata );
		}
		protected static bool _UpdateData( IDLScope scope, DATA_OBJECT data, PARAMETER_OBJECT Params, ref DL_WRAPPER dldata ) {
			dldata.Data = data;
			dldata.Parameters = Params;
			bool bRes;
			using( var updater = dldata.Updater ) {
				bRes = updater.Update();
			}
			var policy = dldata.Policy;
			if( policy != null )
				policy.NotifyUpdate();
			//!! Deal with Scope storage if scope != null - zero overwrite concern 
			if( scope != null ) {
				// ** KeyName unique portion is based on PARAMETER_TYPE on update
				var key = dldata.GetKey( scope, Params );
				scope[key]=data;
			}
			return bRes;
		}

		#if othernamedfunctions
		public static bool Update( IDLScope scope, dUpdate UpdateFn, DATA_OBJECT data, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			dldata.Data = data;
			dldata.Parameters = Params;
			bool bRes = UpdateFn();
			if( dldata.Policy != null )
				dldata.Policy.NotifyUpdate();
			return bRes;
		}
		#endif
		#endregion
	}

	public abstract class DLDataCommandDelete<DL_WRAPPER, PARAMETER_OBJECT> :
		DLCommandBase<PARAMETER_OBJECT>, IDLDataCommandDelete<PARAMETER_OBJECT>
		where DL_WRAPPER : DLDataCommandDelete<DL_WRAPPER, PARAMETER_OBJECT>, new() {

		#region DLDataCommand... properties
		public abstract IDLDeleter Deleter { get; }
		#endregion

		#region Delete Methods - Parameter Required
		public static bool DeleteData( HttpContext context, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return DeleteData( dldata.Scope( context ), Params );
		}
		public static bool DeleteData( IDLScope scope, PARAMETER_OBJECT Params ) {
			DL_WRAPPER dldata = new DL_WRAPPER();
			return DeleteData( scope, Params, ref dldata );
		}
		public static bool DeleteData( IDLScope scope, PARAMETER_OBJECT Params, ref DL_WRAPPER dldata ) {
			dldata.Parameters = Params;// make sure new dlcommandbase has access to params
			bool bRes;
			using( var deleter = dldata.Deleter ) {
				bRes = deleter.Delete();
			}
			var policy = dldata.Policy;
			if( policy != null )
				policy.NotifyDelete();
			//!! Deal with Scope storage if scope != null - zero overwrite concern 
			if( scope != null ) {
				// ** KeyName unique portion is based on PARAMETER_TYPE on delete
				var key = dldata.GetKey( scope, Params );
				scope.Delete (key); // created object may be complete
			}
			return bRes;
		}
		#endregion
	}

	#if unnecessary // We don't need to define DLLocalScope because all we need to do is pass null for scope to DLCommand.. objects
	public class DLLocalScope : IDLScope { }
	#endif

	/// <summary>
	/// Page scope attaches to Context.Items - It is destroyed with garbage collection after page lifespan
	/// Key is based on first object only - ie. it is not uniquely keyed
	/// </summary>
	public class DLPageScope : IDLScope {
		protected IDictionary D = null;
		public DLPageScope( HttpContext context ) { D = context.Items; }
		public object this[string key] { get { return D[key]; } set { D[key] = value; } }
		public void Delete( string key ) { D.Remove( key ); }
		public bool Locked { get { return false; } }
	}
/*    public class DLPageScope<T> : IDLScope {
		protected IDictionary D = null;
		public DLPageScope( System.Web.HttpContext context ) { D = context.Items; }
		public T this[string key] { get { return (T)D[key]; } }
	}*/

	/// <summary>
	/// Session scope attaches to Session.Items - It is persisted per the devinition of the session lifespan
	/// Key is based on both objects - ie. it is uniquely keyed by object 2
	/// </summary>
	public class DLSessionScope : IDLScope {
		protected System.Web.SessionState.HttpSessionState D = null;
		public DLSessionScope( HttpContext context ) { D = context.Session; }
		public DLSessionScope( System.Web.SessionState.HttpSessionState session ) { D = session; }
		public object this[string key] { get { return D[key]; } set { D[key] = value; } }
		public void Delete( string key ) { D.Remove( key ); }
		public bool Locked { get { return false; } }
	}
/*    public class DLSessionScope<T> : IDLScope {
		protected System.Web.SessionState.HttpSessionState D = null;
		public DLSessionScope( System.Web.HttpContext context ) { D = context.Session; }
		public DLSessionScope( System.Web.SessionState.HttpSessionState session ) { D = session; }
		public T this[string key] { get { return (T)D[key]; } }
	}*/


	public class DLCacheScope : IDLScope {
		protected System.Web.Caching.Cache D = null;
		public DLCacheScope( HttpContext context ) { D = context.Cache; }
		public DLCacheScope( System.Web.Caching.Cache cache ) { D = cache; }
		public object this[string key] { get { return D[key]; } set { D[key] = value; }}
		public void Delete( string key ) { D.Remove( key ); }
		public bool Locked { get { return false; } }
	}

	public class DLCacheLockedScope : IDLScope {// Some Cache gains performance from locking on heavy loaded objects
		protected System.Web.Caching.Cache D = null;
		public DLCacheLockedScope( HttpContext context ) { D = context.Cache; }
		public DLCacheLockedScope( System.Web.Caching.Cache cache ) { D = cache; }
		public object this[string key] { get { return D[key]; } set { D[key] = value; }}
		public void Delete( string key ) {
			lock( KeyLockObject.Get( key ) ) {
				D.Remove( key );
				KeyLockObject.Remove( key );//!! is this safe
			}
		}
		public bool Locked { get { return true; } }
	}
/*    public class DLCacheScope<T> : IDLScope where T : new() {
		protected System.Web.Caching.Cache D = null;
		public DLCacheScope( System.Web.HttpContext context ) { D = context.Cache; }
		public DLCacheScope( System.Web.Caching.Cache cache ) { D = cache; }
		public T this[string key] { get { return (T)D[key]; } }
	}*/


	/// <summary>
	///!! Temporarily set to pageScope - implement later in another file
	/// </summary>
	public class DLAutoScope : DLPageScope {
		public DLAutoScope( HttpContext context ) : base(context) { }
	}


	public class DLExpirePolicyCollection : IDLExpirePolicy {
		public List<IDLExpirePolicy> Policies = new List<IDLExpirePolicy>();
		public DLExpirePolicyCollection( params IDLExpirePolicy[] list ) { Policies.AddRange( list ); }
		public void Add( IDLExpirePolicy policy ) {
			Policies.Add( policy );
		}
		public bool IsExpired() {
			for( int i = 0, len = Policies.Count; i < len; i++ )
				if( Policies[i].IsExpired() )
					return true;
			return false;
		}
		public void NotifyCreate() {}
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}

	public class DLDateTimeExpirePolicy : IDLExpirePolicy {
		public DateTime AbsoluteExpiration;// = Cache.NoAbsoluteExpiration;
		public DLDateTimeExpirePolicy( DateTime time ) { AbsoluteExpiration = time; }
		public bool IsExpired() {
			if( DateTime.Now >= AbsoluteExpiration ) {
				AbsoluteExpiration = DateTime.MaxValue;
				return true;
			}
			return false;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}
	public class DLTimeSpanExpirePolicy : IDLExpirePolicy {
		public TimeSpan SlidingExpiration;// = Cache.NoSlidingExpiration;
		public DateTime NextExpire = DateTime.Now;
		public DLTimeSpanExpirePolicy( TimeSpan span ) { SlidingExpiration = span; }
		public bool IsExpired() {
			DateTime now = DateTime.Now;
			if( now >= NextExpire ) {
				NextExpire = DateTime.MinValue;
				return true;
			}
			NextExpire = now + SlidingExpiration;
			return false;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}
	public class DLFileUpdateExpirePolicy : IDLExpirePolicy {
		public string FileName;
		public DLFileUpdateExpirePolicy( string file ) { FileName = file; }
		public bool IsExpired() {
			//!! need to implement
			return false;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}
	public class DLLocalKeyExpirePolicy : IDLExpirePolicy {
		public string Key;
		public DLLocalKeyExpirePolicy( string key ) { Key = key; }
		public bool IsExpired() {
			//!! need to implement
			return false;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}

	public class DLSharedKeyExpirePolicy<K> : IDLExpirePolicy {
		public string Key;
		public string DBProfile;
		public StaticParams IParams = null;
		public DLSharedKeyExpirePolicy( string dbProfile, string key, StaticParams pars ) { DBProfile = dbProfile; Key = key; IParams = pars; }
		public bool IsExpired() {
			//!! Reevaluate this implementation - compare to what? Should entire key be string? If no value, set value?
			bool isExpired = false;
			using( DBHelper dbh = new DBHelper( DBProfile ) ) {
				using (IDataReader r = dbh.GetDataReader("spIsExpired", DBHelper.SqlParameter("Key", Key) ) ) {
					if( r.Read() ) {
						DateTime date = r.GetDateTime( 0 );
					}
#if ca2202saysnotrequired
					r.Close();
#endif
				}
			}
			return isExpired;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}
	public class DLDBExpirePolicy : IDLExpirePolicy {
		public string DBKey;
		public DLDBExpirePolicy( string dbKey ) { DBKey = dbKey; }
		public bool IsExpired() {
			//!! need to implement
			return false;
		}
		public void NotifyCreate() { }
		public void NotifyUpdate() { }
		public void NotifyDelete() { }
	}
	
}
