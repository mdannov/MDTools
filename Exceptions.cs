#if wtf
namespace MDTools {

	/// <summary>
	/// Base Class for MDTools Exceptions
	/// </summary>
	public class Exception : System.Exception {
		public Exception( ) : base( ) { }
		public Exception( string message ) : base(message) {}
		public Exception( string message, System.Exception innerException ): base(message, innerException) {}
		public Exception( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
	}

	public class LogException : Exception {
		public LogException() : base() { }
		public LogException( string message ) : base(message) {}
		public LogException( string message, System.Exception innerException ): base(message, innerException) {}
		public LogException( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
	}

	public class HackException : Exception {
		public HackException( ) : base( ) { }
		public HackException( string message ) : base(message) {}
		public HackException( string message, System.Exception innerException ) : base( message, innerException ) { }
		public HackException( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
	}

}
#endif
