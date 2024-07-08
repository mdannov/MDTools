using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MDTools.Extension {
	/// <summary>
	/// 
	/// object.MemberwiseClone does a shallow copy and refers to same internal objects
	/// </summary>
	public static class GenericCopyClone {
		/// <summary>
		/// Thread Safe Shallow Clone of Properties in Objects with property exclusion
		/// http://stackoverflow.com/questions/13198658/deep-copy-using-reflection-in-an-extension-method-for-silverlight
		/// </summary>
		public static T ShallowCloneObject<T>( this T original, IList<string> propertyExcludeList = null ) {
			try {
				Monitor.Enter( _lock );
				T copy = Activator.CreateInstance<T>();
				PropertyInfo[] piList = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				foreach( PropertyInfo pi in piList ) {
					if( propertyExcludeList == null || !propertyExcludeList.Contains( pi.Name ) ) {
						if( pi.GetValue( copy, null ) != pi.GetValue( original, null ) ) {
							pi.SetValue( copy, pi.GetValue( original, null ), null );
						}
					}
				}
				return copy;
			}
			finally {
				Monitor.Exit( _lock );
			}
		}
		private readonly static object _lock = new object();

		/// <summary>
		/// 
		/// Object must be [Serializable]
		/// http://www.codeproject.com/Articles/23832/Implementing-Deep-Cloning-via-Serializing-objects
		/// </summary>
		public static T SlowDeepBinaryClone<T>( T source ) {
			// Don't serialize a null object, simply return the default for that object
			if( Object.ReferenceEquals( source, null ) )
				return default( T );
			if( !source.GetType().IsSerializable ) // if (!typeof(T).IsSerializable)
				throw new ArgumentException( "The type must be serializable.", "source" );
			IFormatter formatter = new BinaryFormatter();
			using( Stream stream = new MemoryStream() ) {
				formatter.Serialize( stream, source );
				stream.Seek( 0, SeekOrigin.Begin );
				return (T)formatter.Deserialize( stream );
			}
		}

	}
}
