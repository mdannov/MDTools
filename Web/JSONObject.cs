/*
 * 
 * Based on code from .... Updated/Enhanced by Michael Dannov 2011-2013
 * 
 * 
 * Copyright 2010 Facebook, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may
 * not use this file except in compliance with the License. You may obtain
 * a copy of the License at
 *  http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace MDTools.Web {

	/// <summary>
	/// Represents an object encoded in JSON. Can be either a dictionary 
	/// mapping strings to other objects, an array of objects, or a single 
	/// object, which represents a scalar.
	/// </summary>
	public class JSONObject {
		/// <summary>
		/// Creates a JSONObject by parsing a string.
		/// This is the only correct way to create a JSONObject.
		/// </summary>
		public static JSONObject Create( string s ) {
			JavaScriptSerializer js = new JavaScriptSerializer();
			return _Create( js.DeserializeObject( s ) );
		}
		public static JSONObject SetString( string s ) {
			var json = new JSONObject();
			json._stringData = s;
			return json;
		}

		/// <summary>
		/// Returns true if this JSONObject represents a dictionary.
		/// </summary>
		public bool IsDictionary { get { return _dictData != null; } }

		/// <summary>
		/// Returns true if this JSONObject represents an array.
		/// </summary>
		public bool IsArray { get { return _arrayData != null; } }

		/// <summary>
		/// Returns true if this JSONObject represents a string value. 
		/// </summary>
		public bool IsString { get { return _stringData != null; } }

		/// <summary>
		/// Returns true if this JSONObject represents an integer value.
		/// </summary>
		public bool IsInteger { get { Int64 tmp; return Int64.TryParse( _stringData, out tmp ); } }

		/// <summary>
		/// Returns true if this JSONObject represents an double value.
		/// </summary>
		public bool IsDouble { get { double tmp; return double.TryParse( _stringData, out tmp ); } }

		/// <summary>
		/// Returns true if this JSONOBject represents a boolean value.
		/// </summary>
		public bool IsBoolean { get { bool tmp; return bool.TryParse( _stringData, out tmp ); } }

		/// <summary>
		/// Returns this JSONObject as a dictionary
		/// </summary>
		public Dictionary<string, JSONObject> Dictionary { get { return _dictData; } }

		/// <summary>
		/// Returns this JSONObject from the dictionary
		/// </summary>
		public JSONObject this[string key] { get { JSONObject tmp=null; _dictData.TryGetValue(key, out tmp); return tmp==null ? new JSONObject() : tmp; } }
	
		/// <summary>
		/// Returns this JSONObject as an array
		/// </summary>
		public JSONObject[] Array { get { return _arrayData; } }

		/// <summary>
		/// Returns this JSONObject from the dictionary
		/// </summary>
		public JSONObject this[int index] { get { return _arrayData[index]; } }

		/// <summary>
		/// Returns this JSONObject as a string
		/// </summary>
		public string String { get { return _stringData; } }

		/// <summary>
		/// Returns this JSONObject as an integer
		/// </summary>
		public Int64 Integer { get { Int64 tmp=0; Int64.TryParse( _stringData, out tmp ); return tmp; } }
		/// <summary>
		/// Returns this JSONObject as an integer with a default
		/// </summary>
		public Int64 ToInteger(Int64 def) { Int64 tmp = def; Int64.TryParse( _stringData, out tmp ); return tmp; }

		/// <summary>
		/// Returns this JSONObject as an double
		/// </summary>
		public double Double { get { double tmp = 0; double.TryParse( _stringData, out tmp ); return tmp; } }
		/// <summary>
		/// Returns this JSONObject as an double with a default
		/// </summary>
		public double ToDouble( double def ) { double tmp = def; double.TryParse( _stringData, out tmp ); return tmp; }

		/// <summary>
		/// Returns this JSONObject as a boolean
		/// </summary>
		public bool Boolean { get { bool tmp=false; bool.TryParse( _stringData, out tmp ); return tmp; } }
		/// <summary>
		/// Returns this JSONObject as a boolean
		/// </summary>
		public bool ToBoolean(bool def) { bool tmp = def; bool.TryParse( _stringData, out tmp ); return tmp; }

		/// <summary>
		/// Returns this JSONObject as a DateTime
		/// </summary>
		public DateTime DateTime { get { DateTime tmp = default( DateTime ); DateTime.TryParse( _stringData, out tmp ); return tmp; } }
		/// <summary>
		/// Returns this JSONObject as a DateTime
		/// </summary>
		public DateTime ToDateTime( DateTime def ) { DateTime tmp = def; DateTime.TryParse( _stringData, out tmp ); return tmp; }

	
		/// <summary>
		/// Prints the JSONObject as a formatted string, suitable for viewing.
		/// </summary>
		public new string ToString() {
			StringBuilder sb = new StringBuilder();
			_RecursiveObjectToString( this, sb, 0 );
			return sb.ToString();
		}

		#region Private Members

		private string _stringData=null;
		private JSONObject[] _arrayData=null;
		private Dictionary<string, JSONObject> _dictData=null;

		private JSONObject() { }

		/// <summary>
		/// Recursively constructs this JSONObject 
		/// </summary>
		private static JSONObject _Create( object o ) {
			JSONObject json = new JSONObject();
			if( o is object[] ) {
				object[] objArray = o as object[];
				int len = objArray.Length;
				json._arrayData = new JSONObject[len];
				for( int i = 0; i < len; ++i ) {
					json._arrayData[i] = _Create( objArray[i] );
				}
			} else if( o is Dictionary<string, object> ) {
				json._dictData = new Dictionary<string, JSONObject>();
				Dictionary<string, object> dict = o as Dictionary<string, object>;
				foreach( string key in dict.Keys )
					json._dictData[key] = _Create( dict[key] );
			} else if( o != null ) // o is a scalar {
				json._stringData = o.ToString();
			return json;
		}

		private static void _RecursiveObjectToString( JSONObject obj, StringBuilder sb, int level ) {
			if( obj.IsDictionary ) {
				sb.AppendLine();
				_RecursiveDictionaryToString( obj, sb, level + 1 );
			} else if( obj.IsArray ) {
				foreach( JSONObject o in obj.Array ) {
					_RecursiveObjectToString( o, sb, level );
					sb.AppendLine();
				}
			} else // some sort of scalar value {
				sb.Append( obj.String );
		}

		private static void _RecursiveDictionaryToString( JSONObject obj, StringBuilder sb, int level ) {
			foreach( KeyValuePair<string, JSONObject> kvp in obj.Dictionary ) {
				sb.Append( '\t', level );
				sb.Append( kvp.Key );
				sb.Append( " => " );
				_RecursiveObjectToString( kvp.Value, sb, level );
				sb.AppendLine();
			}
		}

		#endregion

	}

}
