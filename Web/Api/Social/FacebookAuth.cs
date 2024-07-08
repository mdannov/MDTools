using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Text;
using MDTools.Data;
using MDTools.Extension;
using MDTools.Web;

/*	Code Copyright Michael Dannov 2011-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */

namespace MDTools.Web.Api.Facebook {

	public enum RequestMethod { None = 0, Get, Post, Session };

	public class FBAuth {

		#region AppSettings - AppID, ApiKey, ApiSecret, Defaults

		public static string FBAppID = ConfigurationManager.AppSettings["FBAppID"];
		public static string FBApiKey = ConfigurationManager.AppSettings["FBApiKey"];
		public static string FBAppSecret = ConfigurationManager.AppSettings["FBAppSecret"];
		public static string FBDefaultPermissions = ConfigurationManager.AppSettings["FBPerms"];

		#endregion

		#region Public properties

		public HttpContext Context { get; set; }
		public string AccessToken { get; private set; }
		public bool IsValid { get { return !string.IsNullOrEmpty( AccessToken ); } }
		public DateTime TimeStart;
		public double ExpiresSecs = 0;

		#endregion

		#region Initialization

		protected string AccessCode { get; private set; }


		public FBAuth( HttpContext Context ) {
			this.Context = Context;
		}

		public bool ProcessRequest( RequestMethod Method = RequestMethod.Get ) {

			// Check if token is in query string
			SetAccessToken( Method );
			if( !IsValid && Method != RequestMethod.Session ) {
				// If no token, check if we have an access code, so we can get a token
				if( !string.IsNullOrEmpty( SetAccessCode( Method ) ) ) {
					if( TradeCodeForAccessToken() && !ValidateAuth() && !string.IsNullOrEmpty( AccessToken ) )
						// Invalidate because it did not validate
						AccessToken = null;
				}
			}
#if obsolete
				// Now that we have a token, break it up 
				string ClientID = null, SessionKey = null, Sig = null;
				Auth.BreakupCodes( FBAuth.AccessToken, ref ClientID, ref SessionKey, ref key, ref userid, ref Sig );
#endif
			return IsValid;
		}

		#endregion

		public static string GetLoginRedirectUrl( HttpContext Context, string redirectUrl, string fbperms = null, string display = "page" ) {
			if( string.IsNullOrEmpty( fbperms ) )
				fbperms = FBDefaultPermissions;
#if !oldlink
			var id = UniqueDayCookie.Create( Context ).StoredValue;
			return string.Concat(
				"https://www.facebook.com/dialog/oauth/?client_id=", FBAppID,
				"&scope=", fbperms,
				"&state=", id,
				"&display=", display,
				"&redirect_uri=", UrlEncodeQuery(redirectUrl) );
#else
			return string.Concat( "https://graph.facebook.com/oauth/authorize?client_id=",
				FBAppID, "&type=web_server&scope=", fbperms, "&display=page&redirect_uri=", redirectUrl ); 
#endif
		}

		public bool ValidateAuth() {
			var id = UniqueDayCookie.Create( Context ).StoredValue;
			return ( Context.Request["state"] == id );
		}

		private string SessionFBToken { get { return "FBToken" + Context.Session.SessionID; } }

		protected void SetAccessToken( string atoken ) { AccessToken = atoken; }
		protected string SetAccessToken( RequestMethod Method = RequestMethod.Get ) {
			switch( Method ) {
			case RequestMethod.Get:
				AccessToken = Context.Request.QueryString["token"];
				break;
			case RequestMethod.Post:
				AccessToken = Context.Request.Form["token"];
				break;
			case RequestMethod.Session:
				AccessToken = Context.Session.ExpiringItem<string>( SessionFBToken );
				if( !string.IsNullOrEmpty( AccessToken ) ) {
					Context.Session.Remove( SessionFBToken );// If found, expire it
				}
				break;
			}
			return AccessToken;
		}
		public void SaveSessionAccessToken( string Token ) {
			AccessToken = Context.Session.ExpiringItem<string>( SessionFBToken, Token, 60 );
		}
		protected string SetAccessCode( RequestMethod Method = RequestMethod.Get ) {
			switch( Method ) {
			case RequestMethod.Get:
				AccessCode = Context.Request.QueryString["code"];
				break;
			case RequestMethod.Post:
				AccessCode = Context.Request.Form["code"];
				break;
			}
			return AccessCode;
		}

		protected string GetAuthURL( string redirectUrl ) {
			return string.Concat( "https://graph.facebook.com/oauth/access_token?client_id=",
				FBAppID, "&client_secret=", FBAppSecret, "&code=", AccessCode, "&redirect_uri=", UrlEncodeQuery(redirectUrl)
			);
		}

		public static string UrlEncodeQuery( string redirectPath ) {
			// Separate path and query to urlencode so it matches original
			int pos = redirectPath.IndexOf( '?' );
			var path = redirectPath.Substring( 0, pos );
			int codepos = redirectPath.IndexOf( "&code", pos );
			var urlqs = codepos >= 0 ? redirectPath.Substring( pos, codepos - pos ) : redirectPath.Substring( pos );
			return path + urlqs.UrlEncode();
		}

		protected bool TradeCodeForAccessToken() {
			// Get the Access Token and validate
			try {
				// Retrieve the AccessToken
				var uri = Context.Request.Url;
				var webres = new WebRetrieve( FBApi.USER_AGENT ).Get( 
					GetAuthURL( Url.ToString( uri.Scheme, uri.Host, Context.Request.RawUrl ) )
				);
				if( !string.IsNullOrEmpty( webres ) ) {
					var qs = HttpUtility.ParseQueryString( webres );
					AccessToken = qs["access_token"];
					var exp = qs["expires"];
					TimeStart = DateTime.Now;
					if( !string.IsNullOrEmpty( exp ) )
						Double.TryParse( exp, out ExpiresSecs );
				}
			}
			catch( Exception ex ) {
				return false;
			}
			return true;
		}



		#region Permission Requests

		/// <summary>
		/// Returns all permissions still required if permissions are already available
		/// Format: comma-separated permissions
		/// </summary>
		/// <returns>Combined permissions</returns>
		public static string NeededPermissions( string currentPermissions, string requestedPermissions ) {
			// If no permissions are requested, nothing to do
			if( string.IsNullOrEmpty( requestedPermissions ) )
				return null;
			// If no existing permissions, all requested are required
			if( string.IsNullOrEmpty( currentPermissions ) )
				return requestedPermissions;
			return _NeededPermissions( currentPermissions, requestedPermissions.Split( ',' ) );
		}
		/// <summary>
		/// Returns all permissions still required if permissions are already available
		/// Format: comma-separated permissions
		/// </summary>
		/// <returns>Combined permissions</returns>
		public static string NeededPermissions( string currentPermissions, params string[] requestedPermissions ) {
			// If no permissions are requested, nothing to do
			if( requestedPermissions == null )
				return null;
			int len = requestedPermissions.Length;
			if( len <= 0 )
				return null;
			// If no existing permissions, all requested are required
			if( string.IsNullOrEmpty( currentPermissions ) )
				return string.Join( ",", requestedPermissions );
			return _NeededPermissions( currentPermissions, requestedPermissions );
		}
		private static string _NeededPermissions( string currentPermissions, string[] requestedPermissions ) {
			// Construct needed string by looping through requested to test if they exist in current
			int len = requestedPermissions.Length;
			string Needed = string.Empty;
			bool first = true;
#if slowtokencheck
			var Current = new HashSet<string>( currentPermissions.Split( ',' ) );
#endif
			for( int i = 0; i < len; i++ ) {
				var cur = requestedPermissions[i];
#if slowtokencheck
				if( !Current.Contains( cur ) )
#else
				if( !currentPermissions.FastTokenCheck( cur, ',' ) )
#endif
					if( first ) {
						Needed += cur;
						first = false;
					} else
						Needed = string.Concat( Needed, ",", cur );
			}
			// Return the combined set of required permissions or null if none required
			return Needed;
		}

		public static string CombinedPermissions( string currentPermissions, string requestedPermissions ) {
			var neededPermissions = NeededPermissions( currentPermissions, requestedPermissions );
			if( string.IsNullOrEmpty( neededPermissions ) )
				return currentPermissions;
			if( string.IsNullOrEmpty( currentPermissions ) )
				return neededPermissions;
			return string.Concat( currentPermissions, ",", neededPermissions );
		}
		public static string CombinedPermissions( string currentPermissions, params string[] requestedPermissions ) {
			var neededPermissions = NeededPermissions( currentPermissions, requestedPermissions );
			return AppendPermissions( currentPermissions, neededPermissions );
		}

		public static string AppendPermissions( string currentPermissions, string requestedPermissions ) {
			if( string.IsNullOrEmpty( requestedPermissions ) )
				return currentPermissions;
			if( string.IsNullOrEmpty( currentPermissions ) )
				return requestedPermissions;
			return string.Concat( currentPermissions, ",", requestedPermissions );
		}
		public static string AppendPermissions( string currentPermissions, params string[] requestedPermissions ) {
			if( requestedPermissions == null )
				return currentPermissions;
			string appendPermissions = string.Join( ",", requestedPermissions );
			if( string.IsNullOrEmpty( currentPermissions ) )
				return appendPermissions;
			return string.Concat( currentPermissions, ",", appendPermissions );
		}

		public static string GetPermissionsRedirectUrl( string redirPath, string permissions ) {
			// Since some permissions were required, we will need to redirect to new auth request
			return FBAuth.GetLoginRedirectUrl( HttpContext.Current, redirPath, permissions );
		}
		public static string GetPermissionsRedirectUrl( HttpContext Context, string permissions ) {
			// Since some permissions were required, we will need to redirect to new auth request
			return FBAuth.GetLoginRedirectUrl( Context, new Url( Context ).ToString(), permissions );
		}
		public static string GetPermissionsRedirectUrl( HttpContext Context, string currentPermissions, string requestedPermissions ) {
			// Check if any permissions are needed
			var neededPermissions = NeededPermissions( currentPermissions, requestedPermissions );
			// If none were needed, exit
			if( neededPermissions == null )
				return null;
			// Since some permissions were required, we will need to redirect to new auth request
			return FBAuth.GetLoginRedirectUrl( Context, new Url( Context ).ToString(), neededPermissions );
		}
		public static string GetPermissionsRedirectUrl( HttpContext Context, string currentPermissions, params string[] requestedPermissions ) {
			// Check if any permissions are needed
			var neededPermissions = NeededPermissions( currentPermissions, requestedPermissions );
			// If none were needed, exit
			if( neededPermissions == null )
				return null;
			// Since some permissions were required, we will need to redirect to new auth request
			// Build url with permissions on the return
			var url = new Url( Context ).AppendQS( "permissions", neededPermissions );
			return FBAuth.GetLoginRedirectUrl( Context, url.ToString(), neededPermissions );
		}
		public static string PullPermissionsFromUrl( Url url ) {
			var perms = url.QueryString["permissions"];
			if( string.IsNullOrEmpty( perms ) )
				return null;
			url.RemoveQS( "permissions" );
			return perms;
		}

		#endregion

		#region Obsolete Code

		/// <summary>
		/// from: http://benbiddington.wordpress.com/2010/04/23/facebook-graph-api-getting-access-tokens/
		/// </summary>
		[Obsolete( "Facebook changed its access token format so this no longer works.", true )]
		public static void BreakupCodes( string accessCode, ref string ClientID, ref string SessionKey, ref string Session, ref string UserID, ref string Sig ) {
			var parts = accessCode.Split( '|' );
			switch( parts.Length ) {
			case 2:
				ClientID = null;
				SessionKey = parts[0];
				Sig = parts[1];
				break;
			case 3:
				ClientID = parts[0];
				SessionKey = parts[1];
				Sig = parts[2];
				break;
			}
			if( !string.IsNullOrEmpty( SessionKey ) ) {
				int pos = SessionKey.IndexOf( '-' );
				int dotpos = SessionKey.IndexOf( '.' );
				if( pos != -1 ) {
					// <session>.tc-<userid>
					UserID = SessionKey.Substring( pos + 1 );
					Session = ( dotpos != -1 ) ? SessionKey.Substring( 0, dotpos ) : SessionKey.Substring( 0, pos );
				} else {
					// <session>.tc
					UserID = null;
					Session = ( dotpos != -1 ) ? SessionKey.Substring( 0, dotpos ) : SessionKey;
				}
			}
		}

		#endregion

	}

}
