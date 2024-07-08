using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace MDTools.IO {


	public interface ILock : IDisposable {
		bool Lock();
		void Release();
		ISafeLock AquireSafeLock();
	}

	public interface ISafeLock {
		ISafeLock Attach();
		void Detach();
	}




#if baddesign
	public class SafeLock : ISafeLock, IDisposable {
		public int counter = 0;

		ILock Lock = null;

		public SafeLock( ILock lk ) {
			this.Lock = lk;
		}

		public ISafeLock Attach() {
			counter++;
			return this;
		}

		public void Detach() {
			counter--;
			if( counter <= 0 && Lock != null ) {
				Lock.Dispose();
				Lock = null;
			}
		}

		public void Dispose() {
			if( Lock != null )
				Lock.Dispose();
		}

	}

	/// <summary>
	/// This lock pattern will only work in a webgarden or a webfarm with network access
	/// Make sure the physical file location has proper permissions by all workers and file exists
	/// </summary>
	public class LockFile : ILock, IDisposable {

		protected string filepath;
		public static Stream file=null;
		protected ISafeLock sLock = null;
		

		public LockFile( string filepath ) {
			this.filepath = filepath;
		}

		public ISafeLock AquireSafeLock() {
			return ( sLock != null ) ? sLock.Attach() : null;
		}

		public bool Lock() {

			if( !File.Exists( this.filepath ) )
				throw new IOException( "File must exist" );

			Stream temp = null;
			try {
				temp = File.Open( this.filepath, FileMode.Open, FileAccess.Read, FileShare.None );
			}
			catch( IOException ) {
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//xxxxxxxxxxxxxxxxxx-----or does not exist (has already been processed)
				return false;
			}
			//file is now locked and owned
			if( temp != null ) {
				file = temp;
				sLock = new SafeLock( this );
				return true;
			}
			return false;
		}

		public void Release() {
			if( file != null ) {
				file.Close();
				file.Dispose();
			}
			file = null;
			sLock = null;
		}

		public void Dispose() {
			Release();
		}

	}
#endif

}