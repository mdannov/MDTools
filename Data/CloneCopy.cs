using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Data {

	public abstract class SimpleCopy {
		public void CopyFrom<BASECLASS>( BASECLASS copyMe ) {
			Type t = copyMe.GetType();
			foreach( FieldInfo fieldInf in t.GetFields() ) {
				fieldInf.SetValue( this, fieldInf.GetValue( copyMe ) );
			}
			foreach( PropertyInfo propInf in t.GetProperties() ) {
				if(propInf.CanRead && propInf.CanWrite)
					propInf.SetValue( this, propInf.GetValue( copyMe ) );
			}
		}

	}
}
