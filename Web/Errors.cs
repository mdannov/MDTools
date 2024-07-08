using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MDTools {

	public enum ErrorTypes {
		NO_ERROR = 0,
		CONNECT_FAILURE = 1,
		AUTHENTICATION_ERROR = 2,
		ACCESSRIGHTS_ERROR = 3,
		DATA_MALFORMED = 4,
		TRANSACTION_FAILURE = 5,
		UNKNOWN = 254,
		CRITICAL_ERROR = 255
	}

}