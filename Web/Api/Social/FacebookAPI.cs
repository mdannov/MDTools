/*
 * Based on information and sources from various sources and updated to match:
 * https://developers.facebook.com/docs/reference/api/
 * 
 * Updated/Enhanced by Michael Dannov 2011
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using MDTools.Web;
using MDTools.Extension;

namespace MDTools.Web.Api.Facebook {

	enum HttpVerb {
		GET,
		POST,
		DELETE
	}

	/// <summary>
	/// Wrapper around the Facebook Graph API. 
	/// </summary>
	public class FBApi {

		public string AccessToken { get; private set; }

		/// <summary>
		/// Create a new instance of the API, using the given token to
		/// authenticate.
		/// </summary>
		/// <param name="token">The access token used for authentication</param>
		public FBApi( string token ) {
			AccessToken = token;
		}

		#region Generalized Facebook API Calls

#if movedtoJSONRetrieve

		protected Url ConstructUrl( string relativePath, NameValueCollection args, bool isSecure ) {
			var url = new Url( isSecure ? Url.Schemes.https : Url.Schemes.http, "graph.facebook.com", relativePath, null );
			if( !string.IsNullOrEmpty( AccessToken ) ) {
				if( args == null )
					args = new NameValueCollection( 1 );
				args["access_token"] = AccessToken;
			}
			if( args != null )
				url.QueryString = args;
			return url;
		}

		/// <summary>
		/// Makes a Facebook Graph API GET request.
		/// </summary>
		/// <param name="relativePath">The path for the call,
		/// e.g. /username</param>
		public JSONObject Get( string relativePath ) {
			return GetRequest( Construct( relativePath, null, true ).ToString(), false );
		}

		/// <summary>
		/// Makes a Facebook Graph API GET request.
		/// </summary>
		/// <param name="relativePath">The path for the call,
		/// e.g. /username</param>
		/// <param name="args">A dictionary of key/value pairs that
		/// will get passed as query arguments.</param>
		public JSONObject Get( string relativePath, NameValueCollection args ) {
			return GetRequest( Construct( relativePath, args, true ).ToString(), false );
		}

		/// <summary>
		/// Makes a Facebook Graph API DELETE request.
		/// </summary>
		/// <param name="relativePath">The path for the call,
		/// e.g. /username</param>
		protected JSONObject Delete( string relativePath ) {
			return GetRequest( Construct( relativePath, null, true ).ToString(), true );
		}

		/// <summary>
		/// Makes a Facebook Graph API POST request.
		/// </summary>
		/// <param name="relativePath">The path for the call,
		/// e.g. /username</param>
		protected JSONObject Post( string relativePath, NameValueCollection args ) {
			return PostRequest( Construct( relativePath, args, true ) );
		}

		protected static JSONObject CheckJSON( JSONObject json ) {
			if( json.IsDictionary && json.Dictionary.ContainsKey( "error" ) ) {
				throw new Exception( string.Concat( json["error"]["type"].String, ": ", json["error"]["message"].String ) );
			}
			return json;
		}

		/// <summary>
		/// Make an HTTP request, with the given query args
		/// </summary>
		protected JSONObject PostRequest( Url url ) {

			HttpWebRequest request = WebRequest.Create( url.ToString() ) as HttpWebRequest;
			request.Method = "POST";

			string postData = url.Query;

			ASCIIEncoding encoding = new ASCIIEncoding();
			byte[] postDataBytes = encoding.GetBytes( postData );

			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = postDataBytes.Length;

			using( Stream requestStream = request.GetRequestStream() ) {
				requestStream.Write( postDataBytes, 0, postDataBytes.Length );
				requestStream.Close();
			}

			string res = null;
			using( HttpWebResponse response = request.GetResponse() as HttpWebResponse ) {
				using( StreamReader reader = new StreamReader( response.GetResponseStream() ) ) {
					res = reader.ReadToEnd();
				}
			}

			if( string.IsNullOrEmpty( res ) )
				return null;
			JSONObject json = null;
			try {
				json = JSONObject.Create( res );
			}
			catch {
				json = JSONObject.SetString( res );
			}
			return CheckJSON( json );
		}


		/// Makes a Facebook Graph API GET request.
		protected JSONObject Get( string url ) {
			return CheckJSON(Request( "GET", url, null ));
		}
		protected JSONObject Post( string url, NameValueCollection postargs ) {
			return CheckJSON(Request( "POST", url, postargs ));
		}
		protected JSONObject Post( string url) {
			return CheckJSON(Request( "DELETE", url, null ));
		}
		/// <summary>
		/// Make an HTTP request, with the given query args
		/// </summary>
		protected JSONObject JSONRequest( string method, string url, NameValueCollection args, string UserAgent=null ) {
			HttpWebRequest request = WebRequest.Create( url ) as HttpWebRequest;
			request.Method = method;
			if( args != null ) {
				byte[] postDataBytes = Encoding.UTF8.GetBytes( args.ToString() );
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = postDataBytes.Length;

				using( Stream requestStream = request.GetRequestStream() ) {
					requestStream.Write( postDataBytes, 0, postDataBytes.Length );
					requestStream.Close();
				}
			}
			string res = null;
			using( HttpWebResponse response = request.GetResponse() as HttpWebResponse ) {
				using( StreamReader reader = new StreamReader( response.GetResponseStream() ) ) {
					res = reader.ReadToEnd();
				}
			}
			if( string.IsNullOrEmpty( res ) )
				return null;
			JSONObject json = null;
			try {
				json = JSONObject.Create( res );
			}
			catch {
				json = JSONObject.SetString( res );
			}
			return CheckJSON( json );
		}
#endif

		#endregion

		#region Support Facebook Calls

		public const string USER_AGENT = "FBApi";



		protected static JSONObject CheckJSON( JSONObject json ) {
			if( json.IsDictionary && json.Dictionary.ContainsKey( "error" ) ) {
				throw new Exception(  string.Concat( json["error"]["type"].String, ": ", json["error"]["message"].String ) );
			}
			return json;
		}

		/// <summary>
		/// Construct Path for Request to Facebook graph api: combine namevaluecollection into returned urlpath
		/// </summary>
		public static string FBPath( string objname, NameValueCollection qs, string AccessToken = null ) {
			if( string.IsNullOrEmpty( objname ) )
				return null;
			if( objname[0] != '/' )
				objname = '/' + objname;
			if( !string.IsNullOrEmpty( AccessToken ) ) {
				if( qs == null )
					qs = new NameValueCollection( 1 );
				qs.Add( "access_token", AccessToken );
			}
			return "https://graph.facebook.com" + WebRetrieve.AppendQuery( objname, qs );
		}
		/// <summary>
		/// Construct Path for Request to Facebook graph api: add access token into returned urlpath, if required
		/// </summary>
		public static string FBPath( string objname, string AccessToken ) {
			return FBPath( objname, null, AccessToken );
		}

		public JSONObject GetObject( string objname ) {
			return new JSONRetrieve( USER_AGENT, CheckJSON ).Get( FBPath( objname, null ) );
		}
		public JSONObject GetObjectWithToken( string objname ) {
			return new JSONRetrieve( USER_AGENT, CheckJSON ).Get( FBPath( objname, AccessToken ) );
		}
		public JSONObject PostObjectWithToken( string objname, NameValueCollection postargs ) {
			return new JSONRetrieve( USER_AGENT, CheckJSON ).Post( FBPath( objname, AccessToken ), postargs );//!! do I need AT?
		}
		public JSONObject DeleteObjectWithToken( string objname, NameValueCollection postargs ) {
			return new JSONRetrieve( USER_AGENT, CheckJSON ).Delete( FBPath( objname, AccessToken ), postargs );//!! do I need AT?
		}
		public JSONObject UploadToObjectWithToken( string objname, NameValueCollection postargs, string FileContentRef, string filename ) {
			return new JSONRetrieve( USER_AGENT, CheckJSON ).MultipartUploadFile( FBPath( objname, AccessToken ), FileContentRef, filename, postargs );//!! do I need AT?
		}

		#endregion

		#region Get Public Objects

		public JSONObject GetUser( string user, bool publicView = true ) { return publicView ? GetObject( user ) : GetObjectWithToken( user ); }
		public JSONObject GetPage( string id, bool publicView = true ) { return publicView ? GetObject( id ) : GetObjectWithToken( id ); }
		public JSONObject GetGroup( string id, bool publicView = true ) { return publicView ? GetObject( id ) : GetObjectWithToken( id ); }
		public JSONObject GetApplication( string id, bool publicView = true ) { return publicView ? GetObject( id ) : GetObjectWithToken( id ); }
		public JSONObject GetPhoto( string id, bool publicView = true ) { return publicView ? GetObject( id ) : GetObjectWithToken( id ); }
		public JSONObject GetAlbum( string id, bool publicView = true ) { return publicView ? GetObject( id ) : GetObjectWithToken( id ); }

		#region These require an AccessToken basis

		public JSONObject GetEvent( string id ) { return GetObjectWithToken( id ); }
		public JSONObject GetStatusMessage( string id ) { return GetObjectWithToken( id ); }
		public JSONObject GetVideo( string id ) { return GetObjectWithToken( id ); }
		public JSONObject GetNote( string id ) { return GetObjectWithToken( id ); }

		#endregion

		#region Support Objects

		public enum ProfileSizes { Default, small, normal, large };
		/// <summary>
		/// Returns url to small default profile picture
		/// </summary>
		public string GetProfilePicture( string profile, ProfileSizes sizes = ProfileSizes.Default, bool secure = false ) {
			if( string.IsNullOrEmpty( profile ) )
				return null;
			if( profile[0] != '/' )
				profile = '/' + profile;
			profile += "/Picture";
			var args = new NameValueCollection( 2 );
			if( sizes != ProfileSizes.Default )
				args["type"] = sizes.ToString();
			if( secure )
				args["return_ssl_resources"] = "1";
			var res = args.Count > 0 ? GetObject( FBPath( profile, args ) ).String : GetObject( profile ).String;
			if( string.IsNullOrEmpty( res ) || res[0] == '{' )
				return null;
			return res;
		}
		public bool GetCheckin( string id ) {
			string truefalse = GetObjectWithToken( id ).String;
			bool res = false;
			bool.TryParse( truefalse, out res );
			return res;
		}
		public JSONObject GetID( string objref ) { return GetObject( "?ids=" + objref ); }

		public enum SearchTypes { Default, home, post, user, page, Event, group, place, checkin };

		public JSONObject Search( string query, SearchTypes type = SearchTypes.Default, NameValueCollection Extras = null ) {
			var args = new NameValueCollection( 2 );
			if( !string.IsNullOrEmpty( query ) )
				args["q"] = query;
			switch( type ) {
			case SearchTypes.Default:
				break;
/*				case SearchTypes.Place:
					// To search for objects near a geographical location, use type=location and add the center and distance parameters: https://graph.facebook.com/search?type=location&center=37.76,-122.427&distance=1000
					// To search for objects at a particular place, use type=location and specify the ID of the place. For example for Posts at Facebook HQ, use: https://graph.facebook.com/search?type=location&place=166793820034304
					args["type"] = type.ToString().ToLower();
					break;*/
			case SearchTypes.Event:
				args["type"] = "event";
				break;
			default:
				args["type"] = type.ToString();
				break;
			}
			if( Extras != null )
				args.Add( Extras );

			return this.GetObjectWithToken( FBPath("search", args) );
		}

		#endregion
		#endregion

		#region Get Private Profile Objects

		// limit, offset: https://graph.facebook.com/me/likes?limit=3
		// until, since (a unix timestamp or any date accepted by strtotime): https://graph.facebook.com/search?until=yesterday&q=orange

		public string GetMyProfileID( ) { 
			var json = GetObjectWithToken( "/me?fields=id" );
			return json.IsString ? json.String : json["id"].String;
		}
		public JSONObject GetMyProfile( string fields = null ) { return GetObjectWithToken( string.IsNullOrEmpty( fields ) ? "/me" : "/me?fields=" + fields ); }
		public JSONObject GetMyFriends() { return GetObjectWithToken( "/me/friends" ); }
		public JSONObject GetMyNewsFeed( bool location = false ) { return GetObjectWithToken( location ? "/me/home" : "/me/home?with=location" ); }
		public JSONObject GetMyWall( bool location = false ) { return GetObjectWithToken( location ? "/me/feed" : "/me/feed?with=location" ); }
		public JSONObject GetMyPosts( bool location = false ) { return GetObjectWithToken( location ? "/me/posts" : "/me/posts?with=location" ); }
		public JSONObject GetMyLikes() { return GetObjectWithToken( "/me/likes" ); }
		public JSONObject GetMyMovies() { return GetObjectWithToken( "/me/movies" ); }
		public JSONObject GetMyMusic() { return GetObjectWithToken( "/me/music" ); }
		public JSONObject GetMyBooks() { return GetObjectWithToken( "/me/books" ); }
		public JSONObject GetMyNotes() { return GetObjectWithToken( "/me/notes" ); }
		public JSONObject GetMyPhotos() { return GetObjectWithToken( "/me/photos" ); }
		public JSONObject GetMyAlbums() { return GetObjectWithToken( "/me/albums" ); }
		public JSONObject GetMyVideos() { return GetObjectWithToken( "/me/videos" ); }
		public JSONObject GetMyVideoUploads() { return GetObjectWithToken( "/me/videos/uploaded" ); }
		public JSONObject GetMyEvents() { return GetObjectWithToken( "/me/events" ); }
		public JSONObject GetMyGroups() { return GetObjectWithToken( "/me/groups" ); }
		public JSONObject GetMyCheckins() { return GetObjectWithToken( "/me/checkins" ); }
		public JSONObject GetMyLocations() { return GetObjectWithToken( "/me/locations" ); }
		public JSONObject GetMyPermissions() { return GetObjectWithToken( "/me/permissions" ); }
		public JSONObject GetMyPermissions( string permissionname ) { return GetObjectWithToken( "/me/permissions/" + permissionname ); }

		#endregion

		#region Get/Create/Update/Delete Public Profile Objects

		/// <summary>
		/// Can be used to get user, event, ...
		/// </summary>
		public JSONObject GetProfile(string profile) { return GetObjectWithToken( '/' + profile ); }
		public JSONObject CreateProfile( string profile, NameValueCollection nv ) { return PostObjectWithToken( '/' + profile, nv ); }
		public JSONObject UpdateProfile( string profile, NameValueCollection nv ) { return PostObjectWithToken( '/' + profile, nv ); }
		public JSONObject DeleteProfile( string profile, NameValueCollection nv ) { return DeleteObjectWithToken( '/' + profile, nv ); }
		public JSONObject UploadToProfile( string profile, NameValueCollection nv, string filename, string fileContentRef = "source" ) { return UploadToObjectWithToken( '/' + profile, nv, fileContentRef, filename ); }
		public JSONObject GetFQL( string select ) { return GetObjectWithToken( "/FQL?q=" + select ); }
	
		#endregion

		public const int MaxProfileNameLength = 75;

	}


	public static class FBPermissions {
		/* List of all Permissions from 9/2012
		   http://developers.facebook.com/docs/authentication/permissions/  */

		/*
			user_about_me	friends_about_me	Provides access to the "About Me" section of the profile in the about property
			user_activities	friends_activities	Provides access to the user's list of activities as the activities connection
			user_birthday	friends_birthday	Provides access to the birthday with year as the birthday property
			user_checkins	friends_checkins	Provides read access to the authorized user's check-ins or a friend's check-ins that the user can see. This permission is superseded by user_status for new applications as of March, 2012.
			user_education_history	friends_education_history	Provides access to education history as the education property
			user_events	friends_events	Provides access to the list of events the user is attending as the events connection
			user_groups	friends_groups	Provides access to the list of groups the user is a member of as the groups connection
			user_hometown	friends_hometown	Provides access to the user's hometown in the hometown property
			user_interests	friends_interests	Provides access to the user's list of interests as the interests connection
			user_likes	friends_likes	Provides access to the list of all of the pages the user has liked as the likes connection
			user_location	friends_location	Provides access to the user's current location as the location property
			user_notes	friends_notes	Provides access to the user's notes as the notes connection
			user_photos	friends_photos	Provides access to the photos the user has uploaded, and photos the user has been tagged in
			user_questions	friends_questions	Provides access to the questions the user or friend has asked
			user_relationships	friends_relationships	Provides access to the user's family and personal relationships and relationship status
			user_relationship_details	friends_relationship_details	Provides access to the user's relationship preferences
			user_religion_politics	friends_religion_politics	Provides access to the user's religious and political affiliations
			user_status	friends_status	Provides access to the user's status messages and checkins. Please see the documentation for the location_post table for information on how this permission may affect retrieval of information about the locations associated with posts.
			user_subscriptions	friends_subscriptions	Provides access to the user's subscribers and subscribees
			user_videos	friends_videos	Provides access to the videos the user has uploaded, and videos the user has been tagged in
			user_website	friends_website	Provides access to the user's web site URL
			user_work_history	friends_work_history	Provides access to work history as the work property
			email	N/A	Provides access to the user's primary email address in the email property. Do not spam users. Your use of email must comply both with Facebook policies and with the CAN-SPAM Act.
			read_friendlists	Provides access to any friend lists the user created. All user's friends are provided as part of basic data, this extended permission grants access to the lists of friends a user has created, and should only be requested if your application utilizes lists of friends.
			read_insights	Provides read access to the Insights data for pages, applications, and domains the user owns.
			read_mailbox	Provides the ability to read from a user's Facebook Inbox.
			read_requests	Provides read access to the user's friend requests
			read_stream	Provides access to all the posts in the user's News Feed and enables your application to perform searches against the user's News Feed
			xmpp_login	Provides applications that integrate with Facebook Chat the ability to log in users.
			ads_management	Provides the ability to manage ads and call the Facebook Ads API on behalf of a user.
			create_event	Enables your application to create and modify events on the user's behalf
			manage_friendlists	Enables your app to create and edit the user's friend lists.
			manage_notifications	Enables your app to read notifications and mark them as read. Intended usage: This permission should be used to let users read and act on their notifications; it should not be used to for the purposes of modeling user behavior or data mining. Apps that misuse this permission may be banned from requesting it.
			user_online_presence	Provides access to the user's online/offline presence
			friends_online_presence	Provides access to the user's friend's online/offline presence
			publish_checkins	Enables your app to perform checkins on behalf of the user.
			publish_stream	Enables your app to post content, comments, and likes to a user's stream and to the streams of the user's friends. This is a superset publishing permission which also includes publish_actions. However, please note that Facebook recommends a user-initiated sharing model. Please read the Platform Policies to ensure you understand how to properly use this permission. Note, you do not need to request the publish_stream permission in order to use the Feed Dialog, the Requests Dialog or the Send Dialog.
			rsvp_event	Enables your application to RSVP to events on the user's behalf
			publish_actions	N/A	Allows your app to publish to the Open Graph using Built-in Actions, Achievements, Scores, or Custom Actions. Your app can also publish other activity which is detailed in the Publishing Permissions doc. Note: The user-prompt for this permission will be displayed in the first screen of the Enhanced Auth Dialog and cannot be revoked as part of the authentication flow. However, a user can later revoke this permission in their Account Settings. If you want to be notified if this happens, you should subscribe to the permissions object within the Realtime API.
			user_actions.music	friends_actions.music	Allows you to retrieve the actions published by all applications using the built-in music.listens action.
			user_actions.news	friends_actions.news	Allows you to retrieve the actions published by all applications using the built-in news.reads action.
			user_actions.video	friends_actions.video	Allows you to retrieve the actions published by all applications using the built-in video.watches action.
			user_actions:APP_NAMESPACE	friends_actions:APP_NAMESPACE	Allows you retrieve the actions published by another application as specified by the app namespace. For example, to request the ability to retrieve the actions published by an app which has the namespace awesomeapp, prompt the user for the users_actions:awesomeapp and/or friends_actions:awesomeapp permissions.
			user_games_activity	friends_games_activity	Allows you post and retrieve game achievement activity.
			manage_pages	Enables your application to retrieve access_tokens for Pages and Applications that the user administrates. The access tokens can be queried by calling /<user_id>/accounts via the Graph API. This permission is only compatible with the Graph API, not the deprecated REST API. 
			See here for generating long-lived Page access tokens that do not expire after 60 days.
		*/
		//!! Finish this section - ,user_events,user_location,rsvp_event,offline_access"
		public const string UserEvents = "user_events";
		public const string UserRSVPEvent = "rsvp_event";
		public const string CreateEvent = "create_event";
		public const string OfflineAccess = "offline_access";

		public static string Join( params string[] perms ) {
			return string.Join( ",", perms );
		}

	}

}
