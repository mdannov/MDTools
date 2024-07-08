using System;
using System.Configuration;
using System.Web;
using MDTools.Web;
using MDTools.Extension;
using MDTools.Web.Api.Facebook;
using System.Collections;


namespace MDTools.Web {

	[Flags]
	public enum GetFlags : short {
		GetCurrentPage = 0x0001,// sets returnurl to current page
		GetEncodedParameter = 0x0002,// gets parameter returnurl=encoded64url from page url 
		GetParameter = 0x0004,	// gets hash returnurl=url from page url 
		GetEncodedHash = 0x0008,// gets hash returnurl=encoded64url from page url 
		GetHas = 0x0010,	// gets parameter returnurl=url from page url 
		GetCookie = 0x0020,		// gets returnurl from cookie; requires manual cleanup
		GetSession = 0x0040,	// gets returnurl from session - session & cookies must be enabled; requires manual cleanup
		GetCache = 0x0080,		// gets returnurl from cache key combined with sessionid - session must be enabled and cookies supported; requires manual cleanup
#if implemented
		GetPageRefererParameter = 0x0100,//previous page must contain returnurl as parameter - works only when browser supports & firewall doesn't remove
		GetPageReferer = 0x0200,// previous page only - works only when browser supports & firewall doesn't remove
		GetViewstate = 0x0400,	// usable only for postback
#endif
		GetParameterOrCookie = GetEncodedParameter | GetCookie,// most reliable combo: cookie for serverside redirects, parameter for clientside
		GetActionable = 0x7FFE,	// All GetFlags but CurrentPage
		GetAll = 0x7FFF,
	};
	public enum SetEnum : short {
		None=0x0000,
		SetEncodedParameter = 0x0002,// appears in url as ?returnurl=encoded64url
		SetParameter = 0x0004,	// appears in url as ?returnurl=url
		SetEncodedHash = 0x0008,// appears in url as #returnurl=encoded64url
		SetHash = 0x0010,		// appears in url as #returnurl=url
		SetCookie = 0x0020,		// hidden and returned as cookie; requires manual cleanup
		SetSession = 0x0040,	// saves in session - session & cookies must be enabled; requires manual cleanup
		SetCache = 0x0080,		// combined with sessionid - session must be enabled and cookies supported; requires manual cleanup
#if implemented
		SetPageRefererParameter = 0x0100,//previous page must contain returnurl as parameter - works only when browser supports & firewall doesn't remove
		SetPageReferer = 0x0200,// previous page only - works only when browser supports & firewall doesn't remove
		SetViewstate = 0x0400,	// usable only for postback
#endif
		SetSameMethod = 0x0800,	// use the matching Set method as the discovered by Get Method (only if Get returnurl found and matching Set flag passed)
	}

	/// <summary>
	/// Global Settings
	/// </summary>
	public class ReturnUrlFlow {

		public enum EncodeDecode { NoChange = 0, Encode, Decode };

		/// <summary>
		/// If you wish to modify this internal name, do so at the application scope
		/// </summary>
		public static string ReturnUrlKey = "returnurl";

		#region Static Primary Functions

		protected static GetFlags GetEncodedFlags = (GetFlags.GetCookie | GetFlags.GetEncodedParameter);
		public static bool RequiresEncode( GetFlags flags ) {
			// Set if one of the setters
			return ( ( flags & GetEncodedFlags ) > 0 );
		}

		protected static GetFlags GetEmpty = (GetFlags)0;
		protected static GetFlags GetParameterFlags = ( GetFlags.GetParameter | GetFlags.GetEncodedParameter );
		protected static GetFlags GetHashFlags = ( GetFlags.GetHas | GetFlags.GetEncodedHash );
		protected static GetFlags GetNonParameterFlags = ( GetFlags.GetAll & ~GetParameterFlags );

		/// <summary>
		/// Retrieve link using ReturnUrl, get ReturnUrl, pass-thru ReturnUrl, or Set ReturnUrl
		/// </summary>
		/// <param name="Path">Path to Url to return; null returns returnurl or current path if ReturnUrl is null as well</param>
		/// <param name="getflags">Get flags - finds first ReturnUrl in order of GetFlags: Parameter, Cookie, Session, then Cache</param>
		/// <param name="setflags">Set flags - set the appropriate storage method based on SetEnum</param>
		/// <param name="ReturnUrl">Set returnurl path manually; set null to use current page</param>
		/// <param name="encoded">If ReturnUrl passed in is 64bit encoded</param>
		/// <param name="AddQueryToPage">Append query parameters to Path Url</param>
		/// <param name="UrlEncodeQuery">Make appended query parameters UrlEncoded</param>
		public static string LinkTo( HttpContext Context, string Path, GetFlags getflags, SetEnum set, string ReturnUrl, bool encoded, string AddQueryToPage = null, bool UrlEncodeQuery = false ) {
			// Get existing return url
			bool isParam = false;
#if DEBUG
			if( string.IsNullOrEmpty( ReturnUrl ) )
				throw new ArgumentException( "ReturnUrl required" );
#endif
			// Check if encoding or decoding  of returnurl is required
			bool encode = RequiresEncode( getflags );
			if( encode ) {
				if( !encoded ) {
					ReturnUrl = string.IsNullOrEmpty( ReturnUrl ) ? null : EncodeShortUrl( Context, ReturnUrl );
					encoded = true;
				}
			} else {
				// decode
				if( encoded ) {
					ReturnUrl = string.IsNullOrEmpty( ReturnUrl ) ? null : DecodeShortUrl( Context, ReturnUrl );
					encoded = false;
				}
			}
			// Setup Page Url
			string LinkUrl = Path;
			// If querystring was to be added to the Path it should be done before returnurl
			if( ( !string.IsNullOrEmpty( AddQueryToPage ) ) && !string.IsNullOrEmpty( LinkUrl ) ) {
				//!! Need to test for hash # to prevent breakage
				if( UrlEncodeQuery ) {
					if( LinkUrl.IndexOf( "%3F" ) >= 0 ) {
						LinkUrl += ( "%26" + HttpUtility.UrlEncode( AddQueryToPage ) );
						isParam = true;
					} else
						LinkUrl += ( "%3F" + HttpUtility.UrlEncode( AddQueryToPage ) );
				} else {
					if( LinkUrl.IndexOf( '?' ) >= 0 ) {
						LinkUrl += ( '&' + AddQueryToPage );
						isParam = true;
					} else
						LinkUrl += ( '?' + AddQueryToPage );
				}
			}
			// Set storage method for ReturnUrl
			if( !string.IsNullOrEmpty( ReturnUrl ) ) {
				switch( set ) {
				case SetEnum.None:
					break;
				case SetEnum.SetEncodedParameter:
				case SetEnum.SetParameter:
					if( !string.IsNullOrEmpty( LinkUrl ) )
						//!! Need to test for hash # to prevent breakage
						if( UrlEncodeQuery )
							LinkUrl += string.Concat( isParam || LinkUrl.IndexOf( "%3F" ) >= 0 ? "%26" : "%3F", ReturnUrlKey, "%3D", encoded ? ReturnUrl : HttpUtility.UrlEncode( ReturnUrl ) );
						else
							LinkUrl += string.Concat( isParam || LinkUrl.IndexOf( '?' ) >= 0 ? "&" : "?", ReturnUrlKey, "=", ReturnUrl );
					isParam = true;
					break;
				case SetEnum.SetEncodedHash:
				case SetEnum.SetHash:
					if( !string.IsNullOrEmpty( LinkUrl ) )
						LinkUrl += string.Concat( LinkUrl.IndexOf( '#' ) >= 0 ? "&" : "#", ReturnUrlKey, "=", ReturnUrl );
					isParam = true;
					break;
				case SetEnum.SetCookie:
					Context.Response.Cookies[ReturnUrlKey].Value = ReturnUrl;
					break;
				case SetEnum.SetSession:
					Context.Session[ReturnUrlKey] = ReturnUrl;
					break;
				case SetEnum.SetCache:
					Context.Session[ReturnUrlKey + Context.Session.SessionID] = ReturnUrl;
					break;
				default:
					throw new ArgumentException( "Must have an implemented Set flag" );
				}
			}
			return string.IsNullOrEmpty( LinkUrl ) ? ReturnUrl : LinkUrl;
		}
		public static string LinkTo( HttpContext Context, string Path, GetFlags getflags, string ReturnUrl, bool encoded, string AddQueryToPage = null, bool UrlEncodeQuery = false ) {
			return LinkTo( Context, Path, getflags, SetEnum.None, ReturnUrl, encoded, AddQueryToPage, UrlEncodeQuery );
		}
		/// <summary>
		/// Retrieve link using ReturnUrl, get ReturnUrl, pass-thru ReturnUrl, or Set ReturnUrl
		///   Get finds first ReturnUrl in order of Parameter, Cookie, Session, then Cache
		///   Set only first method in order of Cookie, Parameter, Session then Cache
		/// </summary>
		/// <param name="Path">Path to Url to return; null returns returnurl or current path if ReturnUrl is null as well</param>
		/// <param name="flags">Get & Set flags - Get Flags will lookup existing returnurl for pass-thru to set the appropriate storage method based on set flags</param>
		/// <param name="AddQueryToPage">Append querystring to Path Url</param>
		/// <param name="AddQueryToReturnUrl">Append querystring to returnurl</param>
		public static string LinkTo( HttpContext Context, string Path, GetFlags getflags, SetEnum set, string AddQueryToPage = null, string AddQueryToReturnUrl = null, bool UrlEncodeQuery = false ) {
			// This Method requires a set function
			if( set == SetEnum.None )
				// No Set - why are we here
				throw new ArgumentException( "No Set flags - Call GetReturnUrl instead or Pass ReturnUrl string to LinkTo" );
			// Get existing return url
			bool isParam;
			bool encoded;
			// If no ReturnUrl override and GetExisting, then set ReturnUrl
			string ReturnUrl = GetReturnUrl( Context, getflags, ref set, out isParam, out encoded, EncodeDecode.NoChange, AddQueryToReturnUrl );
			return LinkTo( Context, Path, getflags, set, ReturnUrl, encoded, AddQueryToPage, UrlEncodeQuery );
		}
		public static string LinkTo( HttpContext Context, string Path, GetFlags getflags, string AddQueryToPage = null, string AddQueryToReturnUrl = null, bool UrlEncodeQuery = false ) {
			return LinkTo( Context, Path, getflags, SetEnum.None, AddQueryToPage, AddQueryToReturnUrl, UrlEncodeQuery );
		}

		protected static string GetReturnUrl( HttpContext Context, GetFlags getflags, ref SetEnum set, out bool isParam, out bool encoded, EncodeDecode coding = EncodeDecode.Decode, string AddQueryToReturnUrl = null ) {
			bool found = false;
			encoded = false;
			isParam = false;
			// Check ReturnUrl in Parameters
			string ReturnUrl = null;
			if( ( getflags & GetParameterFlags ) > 0 ) {
				ReturnUrl = Context.Request.QueryString[ReturnUrlKey];
				if( ReturnUrl != null ) {
					if( ( getflags & GetFlags.GetEncodedParameter ) == GetFlags.GetEncodedParameter ) {
						if( set == SetEnum.SetSameMethod )
							set = SetEnum.SetEncodedParameter;
						encoded = true;
					} else {
						if( set == SetEnum.SetSameMethod )
							set = SetEnum.SetParameter;
						encoded = false;
					}
					isParam = true;
					found = true;
				}
			}
			if( ( getflags & GetHashFlags ) > 0 ) {
				ReturnUrl = Context.Request.QueryString[ReturnUrlKey];
				if( ReturnUrl != null ) {
					if( ( getflags & GetFlags.GetEncodedHash ) == GetFlags.GetEncodedHash ) {
						if( set == SetEnum.SetSameMethod )
							set = SetEnum.SetEncodedHash;
						encoded = true;
					} else {
						if( set == SetEnum.SetSameMethod )
							set = SetEnum.SetHash;
						encoded = false;
					}
					found = true;
				}
			}
#if implemented
			// Check ReturnUrl in PageRefererParameter
			if( !found && ( flags & ReturnUrlFlags.GetPageRefererParameter ) == ReturnUrlFlags.GetPageRefererParameter ) {
				var referer = Context.Request.UrlReferrer;//.Headers["Referer"];
				if( referer != null ) {
					var url = new Url( ReturnUrl );
					if( !string.IsNullOrEmpty( url.Query ) )
						ReturnUrl = url.QueryString[returnurlName];
					if( ReturnUrl != null ) {
						// returnurl is not encoded
						encoded = false;
						if( ( flags & ReturnUrlFlags.SetSameMethod ) == ReturnUrlFlags.SetSameMethod ) {
						flags = ~(flags & (ReturnUrlFlags.SetSameMethodAll & ~ReturnUrlFlags.SetPageRefererParameter) );// set matching flag, only if allowed (set)
						found = true;
					}
				}
			}
#endif
			// Check ReturnUrl in Cookie //!! use MDTool Cookie classes
			if( !found && ( getflags & GetFlags.GetCookie ) == GetFlags.GetCookie ) {
				var ck = Context.Request.Cookies[ReturnUrlKey];
				if( ck != null ) {
					ReturnUrl = ck.Value;
					encoded = true;// Cookie requires encoding
					if( set == SetEnum.SetSameMethod && ck != null )
						set = SetEnum.SetCookie;
					found = true;
				}
			}
			// Check ReturnUrl in Session
			if( !found && ( getflags & GetFlags.GetSession ) == GetFlags.GetSession ) {
				ReturnUrl = Context.Session[ReturnUrlKey] as string;
				encoded = false;// Session doesn't require encoding
				if( ReturnUrl != null ) {
					if( set == SetEnum.SetSameMethod )
						set = SetEnum.SetSession;
					found = true;
				}
			}
			// Check ReturnUrl in Cache
			if( !found && ( getflags & GetFlags.GetCache ) == GetFlags.GetCache ) {
				ReturnUrl = Context.Cache[ReturnUrlKey + Context.Session.SessionID] as string;
				encoded = false;// Cache doesn't require encoding
				if( ReturnUrl != null ) {
					if( set == SetEnum.SetSameMethod )
						set = SetEnum.SetCache;
					found = true;
				}
			}
			// Last Resort, set returnurl to current page
			if( !found && ( getflags & GetFlags.GetCurrentPage ) == GetFlags.GetCurrentPage && set != SetEnum.SetSameMethod ) {
				ReturnUrl = Url.ToString( Context.Request.Url.Scheme, Context.Request.Url.Host, Context.Request.RawUrl );
				encoded = false;// Cache doesn't require encoding
				found = true;
			}
			// If encoded, ReturnUrl was set
			if( found ) {
				// If AddQuery to end of ReturnUrl
				if( !string.IsNullOrEmpty( AddQueryToReturnUrl ) ) {
					// Need to decode in order to add querystring
					if( encoded ) {
						ReturnUrl = DecodeShortUrl( Context, ReturnUrl );
						encoded = false;
					}
					//!! Need to test for hash # to prevent breakage
					ReturnUrl += ( ReturnUrl.IndexOf( '?' ) >= 0 ? '&' : '?' ) + AddQueryToReturnUrl;
				}
				// If setting it and already found encoded, we don't want to decode to reencode
				if( encoded ) {
					if( coding == EncodeDecode.Decode ) {
						ReturnUrl = DecodeShortUrl( Context, ReturnUrl );
						encoded = false;
					}
				} else if( coding == EncodeDecode.Encode ) {
					ReturnUrl = EncodeShortUrl( Context, ReturnUrl );
					encoded = true;
				}
			}
			return ReturnUrl;
		}
		public static string GetReturnUrl( HttpContext Context, out bool isParam, GetFlags getflags = GetFlags.GetAll, EncodeDecode decode = EncodeDecode.Decode, string AddQueryToReturnUrl = null ) {
			bool encoded;
			SetEnum set = SetEnum.None;
			return ReturnUrlFlow.GetReturnUrl( Context, getflags, ref set, out isParam, out encoded, decode, AddQueryToReturnUrl );
		}
		public static string GetReturnUrl( HttpContext Context, GetFlags getflags = GetFlags.GetAll, EncodeDecode decode = EncodeDecode.Decode, string AddQueryToReturnUrl = null ) {
			bool isParam;
			bool encoded;
			SetEnum set = SetEnum.None;
			return ReturnUrlFlow.GetReturnUrl( Context, getflags, ref set, out isParam, out encoded, decode, AddQueryToReturnUrl );
		}
		public static string GetThisUrl( HttpContext Context, EncodeDecode decode = EncodeDecode.Encode, string AddQueryToReturnUrl = null ) {
			bool isParam;
			bool encoded;
			SetEnum set = SetEnum.None;
			return ReturnUrlFlow.GetReturnUrl( Context, GetFlags.GetCurrentPage, ref set, out isParam, out encoded, decode, AddQueryToReturnUrl );
		}

		public static string AppendEncodedReturnUrl( HttpContext Context, string Path, string ReturnUrl=null, string AddQueryToPage = null, bool UrlEncodeQuery = false ) {
			return LinkTo( Context, Path, GetEmpty, SetEnum.SetEncodedParameter, ReturnUrl, true, AddQueryToPage, UrlEncodeQuery );
		}
		public static string AppendReturnUrl( HttpContext Context, string Path, string ReturnUrl = null, string AddQueryToPage = null, bool UrlEncodeQuery = false ) {
			return LinkTo( Context, Path, GetEmpty, SetEnum.SetParameter, ReturnUrl, false, AddQueryToPage, UrlEncodeQuery );
		}

		public static string EncodeShortUrl( HttpContext Context, string path ) {
			// If path contains http 
			if( path!=null && path.Length > 6 )
			if( path.StartsWith( "http", true, null ) ) {
				// Check that path is valid with domain
				bool isSecure = false;
				int len = 0;
				switch( path[4] ) {
				case 's':
				case 'S':
					len=5;
					isSecure=true;
					break;
				case ':':
					len=4;
					isSecure = false;
					break;
				}
				if( len > 0 ) {
					// verify scheme and domain
					var host = Uri.SchemeDelimiter + Context.Request.Url.Host;
					int hlen = host.Length;
					if( string.Compare( path, len, host, 0, hlen, true ) == 0 ) {
						len += hlen+1;
						if(path.Length>len)
							path = ( isSecure ? "$/" : "!/" ) + path.Substring( len );
					}
				}
			}
			return path.EncodeRedirectUrlSafeParam();
		}
		public static string DecodeShortUrl( HttpContext Context, string path ) {
			if( path != null && path.Length > 1 ) {
				path = path.DecodeRedirectUrlSafeParam();
				if( path[1] == '/' )
					switch( path[0] ) {
					case '$':
						path = string.Concat( Url.Schemes.https, Uri.SchemeDelimiter, Context.Request.Url.Host, path.Substring( 1 ) );
						break;
					case '!':
						path = string.Concat( Url.Schemes.http, Uri.SchemeDelimiter, Context.Request.Url.Host, path.Substring( 1 ) );
						break;
					}
			}
			return path;
		}


		/// <summary>
		/// Redirect link using ReturnUrl, get ReturnUrl, pass-forward ReturnUrl, or Set ReturnUrl
		///   Gets find first ReturnUrl in order of parameter, Header, then Cookie
		///   Sets only first method in order of Header, Parameter, then cookie
		/// </summary>
		public static void RedirectTo( HttpContext Context, string Page, GetFlags getflags, SetEnum set, string ReturnUrl = null, bool encoded = false, string AddQueryToPage = null, bool UrlEncodeQuery = false ) {
			var link = LinkTo( Context, Page, getflags, set, ReturnUrl, encoded, AddQueryToPage, UrlEncodeQuery );
			// If set not passed in or is not setparam, clear any other clearable types 
			var clearflags = ConvertGetSetToClearFlags( getflags, set );
			if( clearflags > 0 ) // if noparam set flags and get flags passed
				ClearReturnUrl( Context, clearflags );
			Context.Response.Redirect( link );
		}
		public static void RedirectTo( HttpContext Context, string Page, GetFlags getflags, SetEnum set, string AddQueryToPage = null, string AddQueryToReturnUrl = null, bool UrlEncodeQuery = false ) {
			var link = LinkTo( Context, Page, getflags, AddQueryToPage, AddQueryToReturnUrl, UrlEncodeQuery );
			var clearflags = ConvertGetSetToClearFlags( getflags, set );
			if( clearflags > 0 ) // if noparam set flags and get flags passed
				ClearReturnUrl( Context, clearflags );
			Context.Response.Redirect( link );
		}

		public static void RedirectToReturnUrl( HttpContext Context, GetFlags getflags, GetStringFn GetAltFn ) {
			var ReturnUrl = GetReturnUrl(Context, getflags );
			if( string.IsNullOrEmpty( ReturnUrl ) )
				ReturnUrl = GetAltFn();
			ClearReturnUrl( Context, getflags );
			Context.Response.Redirect( ReturnUrl );
		}
		public static void RedirectToReturnUrl( HttpContext Context, GetFlags getflags = GetFlags.GetAll, string altIfNotFound = "/" ) {
			var ReturnUrl = GetReturnUrl( Context, getflags );
			if( string.IsNullOrEmpty( ReturnUrl ) )
				ReturnUrl = altIfNotFound;
			ClearReturnUrl( Context, getflags );
			Context.Response.Redirect( ReturnUrl );
		}

		public static GetFlags ConvertGetSetToClearFlags( GetFlags getflags, SetEnum set ) {
			// Remove the set matching set flag
			return (GetFlags)((int)getflags & ( ~(int)set ));
		}

		public static void ClearReturnUrl( HttpContext Context, GetFlags getflags, SetEnum set ) {
			ClearReturnUrl(Context, ConvertGetSetToClearFlags( getflags, set ));
		}

		public static void ClearReturnUrl( HttpContext Context, GetFlags clearflags = GetFlags.GetAll ) {
			// Clears each item where Set flagged
			if( ( clearflags & GetFlags.GetCookie ) > 0 )
				if( Context.Request.Cookies[ReturnUrlKey] != null )
					Context.Response.Cookies[ReturnUrlKey].Expires = DateTime.Now.AddMonths( -1 );
			if( ( clearflags & GetFlags.GetSession ) > 0 )
				Context.Session.Remove( ReturnUrlKey );
			if( ( clearflags & GetFlags.GetCache ) > 0 )
				Context.Cache.Remove( ReturnUrlKey + Context.Session.SessionID );
#if implemented
			if( ( flags & ReturnUrlFlags.SetPageRefererParameter ) == ReturnUrlFlags.GetPageRefererParameter )
				Context.Response.Headers.Remove( "Referer" );//!! not sure this does anything
#endif
		}

		public static void RedirectNoReturnUrl( HttpContext Context, string ToUrl = null, GetFlags clearflags = GetFlags.GetAll ) {
			// If ToUrl is not specified, uses same page (refresh)
			ClearReturnUrl( Context, clearflags );
			Context.Response.Redirect( Context.Request.StripReturnUrlParam( ToUrl ) );
		}

	}

	public static class ReturnUrlFlowExt {

		public static string StripReturnUrlParam( this HttpRequest Request, string ToUrl = null ) {
			// If ToUrl is not specified, uses same page (refresh)
			if( string.IsNullOrEmpty( ToUrl ) ) {
				if( !string.IsNullOrEmpty( Request[ReturnUrlFlow.ReturnUrlKey] ) )
					ToUrl = new Url( Request.Url.Scheme, Request.Url.Host, Request.RawUrl ).RemoveQS( ReturnUrlFlow.ReturnUrlKey ).ToString();
				else
					ToUrl = Url.ToString( Request.Url.Scheme, Request.Url.Host, Request.RawUrl );
			} else {
				var url = new Url( ToUrl );
				if( !string.IsNullOrEmpty( url.Query ) )
					ToUrl = url.RemoveQS( ReturnUrlFlow.ReturnUrlKey ).ToString();
			}
			return ToUrl;
		}

		#endregion
	}

	public delegate string GetStringFn();



}
