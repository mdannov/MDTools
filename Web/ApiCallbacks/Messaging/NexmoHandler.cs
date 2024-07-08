using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text;
using System.Web;
using MDTools.Web;
using MDTools.Data;
using System.Threading;
using System.Globalization;

namespace MDTools.Web.Api.Messaging.Nexmo.Callback {

	public class DeliveryNotification {
		public string SenderID;
		public string NetworkCode;
		public string MessageID;
		public string ToID;
		public string Status;
		public DeliveryStatus StatusCode;
		public double Cost;
		public DateTime NotificationTime;
		public DateTime DeliveryTime;
		public string InternalRef;
		public enum DeliveryStatus { Delivered = 0, Unknown = 1, TemporaryUnavailable = 2, PermanentUnavailable = 3, Blocked = 4, NotPortable = 5, Rejected = 6, Busy = 7, NetworkError = 8, IllegalNumber = 9, BadMessage = 10, Unroutable = 11, GeneralError = 99 };
	}


	/// <summary>
	/// https://docs.nexmo.com/index.php/sms-api/handle-delivery-receipt
	/// </summary>
	public abstract class DeliveryHandler : IHttpHandler {

		protected abstract void Process( DeliveryNotification resp );

		public void ProcessRequest( HttpContext Context ) {

			var Response = Context.Response;
			var Request = Context.Request;

			var resp = new DeliveryNotification();
			resp.SenderID = Request["to"];
			var txt = Request["network-code"];
			if( !string.IsNullOrEmpty( txt ) )
				resp.NetworkCode = txt;
			resp.MessageID = Request["messageId"];
			resp.ToID = Request["msisdn"];
			resp.Status= Request["status"];
			resp.StatusCode = (DeliveryNotification.DeliveryStatus)int.Parse( Request["err-code"] );
			resp.Cost = double.Parse(Request["price"]);
			resp.DeliveryTime = DateTime.SpecifyKind( DateTime.ParseExact( Request["scts"], "yyMMddHHmm", CultureInfo.InvariantCulture ), DateTimeKind.Utc ).ToLocalTime();
			resp.NotificationTime = DateTime.SpecifyKind( DateTime.ParseExact( Request["message-timestamp"], "yyyy-MM-dd HH:mm:SS", CultureInfo.InvariantCulture ), DateTimeKind.Utc ).ToLocalTime();
			resp.InternalRef = Request["client-ref"];

			Process( resp );

			Response.StatusCode = 200;
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

	}

	public class InboundNotification {
		public string SenderID;
		public string NetworkCode;
		public string MessageID;
		public string ToID;
		public string Type;
		public string Msg;
		public int Part;
		public int TotalParts;
		public string Transation;
		public DeliveryStatus StatusCode;
		public double Cost;
		public DateTime RecievedTime;
		public string InternalRef;
		public enum DeliveryStatus { Delivered = 0, Unknown = 1, TemporaryUnavailable = 2, PermanentUnavailable = 3, Blocked = 4, NotPortable = 5, Rejected = 6, Busy = 7, NetworkError = 8, IllegalNumber = 9, BadMessage = 10, Unroutable = 11, GeneralError = 99 };
	}

	/// <summary>
	/// https://docs.nexmo.com/index.php/sms-api/handle-inbound-message
	/// </summary>
	public abstract class InboundMessageHandler : IHttpHandler {

		protected abstract void Process( InboundNotification resp );

		public void ProcessRequest( HttpContext Context ) {

			var Response = Context.Response;
			var Request = Context.Request;

			var resp = new InboundNotification();
			resp.Type = Request["type"];
			resp.ToID = Request["to"];
			var txt = Request["network-code"];
			if( !string.IsNullOrEmpty( txt ) )
				resp.NetworkCode = txt;
			resp.MessageID = Request["messageId"];
			txt = Request["msisdn"];
			if( !string.IsNullOrEmpty( txt ) )
				resp.SenderID = txt;
			resp.Cost = double.Parse( Request["price"] );
			resp.RecievedTime = DateTime.SpecifyKind( DateTime.ParseExact( Request["message-timestamp"], "yyyy-MM-dd HH:mm:SS", CultureInfo.InvariantCulture ), DateTimeKind.Utc ).ToLocalTime();
			resp.InternalRef = Request["client-ref"];
			resp.Transation = Request["concat-ref"];
			resp.Part = int.Parse(Request["concat-part"]);
			resp.TotalParts = int.Parse( Request["concat-total"] );
			resp.Msg = Request["text"];

			Process( resp );

			Response.StatusCode = 200;
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

	}

}

