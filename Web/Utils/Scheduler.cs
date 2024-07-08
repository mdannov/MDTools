using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Collections;
using System.Text.RegularExpressions;
using MDTools.IO;
using System.Net;
using System.Configuration;

namespace MDTools.Web {

	public class Scheduler {

		public class CacheItem {

			/// <summary>
			/// This implementation will cause the callback mechanism to constantly restart itself. 
			/// This is dangerous because it could cause a stack overflow. 
			/// Better to use this strategy with a limited NumberOfRuns
			/// </summary>
			public CacheItem( string name, Callback callback, ISafeLock slock, params object[] args ) {
				this.name = name;
				this.callback = callback;
				// Defaults
				this.StayAlive = true;
				this.NumberOfRuns = 0;// 0=endless
				this.NumberOfMinutes = 1;// between runs - default 1
				this.Lock = slock;
				this.Args = args;
				AddCacheObject();
			}
			protected Cache cache = HttpRuntime.Cache;
			protected Callback callback = null;
			protected string name;
			protected DateTime lastRun = DateTime.MinValue;
			protected int timesRun = 0;

			// Read-only
			public string Name { get { return name; } }
			public DateTime LastRun { get { return lastRun; } }
			public int TimesRun { get { return timesRun; } }

			// Read-write - these values can be modified during callback
			public bool StayAlive { get; set; }
			public int NumberOfRuns { get; set; }
			public int NumberOfMinutes { get; set; }
			public object[] Args { get; set; }
			public ISafeLock Lock { get; set; }

			public void Invoke() {
				if( !StayAlive )
					return;
				if( callback != null ) {
					callback.Invoke( this, Args );
					lastRun = DateTime.Now;
					timesRun++;
				}
			}
			public void Reactivate() {
				if( StayAlive && (NumberOfRuns == 0 || NumberOfRuns < timesRun ) )
					this.AddCacheObject();
			}

			protected void AddCacheObject() {
				const int SECONDS_IN_MINUTE = 60;
				cache.Add( name, this, null,//** do not use cache.Insert -- will cause race condition loop
						DateTime.Now.AddSeconds( NumberOfMinutes * SECONDS_IN_MINUTE ),
						Cache.NoSlidingExpiration,
						CacheItemPriority.NotRemovable, 
						_CacheCallbackReactivate );
			}

		}

		public delegate void Callback( Scheduler.CacheItem cacheitem, object[] args );

		public static string CacheKey( string name ) {
			return "Scheduler.Job." + name;
		}

		public static void Run( string name, int minutes, int numberOfRuns, Callback callbackMethod, ISafeLock slock, params object[] args ) {
			var key = CacheKey( name );
			// Make sure item is not already present
			if(HttpRuntime.Cache[key]==null)
				new CacheItem( key, callbackMethod, slock, args ) {
					NumberOfRuns = numberOfRuns,
					NumberOfMinutes = minutes,
				};
		}

		public static void Stop( string name ) {

			var key = CacheKey(name);
			CacheItem cacheitem = (CacheItem)HttpRuntime.Cache[key];
			if(cacheitem != null )
				cacheitem.StayAlive = false;
			cacheitem = (CacheItem)HttpRuntime.Cache.Remove( key );
			if( cacheitem != null ) {
				if( cacheitem.Lock != null )
					cacheitem.Lock.Detach();
			}
		}

		public static void SetFinalRun( string name ) {
			CacheItem cacheitem = (CacheItem)HttpRuntime.Cache[CacheKey(name)];
			if( cacheitem != null )
				cacheitem.StayAlive = false;
		}

		public static bool Contains( string name ) {
			return HttpRuntime.Cache[CacheKey( name )] != null;
		}

		private static void _CacheCallbackReactivate( string key, object value, CacheItemRemovedReason reason ) {
			CacheItem cacheitem = (CacheItem)value;
			if( cacheitem == null )
				return;
			// Check if explicitly removed by application with Stop, StayAlive will be false
			// If the application is shutting down however, and it's set up to reinitialize jobs, it will then without another mechanism to prevent
			if( cacheitem.StayAlive ) {
				cacheitem.Invoke();
				if( reason == CacheItemRemovedReason.Removed ) {
					// Set to Invoke the job, but don't reset the callback; that will be done by app restart
					// Application is shutting down; need to restart scheduler in Global.asax.Application_Start
					var stuburl = ConfigurationManager.AppSettings["ResetSchedulerUrl"];
					if(!string.IsNullOrEmpty(stuburl )) {
						try {
							using( WebClient client = new WebClient() ) {
								client.DownloadData( stuburl );
							}
						}
						catch { }
					}
				} else {
					cacheitem.Reactivate();
				}
			}
		}

	}

}
