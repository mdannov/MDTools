using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MDTools.Extension;
using MDTools.Web.Api.Messaging;
using MDTools.Messaging;

namespace MDTools.Web.Api.Messaging.Nexmo {

	public static class Nexmo {
		public static readonly string ApiKey = ConfigurationManager.AppSettings["NexmoApiKey"];
		public static readonly string ApiSecret = ConfigurationManager.AppSettings["NexmoApiSecret"];
		public static readonly bool DeliveryCallback = ConfigurationManager.AppSettings["NexmoDeliveryCallback"] == "1";
		public static readonly bool InboundCallback = ConfigurationManager.AppSettings["NexmoInboundCallback"] == "1";
	}

	public abstract class NexmoBaseApi {
		protected string key = Nexmo.ApiKey;
		protected string secret = Nexmo.ApiSecret;
		protected string baseurl = null;
		public abstract string GatewayUrl { get; }

		public NexmoBaseApi() {
			if( string.IsNullOrEmpty( key ) || string.IsNullOrEmpty( secret ) )
				throw new ArgumentException( "Pass in Key & Secret or set in AppSetting in web.config : NexmoApiKey and NexmoApiSecret" );
			this.baseurl = string.Concat( this.GatewayUrl, "?api_key=", this.key, "&api_secret=", this.secret );
		}
		public NexmoBaseApi( string key, string secret ) {
			this.key = key;
			this.secret = secret;
			this.baseurl = string.Concat( this.GatewayUrl, "?api_key=", this.key, "&api_secret=", this.secret );
		}
	}

	/// <summary>
	/// https://docs.nexmo.com/index.php/sms-api/send-message
	/// </summary>
	public class NexmoSmsApi : NexmoBaseApi {

		public override string GatewayUrl { get { return @"https://rest.nexmo.com/sms/json"; } }
		public string InternalRef { get; set; }

		public NexmoSmsApi() : base() { }
		public NexmoSmsApi( string key, string secret ) : base( key, secret ) { }

		public IMessagingResponse Send( string from, string to, string msg, Importance type = Importance.Info ) {
			return Send( from, to, msg, type, Nexmo.DeliveryCallback );
		}
		public IMessagingResponse Send( string from, string to, string msg, Importance type = Importance.Info, bool deliveryreport = false ) {
			if( string.IsNullOrEmpty( msg ) )
				throw new ArgumentException( "Empty Msg" );
			if( msg.Length > 3200 ) // up to 3200 broken into 152char parts https://help.nexmo.com/entries/24578133-How-multipart-SMS-is-constructed-
				throw new ArgumentException( "Msg too long" );
			var url = string.Concat( this.baseurl, "&from=", from.UrlEncode(), "&to=", to, "&text=", msg.UrlEncode() );
			if( deliveryreport )
				url += "&status-report-req=1";
			if( type != Importance.Info )
				url += "&message-class=0";
			if( !string.IsNullOrEmpty( InternalRef ) )
				url += "&client-ref=" + InternalRef;
			var json = new JSONRetrieve( "Nexmo Api" ).Get( url );
			if( json == null )
				return null;
			if( !json.IsDictionary )
				throw new Exception( "Nexmo returned invalid format" );
			var msgs = json["messages"].Array;
			if( msg == null )
				throw new Exception( "Nexmo returned no messages" );
			var res = new NexmoSmsGatewayResponse();
			for( int i = 0, len = msgs.Length; i < len; i++ ) {
				var m = msgs[i];
				res.SetStatus( m["status"].Integer );
				if( res.StatusCode != 0 ) // if error, set error
					res.Status = m["error-text"].String;
				if( string.IsNullOrEmpty( res.MessageID ) ) // use first messageid
					res.MessageID = m["message-id"].String;
				res.Cost += m["message-price"].Double; // append to cost
				if( i == len - 1 ) // use last for remaining balance
					res.CreditRemaining = m["remaining-balance"].Double;
			}
			return res;
		}
	}

	/// <summary>
	/// NexmoSmsApi with US limitations and uses number pool:
	/// https://nexmo.zendesk.com/entries/22362928-USA-Canada-Direct-route-
	/// Most noteworthy are: 
	/// 1. msg must be initiated through human interaction
	/// 2. max 1 sms msg may be sent per second (those over will be rejected)
	/// 3. max 500 sms message per day per number (those over will be rejected)
	/// ?  unknown if split messages count twoards max msgs per day 
	/// </summary>
	public class NexmoUSSmsApi : NexmoSmsApi { 
		public NexmoUSSmsApi() : base() { }
		public NexmoUSSmsApi( string key, string secret ) : base( key, secret ) { }

	}

	public abstract class NexmoBaseResponse : IMessagingResponse {
		public string Status { get; set; }
		public string MessageID { get; set; }
		public enum Statuses { Success, RetryLater, ArgumentFailure, Blocked, InsufficientCredits, CommunicationFailure, Unknown }
		public Statuses StatusCode { get; set; }
	}
	public class NexmoSmsGatewayResponse : NexmoBaseResponse {
		public void SetStatus( long statuscode ) {
			switch( statuscode ) {
			// Handle these
			case 0:
				this.StatusCode = Statuses.Success;
				return;
			case 1:
				this.StatusCode = Statuses.RetryLater;
				return;
			case 7:
				this.StatusCode = Statuses.Blocked;
				return;
			case 13:
				this.StatusCode = Statuses.CommunicationFailure;
				return;
			// Serious, exception
			case 2:
			case 3:
			case 4:
			case 12:
			case 15:
			case 20:
				this.StatusCode = Statuses.ArgumentFailure;
				break;
			default:
				this.StatusCode = Statuses.Unknown;
				break;
			}
			throw new ArgumentException( "Nexmo indicates error code: " + statuscode );
		}
		public double Cost { get; set; }
		public double CreditRemaining { get; set; }
	}

	public abstract class NexmoVoiceBaseApi : NexmoBaseApi, IMessaging {
		public bool Male { get; set; }	// Default female
		public int Repeat { get; set; }		// Max 10 : Default 1
		public string Language { get; set; }	// Default US English:  https://docs.nexmo.com/index.php/voice-api/text-to-speech#languages
		public bool MachineHangup { get; set; } // Default
		public int MachineDetectionMs { get; set; } // Default 4000, Max 10000

		public NexmoVoiceBaseApi() : base() { }
		public NexmoVoiceBaseApi( string key, string secret ) : base( key, secret ) { }
		protected string SetupMessage( string from, string to, string msg, int maxlen ) {
			if( string.IsNullOrEmpty( msg ) )
				throw new ArgumentException( "Empty Msg" );
			if( msg.Length > maxlen )
				throw new ArgumentException( "Msg too long" );
			var url = string.Concat( this.baseurl, "&from=", from.UrlEncode(), "&to=", to, "&text=", msg.UrlEncode() );
			if( Male )
				url += "&voice=male";
			if( !string.IsNullOrEmpty( Language ) )
				url += "&lg=" + Language;
			if( MachineHangup )
				url += "&machine_detection=hangup";
			if( MachineDetectionMs > 0 )
				url += "&machine_timeout=" + MachineDetectionMs.ToString();
			return url;
		}
		public abstract IMessagingResponse Send( string from, string to, string msg, Importance type = Importance.Info );
	}

	/// <summary>
	/// https://docs.nexmo.com/index.php/voice-api/text-to-speech
	/// </summary>
	public class NexmoTtsApi : NexmoVoiceBaseApi {
		public override string GatewayUrl { get { return @"https://rest.nexmo.com/tts/json"; } }

		public string CallbackUrl { get; set; }

		public NexmoTtsApi() : base() { }
		public NexmoTtsApi( string key, string secret ) : base( key, secret ) { }
		protected IMessagingResponse ReturnResponse( string url ) {
			var json = new JSONRetrieve( "Nexmo Api" ).Get( url );
			if( json == null )
				return null;
			if( !json.IsDictionary )
				throw new Exception( "Nexmo returned invalid format" );
			var res = new NexmoTtsResponse();
			res.SetStatus( json["status"].Integer );
			if( res.StatusCode != 0 ) // if error, set error
				res.Status = json["error-text"].String;
			res.MessageID = json["call-id"].String;
			return res;
		}
		/// <summary>
		/// Call number to present tts message (text-to-speech)
		/// </summary>
		public override IMessagingResponse Send( string from, string to, string msg, Importance type = Importance.Info ) {
			var url = SetupMessage( from, to, msg, 1000 );
			if( !string.IsNullOrEmpty( CallbackUrl ) )
				url += "&callback=" + CallbackUrl;
			return ReturnResponse(url);
		}

		/// <summary>
		/// Call number to present tts message (text-to-speech) with prompt to record numbers
		/// </summary>
		public IMessagingResponse SendAndPrompt( string from, string to, string msg, int maxdigits, string byemsg ) {
			var url = SetupMessage( from, to, msg, 1000 );
			url = url.Replace( this.GatewayUrl, @"https://rest.nexmo.com/tts-prompt/json" );//!! use faster api
			url += string.Concat( "&max_digits=", maxdigits.ToString(), "&bye_text=", byemsg.UrlEncode() );
			if( !string.IsNullOrEmpty( CallbackUrl ) )
				url += "&callback=" + CallbackUrl;
			return ReturnResponse( url );
		}

		/// <summary>
		/// Call number to present tts message (text-to-speech) with prompt to confirm match
		/// </summary>
		public IMessagingResponse SendAndConfigm( string from, string to, string msg, int maxdigits, int matchdigits, string byemsg, string failmsg ) {
			var url = SetupMessage( from, to, msg, 1000 );
			url = url.Replace( this.GatewayUrl, @"https://rest.nexmo.com/tts-prompt/json" );//!! use faster api
			url += string.Concat( "&pin_code=", matchdigits.ToString(), "&max_digits=", maxdigits.ToString(), "&bye_text=", byemsg.UrlEncode(), "&failed_text=", failmsg.UrlEncode() );
			if( !string.IsNullOrEmpty( CallbackUrl ) )
				url += "&callback=" + CallbackUrl;
			return ReturnResponse( url );
		}

		public class NexmoTtsResponse : NexmoBaseResponse {
			public void SetStatus( long statuscode ) {
				switch( statuscode ) {
				// Handle these
				case 0:
					this.StatusCode = Statuses.Success;
					return;
				case 6:
					this.StatusCode = Statuses.Blocked;
					return;
				// Serious, exception
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					this.StatusCode = Statuses.ArgumentFailure;
					break;
				default:
					this.StatusCode = Statuses.Unknown;
					break;
				}
				throw new ArgumentException( "Nexmo indicates error code: " + statuscode );
			}
		}

		public class NexmoVoiceXmlApi : NexmoBaseApi {
			public override string GatewayUrl { get { return @"https://rest.nexmo.com/sms/json"; } }

			/// <summary>
			/// Call number to present tts message (text-to-speech)
			/// </summary>
			public IMessagingResponse Send( string from, string to, string VoiceXMLUrl, Importance type = Importance.Info ) {
				return null;
			}

		}

	}
}