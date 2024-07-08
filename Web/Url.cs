//#define TestCode
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Text;
using System.Web;


/*	Code Copyright Michael Dannov 2010-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */
///	Version 1.7 - 8/15/13 - New additions, performance improvements, bug fixes
///	Version 1.6 - 9/30/12 - Clarification of scheme=null vs empty for Absolute path definition, performance improvements
///	Version 1.5 - 9/30/12 - Comprehensive unit testing, Add variation to support Command alternate instead of filename
///	Version 1.3 - 9/21/12 - Added Set* Api for appending Url construction: eg. var url = new Url().SetPath().SetFilename();
///	Version 1.3 - 8/30/12 - Added shortcut extensions for Uri
///	Version 1.2 - 2/15/12 - Mores testing of Url/UriPath
///	Version 1.1 - 2011    - Fixed 
/// Version 1.0 - 2010    - All Uri & UriPath

namespace MDTools.Web {

	/// <summary>
	/// Fast Parses of minimal http url components
	/// eg. http://www.example.com/over/there/index.dtb?type=animal;name=ferret
	/// </summary>
	public class Url {

		#region Construction
		public Url() {
			_reset();
		}
		private void _reset() {
			scheme = string.Empty; // when empty, ToString() excludes scheme and does not display with "//" when host is available
			host = string.Empty;
			pathOnly = string.Empty;// this is the only case where pathOnly is not "/"
			filename = string.Empty;
			query = string.Empty;
			url = null;
			splitpath = null;
		}
		/// <summary>
		/// Convert from a fullpath url - This will not work with relative uris
		/// </summary>
		public Url( string url ) { Parse( url ); }
		private Url( SerializationInfo info, StreamingContext context ) { Parse( info.GetString( "Url" ) ); }
		public Url( HttpContext Context ) : this( Context.Request.Url ) { }
		public Url( HttpRequest Request ) : this( Request.Url ) { }
		/// <summary>
		/// Convert from a URI - This will not work with relative uris
		/// </summary>
		public Url( Uri uri ) {
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.scheme = uri.Scheme;//eg. foo
			this.host = uri.Authority;
			setPath( uri.AbsolutePath, 0, uri.AbsolutePath.Length );//eg. /over/there/index.dtb
			query = ( uri.Query.Length <= 0 ) ? string.Empty : uri.Query.Substring( 1 ); // removes '?'
			this.url = uri.ToString();
		}
		/// <summary>
		/// Convert from a URI - This will not work with relative uris
		/// </summary>
		public Url( Url url ) {
			// Direct copy
			this.scheme = url.scheme;
			this.host = url.host;
			this.pathOnly = url.pathOnly;
			this.filename = url.filename;
			this.query = url.query;
			this.url = url.url;
		}
		public Url( string path, string query ) {
			Parse( path );
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			/*			if(!string.IsNullOrEmpty(query)) {
							NameValueCollection qs = QueryString;
							if( !string.IsNullOrEmpty( query ) )
								qs.Add( HttpUtility.ParseQueryString( query ) );
							this.query = ToQueryString(qs, false);
						}*/
			this.query = ( query == null ) ? string.Empty : query;
		}
		public Url( string scheme, string host, string pathandquery ) {
			this.url = null;// notifies url reconstruction
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.scheme = scheme;//eg. foo
			this.host = ( host == null ) ? string.Empty : host;
			if( pathandquery == null )
				this.pathOnly = string.Empty;
			else
				setPathAndQuery( pathandquery, 0, pathandquery.Length );//eg. /over/there/index.dtb
		}
		public Url( string scheme, string host, string path, string query ) {
			this.url = null;// notifies url reconstruction
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.scheme = scheme;//eg. foo
			this.host = ( host == null ) ? string.Empty : host;
			if( path == null )
				this.pathOnly = string.Empty;
			else
				setPath( path, 0, path.Length );//eg. /over/there/index.dtb
			this.query = ( query == null ) ? string.Empty : query;
		}
		public Url( string scheme, string host, string path, string filename, string query ) {
			this.url = null; // notifies url reconstruction
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.scheme = scheme;//eg. foo
			this.host = host == null ? string.Empty : host;
			if( string.IsNullOrEmpty( path ) )
				this.pathOnly = "/";
			else
				Path = path; //forces ending with '/'
			this.filename = filename == null ? string.Empty : filename;
			this.query = query == null ? string.Empty : query;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Parse basic http urls from a url string 
		/// eg. http://www.example.com/over/there/index.dtb?type=animal;name=ferret
		/// 
		/// Does not handle ports, username/pw, fragments or non-http/s schemes. 
		/// Use FullUri for more complex and/or other schemes.
		/// </summary>
		public Url Parse( string url ) {
			this.url = null;// force rebuild because url in may not be well-formed
			// Fully resolves Query
			if( string.IsNullOrEmpty( url ) ) {
				_reset();
				return this;
			}
			int pos = url.IndexOf( '?' );
			if( pos < 0 ) {
				// No query found
				query = string.Empty;
				Split_Scheme_FullPath( url, url.Length );
				return this;
			}
			/// eg. http://www.example.com/over/there/index.dtb?
			// Handle Left Side = Path Portion 
			Split_Scheme_FullPath( url, pos );// eg. http://www.example.com/over/there/index.dtb
			// Handle Right Side - Query Portion 
			query = url.Substring( pos + 1 ); //eg. type=animal;name=ferret
			return this;
		}

		/// <summary>
		/// Returns a String that represents the current URL.
		/// </summary>
		/// <returns>Returns a String that represents the current URL.</returns>
		public new string ToString() {
			if( !string.IsNullOrEmpty( url ) )
				return url;
			StringBuilder sb = new StringBuilder( 500 );
			if( Absolute )
				getAbsolute( sb );
			// eg. /over/there/index.dtb?type=animal;name=ferret
			getPathAndQuery( sb );
			// eg. http://www.example.com/over/there/index.dtb?type=animal;name=ferret
			this.url = sb.ToString();// cache for next time
			return this.url;
		}

		public string ToRoot() {
			if( string.IsNullOrEmpty( url ) )
				return url;
			StringBuilder sb = new StringBuilder( 500 );
			if( Absolute )
				getAbsolute( sb );
			sb.Append( '/' );
			return sb.ToString();
		}

		public bool Equals( Url url2 ) {
			if( object.ReferenceEquals( this, url2 ) )
				return true;
			return this.ToString().Equals( url2.ToString() );
		}
		public bool Equals( string url2 ) {
			return this.ToString().Equals( url2 );
		}

		#endregion

		#region Fast URL Build Support

		/// <summary>
		/// Replace the Scheme -- Set to empty if you wish to urls to expand with "//" when host is provided, null to exclude
		/// </summary>
		public Url SetScheme( string scheme ) {
			this.Scheme = scheme;
			return this;
		}
		/// <summary>
		/// Replace the Host
		/// </summary>
		public Url SetHost( string host ) {
			this.Host = host;
			return this;
		}
		/// <summary>
		/// Replace the Domain
		/// </summary>
		public Url SetDomain( string domain ) {
			this.Domain = domain;
			return this;
		}
		/// <summary>
		/// Replace the SubDomain
		/// </summary>
		public Url SetSubDomain( string subdomain ) {
			this.SubDomain = subdomain;
			return this;
		}
		/// <summary>
		/// Replace the TopLevelDomain
		/// </summary>
		public Url SetTopLevelDomain( string topleveldomain ) {
			this.TopLevelDomain = topleveldomain;
			return this;
		}
		/// <summary>
		/// Replace the full Path
		/// </summary>
		public Url SetPath( string path ) {
			this.Path = path;
			return this;
		}
		/// <summary>
		/// Replace the Path Only
		/// </summary>
		public Url SetPathOnly( string pathonly ) {
			this.PathOnly = pathonly;
			return this;
		}
		/// <summary>
		/// Replace the PathAndQuery
		/// </summary>
		public Url SetPathAndQuery( string pathandquery ) {
			this.PathAndQuery = pathandquery;
			return this;
		}
		/// <summary>
		/// Replace the Filename
		/// </summary>
		public Url SetFilename( string filename ) {
			this.Filename = filename;
			return this;
		}
		/// <summary>
		/// Replace the Extension
		/// </summary>
		public Url SetExtension( string extension ) {
			this.Extension = extension;
			return this;
		}
		/// <summary>
		/// Replace the query
		/// </summary>
		public Url SetQuery( string query ) {
			this.Query = query;
			return this;
		}
		/// <summary>
		/// Replace the query
		/// </summary>
		public Url SetQueryString( NameValueCollection qs ) {
			this.QueryString = qs;
			return this;
		}
		/// <summary>
		/// Remove specified list of keys from Query
		/// </summary>
		/// <param name="qs">List of keys</param>
		public Url RemoveQS( params string[] qs ) {
			if( qs == null || qs.Length == 0 )
				return this;
			var QS = HttpUtility.ParseQueryString( this.query );
			bool changed = false;
			for( int i = 0, len = qs.Length; i < len; i++ ) {
				var q = qs[i];
				if( string.IsNullOrEmpty( q ) )
					continue;
				QS.Remove( q );
				changed = true;
			}
			if( changed ) {
				this.query = ToQueryString( QS, false );
				this.url = null;
			}
			return this;
		}
		/// <summary>
		/// Append to the QueryString - (fast) call only when you know you will not duplicate a key
		/// </summary>
		public Url AppendQS( string key, string value ) {
			// Check that key is valid
			if( string.IsNullOrEmpty( key ) )
				return this;
			if( value == null )
				value = string.Empty;
			this.query = string.IsNullOrEmpty( this.query ) ? string.Empty : this.query + '&';
			this.query += ( key + '=' + value );
			this.url = null;
			return this;
		}
		public Url AppendQS( string query ) {
			if( string.IsNullOrEmpty( query ) )
				return this;
			if( string.IsNullOrEmpty( this.query ) )
				this.query = query;
			else
				this.query = string.Concat( this.query, "&", query );
			this.url = null;
			return this;
		}
		/// <summary>
		/// Add or Replace (if existing) key in QueryString
		/// </summary>
		/// <param name="qs">key then value, then key then ... (if value is null or missing, value is set to empty)</param>
		public Url SetQS( params string[] kv ) {
			// Check that key is valid
			if( kv == null )
				return this;
			var len = kv.Length;
			if( len == 0 || string.IsNullOrEmpty( kv[0] ) )
				return this;
			var qs = this.QueryString;
			for( int i = 0; i < len; i++ ) {
				var key = kv[i++];
				if( string.IsNullOrEmpty( key ) )
					continue;
				var value = i > len ? null : kv[i];
				qs[key] = ( value == null ) ? string.Empty : value;
			}
			this.QueryString = qs;
			return this;
		}
		/// <summary>
		/// Add or Replace (if existing) or Remove (if value null) key in QueryString
		/// </summary>
		/// <param name="qs">key then value, then key then ... (if value is null or missing, value is deleted)</param>
		public Url SetAndRemoveQS( params string[] kv ) {
			// Check that key is valid
			if( kv == null )
				return this;
			var len = kv.Length;
			if( len == 0 || string.IsNullOrEmpty( kv[0] ) )
				return this;
			var qs = this.QueryString;
			for( int i = 0; i < len; i++ ) {
				var key = kv[i++];
				if( string.IsNullOrEmpty( key ) )
					continue;
				var value = i > len ? null : kv[i];
				if( value == null )
					qs.Remove( key );
				else
					qs[key] = value;
			}
			this.QueryString = qs;
			return this;
		}
		/// <summary>
		/// Set Absolute
		/// </summary>
		public Url SetAbsolute( bool absolute ) {
			this.Absolute = absolute;
			return this;
		}
		/// <summary>
		/// Set Secure
		/// </summary>
		public Url SetSecure( bool secure ) {
			this.Secure = secure;
			return this;
		}
		#endregion

		#region Common Schemes

		public static class Schemes {
			public const string http = "http";
			public const string https = "https";
			public const string ftp = "ftp";
			// expand...
		}
		#endregion

		#region Common TopLevelDomains

		public static class TopLevelDomains {
			public const string dotcom = ".com";
			public const string dotnet = ".net";
			public const string dotorg = ".org";
			public const string dotbiz = ".biz";
			public const string dotus = ".us";
			public const string dotca = ".ca";
			public const string dotinfo = ".info";
			public const string dotmobi = ".mobi";
			public const string dottv = ".tv";
			public const string dotme = ".me";
			public const string dotmx = ".mx";
			public const string dotws = ".ws";
			public const string dotag = ".ag";
			public const string dotit = ".it";
			public const string dotfr = ".fr";
			public const string dotam = ".am";
			public const string dotasia = ".asia";
			public const string dotat = ".at";
			public const string dotbe = ".be";
			public const string dotbz = ".bz";
			public const string dotcc = ".cc";
			public const string dotde = ".de";
			public const string dotes = ".es";
			public const string doteu = ".eu";
			public const string dotfm = ".fm";
			public const string dotgs = ".gs";
			public const string dotjobs = ".jobs";
			public const string dotjp = ".jp";
			public const string dotms = ".ms";
			public const string dotnl = ".nl";
			public const string dotnu = ".nu";
			public const string dottc = ".tc";
			public const string dottk = ".tk";
			public const string dottw = ".tw";
			public const string dotvg = ".vg";
		}

		#endregion

		#region Main Properties

		/// <summary>
		/// Protocol/Scheme - If Scheme is null, "//" will be prepended to urls with a Host, empty will exclude
		/// eg. foo in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Scheme {
			get { return scheme; }
			set {
				// Performance optimization considerations since scheme is frequently changed (only if url is already calculated)
				if( url != null ) {
					// only url prepend if host is valid
					if( !string.IsNullOrEmpty( host ) ) {
						if( scheme == null ) {	// eg. url="//www.google.com/..."
							// scheme already null so url is prefixed with "//"
							if( value != null ) {
								if( value.Length > 0 )
									// new value is valid; prepend new scheme
									url = string.Concat( value, ":", url );
								else
									// new value is empty; remove "//"
									url = url.Substring( 2 );
							} //else { }
							// new value is also null; no change
						} else {
							if( scheme.Length > 0 ) {// eg. url="http://www.google.com/..."
								// scheme already has value so url is prefixed with "scheme://"
								var pos = url.IndexOf( Uri.SchemeDelimiter );
								if( value == null ) {
									// new value is null; remove scheme past ":"
									url = url.Substring( pos + 1 );
								} else {
									if( value.Length == 0 )
										// new value is empty: remove scheme + "://"
										url = url.Substring( pos + 3 );
									else
										// new value has scheme; replace scheme
										url = value + url.Substring( pos );
								}
							} else {// eg. url="www.google.com/..."
								if( value == null ) {
									// new value is null; add "//"
									url = "//" + url;
								} else {
									if( value.Length > 0 )
										// new value has scheme; add scheme and delime
										url = string.Concat( value, Uri.SchemeDelimiter, url );
									//else { }
									// new value is also empty; no change
								}
							}
						}
					}
				}
				scheme = value;
			}
		}
		/// <summary>
		/// Host
		/// eg. www.example.com in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Host { get { return host; } set { url = null; host = value; } }
		/// <summary>
		/// PathOnly portion of Path - always at least / (unless entire url is just initialized empty), should always end with /
		/// eg. /over/there/ [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string PathOnly {
			get { return pathOnly; }
			set {
				if( value == null ) {
					pathOnly = "/";
					return;
				}
				int len = value.Length;
				pathOnly = ( ( len == 0 ) ? "/" : ( value[len - 1] != '/' ) ? value + '/' : value );	// ensure ending with '/'
				url = null;// notify url reconstruct
				splitpath = null;
			}
		}
		/// <summary>
		/// True if path is root; also may be used to Set Path to Root
		/// </summary>
		public bool IsRoot { get { return PathOnly == "/"; } set { if( value ) PathOnly = "/"; } }
		/// <summary>
		/// Filename (eg. Default in http://www.domain.com/Default.html) = filenames must have extensions otherwise they are part of the path
		/// eg. index [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Filename { get { return filename; } set { url = null; filename = value; } }
		/// <summary>
		/// Query
		/// eg. type=animal;name=ferret in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Query { get { return query; } set { url = null; query = value; } }

		#endregion

		#region Support Properties

		/// <summary>
		/// If Protocol is not set, it could be a relative path or from Root
		/// </summary>
		public bool Absolute {
			get { return !string.IsNullOrEmpty( host ); }
			set {
				if( !string.IsNullOrEmpty( host ) ) {
					// Setting absolute to false, removes host and scheme
					if( value == false ) {
						host = string.Empty;
						url = null;
					} // else {}
					// Host has a value so setting absolute to true does nothing and is safe
				} else if( value == true )
					// Host is empty, so trying to set it to true is impossible
					throw new ArgumentException( "Set Host to make absolute" );
			}
		}
		/// <summary>
		/// Switch scheme from Secure to non or v.v.
		/// </summary>
		public bool Secure {
			get { return !string.IsNullOrEmpty( scheme ) ? scheme.EndsWith( "s" ) : false; }
			set {
				// Throw exception if scheme is not known
				if( string.IsNullOrEmpty( scheme ) )
					throw new ArgumentException( "Scheme must be set" );
				bool isSecure = scheme.EndsWith( "s" );
				// Check if no change
				if( isSecure == value )
					return;
				if( value ) {
					// Change from nonsecure to secure - add "s"
					scheme = scheme + "s";
				} else {
					// Change from secure to nonsecure - remove "s"
					scheme = scheme.Substring( scheme.Length - 1 );
				}
			}
		}
		/// <summary>
		/// Domain
		/// eg. example.com in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Domain {
			get {
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) // 1
					return string.Empty;
				int p2 = host.LastIndexOf( '.', p1 - 1 );
				if( p2 < 0 )//2
					return host;
				return host.Substring( p2 + 1 );
			}
			set {
				if( IsLocalhost )
					throw new ArgumentException( "SubDomain cannot be set in localhost" );
				url = null;// notify url reconstruct
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) {// "google"
					host = value;
					return;
				}
				int p2 = host.LastIndexOf( '.', p1 - 1 );
				if( p2 < 0 ) {// "google.com"
					host = value;
					return;
				}
				host = host.Substring( 0, p1 ) + '.' + value;
			}
		}
		/// <summary>
		/// Top Level Domain
		/// eg. .com in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string TopLevelDomain {
			get {
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) // 1
					return string.Empty;
				return host.Substring( p1 );
			}
			set {
				if( IsLocalhost )
					throw new ArgumentException( "TopLevelDomain cannot be set in localhost" );
				url = null;// notify url reconstruct
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) {
					host = host + '.' + value;
					return;
				}
				host = host.Substring( 0, p1 + 1 ) + value;
			}
		}
		/// <summary>
		/// SubDomain
		/// eg. www in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string SubDomain {
			get {
				//!! check if split('.') is faster
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) // 1
					return string.Empty;
				int p2 = host.LastIndexOf( '.', p1 - 1 );
				if( p2 < 0 )//2
					return string.Empty;
				return host.Substring( 0, p2 );
			}
			set {
				if( IsLocalhost )
					throw new ArgumentException( "SubDomain cannot be set in localhost" );
				url = null;// notify url reconstruct
				int p1 = host.LastIndexOf( '.' );
				if( p1 < 0 ) {//"google"
					host = value + '.' + host;
					return;
				}
				int p2 = host.LastIndexOf( '.', p1 - 1 );
				if( p2 < 0 ) {//"google.com"
					host = value + '.' + host;
					return;
				}
				host = value + '.' + host.Substring( 0, p2 );
			}
		}
		/// <summary>
		/// Returns if domain is localhost
		/// </summary>
		public bool IsLocalhost { get { return host != null && host.StartsWith( "localhost" ); } }
		/// <summary>
		/// Path portion of Path
		/// eg. /over/there/index.dtb [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Path {
			get {
				var sb = new StringBuilder( 120 );
				getPath( sb );
				return sb.ToString();
			}
			set {
				url = null;// notify url reconstruct
				splitpath = null;
				setPath( value, 0, value.Length );
			}
		}
		public string PathOnly_DeLocalized {
			get {
				if( !IsLocalhost )
					return pathOnly;
				int p = pathOnly.IndexOf( '/', 1 );
				if( p < 0 )
					return pathOnly;
				return pathOnly.Substring( p );
			}
			set {
				url = null;// notify url reconstruct
				splitpath = null;
				if( !IsLocalhost )
					pathOnly = value;
				else {
					int p = pathOnly.IndexOf( '/', 1 );
					pathOnly = ( p < 0 ) ? value : pathOnly.Substring( 0, p - 1 ) + value;
				}
			}
		}
		/// <summary>
		/// Extension - includes first dot
		/// eg. dtb [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Extension {
			get {
				int p = filename.IndexOf( '.' );
				return ( p < 0 ) ? string.Empty : filename.Substring( p + 1 );
			}
			set {
				if( string.IsNullOrEmpty( value ) )
					return;
				var ext = value[0] == '.' ? value : '.' + value;	// add dot if not present
				int p = filename.IndexOf( '.' );
				filename = p < 0 ? filename + ext : filename.Substring( 0, p - 1 ) + value;
				url = null;// notify url reconstruct
			}
		}
		/// <summary>
		/// Path & Query portions of Path
		/// eg. /over/there/index.dtb?type=animal;name=ferret#nose 
		///		[in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string PathAndQuery {
			get {
				var sb = new StringBuilder( 280 );
				getPathAndQuery( sb );
				return sb.ToString();
			}
			set {
				setPathAndQuery( value, 0, value.Length );
				url = null;// notify url reconstruct
				splitpath = null;
			}
		}
		/// <summary>
		/// Default port of scheme
		/// eg. /over/there/index.3.dtb [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public virtual int DefaultPort {
			get {
				switch( scheme ) {
				case Schemes.http:
					return 80;
				case Schemes.https:
					return 443;
				}
				return 0;
			}
		}
		/// <summary>
		/// Get a NavmeValueCollection QueryString from the Query
		/// </summary>
		public NameValueCollection QueryString {
			get { return query != null ? HttpUtility.ParseQueryString( query ) : new NameValueCollection(); }
			set { this.url = null; Query = ( value == null || value.Count == 0 ? string.Empty : ToQueryString( value, false ) ); }
		}
		/// <summary>
		/// Get a NavmeValueCollection QueryString from the Query
		/// </summary>
		public string[] SplitPath {
			get {
				int len = PathOnly.Length;
				if( len <= 0 )
					return null;
				if( splitpath != null )
					return splitpath;
				int start = 0;
				if( pathOnly[0] == '/' ) {
					if( len == 1 )
						return splitpath = new string[] { "/" };
					start = 1;
				}
				if( len >= 2 && pathOnly[len - 1] == '/' )
					len--;
				var path = pathOnly.Substring( start, len - start );// remove first and last '/'
				if( path.Length <= 0 )
					return null;
				return splitpath = path.Split( '/' );
			}
			set {
				url = null;// notify url reconstruct
				int len = ( value == null ? 0 : value.Length );
				switch( len ) {
				case 0:
					pathOnly = "/";
					break;
				case 1:
					pathOnly = value[0];
					break;
				default:
					StringBuilder sb = new StringBuilder( len * 10 );
					for( int i = 0; i < len; i++ ) {
						sb.Append( '/' );
						sb.Append( value[i] );
					}
					sb.Append( '/' );
					pathOnly = sb.ToString();
					break;
				}
				splitpath = value;
				return;
			}
		}

		public string VirtualPath {
			get { return '~' + PathAndQuery; }
			set {
				if( value.Length < 0 ) {
					pathOnly = "/";
					filename = string.Empty;
					query = string.Empty;
					return;
				}
				string path = ( value[0] == '~' ) ? value.Substring( 1 ) : value;
				setPathAndQuery( path, 0, path.Length );
			}
		}

		#endregion

		#region Internal Support Functions

		//!! check if it's faster to call reset or leave unassigned
		protected string scheme;
		protected string host;
		protected string pathOnly;
		protected string filename;
		protected string query;
		protected string url;	// internal cache of last rendered output -reset to null on edits
		protected string[] splitpath;

#if whatisthis
		protected void getHost( StringBuilder sb ) {
			if( !string.IsNullOrEmpty( subDomain ) ) {
				sb.Append( subDomain );
				sb.Append('.');
			}
			sb.Append(domain);
		}
		protected void setHost( string str, int start, int count ) {
			// Fully Resolves SubDomain & Domain
			int pos = str.IndexOf( '.', start, count );//first dot
			if( pos <= 0 ) {
				domain = str.Substring( start, count );
				if( !IsLocalhost )
					// Host name is not valid
					throw new ArgumentException("Host name is not properly formatted");
				subDomain = string.Empty;
				return;
			}
			int countdot = count - ( pos - start )-1;
			int lastpos = str.IndexOf( '.', pos+1, countdot );//last dot
			if( lastpos<0 ) {
				// no subdomain
				domain = str.Substring(start, count);//whole
				subDomain = string.Empty;
				return;
			}
			//start  pos  start+count-1
			//|_______|_______________|
			//len=pos-start+1
			//         len=start+count-pos
			// eg. www.example.com
			subDomain = str.Substring( start, pos-start );//eg. www
			domain = str.Substring( pos + 1, countdot );// eg. example.com
		}
#endif
		protected void getAbsolute( StringBuilder sb ) {
			// If host has no value, there is nothing to add
			if( string.IsNullOrEmpty( host ) )
				return;
			// Host is assigned, so append
			if( scheme == null )
				sb.Append( "//" );
			else if( scheme.Length > 0 ) {
				// eg. http://
				sb.Append( scheme );
				sb.Append( Uri.SchemeDelimiter );
			}
			// eg. www.example.com
			sb.Append( host );
		}

		protected void getPath( StringBuilder sb ) {
			if( !string.IsNullOrEmpty( pathOnly ) ) {
				sb.Append( pathOnly );
				if( !string.IsNullOrEmpty( filename ) ) {
					/*					if( pathOnly[pathOnly.Length - 1] != '/' )	// last character of pathonly isn't '/'
											sb.Append( '/' );*/
					sb.Append( filename );
				}
			}
		}
		protected void setPath( string str, int start, int count ) {
			// Fully Resolves PathOnly and Filename - filename must contain dot to be considered a filename
			// Test Variants: test [path:/test/ filename:]; /test [path:/test/ filename:]
			//		/test/file [path:/test/file filename:]; /test/file.pl [path:/test/ filename:file.pl]; 
			//		/test.test/file [path:/test.test/file filename:]; /test.test/file.pl [path:/test.test/ filename:file.pl]; 
			if( count == 0 ) {
				// Nothing to process, therefore empty
				filename = string.Empty;
				pathOnly = "/";
				return;
			}
			int pos = str.LastIndexOf( '/', start + count - 1, count );
			int dot = str.LastIndexOf( '.', start + count - 1, count );
			if( dot < 0 || pos > dot ) { // dot not found or dot appears before slash
				// No extension, therefore no filename
				filename = string.Empty;
				pathOnly = str.Substring( start, count );// eg. /over/there/
				if( pos < start + count - 1 )
					pathOnly += '/';
				if( pos < 0 )
					pathOnly = '/' + pathOnly;
				return;
			}
			//Extension discovered
			if( pos < 0 ) {
				// Slash not found so entire thing is filename
				pathOnly = "/";
				filename = str.Substring( start, count );// all
				return;
			}
			// include the last '/'
			pathOnly = str.Substring( start, pos - start + 1 );// eg. /over/there/
			filename = str.Substring( pos + 1, count - ( pos - start ) - 1 );//eg. index
		}
		protected virtual void getPathAndQuery( StringBuilder sb ) {
			getPath( sb );
			if( !string.IsNullOrEmpty( query ) ) {
				sb.Append( '?' );
				sb.Append( query );
			}
		}
		protected virtual void setPathAndQuery( string str, int start, int count ) {
			if( count == 0 ) {
				pathOnly = "/";
				filename = string.Empty;
				query = string.Empty;
				return;
			}
			// Fully resolves Query
			int pos = str.IndexOf( '?', start, count );
			if( pos < 0 ) {
				// No query found
				setPath( str, start, count );// whole
				query = string.Empty;
				return;
			}
			/// eg. /over/there/index.dtb?
			// Handle Left Side = Path Portion 
			setPath( str, start, pos - start );// eg. /over/there/index.dtb
			// Handle Right Side - Query Portion 
			query = str.Substring( pos + 1 ); //eg. type=animal;name=ferret
		}
		protected void Split_Scheme_FullPath( string str, int count ) {
			// Fully Resolves Scheme
			// eg. http://www.example.com/test //www.example.com/test www.example.com /test 
			int pos = str.IndexOf( Uri.SchemeDelimiter, 0, count );// Uri.SchemeDelimiter = "://"
			if( pos < 0 ) {
				// No schemedelimiter
				Split_Domain_Path( str, 0, count );// whole
				scheme = string.Empty;
				return;
			}
			scheme = str.Substring( 0, pos );// eg. http
			Split_Domain_Path( str, pos + 1, count - pos - 1 );// eg. //www.example.com/over/there/index.dtb
#if delete
			// Verify next part is "//"
			if( count - pos > 2 && str[pos + 2] == '/' && str[pos + 3] == '/' ) {
				// we have a host in string
				// eg. http://www.example.com/over/there/index.dtb
				return;
			} 
			// host not found
			//int schlen = Uri.SchemeDelimiter.Length;// +3
			setPath( str, pos + 1, count - pos );
#endif
		}
		protected void Split_Domain_Path( string str, int start, int count ) {
			// Fully Resolves Host
			// eg. //www.example.com/test www.example.com /test
			// Verify next part is "/" or "//" or empty
			if( count - start > 2 && str[start] == '/' && str[start + 1] == '/' ) {
				// domain found
				start += 2;
				count -= 2;
			}
			int pos = str.IndexOf( '/', start, count );// first '/'
			if( pos < 0 ) {
				// No path found, so only host
				host = str.Substring( start, count );//whole
				pathOnly = "/";
				filename = string.Empty;
				return;
			}
			// eg. www.example.com/over/there/index.dtb
			host = str.Substring( start, pos - start );// eg. www.example.com
			// include the first '/'
			setPath( str, pos, start + count - pos );// eg. /over/there/index.dtb
		}
		/*
				[SecurityCritical]
				protected virtual void GetObjectData( SerializationInfo info, StreamingContext context ) {
					info.AddValue( "Url", ToString() );
				}
		//		[SecurityPermissionAttribute( SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter )]
				[SecurityCritical]
				void ISerializable.GetObjectData( SerializationInfo info, StreamingContext context ) {
					if( info == null )
						throw new ArgumentNullException( "info" );
					GetObjectData( info, context );
				} */
		#endregion

		#region Static Helpers

		public static string ToQueryString( NameValueCollection qs, bool prefix = false ) {
			return qs.ToQueryString( prefix );
		}
		/// <summary>
		/// Construct an appropriate domain root url - null scheme will produce //domain
		/// </summary>
		public static string ToRoot( string scheme, string host ) {
			if( scheme == null )
				// Scheme is null so prepend "//"
				return string.Concat( "//", host, "/" );
			if( scheme.Length > 0 )
				// Scheme has value so prepend scheme + "://"
				return string.Concat( scheme, Uri.SchemeDelimiter, host, "/" );
			// Scheme is empty so just send host
			return host + '/';
		}
		public static string ToRoot( HttpContext Context ) {
			return ToRoot( null, Context.Request.Url.Host );
		}
		public static string ToRoot( HttpRequest Request ) {
			return ToRoot( null, Request.Url.Host );
		}
#if nevermind
		public static string ToPath( string scheme, string host, params string[] path ) {
			StringBuilder sb = new StringBuilder( 1024 );
//			bool prefix = path.StartsWith( "/" );
			if( scheme == null )
				// Scheme is null so prepend "//"
				sb.Append( "//" );
			else if( scheme.Length > 0 ) {
				// Scheme has value so prepend scheme + "://"
				sb.Append( scheme );
				sb.Append( Uri.SchemeDelimiter);
			}
			sb.Append( host );
			int len = path.Length;
			if(len==0)
				sb.Append('/');
			else
			for( int i = 0; i < len; i++ ) {
				var p = path[i];
				if( p == null )
					continue;
				int plen = p.Length;
				if( plen==0 )
					continue;
				if(p[0]=='/')
					sb.Append(p);
				else {
					sb.Append( '/' );
					sb.Append( p );
				}
			}
			// Scheme is empty so just send host and path
			return sb.ToString();
		}
#endif
		/// <summary>
		/// Construct a full Url string without instantiating an object or parsing - constructed from valid differentiated parameters
		/// </summary>
		public static string ToString( string scheme, string host, string path, string query = null ) {
			var prefix = path.StartsWith( "/" ) ? string.Empty : "/";
			bool queryavail = !string.IsNullOrEmpty( query );
			if( string.IsNullOrEmpty( host ) )
				return queryavail ?
					string.Concat( prefix, path, "?", query ) :
					prefix + path;
			if( scheme == null )
				// Scheme is null so prepend "//"
				return queryavail ?
					string.Concat( "//", host, prefix, path, "?", query ) :
					string.Concat( "//", host, prefix, path );
			// Scheme has value so prepend scheme + "://"
			if( scheme.Length > 0 )
				return queryavail ?
					string.Concat( scheme, Uri.SchemeDelimiter, host, prefix, path, "?", query ) :
					string.Concat( scheme, Uri.SchemeDelimiter, host, prefix, path );
			// Scheme is empty so just send host and path
			return queryavail ?
				string.Concat( host, prefix, path, "?", query ) :
				string.Concat( host, prefix, path );
		}
		/// <summary>
		/// Append a querystring to a path string if exists - delimits '?' between strings
		/// </summary>
		public static string AppendQuery( string path, string query = null ) {
			bool prefix = !path.StartsWith( "/" );
			if( string.IsNullOrEmpty( query ) )
				return prefix ? '/' + path : path;
			return prefix ?
					string.Concat( "/", path, "?", query ) :
					string.Concat( path, "?", query );
		}
		/// <summary>
		/// Append a Path - delimits '/' between two strings - can be used to combine path and filename or append to a path or domain
		/// </summary>
		public static string AppendPath( string path, string append ) {
			if( string.IsNullOrEmpty( path ) )
				return append;
			return ( path[path.Length - 1] == '/' ) ?
				path + append :
				string.Concat( path, "/", append );
		}

		#endregion

	}

	/// <summary>
	/// Parses a given string representation of a URI into its components and optionally reassembles these components back to a string representation.
	/// Details here: http://en.wikipedia.org/wiki/URI_scheme
	/// 
	///   foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
	///   \ /   \_______________/ \_____________/ \__/\__________/       \_/ \_____________________/ \__/
	///    |           |          \_/       | \_/   |      |      \______|/          |                |
	///    |       userinfo        |  \_____|__|/ port  pathonly    |    |         query          fragment
	///    |    \__________________|_____|__|__|_____/\_____________|____|/
	/// scheme                  |  |     |  |  |             |      |    |
	///                     authority    | host|            path    |    |
	///                            |   domain  |                    |    |
	///                         subdomain      |               filename  |
	///                                  topleveldomain     command-+    |
	///                                                      _|_         |
	///													    /   \    extension
	///   foo://username:password@www.example.com:8042/over/there?type=animal;name=ferret#nose
	/// </summary>
	public class FullUri : Url {

		#region Construction
		public FullUri()
			: base() {
			port = 0;
			username = string.Empty;
			password = string.Empty;
			fragment = string.Empty;
		}
		public FullUri( HttpContext Context ) : this( Context.Request.Url ) { }
		public FullUri( string url ) { Parse( url ); }
		/// <summary>
		/// Convert from a URI - This may not work with relative uris
		/// </summary>
		public FullUri( Uri uri ) {
			this.url = uri.ToString();// this operation is fastest way to save cache
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			scheme = uri.Scheme;//eg. foo
			setAuthority( uri.Authority, 0, uri.Authority.Length );//eg. username:password@www.example.com:8042
			setPath( uri.AbsolutePath, 0, uri.AbsolutePath.Length );//eg. /over/there/index.dtb
			setFullQuery( uri.Query, 0, uri.Query.Length );//eg. type=animal;name=ferret#nose
		}
		public FullUri( string scheme, string host, string path, string query ) {
			this.url = null;
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.scheme = scheme;//eg. foo
			setAuthority( host, 0, host.Length );//eg. username:password@www.example.com:8042
			setPath( path, 0, path.Length );//eg. /over/there/index.dtb
			setFullQuery( query, 0, query.Length );//eg. type=animal;name=ferret#nose
		}
		private FullUri( SerializationInfo info, StreamingContext context ) { Parse( info.GetString( "Url" ) ); }
		#endregion

		#region Public Methods
		// Separate All Parts - Parse_SAP_QF
		protected new void Parse( string url ) {
			this.url = url;
			// Break url into its parts
			int pos = url.IndexOf( '?' );
			if( pos < 0 ) {
				// No query found
				Split_Scheme_AuthPath( url, url.Length );
				return;
			}
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			// Handle Left Side = Path Portion
			Split_Scheme_AuthPath( url, pos + 1 );// eg. foo://username:password@www.example.com:8042/over/there/index.dtb
			// Handle Right Side - Query Portion
			pos++;
			setFullQuery( url, pos, url.Length - pos );// eg. type=animal;name=ferret#nose

			/*			// Attempt at faster version
						/// <summary>
						/// Breaks apart a uri from a string into its Url parts
						/// </summary>
						/// <param name="url">The URL path.</param>
						if( string.IsNullOrEmpty(url) )
							throw new ArgumentNullException();
						int pathpos = url.IndexOf(Uri.SchemeDelimiter, 0 );
						if( pathpos > 0 ) {
							// Found Protocol
							Protocol = url.Substring( 0, pathpos );
							pathpos = Uri.SchemeDelimiter.Length; // move to begining of path
						} else
							// No protocol
							pathpos = 0;
						// Right Side of ? Query Half
						int querpos = url.IndexOf( '?', pathpos );
						if( querpos < 0 ) {
							Query = string.Empty;
							querpos = url.Length-1;// point to end of string
						} else {
							Query = url.Substring( querpos + 1 );
						}
						// Left Side of ?
						url = url.Substring(pathpos, querpos-pathpos );
						int atpos = url.IndexOf( '@' );
						if(atpos>=0) {
							// Left-side of @
							// username and/or password available
							int pwpos = url.IndexOf( ':', 0, atpos );
							if(pwpos<0) {
								// Only username
								Username = url.Substring(0, atpos);
							} else {
								// Both username and pw
								Username = url.Substring(0, pwpos);
								Password = url.Substring(pwpos+1);
							}
						} else
							atpos=0;	// reset to beginning

						// Right-side of @ (or none)
						int pwpos = url.IndexOf( ':', atpos );

						/// ... incomplete /untested
				*/
		}

		/// <summary>
		/// Returns a String that represents the current URL.
		/// </summary>
		/// <returns>Returns a String that represents the current URL.</returns>
		public new string ToString() {
			if( !string.IsNullOrEmpty( url ) )
				return url;
			StringBuilder sb = new StringBuilder( 500 );
			if( Absolute ) {
				// eg. foo://
				sb.Append( scheme );
				sb.Append( Uri.SchemeDelimiter );
				// eg. username:password@www.example.com:8042
				getAuthority( sb );
			}
			// eg. /over/there/index.dtb?type=animal;name=ferret#nose
			getPathAndQuery( sb );
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose
			this.url = sb.ToString();
			return this.url;

		}

		public Uri ToUri() { return new Uri( ToString() ); }

		#endregion

		#region Common Schemes
		public new static class Schemes {
			public const string http = "http";
			public const string https = "https";
			public const string ftp = "ftp";
			public const string ssh = "ssh";
			public const string telnet = "telnet";
			public const string smtp = "smtp";
			public const string gopher = "gopher";
			public const string pop3 = "pop3";
			public const string nntp = "nntp";
			public const string news = "news";
			public const string sftp = "sftp";
		}
		#endregion

		#region Main Properties
		/// <summary>
		/// Port (only if part of URL)
		/// eg. 8042 in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public int Port { get { return port; } set { url = null; port = value; } }
		/// <summary>
		/// Username (if applicable)
		/// eg. username in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Username { get { return username; } set { url = null; username = value; } }
		/// <summary>
		/// Password (if applicable, if username present)
		/// eg. password in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Password { get { return password; } set { url = null; password = value; } }
		/// <summary>
		/// Fragment
		/// eg. nose in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Fragment { get { return fragment; } set { url = null; fragment = value; } }

		#endregion

		#region Support Properties

		/// <summary>
		/// Port if available, otherwise passes the default port for that scheme (if found)
		/// eg. 80 in [foo://username:password@www.example.com/]
		/// </summary>
		public int PortOrDefault { get { return ( port == 0 ) ? DefaultPort : port; } }
		/// <summary>
		/// Authority
		/// eg. username:password in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string UserInfo {
			get {
				var sb = new StringBuilder( 120 );
				getUserInfo( sb, false );
				return sb.ToString();
			}
			set {
				url = null;
				setUserInfo( value, 0, value.Length );
			}
		}
		/// <summary>
		/// Authority
		/// eg. username:password@www.example.com:8042
		///		in [foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string Authority {
			get {
				var sb = new StringBuilder( 120 );
				getAuthority( sb );
				return sb.ToString();
			}
			set {
				url = null;
				setAuthority( value, 0, value.Length );
			}
		}
		/// <summary>
		/// FullQuery
		/// eg. type=animal;name=ferret#nose [in foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose]
		/// </summary>
		public string FullQuery {
			get {
				var sb = new StringBuilder( 120 );
				getFullQuery( sb, false );
				return sb.ToString();
			}
			set {
				url = null;
				setFullQuery( value, 0, value.Length );
			}
		}
		public override int DefaultPort {
			get {
				string[] protocols = new string[] { "http", "https", "ftp", "ssh", "telnet", "smtp", "gopher", "pop3", "nntp", "news", "sftp", "ssl", "tls" };
				int[] ports = new int[] { 80, 443, 21, 22, 23, 25, 70, 110, 119, 119, 22, 990, 990 };
				int index = Array.IndexOf( protocols, scheme.ToLower(), 0, protocols.Length );
				if( index == -1 )
					return 0; // not a default protocol
				else
					return ports[index];
			}
		}
		#endregion

		#region Internal Support Functions

		protected int port;
		protected string username;
		protected string password;
		protected string fragment;

		protected void getUserInfo( StringBuilder sb, bool append ) {
			if( !string.IsNullOrEmpty( username ) ) {
				sb.Append( username );
				if( !string.IsNullOrEmpty( password ) ) {
					sb.Append( ':' );
					sb.Append( password );
				}
				if( append )
					sb.Append( '@' );
			}
		}
		protected void setUserInfo( string str, int start, int count ) {
			// Fully Resolves Username & Password
			int pos = str.IndexOf( ':', start, count );
			if( pos < 0 ) {
				// No password found 
				username = str.Substring( start, count );// whole
				password = string.Empty;
				return;
			}
			// eg. username:password 
			username = str.Substring( start, pos - start );//eg. username
			password = str.Substring( pos + 1, count - ( pos - start ) - 1 );//eg. password
		}
		protected void getAuthority( StringBuilder sb ) {
			getUserInfo( sb, true );
			sb.Append( host );
			if( port > 0 ) {
				sb.Append( ':' );
				sb.Append( port );
			}
		}
		protected void setAuthority( string str, int start, int count ) {
			int pos = str.IndexOf( '@', start, count );
			if( pos < 0 ) {
				// No userinfo
				Split_Host_Port( str, start, count );// whole
				username = string.Empty;
				password = string.Empty;
				return;
			}
			// eg. username:password@www.example.com:8042
			setUserInfo( str, start, pos - start );//eg. username:password
			Split_Host_Port( str, pos + 1, count - ( pos - start ) - 1 );//eg. www.example.com:8042
		}
		protected override void getPathAndQuery( StringBuilder sb ) {
			getPath( sb );
			getFullQuery( sb, true );
		}
		protected override void setPathAndQuery( string str, int start, int count ) {
			// Fully resolves Query
			int pos = str.IndexOf( '?', start, count );
			if( pos < 0 ) {
				// No query found
				setPath( str, start, count );// whole
				query = string.Empty;
				fragment = string.Empty;
				return;
			}
			// eg. /over/there/index.dtb?type=animal;name=ferret#nose
			// Handle Left Side = Path Portion 
			setPath( str, start, pos - start );// eg. /over/there/index.dtb
			// Handle Right Side - Query Portion 
			setFullQuery( str, pos + 1, count - ( pos - start ) - 1 );// eg. type=animal;name=ferret#nose
		}

		protected void Split_Host_Port( string str, int start, int count ) {
			// Fully Resolves Port and Host
			int pos = str.IndexOf( ':', start, count );
			if( pos < 0 ) {
				// No Port
				host = str.Substring( start, count );//whole
				port = 0;
				return;
			}
			// eg. www.example.com:8042
			host = str.Substring( start, pos - start );//eg. www.example.com
			port = Convert.ToInt32( str.Substring( pos + 1, count - ( pos - start ) - 1 ) );//eg. 8042
		}
		protected void Split_Scheme_AuthPath( string str, int count ) {
			// Fully Resolves Scheme
			int pos = str.IndexOf( Uri.SchemeDelimiter, 0, count );
			if( pos < 0 ) {
				// No schemedelimiter
				Split_Authority_Path( str, 0, count );// whole
				scheme = string.Empty;
				return;
			}
			// eg. foo://username:password@www.example.com:8042/over/there/index.dtb
			scheme = str.Substring( 0, pos );// eg. foo
			int schlen = Uri.SchemeDelimiter.Length;
			Split_Authority_Path( str, pos + schlen, count - pos - schlen - 1 );// eg. username:password@www.example.com:8042/over/there/index.dtb
		}
		protected void Split_Authority_Path( string str, int start, int count ) {
			int pos = str.IndexOf( '/', start, count );// first '/'
			if( pos < 0 ) {
				// No path found
				setAuthority( str, start, count );// whole
				pathOnly = string.Empty;
				filename = string.Empty;
				return;
			}
			// eg. username:password@www.example.com:8042/over/there/index.dtb
			setAuthority( str, start, pos - start );// eg. username:password@www.example.com:8042
			// include the first '/'
			setPath( str, pos, count - ( pos - start ) );// eg. /over/there/index.dtb
		}
		protected void getFullQuery( StringBuilder sb, bool prefix ) {
			if( !string.IsNullOrEmpty( query ) ) {
				if( prefix )
					sb.Append( '?' );
				sb.Append( query );
			}
			if( !string.IsNullOrEmpty( fragment ) ) {
				sb.Append( '#' );
				sb.Append( fragment );
			}
		}
		protected void setFullQuery( string str, int start, int count ) {
			// Fully Resolves Query & Fragment
			int pos = str.IndexOf( '#', start, count );
			if( pos < 0 ) {
				// No query found
				query = str.Substring( start, count );// whole
				fragment = string.Empty;
				return;
			}
			// eg. type=animal;name=ferret#nose
			query = str.Substring( start, pos - start );// eg. type=animal;name=ferret
			fragment = str.Substring( pos + 1 );// eg. nose
		}
		#endregion

#if TestCode
		public static void PerfTime() {

			//var urlstr = "foo://username:password@www.example.com:8042/over/there/index.dtb?type=animal;name=ferret#nose";
			var urlstr = "http://www.example.com/over/there/index.dtb?type=animal;name=ferret";
			FullUri fUri = null;
			Url url = null;
			Uri uri=null;
			UriBuilder newurl = null;
			string res;
			DateTime then;
			string time;

			uri = new Uri( urlstr );
			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				url = new Url(uri);
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
//			url.PathAndQuery="/test.aspx?test=1#fragement";

/*			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				newurl = new UriBuilder( urlstr );
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				res = newurl.Host;
				res = newurl.ToString();
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
*/
			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				fUri = new FullUri( urlstr );
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
//			fUri.PathAndQuery = "/test.aspx?test=1#fragement";

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				res = fUri.Host;
				res = fUri.ToString();
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				url = new Url( urlstr );
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				res = url.Host;
				res = url.ToString();
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				uri = new Uri( urlstr );
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();

			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				res = uri.Host;
				res = uri.ToString();
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
			// Test Variants: test [path:/test/ filename:]; /test [path:/test/ filename:]
			//		/test/file [path:/test/file filename:]; /test/file.pl [path:/test/ filename:file.pl]; 
			//		/test.test/file [path:/test.test/file filename:]; /test.test/file.test [path:/test.test/ filename:file.test]; 
			var t1 = new Url( "http://www.scenecalendar.com" );
			var t2 = new Url( "http://www.scenecalendar.com/" );
			var t3 = new Url( "http://www.scenecalendar.com/test" );
			var t4 = new Url( "http://www.scenecalendar.com/test/" );
			var t5 = new Url( "http://www.scenecalendar.com/test.test/" );
			var t6 = new Url( "http://www.scenecalendar.com/test.test/test" );
			var t7 = new Url( "http://www.scenecalendar.com/test.test/file.pl" );
		}
#endif
	}

	/// <summary>
	/// Summary description for Uri
	/// </summary>
	public static class UriPath {

		#region Uri Extensions

		public static string SubDomain( this Uri uri ) {
			var host = uri.Host;
			int p1 = host.LastIndexOf( '.' );
			if( p1 < 0 ) // 1
				return string.Empty;
			int p2 = host.LastIndexOf( '.', p1 - 1 );
			if( p2 < 0 )//2
				return string.Empty;
			return host.Substring( 0, p2 );
		}
		public static string Domain( this Uri uri ) {
			var host = uri.Host;
			int p1 = host.LastIndexOf( '.' );
			if( p1 < 0 ) // 1
				return string.Empty;
			int p2 = host.LastIndexOf( '.', p1 - 1 );
			return ( p2 < 0 ) ? host : host.Substring( p2 + 1 );
		}
		public static string TopLevelDomain( this Uri uri ) {
			var host = uri.Host;
			int p1 = host.LastIndexOf( '.' );
			return ( p1 < 0 ) ? string.Empty : host.Substring( p1 );
		}
		public static string Path( this Uri uri ) {
			var path = uri.PathAndQuery;
			int p1 = path.IndexOf( '?' );
			return ( p1 < 0 ) ? path : path.Substring( 0, p1 );
		}
		public static string PathOnly( this Uri uri ) {
			var path = uri.PathAndQuery;
			int p1 = path.LastIndexOf( '/' );//!! Not reliable if / appears inside a querystring
			if( p1 < 0 ) // 1
				return "/";
			return path.Substring( 0, p1 + 1 );
		}
		public static string Filename( this Uri uri ) {
			var path = uri.PathAndQuery;
			int p1 = path.LastIndexOf( '/' );//!! Not reliable if / appears inside a querystring
			int p2 = path.IndexOf( '?', p1+1 );
			if( p2 < 0 )
				return path.Substring( p1 < 0 ? 0 : p1 + 1 );
			else
				return path.Substring( p1 < 0 ? 0 : p1 + 1, p2 - p1 - 1 );
		}
		public static string Extension( this Uri uri ) {
			var path = uri.PathAndQuery;
			int p1 = path.LastIndexOf( '.' );//!! Not reliable if . appears inside a querystring 
			if( p1 < 0 )
				return string.Empty;
			int p2 = path.IndexOf( '?', p1+1 );
			return ( p2 < 0 ) ? path.Substring( p1 + 1 ) : path.Substring( p1 + 1, p2 - p1 - 1 );
		}
		public static void SplitUserInfo( this Uri uri, string username, string password ) {
			var userinfo = uri.UserInfo;
			if( string.IsNullOrEmpty( userinfo ) )
				return;
			var parts = userinfo.Split( ':' );
			if( parts.Length <= 1 ) {
				// No password found 
				username = userinfo;//whole
				password = string.Empty;
				return;
			}
			// eg. username:password 
			username = parts[0];//eg. username
			password = parts[1];//eg. password
		}

		public static string DeLocalPath( this Uri uri ) {
			if( !uri.Host.StartsWith( "localhost" ) )
				return uri.AbsolutePath;
			var absPath = uri.AbsolutePath;
			int p = absPath.IndexOf( '/', 1 );
			return absPath.Substring( p );
		}

		public static string ToQueryString( this NameValueCollection qs, bool prefix = false ) {
			int len = qs.Count;
			if( len == 0 )
				return string.Empty;
			StringBuilder sb = new StringBuilder( len * 15 );
			if( prefix )
				sb.Append( '?' );
			bool bStart = false;
			for( int i = 0; i < len; i++ ) {
				if( bStart )
					sb.Append( '&' );
				sb.Append( qs.GetKey( i ) );
				sb.Append( '=' );
				sb.Append( qs[i] );
				bStart = true;
			}
			return sb.ToString();
		}

		#endregion
	}
}
