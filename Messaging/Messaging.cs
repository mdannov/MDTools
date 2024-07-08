using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Messaging {
	public interface IMessaging {
		IMessagingResponse Send( string from, string to, string msg, Importance type = Importance.Info );
	}
	public interface IMessagingResponse {
		string Status { get; set; }
		string MessageID { get; set; }

	}

	public interface ISMS : IMessaging {

	}
	public interface ISMSGateway : ISMS {
		string GatewayUrl { get; }
	}


	public interface IEmail : IMessaging {

	}

	public enum Importance { Info, Important, Critical };


}
