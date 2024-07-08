using System;
using System.Collections;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Collections.Generic;

namespace MDTools.Config {

    /*
     * <configurations>
     *   <DataLayerSection>
     *     <DataLayer ID="key" Scope="Page" />
     *     <DataLayer ID="key" Scope="Session" />
     *     <DataLayer ID="key" Scope="Cache" KeyBySession="[true|false] ExpireMins="[mins after create]" Slide="[true|false]" >
     *       <Dependency Type="Key"  Name="key" />
     *       <Dependency Type="File" Name="filename" />
     *       <Dependency Type="SQL"  Name="table" DB="database entry" />
     *     </DataLayer>
     *   </DataLayerSection>
     * </configurations>
     */


    public class DataLayerSection : ConfigurationSection {
        [ConfigurationProperty( "DataLayerSection", IsDefaultCollection = false )]
        [ConfigurationCollection( typeof( DataLayerCollection ), CollectionType = ConfigurationElementCollectionType.BasicMap )]
        public DataLayerCollection DataLayer { get { return (DataLayerCollection) base["DataLayerSection"]; } }
    }

    public class DataLayerCollection : ConfigurationElementCollection {

        public override ConfigurationElementCollectionType CollectionType { get { return ConfigurationElementCollectionType.BasicMap; } }
        public DataLayerElement this[int index] {
            get { return (DataLayerElement) BaseGet( index ); }
            set {
                if( BaseGet( index ) != null )
                    BaseRemoveAt( index );
                BaseAdd( index, value );
            }
        }

        public void Add( DataLayerElement element ) { BaseAdd( element ); }
        public void Clear() { BaseClear(); }
        protected override ConfigurationElement CreateNewElement() { return new DataLayerElement(); }
        protected override object GetElementKey( ConfigurationElement element ) { return ( (DataLayerElement) element ).ID ; }
        public void Remove( DataLayerElement element ) { BaseRemove( element.ID ); }
        public void Remove( string name ) { BaseRemove( name ); }
        public void RemoveAt( int index ) { BaseRemoveAt( index ); }
    }

    public class DataLayerElement : ConfigurationElement {

        [ConfigurationProperty( "ID", IsRequired = true )]
        public string ID { 
            get { return (string) this["ID"]; }
            set { this["ID"] = value; }
        }
        [ConfigurationProperty( "Scope", DefaultValue = "Page", IsRequired = false )]
        public string Scope {   // Page | Session | Cache
            get { return (string) this["Scope"]; }
            set { this["Scope"] = value; }
        }

        #region AppliesToScopeCacheOnly
        [ConfigurationProperty( "KeyBySession", DefaultValue = false, IsRequired = false )]
        public Boolean KeyBySession {
            get { return (Boolean) this["KeyBySession"]; }
            set { this["KeyBySession"] = value; }
        }
        [ConfigurationProperty( "ExpireMins", IsRequired = false )]
        public int ExpireMins {
            get { return (int) this["ExpireMins"]; }
            set { this["ExpireMins"] = value; }
        }
        [ConfigurationProperty( "Slide", DefaultValue = false, IsRequired = false )]
        public Boolean Slide {
            get { return (Boolean) this["Slide"]; }
            set { this["Slide"] = value; }
        }

        // Create a "Dependency" element.
        [ConfigurationProperty( "Dependency", IsDefaultCollection = false )]
        [ConfigurationCollection( typeof( DependencyCollection ), CollectionType = ConfigurationElementCollectionType.BasicMap )]
        public DependencyCollection Dependency { 
            get { return (DependencyCollection) base["Dependency"]; }
            set { this["Dependency"] = value; }
        }

        #endregion
    }

    public class DependencyCollection : ConfigurationElementCollection {

        public override ConfigurationElementCollectionType CollectionType { get { return ConfigurationElementCollectionType.BasicMap; } }
        public DependencyElement this[int index] {
            get { return (DependencyElement) BaseGet( index ); }
            set {
                if( BaseGet( index ) != null )
                    BaseRemoveAt( index );
                BaseAdd( index, value );
            }
        }

        public void Add( DependencyElement element ) { BaseAdd( element ); }
        public void Clear() { BaseClear(); }
        protected override ConfigurationElement CreateNewElement() { return new DependencyElement(); }
        protected override object GetElementKey( ConfigurationElement element ) { var e = (DependencyElement)element; return(e.Type + e.Name); }
        public void Remove( DependencyElement element ) { BaseRemove( element.Type+element.Name ); }
        public void Remove( string name ) { BaseRemove( name ); }
        public void RemoveAt( int index ) { BaseRemoveAt( index ); }
    }


    // Define the "font" element
    // with "name" and "size" attributes.
    public class DependencyElement : ConfigurationElement {
        [ConfigurationProperty( "Type", DefaultValue = "Key", IsRequired = true )]
        public String Type {    // [Key | File | SQL]
            get { return (String) this["Type"]; }
            set { this["Type"] = value; }
        }

        [ConfigurationProperty( "Name", IsRequired = true )]
        public String Name {    // [Key | File | SQL]
            get { return (String) this["Name"]; }
            set { this["Name"] = value; }
        }

        [ConfigurationProperty( "DB", IsRequired = false )]
        public String DB {
            get { return (String) this["DB"]; }
            set { this["DB"] = value; }
        }
    }

    /*public class DataLayerConfig {

        public void DataLayerInit() {
            DataLayerSection section = ConfigurationManager.GetSection("DataLayerSection") as DataLayerSection;
            DataLayerCollection data = section.DataLayer;
//            for(int i=0, len=data.Count; i<len; i++) {
        }
    }*/

}
