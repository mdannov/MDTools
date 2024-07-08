using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Web;
using MDTools.Web;

namespace MDTools.Web {
	/// <summary>
	/// Summary description for WebException
	/// </summary>
	public class WebException : HttpException {
		protected string IP = string.Empty;

		public WebException( HttpRequest request ) { this.IP = UserHelper.GetClientIP( request ); }
		public WebException( HttpRequest request, string message ) : base( message ) { this.IP = UserHelper.GetClientIP( request ); }
		public WebException( HttpRequest request, int httpCode, string message, Exception innerException ) : base( httpCode, message, innerException ) { this.IP = UserHelper.GetClientIP( request ); }
		public WebException( HttpRequest request, int httpCode, string message, int hr ) : base( httpCode, message, hr ) { this.IP = UserHelper.GetClientIP( request ); }
		public WebException( HttpRequest request, int httpCode, string message ) : base( httpCode, message ) { this.IP = UserHelper.GetClientIP( request ); }
		public WebException( HttpRequest request, string message, Exception innerException ) : base( message, innerException ) { this.IP = UserHelper.GetClientIP( request ); }
		protected WebException( SerializationInfo info, StreamingContext context )
			: base( info, context ) {
			if( info != null )
				this.IP = info.GetString( "IP" );
		}/*
	public override void GetObjectData( SerializationInfo info, StreamingContext context ) {
		base.GetObjectData( info, context );
		if( info != null )
			info.AddValue( "IP", this.IP );
	}*/

	}
}