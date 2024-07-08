using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;


/*	Code Copyright Michael Dannov 2008-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */

namespace MDTools.Data {

	/// <summary>
	/// Summary description for DBHelper
	/// </summary>
	public class DBHelper : IDisposable {
		private string connectionString = null;
		private SqlConnection sqlConn = null;
		private SqlTransaction sqlTrans = null;
		private bool isDisposed = false;
		private SqlCommand cmd = null;
		private SqlDataReader rdr = null;

		#region Initialization Functions

		public string ConnectionString {
			get { return connectionString; }
			set { connectionString = value; }
		}

		public SqlConnection SqlConn {
			get { return sqlConn; }
			set { sqlConn = value; }
		}

		public SqlCommand Cmd {
			get { return cmd; }
			set { cmd = value; }
		}

		public DBHelper( string connectionIdentifier ) {
			SetConnection( connectionIdentifier, false );
		}
		public DBHelper( string connIdentifierOrString, bool asConnectionString ) {
			SetConnection( connIdentifierOrString, asConnectionString );
		}
		public DBHelper( string connectionIdentifier, SqlCommand cmd ) {
			SetConnection( connectionIdentifier, false );
			this.cmd = cmd;
		}
		public DBHelper( string connIdentifierOrString, bool asConnectionString, SqlCommand cmd ) {
			SetConnection( connIdentifierOrString, asConnectionString );
			this.cmd = cmd;
		}
		/// <summary>
		/// Establish Connection based on a connection identifier or connection string 
		/// <param name="connIdentifierOrString">Connection String or identifier from web.config</param>
		/// <param name="isConnectionString">true if full connection string; false if identifier</param>
		/// </summary>
		private void SetConnection( string connIdentifierOrString, bool isConnectionString ) {
			if( isConnectionString )
				connectionString = connIdentifierOrString;
			else {
				connectionString = ConfigurationManager.ConnectionStrings[connIdentifierOrString].ConnectionString;
			}
			sqlConn = new SqlConnection( connectionString );
		}

		~DBHelper() {
			Dispose( false );
		}

		// Implement IDisposable. Effectively the same as a Close
		public void Dispose() {
			Dispose( true );
			// Take yourself off the Finalization queue 
			// to prevent finalization code for this object
			// from executing a second time.
			//GC.SuppressFinalize(this);
		}

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the 
		// runtime from inside the finalizer and you should not reference 
		// other objects. Only unmanaged resources can be disposed.
		private void Dispose( bool disposing ) {
			// Check to see if Dispose has already been called.
			if( !this.isDisposed ) {
				// If disposing equals true, dispose all managed 
				// and unmanaged resources.
				if( disposing ) {
					Close();
					if( sqlTrans != null )
						sqlTrans.Dispose();
					sqlTrans = null;
					if( sqlConn != null )
						sqlConn.Dispose(); //!! Calls Dispose
					sqlConn = null;
				}
			}
			isDisposed = true;
		}

		/// <summary>
		/// At a minimum, make sure every DBHelper Object is Disposed OR Closed. 
		///		Make sure construction appears in a using statement and Close is a part of a 
		///		final so an exception cannot walk around it; otherwise you may leak connections 
		/// </summary>
		public void Close() {
			// Close any open resources
			if( rdr != null && !rdr.IsClosed )
				rdr.Close();
			rdr = null;
			cmd = null;
			if( sqlConn != null && sqlConn.State == ConnectionState.Open )
				sqlConn.Close(); //!! Calls Close instead of Dispose so connection string is maintained for another Open()
		}
		#endregion


		#region GetDataReader Variations

		/// <summary>
		/// Get a DataReader object from cmd passed in constructor; no parameters will be passed
		/// </summary>
		public SqlDataReader GetDataReader() {
			return GetDataReader( this.cmd );
		}
		/// <summary>
		/// Get a DataReader object; no parameters will be passed
		/// <param name="sqlString"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		public SqlDataReader GetDataReader( SqlCommand cmd ) {
			if( sqlConn.State != ConnectionState.Open )
				sqlConn.Open();
			cmd.Connection = sqlConn;
			return cmd.ExecuteReader( CommandBehavior.CloseConnection );
		}
		/// <summary>
		/// Get a DataReader object; no parameters will be passed
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// </summary>
		public SqlDataReader GetDataReader( string sqlString ) {
			return GetDataReader( SqlCommand( sqlString ) );
		}
		/// <summary>
		///  Get a DataReader object; accept unlimited SqlParameter objects
		/// </summary>
		public SqlDataReader GetDataReader( string sqlString, params SqlParameter[] parms ) {
			return GetDataReader( SqlCommand( sqlString, parms ) );
		}

#if SUPPORT_ONE_PARAM_GetDataReader
		/// <summary>
		/// Get a DataReader object; accept 1 parameter versions
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// </summary>
		public SqlDataReader GetDataReader( string sqlString, string ParamName, string ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
		public SqlDataReader GetDataReader( string sqlString, string ParamName, DateTime ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
		public SqlDataReader GetDataReader( string sqlString, string ParamName, Int32 ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
		public SqlDataReader GetDataReader( string sqlString, string ParamName, Int64 ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
		public SqlDataReader GetDataReader( string sqlString, string ParamName, bool ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
		public SqlDataReader GetDataReader( string sqlString, string ParamName, SqlMoney ParamValue ) {
			SqlCommand cmd = CreateSqlCommand( sqlString );
			cmd.Parameters.Add( DBHelper.SqlParameter( ParamName, ParamValue ) );
			return GetDataReader( cmd );
		}
#endif
		#endregion

		#region Command Execute Functions
		/// <summary>
		/// Execute the sql statement from command passed in constructor; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int Execute() {
			return Execute( this.cmd );
		}
		/// <summary>
		/// Execute the sql statement; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int Execute( string sqlString ) {
			return Execute( DBHelper.SqlCommand(sqlString) );
		}
		/// <summary>
		/// Execute the sql statement; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <param name="parms">SqlParameter objects</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		/// </summary>
		public int Execute( string sqlString, params SqlParameter[] parms ) {
			return Execute( DBHelper.SqlCommand(sqlString, parms) );
		}
		/// <summary>
		/// Execute the sql statement; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int Execute( SqlCommand cmd ) {
			bool bForceOpen = ( sqlConn.State != ConnectionState.Open );
			if( bForceOpen )
				sqlConn.Open();
			cmd.Connection = sqlConn;
			int ret = cmd.ExecuteNonQuery();
			if( bForceOpen )
				sqlConn.Close();
			return ret;
		}
		/// <summary>
		/// Just execute the sql statement (using cmd passed in constructor) but return the return value returned from the stored procedure [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>make sure your RETURNTYPE matches the precisely with the one returned from the stored procedure. 
		/// Use object for RETURNTYPE if you don't want to risk an exception</returns>
		public RETURNTYPE Execute<RETURNTYPE>( SqlDbType sqltype, int size ) where RETURNTYPE : new() {
			return Execute<RETURNTYPE>( cmd, sqltype, size );
		}
		/// <summary>
		/// Just execute the sql statement but return the return value returned from the stored procedure [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <returns>make sure your RETURNTYPE matches the precisely with the one returned from the stored procedure. 
		/// Use object for RETURNTYPE if you don't want to risk an exception</returns>
		public RETURNTYPE Execute<RETURNTYPE>( string sqlString, SqlDbType sqltype, int size ) where RETURNTYPE : new() {
			return Execute<RETURNTYPE>( DBHelper.SqlCommand( sqlString), sqltype, size );
		}
		/// <summary>
		/// Just execute the sql statement but return the return value returned from the stored procedure [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <param name="parms">SqlParameter objects</param>
		/// <returns>make sure your RETURNTYPE matches the precisely with the one returned from the stored procedure. 
		/// Use object for RETURNTYPE if you don't want to risk an exception</returns>
		public RETURNTYPE Execute<RETURNTYPE>( string sqlString, SqlDbType sqltype, int size, params SqlParameter[] parms ) where RETURNTYPE : new() {
			return Execute<RETURNTYPE>( DBHelper.SqlCommand( sqlString, parms ), sqltype, size );
		}
		/// <summary>
		/// Just execute the sql statement but return the return value returned from the stored procedure [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>make sure your RETURNTYPE matches the precisely with the one returned from the stored procedure. 
		/// Use object for RETURNTYPE if you don't want to risk an exception</returns>
		public RETURNTYPE Execute<RETURNTYPE>( SqlCommand cmd, SqlDbType sqltype, int size ) where RETURNTYPE : new() {
			if( !cmd.Parameters.Contains( "@RETURN_VALUE" ) )
				cmd.Parameters.Add( new SqlParameter( "@RETURN_VALUE", sqltype, size,
					ParameterDirection.ReturnValue, true, 10, 0, null, DataRowVersion.Current, new RETURNTYPE() ) );
			Execute( cmd );
			SqlParameter o = cmd.Parameters["@RETURN_VALUE"];
			if( o == null )
				throw new ArgumentOutOfRangeException( "Stored procedure does not contain a return" );
			return (RETURNTYPE)o.Value;
		}
		/// <summary>
		/// Execute a stored procedure (using cmd passed in constructor) and return the first column from returned SELECT. [ExecuteScalar]
		/// If you want the identity after an insert, end statment or stored procedure with "SELECT SCOPE_IDENTITY()"
		/// </summary>
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// <returns>make sure your IDENTITYTYPE matches precisely with the type returned by the SELECT
		/// Use object for IDENTITYTYPE if you don't want to risk an exception</returns>
		public IDENTITYTYPE Execute<IDENTITYTYPE>() {
			return Execute<IDENTITYTYPE>( cmd );
		}
		/// <summary>
		/// Execute a stored procedure and return the first column from returned SELECT. [ExecuteScalar]
		/// If you want the identity after an insert, end statement or stored procedure with "SELECT SCOPE_IDENTITY()"
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <returns>make sure your IDENTITYTYPE matches precisely with the type returned by the SELECT
		/// Use object for IDENTITYTYPE if you don't want to risk an exception</returns>
		public IDENTITYTYPE Execute<IDENTITYTYPE>( string sqlString ) {
			return Execute<IDENTITYTYPE>( DBHelper.SqlCommand( sqlString ) );
		}
		/// <summary>
		/// Execute a stored procedure and return the first column from returned SELECT. [ExecuteScalar]
		/// If you want the identity after an insert, end statement or stored procedure with "SELECT SCOPE_IDENTITY()"
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <param name="parms">SqlParameter objects</param>
		/// <returns>make sure your IDENTITYTYPE matches precisely with the type returned by the SELECT
		/// Use object for IDENTITYTYPE if you don't want to risk an exception</returns>
		public IDENTITYTYPE Execute<IDENTITYTYPE>( string sqlString, params SqlParameter[] parms ) {
			return Execute<IDENTITYTYPE>( DBHelper.SqlCommand( sqlString, parms ) );
		}
		/// <summary>
		/// Execute a stored procedure and return the first column from returned SELECT. [ExecuteScalar]
		/// If you want the identity after an insert, end statment or stored procedure with "SELECT SCOPE_IDENTITY()"
		/// </summary>
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// <returns>make sure your IDENTITYTYPE matches precisely with the type returned by the SELECT
		/// Use object for IDENTITYTYPE if you don't want to risk an exception</returns>
		public IDENTITYTYPE Execute<IDENTITYTYPE>( SqlCommand cmd ) {
			bool bForceOpen = ( sqlConn.State != ConnectionState.Open );
			if( bForceOpen )
				sqlConn.Open();
			cmd.Connection = sqlConn;
			// Use DataReader instead of ExecuteScalar because scalar gets confused if SP returns both return and dataset results
			IDENTITYTYPE ret=default(IDENTITYTYPE);
			using( SqlDataReader r = GetDataReader( cmd ) ) {
				if( r.Read() ) {
					 ret = (IDENTITYTYPE)r[0];
				}
#if ca2202saysnotrequired
				if( !r.IsClosed )
					r.Close();
#endif
			}//			IDENTITYTYPE ret = (IDENTITYTYPE)cmd.ExecuteScalar();
			if( bForceOpen )
				sqlConn.Close();
			return ret;
		}
		/// <summary>
		/// Execute the sql statement (transactional) from cmd passed in constructor; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int ExecuteTrans() {
			return ExecuteTrans( cmd );
		}
		/// <summary>
		/// Execute the sql statement (transactional) from cmd passed in constructor; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int ExecuteTrans( string sqlString ) {
			return ExecuteTrans( DBHelper.SqlCommand( sqlString) );
		}
		/// <summary>
		/// Execute the sql statement (transactional) from cmd passed in constructor; delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <param name="parms">SqlParameter objects</param>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int ExecuteTrans( string sqlString, params SqlParameter[] parms ) {
			return ExecuteTrans( DBHelper.SqlCommand( sqlString, parms ) );
		}
		/// <summary>
		/// Execute the sql statement (transactional); delete, update, insert, stored proc, etc. [ExecuteNonQuery]
		/// <param name="cmd"> pass the modified SqlCommand object returned GetSqlCommand()</param>
		/// </summary>
		/// <returns>returns rows affected if SQL nocount is set to off</returns>
		public int ExecuteTrans( SqlCommand cmd ) {
			int cnt = 0;
			bool bForceOpen = ( sqlConn.State != ConnectionState.Open );
			if( bForceOpen )
				sqlConn.Open();
			cmd.Connection = sqlConn;
			SqlTransaction sqlTran = sqlConn.BeginTransaction();
			cmd.Transaction = sqlTran;
			try {
				cnt = cmd.ExecuteNonQuery();
				sqlTran.Commit();
			}
			catch( System.Exception ex ) {
				sqlTran.Rollback();
				throw;
			}
			finally {
				if( bForceOpen )
					sqlConn.Close();
			}
			return cnt;
		}

		#endregion


		#region Static SqlParameter Helpers

		public static SqlParameter SqlParameter( string ParamName, string ParamValue ) {
			SqlParameter param = ( ParamValue != null ) ? new SqlParameter( ParamName, ParamValue ) : new SqlParameter( ParamName, DBNull.Value );
			param.SqlDbType = SqlDbType.VarChar;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, char [] ParamValue ) {
			SqlParameter param = ( ParamValue != null ) ? new SqlParameter( ParamName, ParamValue ) : new SqlParameter( ParamName, DBNull.Value );
			param.SqlDbType = SqlDbType.Char;
			param.Size = ParamValue.Length;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, DateTime ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.DateTime;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, DateTime ParamValue, bool small ) {
			SqlParameter param;
			if( !small ) {
				param = new SqlParameter( ParamName, ParamValue );
				param.SqlDbType = SqlDbType.DateTime;
			} else {
				param = new SqlParameter( ParamName, ParamValue.ToSmall() );
				param.SqlDbType = SqlDbType.SmallDateTime;
			}
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, SmallDateTime ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue.ToSmall() );
			param.SqlDbType = SqlDbType.SmallDateTime;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, Int64 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.BigInt;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, Int32 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Int;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, Int16 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.SmallInt;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, UInt64 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.BigInt;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, UInt32 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Int;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, UInt16 ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.SmallInt;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, byte ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.TinyInt;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, bool ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Bit;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, SqlMoney ParamValue, bool small =false ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = !small ? SqlDbType.Money : SqlDbType.SmallMoney;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, Guid ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.UniqueIdentifier;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, decimal ParamValue, byte Prec, byte Scale ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Decimal;
			// Example: Scale 9 Precision 3 means it stores a value with precision of 6 digits and 3 past the dot ######.###
			param.Scale = Scale;
			param.Precision = Prec;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, DataTable ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Structured;
			return param;
		}
		public static SqlParameter SqlParameter( string ParamName, DBNull ParamValue ) {
			SqlParameter param = new SqlParameter( ParamName, ParamValue );
			param.SqlDbType = SqlDbType.Structured;
			return param;
		}

		public static SqlParameter SqlParameterNull( string ParamName, SqlDbType type ) {
			SqlParameter param = new SqlParameter( ParamName, DBNull.Value );
			param.SqlDbType = type;
			return param;
		}

		/// <summary>
		/// (make sure to dispose return result)
		/// </summary>
		public static DataTable SqlDataTable( string[] columns, Type[] coltypes, params object[] cell ) {
			int clen = columns.Length;
			int celllen = cell.Length;
			if((celllen % clen)>0)
				throw new ArgumentException("Cell count does not align with columns.");
			if(clen != coltypes.Length )
				throw new ArgumentException("Column count does not match coltypes count.");
			var ParamValue = new DataTable();
			// Insert columns
			for( int h = 0; h < clen; h++ )
				ParamValue.Columns.Add( columns[h], coltypes[h] );
			int rlen = celllen / clen;
			int pos = 0;
			for( int r = 0; r < rlen; r++ )
				for( int c = 0; c < clen; c++, pos++ )
					ParamValue.Rows[r][c] = cell[pos];
			return ParamValue;
		}
		public static DataTable SqlDataTable( string[] columns, Type[] coltypes, params object[][] rows ) {
			int clen = columns.Length;
			if(clen != coltypes.Length )
				throw new ArgumentException("Column count does not match coltypes count.");
			var ParamValue = new DataTable();
			// Insert columns
			for( int c = 0; c < clen; c++ )
				ParamValue.Columns.Add( columns[c], coltypes[c] );
			for( int r = 0, rlen = rows.Length; r < rlen; r++ )
				ParamValue.Rows.Add( rows[r] );
			return ParamValue;
		}

		#endregion

		#region Static SqlCommand Helpers

		protected static readonly HashSet<string> sqlcmds = new HashSet<string>() {
			"SELECT", "UPDATE", "INSERT", "DELETE", "CREATE", "SET", "EXEC", "EXECUTE", "ALTER", "DECLARE", "DROP", "TRUNCATE", "WITH",
			"ENABLE", "DISABLE", "IF", "CASE", "BEGIN", "--", "/*", "RETURN", "WHILE", "TRY", "WAITFOR", "PRINT", "BULK", "MERGE",
#if AdvancedSql
			"ADD", "GRANT", "CLOSE", "OPEN", "DENY", "REVERT", "REVOKE", "GET", "MOVE", "RECEIVE", "SEND"
#endif
			// Excluded: "USE", "CASE", "GOTO", "END", "BREAK", "CONTINUE", "CATCH", "RAISEERROR", "GO", "OUTPUT", "USING", "OPTION"
		};
		protected const int MAX_SQL_COMMAND_SIZE = 8;
		protected static readonly char[] whitespace = new char[] { ' ', '\t', '\n', '\r', ';' };

		/// <summary>
		/// Get a SqlCommand object for parameter passing or tweaking; 
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// </summary>
		public static SqlCommand SqlCommand( string sqlString ) {
			if( string.IsNullOrEmpty( sqlString ) )
				throw new ArgumentException( "sqlString cannot be empty or null" );
			sqlString = sqlString.TrimStart( whitespace );
			SqlCommand cmd = new SqlCommand( sqlString );
			int len = sqlString.Length;
			int pos = sqlString.IndexOfAny( whitespace );
			if( pos <= -1 || pos > MAX_SQL_COMMAND_SIZE)
				// No white-space or too big to be a sql command
				cmd.CommandType = CommandType.StoredProcedure;
			else
				// white-space occurred 
				cmd.CommandType = ( sqlcmds.Contains( sqlString.Substring( 0, pos ) ) ? CommandType.Text : CommandType.StoredProcedure );//!! write faster lookup
			return cmd;
		}
		/// <summary>
		/// Get a SqlCommand object for parameter passing or tweaking; 
		/// <param name="sqlString"> pass in a SQL SELECT or stored procedure name</param>
		/// <param name="parms">SqlParameter objects</param>
		/// </summary>
		public static SqlCommand SqlCommand( string sqlString, params SqlParameter[] parms ) {
			if( parms == null )
				return SqlCommand( sqlString );
			SqlCommand cmd = SqlCommand( sqlString );
			int len = parms.Length;
			for( int i = 0; i < len; i++ ) {
				SqlParameter p = parms[i];
				if( p != null )
					cmd.Parameters.Add( p );
			}
			return cmd;
		}

		#endregion


		public static DateTime MinSqlDateTime = SqlDateTime.MinValue.Value;
	}

}