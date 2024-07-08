using System;
using System.Linq;
using System.Web;
using MDTools.Web;
using MDTools.IO;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace MDTools.Web {

	public interface IJob : IDisposable {
		int MinsBetweenJobs { get; }
		int NumberOfRuns { get; }
		ErrorTypes Process();
	}

	public interface ITask : IDisposable {
		ErrorTypes Process( int batchID );
	}


	public class CronJobUrl : IJob, IDisposable {

		public string Url;
		public string UserAgent = "MDTools Server"; // Default, override
		protected int numberofRuns = 0;
		public int NumberOfRuns { get { return numberofRuns; } set { numberofRuns = value; } }
		public int minsBetweenJobs = 1;
		public int MinsBetweenJobs { get { return minsBetweenJobs; } set { minsBetweenJobs = value; } }
		
		public CronJobUrl(string cronjob_url) {
			Url = cronjob_url;
		}

		public ErrorTypes Process() {
			var response = Post(Url);

			ErrorTypes error = ErrorTypes.NO_ERROR;
			try {
				var json = new JavaScriptSerializer();
				json.RegisterConverters( new[] { new DynamicJsonConverter() } );

				dynamic d = json.Deserialize( response, typeof(object) );

				this.numberofRuns = d.NumberOfRuns;
				this.minsBetweenJobs = (int)d.NumberOfRuns;
				error = (ErrorTypes)d.Error;
			}
			catch {
				return ErrorTypes.UNKNOWN;
			}
			return error;
		}
			

		public const string CRONJOB_HEADER="CronJob";

		protected string Post( string Url, string parameters = null ) {
			// Create web request to mtgox
			try {
				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create( Url );

				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.Method = "POST";
				webRequest.UserAgent = UserAgent;
				webRequest.Accept = "application/json";
				// Required to authenticate
				webRequest.Headers[CRONJOB_HEADER] = "1";

				if( parameters == null )
					parameters = string.Empty;
				byte[] byteArray = Encoding.UTF8.GetBytes( parameters );
				webRequest.ContentLength = byteArray.Length;

				using( Stream dataStream = webRequest.GetRequestStream() ) {
					dataStream.Write( byteArray, 0, byteArray.Length );
				}

				using( WebResponse webResponse = webRequest.GetResponse() ) {
					using( Stream str = webResponse.GetResponseStream() ) {
						using( StreamReader sr = new StreamReader( str ) ) {
							return sr.ReadToEnd();
						}
					}
				}
			}
			catch( Exception ex ) {
				return string.Empty;
			}
		}
		protected string Get( string Url ) {
			try {
				// !! convert to use MDTools.HttpRequest
				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create( Url );
				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.Method = "GET";
				webRequest.UserAgent = UserAgent;
				webRequest.Accept = "application/json";
				webRequest.Headers[CRONJOB_HEADER] = "1";

				using( WebResponse webResponse = webRequest.GetResponse() ) {
					using( Stream str = webResponse.GetResponseStream() ) {
						using( StreamReader sr = new StreamReader( str ) ) {
							return sr.ReadToEnd();
						}
					}
				}
			}
			catch( Exception ex ) {
				return string.Empty;
			}
		}

		public static bool ValidatePost(HttpRequestBase Request) {
			return Request.Headers[CRONJOB_HEADER] != null;
		}
		public static bool ValidatePost( HttpRequest Request ) {
			return Request.Headers[CRONJOB_HEADER] != null;
		}

		public void Dispose() {}

	}


	public static class JobMgr {

		public static void Initialize( bool singleinstance, params IJob[] jobs ) {

			// Make sure only one worker thread is processing jobs
			ILock Lock = null;
			if( singleinstance ) {
/*				Lock = new LockFile( HttpContext.Current.Server.MapPath( "~/filelock.txt" ) );
				// If lock already owned, then don't setup jobs
				if( !Lock.Lock() )
					return;*/
			}

			foreach( var job in jobs ) {
				MDTools.Web.Scheduler.Run(
					job.ToString(),
					job.MinsBetweenJobs,
					job.NumberOfRuns,
					delegate( Scheduler.CacheItem cacheitem, object[] args ) {
						var error = job.Process();
						if( error == ErrorTypes.CRITICAL_ERROR ) {
							cacheitem.StayAlive=false;
							cacheitem.Lock.Detach();
							job.Dispose();
							return;
						}
						// Reset time to new time
						cacheitem.NumberOfMinutes = job.MinsBetweenJobs;
					},
					Lock!=null ? Lock.AquireSafeLock() : null,
					null 
				);

			}

		}

		public static void Initialize( params string[] cronjoburls ) {

			foreach( var cronjoburl in cronjoburls ) {
				using( var job = new CronJobUrl( cronjoburl ) ) {
					job.NumberOfRuns = 1;
					InitCronjob( job );
				}
			}

		}
		public static void InitCronjob( CronJobUrl job ) {

				MDTools.Web.Scheduler.Run(
					job.Url,
					job.MinsBetweenJobs,
					job.NumberOfRuns,
					delegate( Scheduler.CacheItem cacheitem, object[] args ) {
						// Run once
						var error = job.Process();
						cacheitem.StayAlive = false;
						job.Dispose();
						// Reinit the process as its shutting down
						InitCronjob( job );//!! Consider moving this to called page worker
						return;
					},
					null,
					null
				);
		}

		public static void CloseIfNotRunning( params IJob[] jobs ) {
			foreach( var job in jobs ) {
				if( !MDTools.Web.Scheduler.Contains( job.ToString() ) ) {
					job.Dispose();
				}
			}
		}

	}

}
