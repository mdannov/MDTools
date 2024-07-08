using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace MDTools.IO {

	public static class IOHelper {
		/// <summary>
		/// Check that the file is ready to create - verifies doesn't already exist and directory structure in place
		/// </summary>
		/// <param name="createDirs">tells function to create directory structure sa required</param>
		/// <returns></returns>
		public static bool VerifyDirectoryInPath( string path, bool createDirIfNec ) {
			var Dir = System.IO.Path.GetDirectoryName( path );
			if( createDirIfNec ) {
				Directory.CreateDirectory( Dir );	// create dir does nothing it it already exists
				return true;
			}
			return( Directory.Exists( Dir ));
		}

		public static byte[] BinaryFromFile( string fullFilePath ) {
#if testifthisisfasterandreliable //!!
			return File.ReadAllBytes( fullFilePath );
#endif
			byte[] data = null;
			using( FileStream sourceFile = new FileStream( fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read ) ) {
				int FileSize = (int)sourceFile.Length;
				data = new byte[FileSize];
				sourceFile.Read( data, 0, FileSize );
			}
			return data;
		}
		public static void BinaryToFile( string fullFilePath, byte[] data ) {
			File.WriteAllBytes( fullFilePath, data );
		}

		public static string TextFromFile( string fullFilePath ) {
#if testifthisisfasterandreliable //!!
			return File.ReadAllText( fullFilePath );
#endif
			using( StreamReader reader = new StreamReader( fullFilePath ) ) {
				return reader.ReadToEnd();
			}
		}
		public static void TextToFile( string fullFilePath, string text ) {
			File.WriteAllText( fullFilePath, text );
		}

	}

}