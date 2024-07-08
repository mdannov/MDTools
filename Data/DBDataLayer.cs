using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Xml;
using MDTools.Extension;

/*	Code Copyright Michael Dannov 2009-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */


namespace MDTools.Data {
	
	public interface IDLDBDeserializer {
		void DeserializeRow( IDataRecord rdr );  // Deserialize sqldatareader into class
	}
	public interface IDLDBSerializer {
		bool SerializeRow( DataRow row, int pos );  // Serialize Datarow for batch updates into class - returns true if valid row
	}
	public interface IDLXMLHelper {
		void Fill( XmlNode rdr );  // Deserialize sqldatareader into class
	}

	public class DLDBLoader : IDLLoader, IDisposable {
		public string DBProfile;
		public SqlCommand Cmd = null;
		public IDLDBDeserializer DBPopulater;

		public DLDBLoader( string dbProfile, SqlCommand cmd, IDLDBDeserializer db ) {
			DBProfile = dbProfile;
			Cmd = cmd;
			DBPopulater = db;
		}

		public bool Load() {
			bool loaded = false;
			using( DBHelper dbh = new DBHelper( DBProfile ) ) {
				Cmd.Connection = dbh.SqlConn;
				using( SqlDataReader r = dbh.GetDataReader( Cmd ) ) {
					// Handles case where multiple resultsets of the same data is sent back
					do {
						while( r.Read() ) {
							// Load ProfileObj
							DBPopulater.DeserializeRow( r );
							loaded = true;
						}
					} while( r.NextResult() );
				}
			}
			return loaded;
		}

		public void Dispose() {
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected virtual void Dispose( bool disposing ) {
			if( disposing ) {
				if( Cmd != null ) {
					Cmd.Dispose();
					Cmd = null;
				}
			}
		}
	}


	public class DLDBUpdater : IDLUpdater, IDLDeleter, IDLCreater, IDisposable {
		public string DBProfile;
		public SqlCommand Cmd=null;

		public DLDBUpdater( string dbProfile, SqlCommand cmd ) {
			DBProfile = dbProfile;
			Cmd = cmd;
		}
		public bool Update() { return Execute(); }
		public bool Delete() { return Execute(); }
		public CREATEDKEY Create<CREATEDKEY>() {
			using( DBHelper dbh = new DBHelper( DBProfile ) ) {
				Cmd.Connection = dbh.SqlConn;
				return dbh.Execute<CREATEDKEY>( Cmd );
			}
		}
		public bool Execute() {
			using( DBHelper dbh = new DBHelper( DBProfile ) ) {
				Cmd.Connection = dbh.SqlConn;
				return dbh.Execute( Cmd ) != 0;
			}
		}

		public void Dispose() {
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected virtual void Dispose( bool disposing ) {
			if( disposing ) {
				if( Cmd != null ) {
					Cmd.Dispose();
					Cmd = null;
				}
			}
		}

	}

	public class DLDBBatchUpdater : IDLUpdater, IDLDeleter, IDLCreater, IDisposable {
		public string DBProfile;
		public SqlCommand Cmd;
		public IDLDBSerializer DBPopulater;
		protected DataTable Tbl = null;
		protected SqlDataAdapter Adapter = null;

		public DLDBBatchUpdater( string dbProfile, SqlCommand cmd ) {
			DBProfile = dbProfile;
			Cmd = cmd;
		}
		public bool Update() {
			if( !Init() )
				return false;
			Adapter = new SqlDataAdapter();
			Adapter.UpdateCommand = Cmd;
			return Execute();
		}
		public bool Delete() {
			if( !Init() )
				return false;
			Adapter = new SqlDataAdapter();
			Adapter.DeleteCommand = Cmd;
			return Execute();
		}
		public CREATEDKEY Create<CREATEDKEY>() {
			if( !Init() )
				return default( CREATEDKEY );
			Adapter = new SqlDataAdapter();
			Adapter.InsertCommand = Cmd;
			return default( CREATEDKEY ); // not relevant
		}
		protected bool Init() {
			Tbl = new DataTable();
			bool valid = true;
			int len = 0;
			for( len = 0; valid; len++ ) {
				DataRow row = Tbl.NewRow();
				valid = DBPopulater.SerializeRow( row, len );
				Tbl.Rows.Add( row );
			}
			return len>0;
		}
		protected bool Execute() {
			using( DBHelper dbh = new DBHelper( DBProfile ) ) {
				Cmd.Connection = dbh.SqlConn;
				Cmd.UpdatedRowSource = UpdateRowSource.None;
				Adapter.UpdateBatchSize = Tbl.Rows.Count;
				Adapter.Update( Tbl );
			}
			return true;
		}

		public void Dispose() {
			Dispose( true );
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing) {
			if( disposing ) {
				if( Adapter != null ) {
					Adapter.Dispose();
					Adapter = null;
				}
				if( Tbl != null ) {
					Tbl.Dispose();
					Tbl = null;
				}
			}
		}

	}


	public static class DataExtender {
		/// <summary>
		/// Get a required string - data cannot be DBNull
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static string Get( this IDataRecord dr, string key ) {
			return dr[key] as string;
		}
		/// <summary>
		/// Get an optional string in Utf-8 format; if DBNull then default value
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static string Get( this IDataRecord dr, string key, string def ) {
			object o = dr[key];
			return ( o == System.DBNull.Value ) ? def : o as string;
		}
		/// <summary>
		/// Get a required string in Utf-8 format - data cannot be DBNull
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static string GetUtf8( this IDataRecord dr, string key ) {
			var str = dr[key] as string;
			return str.ToUtf8FromSql();
		}
		/// <summary>
		/// Get an optional string; if DBNull then default value
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static string GetUtf8( this IDataRecord dr, string key, string def ) {
			object o = dr[key];
			if( o == System.DBNull.Value )
				return def;
			return ( o as string ).ToUtf8FromSql();
		}
		/// <summary>
		/// Get a required field - data cannot be DBNull
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static TYPE Get<TYPE>( this IDataRecord dr, string key ) {
			return (TYPE)dr[key];
		}
		/// <summary>
		/// Get an optional value; if DBNull then default value
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static TYPE Get<TYPE>( this IDataRecord dr, string key, TYPE def ) {
			object o = dr[key];
			return ( o == System.DBNull.Value ) ? def : (TYPE)o;
		}
		/// <summary>
		/// Get a required DateTime field - data cannot be DBNull - small does smalldatetime translation for max/min
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static DateTime GetDateTime( this IDataRecord dr, string key, bool small ) {
			object o = dr[key];
			return small ? SmallDateTimeExtension.FromSmall( (DateTime)o ) : (DateTime)o;
		}
		/// <summary>
		/// Get an optional value; if DBNull then default value - small does smalldatetime translation for max/min
		/// direct type conversion - datatypes must match schema conversion type exactly
		/// </summary>
		public static DateTime GetDateTime( this IDataRecord dr, string key, DateTime def, bool small ) {
			object o = dr[key];
			return ( o == System.DBNull.Value ) ? def : 
				(small ? SmallDateTimeExtension.FromSmall( (DateTime)o ) : (DateTime)o);
		}
		/// <summary>
		/// Check if key is null
		/// </summary>
		public static bool IsDBNull( this IDataRecord dr, string key ) {
			object o = dr[key];
			return ( o == System.DBNull.Value );
		}

	}

	#region DateTime Conversion Helpers
	/// <summary>
	/// Used as a type translation for max/min DateTime
	/// </summary>
	public class SmallDateTime {
		public DateTime DateTime;
		public SmallDateTime( DateTime data ) {
			this.DateTime = data;
		}
		public DateTime FromSmall() {
			if( this.DateTime == SmallDateTimeExtension.MinValue )
				return DateTime.MinValue;
			if( this.DateTime == SmallDateTimeExtension.MaxValue )
				return DateTime.MaxValue;
			return this.DateTime;
		}
		public DateTime ToSmall() {
			if( this.DateTime < SmallDateTimeExtension.MinValue )
				return SmallDateTimeExtension.MinValue;
			if( this.DateTime > SmallDateTimeExtension.MaxValue )
				return SmallDateTimeExtension.MaxValue;
			return this.DateTime;
		}
	}
	public static class SmallDateTimeExtension {
		public static DateTime FromSmall( this DateTime data ) {
			if( data == MinValue )
				return DateTime.MinValue;
			if( data == MaxValue )
				return MaxValue;
			return data;
		}
		public static DateTime ToSmall( this DateTime data ) {
			if( data < MinValue )
				return MinValue;
			if( data > MaxValue )
				return MaxValue;
			return data;
		}
		public static readonly DateTime MinValue = new DateTime( 1900, 1, 1 );
		public static readonly DateTime MaxValue = new DateTime( 2079, 6, 6 );
	}
	#endregion


	public class StaticParams {
		public string S1 = null;
		public string S2 = null;
		public Int64 L1 = -1;
		public Int64 L2 = -1;
		public Int32 I1 = -1;
		public Int32 I2 = -1;
		public DateTime D1 = DateTime.MinValue;

		public void AddSqlParameters( SqlCommand cmd, bool isReturnVal ) {
			if( !string.IsNullOrEmpty( S1 ) )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "S1" : "S1Val", S1 ) );
			if( !string.IsNullOrEmpty( S2 ) )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "S2" : "S2Val", S2 ) );
			if( L1 != -1 )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "L1" : "L1Val", L1 ) );
			if( L2 != -1 )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "L2" : "L2Val", L2 ) );
			if( I1 != -1 )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "I1" : "I1Val", I1 ) );
			if( I2 != -1 )
				cmd.Parameters.Add( DBHelper.SqlParameter( !isReturnVal ? "I2" : "I2Val", I2 ) );
		}
	}

}