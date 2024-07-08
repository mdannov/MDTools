using System;
using System.Data;
using System.Security.Principal;
using System.Web;
using System.Web.Profile;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace MDTools.Web {

	/// <summary>
	/// User Helper Functions
	/// </summary>
	public static class UserHelper {

		public static Guid GetUserID( HttpContext Context ) {
			object o = Context.Items["$UserID"];
			if( o != null )
				return (Guid)o;
			if( !Context.IsLoggedOn() )
				return Guid.Empty;
			Guid userID = UserID;
			Context.Items["$UserID"] = userID;
			return userID;

		}

		public static Guid UserID {
			//!!Performance improve this with Cache so database is not hit every time
			get {
				// Get the Unique Key for user
				MembershipUser u = Membership.GetUser( false ); // does not update user last access date
				if( u == null )
					return Guid.Empty;
				return (Guid)u.ProviderUserKey;
			}
		}
		public static Guid UserID_SetLastAccess {
			get {
				// Get the Unique Key for user
				MembershipUser u = Membership.GetUser( true ); // does not update user last access date
				return (Guid)u.ProviderUserKey;
			}
		}
		public static string GetClientIP( this HttpRequest Req ) {
			string ip = Req.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if( !string.IsNullOrEmpty( ip ) )
				return ip.Split( ',' )[0].Trim();
			return Req.ServerVariables["REMOTE_ADDR"];
		}
		public static UInt32 IPtoInt( string ipstr ) {
			System.Net.IPAddress ip;
			if( System.Net.IPAddress.TryParse( ipstr, out ip ) ) {
				byte[] ips = ip.GetAddressBytes();
				return (UInt32)( ( ips[0] << 24 ) | ( ips[1] << 16 ) | ( ips[2] << 8 ) | ips[3] );// returns the reverse bytes of ip.Address
			}
			return 0;
		}
		public static bool IsLoggedOn( this HttpContext Context ) {
			object o = Context.Items["$LoggedOn"]; //!! Test if this is faster
			if( o != null )
				return (bool)o;
			bool res = Context.User.Identity.IsAuthenticated;
			Context.Items["$LoggedOn"] = res;
			return res;
		}
		public static bool IsLoggedOn( this IPrincipal User ) {
			return User.Identity.IsAuthenticated;
		}
		public static void LogOut( this HttpContext Context ) {
			FormsAuthentication.SignOut();
			Roles.DeleteCookie();
			Context.Session.Abandon();
			Context.Response.Cache.SetCacheability( HttpCacheability.NoCache );
			Context.Response.Cache.SetExpires( DateTime.Now.AddSeconds( -1 ) );
			Context.Response.Cache.SetNoStore();
			Context.Response.AppendHeader( "Pragma", "no-cache" );
		}

	}

	public abstract class UserProfileBase<T> : ProfileBase where T:ProfileBase {

#if alreadyinbase
		public object this[string propertyName] {
			get { return base[propertyName]; }
			set { base[propertyName] = value; }
		}

		public string UserName {
			get { return base.UserName; }
		}
		public bool IsAnonymous {
			get { return base.IsAnonymous; }
		}
		public bool IsDirty {
			get { return base.IsDirty; }
		}
		public System.DateTime LastActivityDate {
			get { return base.LastActivityDate; }
		}
		public System.DateTime LastUpdatedDate {
			get { return base.LastUpdatedDate; }
		}
		public void Save() {
			base.Save();
		}

#endif
		public TYPE GetPropertyValue<TYPE>( string propertyName ) {
			return (TYPE)base.GetPropertyValue( propertyName );
		}
		public void SetPropertyValue<TYPE>( string propertyName, TYPE propertyValue ) {
			base.SetPropertyValue( propertyName, propertyValue );
		}

		public static T Current {
			get {
#if cached
					var Context = HttpContext.Current;
					ProfileSettings Prof = (ProfileSettings)Context.Items["$$Profile"];
					if( Prof == null ) {
						Prof = new ProfileSettings( HttpContext.Current );
						Context.Items["$$Profile"] = Prof;
					}
					return Prof;
#else
				return (T)HttpContext.Current.Profile;
				//return Create( HttpContext.Current, true );
#endif
			}
		}

		public static T Create( string userName ) {
			return (T)ProfileBase.Create( userName );
		}
		public static T Create( string userName, bool authenticated ) {
			return (T)ProfileBase.Create(userName, authenticated);
		}
		public static T Create( HttpContext Context, bool authenticated = true ) {
			return Create( Context.User.Identity.Name, authenticated );
		}
		public static T Create( IPrincipal User, bool authenticated = true ) {
			return Create( User.Identity.Name, authenticated );
		}
		public static T Create( IIdentity Identity, bool authenticated = true ) {
			return Create( Identity.Name, authenticated );
		}
		public static T Create( bool authenticated = true ) {
			return Create( Membership.GetUser().UserName );
		}
#if redundant
		public static T GetProfile( string userName, bool authenticated = true ) {
			var Prof = new T();
			Prof.base = base.Create( userName, authenticated );
			return Prof;
		}
		public static T GetProfile( HttpContext Context, bool authenticated = true ) {
			return GetProfile( Context.User.Identity.Name, authenticated );
		}
		public static T GetProfile( IPrincipal User, bool authenticated = true ) {
			return GetProfile( User.Identity.Name, authenticated );
		}
		public static T GetProfile( IIdentity Identity, bool authenticated = true ) {
			return GetProfile( Identity.Name, authenticated );
		}
		public static T GetProfile( bool authenticated = true ) {
			return GetProfile( Membership.GetUser().UserName );
		}
#endif
	}

}