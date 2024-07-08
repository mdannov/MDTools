using System;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Collections.Generic;
using System.Collections;
using System.Web.UI;
using MDTools.Data;
using MDTools.Math;
using MDTools.Extension;

/*
 * baseCookie (abstract) - Key (get-only), [secureOnly=false, lockoutscript=true, expiration=session, domain=null, acrossdomain=false, path=empty]
 *
 *	 basePropCookie (abstract) - SecureOnly, Domain, AcrossDomain, Path
 *
 *	    baseRegularCookie (abstract) - Expiration, IsSession, IsPermanent
 *	       RegularCookie - Value, LockoutScript
 *		   RegularKeyedCookie - Array[].Value, LockoutScript
 *		   
 *         BinaryCookie (use with Encrypted values) - Value (byte[]) 
 *
 *	    CountdownCookie - Value, [expiration set from server now, lockoutscript=false]
 *
 *	    SessionCookie - Value
 *	    SessionKeyedCookie - Array[].Value
 *
 *	    basePersistedCookie (abstract) - Array[].Value [persisted on reference, expiration=permanent]
 *		   PersonalizationCookie [key="MCF", lockoutscript=false]
 *
 *	 ClientCookie - Value (get-only)
 *	 ClientLocalCookie - Array[].Value (get-only) [key="LOCAL"]
 *
 *   baseGuidCookie (abstract) - Guid (get-only) [persisted on reference]
 *      UniqueCookie - [expiration=permanent, key="Guid"]
 *      UniqueDayCookie - [expiration=24hours, key="DGuid"]
*/

namespace MDTools.Web {

	public abstract class baseCookie {
		/// <summary>
		/// Secure HTTP can access the cookie only - default false
		/// </summary>
		protected bool secureOnly = false;		
		/// <summary>
		/// Lockout Client-side script from accessing cookie - default true
		/// </summary>
		protected bool lockoutScript = true;
		/// <summary>
		/// Expiration of cookie - defaults to none, i.e. session cookie unless otherwise specified
		/// </summary>
		protected DateTime expiration = DateTime.MinValue;// equiv to Session
		/// <summary>
		/// Domain cookie access is limited to - defaults to current domain; set to string.empty to allow all domains to access
		/// </summary>
		protected string domain = null;// null means current domain
		/// <summary>
		/// Cookie is accessible across the entire domain, not just the current subdomain - defaults to false
		/// </summary>
		protected bool acrossDomain = false;
		/// <summary>
		/// Path cookie access is limited to - defaults to none, i.e. unlimited access to all paths
		/// </summary>
		protected string path = string.Empty;
		/// <summary>
		/// Key base of this Cookie
		/// </summary>
		public string Key { get { return key; } }

		protected HttpCookie writeCookie = null;//cache
		protected HttpCookie reqCookie = null;// cache
		protected HttpContext context;
		protected string key;

		protected baseCookie() { }

		protected HttpCookie setCookie() {
			// If we've already created the cookie and set it up, we don't need to again in case the value changes again
			if( writeCookie == null ) {
				writeCookie = context.Response.Cookies[key];// this creates one if response cookie not already created
				// If not session, then set the expiration date
				writeCookie.Expires = this.expiration;
				// Domain is not set, it automatically defaults to current
				_SetDomain();
				writeCookie.Path = !string.IsNullOrEmpty( this.path ) ? this.path : "/";//context.Request.Url.PathOnly();
				writeCookie.HttpOnly = this.lockoutScript;
				writeCookie.Secure = this.secureOnly;
			}
			return writeCookie;	// never returns null
		}
		protected void _SetDomain() {
			string domain = null;
			if( this.domain == null )
				domain = this.acrossDomain ? '.' + context.Request.Url.Domain() : context.Request.Url.Host;
			else if( this.domain != string.Empty )
				domain = this.domain;// this instance, you cannot overwrite the existing domain
			if( domain != null )
				writeCookie.Domain = domain;
		}
		protected HttpCookie getCookie() {
			// If update, it no longer returns the incoming value; now returns the outgoing value
			if( writeCookie != null )
				return writeCookie; // context.Response.Cookies[key];
			// If there are response cookie, there may already be an update pending already we need to gather
			if( context.Response.Cookies.Count > 0 ) {
				// prevents multiple recreations
				if( context.Response.Cookies.AllKeys.Contains( this.key ) )
					return setCookie();	// this will overwrite existing write settings
			}
			if( reqCookie != null )
				return reqCookie;
			reqCookie = context.Request.Cookies[key];
			return reqCookie;
		}
		protected string baseValue {
			get {
				var ck = getCookie();
				return ck == null ? null : HttpUtility.UrlDecode( ck.Value );
			}
			set {
				if( value == null )
					return;
				// If value hasn't changed to set, don't create new cookie
				if( value == baseValue )
					return;
				// Set the cookie				
				setCookie().Value = HttpUtility.UrlEncode( value );
			}
		}
		protected string getArray( string subkey ) {
			var ck = getCookie();
			return ck == null ? null : HttpUtility.UrlDecode( ck[subkey] );
		}
		protected void setArray( string subkey, string value ) {
			var ck = setCookie();
			if( ck == null )
				return;
			ck[subkey] = HttpUtility.UrlEncode( value );
		}
		public void Delete() {
			HttpCookie ck = null;
			// If request cookie exists, then we most expire in a response to delete the cookie
			if( reqCookie != null )
				ck = reqCookie;
			else
				ck = context.Request.Cookies[this.key];
			if( ck != null ) {
				expiration = DateTime.Now.AddYears( -30 );
				writeCookie = context.Response.Cookies[this.key];
				writeCookie.Expires = expiration;
				return;
			}
			// If update cookie only, then we need only remove it from the response 
			if( writeCookie != null )
				ck = writeCookie;
			else if( context.Response.Cookies.Count > 0 ) {
				// prevents multiple recreations
				if( context.Response.Cookies.AllKeys.Contains( this.key ) ) {
					ck = context.Response.Cookies[this.key];
				}
			}
			if( ck != null ) {
				context.Response.Cookies.Remove( this.key );
				writeCookie = null;
			}
		}
		public bool AlreadyExists {
			get {
				return context.Request.Cookies[this.key] != null;
			}
		}
	

	}

	public abstract class basePropCookie : baseCookie {
		/// <summary>
		/// Secure HTTP can access the cookie only - default false
		/// </summary>
		public bool SecureOnly { 
			get { return secureOnly; } 
			set { 
				secureOnly = value; 
				if( writeCookie != null ) 
					writeCookie.Secure = secureOnly; 
			} 
		}
		/// <summary>
		/// Domain cookie access is limited to - defaults to current domain; set to string.empty to allow all domains to access
		/// </summary>
		public string Domain { 
			get { return domain; } 
			set { 
				domain = value;
				if( writeCookie != null )
					_SetDomain();
			}
		}
		/// <summary>
		/// Cookie is accessible across the entire domain, not just the current subdomain - defaults to false
		/// </summary>
		public bool AcrossDomain {
			get { return acrossDomain; } 
			set { 
				acrossDomain = value;
				if( writeCookie != null )
					_SetDomain();
			}
		}
		/// <summary>
		/// Path cookie access is limited to - defaults to none, i.e. unlimited access to all paths
		/// </summary>
		public string Path { 
			get { return path; } 
			set { 
				path = value;
				if( writeCookie != null )
					writeCookie.Path = !string.IsNullOrEmpty( this.path ) ? this.path : context.Request.Url.PathOnly();
			}
		}

		public basePropCookie( HttpContext Context, string Key ) {
			this.context = Context;
			this.key = Key;
		}

	}

	public abstract class baseRegularCookie : basePropCookie {
		/// <summary>
		/// Expiration of cookie - defaults to none, i.e. session cookie unless otherwise specified
		/// </summary>
		public DateTime Expiration { 
			get { return expiration; } 
			set { 
				expiration = value;
				if( writeCookie != null )
					writeCookie.Expires = expiration;
			} 
		}
		/// <summary>
		/// If read, tells if cookie is Session; if set to true, converts the cookie to session
		/// </summary>
		public bool IsSession { 
			get { return expiration.Equals( DateTime.MinValue ); } 
			set {
				if( value ) {
					expiration = DateTime.MinValue;
					if( writeCookie != null )
						writeCookie.Expires = expiration;
				}
			}
		}
		/// <summary>
		/// If read, tells if cookie is never to expire; if set to true, ensures the cookie never expires
		/// </summary>
		public bool IsPermanent { 
			get { return expiration.Equals( DateTime.MaxValue ); } 
			set {
				if( value ) {
					expiration = DateTime.MaxValue;
					if( writeCookie != null )
						writeCookie.Expires = expiration;
				}
			} 
		}
		public baseRegularCookie( HttpContext Context, string Key ) : base( Context, Key) {}

	}

	public class RegularCookie : baseRegularCookie {
		/// <summary>
		/// </summary>
		public bool LockoutScript {
			get { return lockoutScript; }
			set {
				lockoutScript = value;
				if( writeCookie != null )
					writeCookie.HttpOnly = lockoutScript;
			}
		}
		public RegularCookie( HttpContext Context, string Key ) : base( Context, Key ) { }
		public string Value { get { return baseValue; } set { baseValue = value; } }
	}

	public class RegularKeyedCookie : baseRegularCookie {
		/// <summary>
		/// </summary>
		public bool LockoutScript {
			get { return lockoutScript; }
			set {
				lockoutScript = value;
				if( writeCookie != null )
					writeCookie.HttpOnly = lockoutScript;
			}
		}
		public RegularKeyedCookie( HttpContext Context, string Key ) : base( Context, Key ) { }

		public string this[string subkey] { get { return getArray( subkey ); } set { setArray( subkey, value ); } }

		public void RemoveKey( string key ) {
			// Check if key is present to remove
			if( getCookie()[key] == null ) return;
			// Remove the key
			setCookie().Values.Remove( key );
		}

	}

	public class BinaryCookie : baseRegularCookie {
		public BinaryCookie( HttpContext Context, string Key ) : base( Context, Key ) { }
		public byte[] Value { 
			get { 
				var val = baseValue;
				return !string.IsNullOrEmpty( val ) ? val.ToByteArray() : null;
			}
			set { baseValue = value != null ? value.GetString() : string.Empty; } 
		}
	}

	/// <summary>
	/// Cookie who's expiration is reset every time its accessed --!! untested
	/// </summary>
	public class CountdownCookie : basePropCookie {
		public CountdownCookie( HttpContext Context, string Key, TimeSpan ts ) : base(Context, Key) {
			expiration = DateTime.Now.Add( ts );
			lockoutScript = false;
		}
		public string Value { get { return baseValue; } set { baseValue = value; } }
	}

	/// <summary>
	/// Cookie that is created by javascript on client side / read-only
	/// </summary>
	public class ClientCookie : baseCookie {
		public ClientCookie( HttpContext Context, string Key ) {
			this.context = Context;
			this.key = Key;
			lockoutScript = false;
		}
		public string Value { get { return baseValue; } }
	}

	/// <summary>
	/// Keyed Cookie that is created by javascript on client side / read-only
	/// </summary>
	public class ClientLocalCookie : baseCookie {
		public const string LOCAL_KEY = "LOCAL";
		public ClientLocalCookie( HttpContext Context ) {
			this.context = Context;
			this.key = LOCAL_KEY;
			lockoutScript = false;
		}
		public string this[string subkey] { get { return getArray( subkey ); } }
		public new static bool AlreadyExists( HttpContext Context ) { return Context.Request[LOCAL_KEY] != null; }
	}


	/// <summary>
	/// Cookie that lives for the life of the browser session
	/// </summary>
	public class SessionCookie : basePropCookie {
		public SessionCookie( HttpContext Context, string Key ) : base( Context, Key ) {
			this.expiration = DateTime.MinValue;
		}
		public string Value { get { return baseValue; } }
	}

	/// <summary>
	/// Cookie that lives for the life of the browser session
	/// </summary>
	public class SessionKeyedCookie : basePropCookie {
		public SessionKeyedCookie( HttpContext Context, string Key ) : base( Context, Key ) {
			this.expiration = DateTime.MinValue;
		}
		public string this[string subkey] { get { return getArray( subkey ); } set { setArray( subkey, value ); } }
		public void RemoveKey( string key ) {
			// Check if key is present to remove
			if( getCookie()[key] == null ) return;
			// Remove the key
			setCookie().Values.Remove( key );
		}
	}


	/// <summary>
	/// Cookie that is keyed and can store multiple values
	/// </summary>
	public abstract class basePersistedCookie : basePropCookie {
		public basePersistedCookie( HttpContext Context, string Key ) : base(Context, Key) {
			expiration = DateTime.MaxValue;
		}
		/// <summary>
		/// Set baseValue or else call this function in inherited classes after setting properties to ensure correct creation
		/// </summary>
		protected void Setup() {
			var ck = getCookie();
			if( ck == null )
				setCookie().Value = string.Empty;
		}
		public string this[string subkey] { get { return getArray( subkey ); } set { setArray( subkey, value ); } }
		public void RemoveKey( string key ) {
			// Check if key is present to remove
			if( getCookie()[key] == null ) return;
			// Remove the key
			setCookie().Values.Remove( key );
		}
	}


	/// <summary>
	/// PersonalizationCookie is a helper class It's a Permanent KeyedCookie with "MCP" 
	/// (Master Client Profile) as its key
	/// These cookies are automatically persisted even if empty
	/// </summary>
	public class PersonalizationCookie : basePersistedCookie {
		public const string PERSONALIZATION_KEY = "MCF";
		public PersonalizationCookie( HttpContext Context ) : base( Context, PERSONALIZATION_KEY ) {
			this.expiration = DateTime.MaxValue;
			lockoutScript = false;		// allow client scripts to access
			acrossDomain = true;
			// If cookie doesn't exist, create it to store placeholder
			if( baseValue == null ) 
				baseValue = string.Empty;
			//Setup();// ^^ above does this
		}
		public new static bool AlreadyExists( HttpContext Context ) { return Context.Request[PERSONALIZATION_KEY] != null; }
	}


	/// <summary>
	/// Unique identifier that lives till the end of the day on client 
	/// These cookies are automatically persisted even if empty
	/// </summary>
	public abstract class baseGuidCookie : baseCookie {
		protected Guid guid;
		public Guid Guid { get { return this.guid; } }
		public string StoredValue { get { return this.baseValue; } }
		public baseGuidCookie( HttpContext Context, DateTime Expiration, string Key ) {
			this.context = Context;
			this.key = Key;
			expiration = Expiration;
			acrossDomain = true;
			lockoutScript = true;

			var v = baseValue;
			if( v != null ) {
				try {
					// Convert from string - if invalid, create new one
					this.guid = v.GuidFromBase64();//new Guid( v );
				}
				catch {
					v = null;
				}
			}
			if( v == null ) {
				this.guid = Guid.NewGuid();
				// store the Guid
				baseValue = guid.ToBase64();//.ToString("N");
			}
		}
	}

	/// <summary>
	/// Unique identifier that lives for 24 hours on client 
	/// These cookies are automatically persisted 
	/// </summary>
	public class UniqueDayCookie : baseGuidCookie {
		protected const string GUID_KEY = "DayKey";
		public UniqueDayCookie( HttpContext Context ) : base( Context, DateTime.Today.AddDays(1), GUID_KEY ) {}
		public static UniqueDayCookie Create( HttpContext Context ) { return new UniqueDayCookie( Context ); }
		public new static bool AlreadyExists( HttpContext Context ) { return Context.Request[GUID_KEY] != null; }
	}

	/// <summary>
	/// Cookie that lives forever on client browser unless cleared
	/// These cookies are automatically persisted 
	/// </summary>
	public class UniqueLifeCookie : baseGuidCookie {
		protected const string GUID_KEY = "LifeKey";
		public UniqueLifeCookie( HttpContext Context ) : base( Context, DateTime.MaxValue, GUID_KEY ) { }
		public static UniqueLifeCookie Create( HttpContext Context ) { return new UniqueLifeCookie( Context ); }
		public new static bool AlreadyExists( HttpContext Context ) { return Context.Request[GUID_KEY] != null; }
	}


//!! Legacy Keys = Convert old keys to new
//!! Obsolete Keys = Delete old keys
	/// <summary>
	/// List of Cookies that need to be Deleted or Replaced
	/// </summary>
	public static class CookieTools {
		public const int LEGACY_MAX_COOKIES = 20;
		public static void CleanupLegacyCookies(HttpContext Context, List<string> list) {
			var inCookies = Context.Request.Cookies;
			var outCookies = Context.Response.Cookies;
			for( int i = 0, len = list.Count; i < len; i++ ) {
				var ck = inCookies[list[i]];
				if(ck!=null) {
					ck.Expires = DateTime.Now.AddDays( -20 );	// expire to remove
					outCookies.Set( ck );
				}
			}
		}
		public static void CleanupLegacyPath( HttpContext Context, string path ) {
			bool hasPath = false;
			if( string.IsNullOrEmpty( path ) ) {
				hasPath = true;
				path = path.ToLower();
			}
			var inCookies = Context.Request.Cookies;
			var outCookies = Context.Response.Cookies;
			for( int i = 0, len = inCookies.Count; i < len; i++ ) {
				var ck = inCookies[i];
				if( hasPath && ck.Path.ToLower().Equals( path ) )
					continue;
				ck.Expires = DateTime.Now.AddDays( -20 );	// expire to remove
				outCookies.Set( ck );
			}
		}
		public static void DeleteAll( HttpContext Context ) {
			CleanupLegacyPath( Context, null );
		}
		public static void TestCount( HttpContext Context ) {
			// Starting count
			var inCookies = Context.Request.Cookies;
			int count = inCookies.Count;
			// Now check if there are new response cookies that aren't replacing existing that aren't expired
			var outCookies = Context.Response.Cookies;
			for( int i = 0, len = outCookies.Count; i < len; i++ ) {
				string findvalue = outCookies[i].Value;
				var ck = inCookies[findvalue];
				if( ck == null )
					count++;
				else
					if( ck.Expires <= DateTime.Now )
						count++;
			}
			if( count > LEGACY_MAX_COOKIES )
				throw new System.ArgumentOutOfRangeException( "Cookies", "Too many" );
		}
	}
}
