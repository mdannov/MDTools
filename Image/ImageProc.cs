using System;
using System.Collections.Generic;
using System.Drawing;   
using System.Drawing.Imaging;   
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI.HtmlControls;
using MDTools.IO;

namespace MDTools.Images {
	/// <summary>
	/// Image Processing Helpers
	/// </summary>
	public static class ImageProc {
		/// <summary>
		/// If height of width is larger
		/// </summary>
		public static bool IsLarger( this Size src, int maxWidth, int maxHeight ) { return ( src.Width >= maxWidth && src.Height >= maxHeight ); }
		public static bool IsSmaller( this Size src, int minWidth, int minHeight ) { return ( src.Width <= minWidth && src.Height <= minHeight ); }
		public static bool IsProportional( this Size src ) { return ( src.Width > src.Height ); }
		public static bool IsLandscape( this Size src ) { return ( src.Width > src.Height ); }
		/// <summary>
		/// Src from http://ramanisandeep.wordpress.com/tag/proportional-resizing-of-image-in-aspnet-20/
		/// </summary>
		public static Size ResizeProportionally( this Size src, int maxWidth, int maxHeight ) {
			var ratioX = (float)maxWidth / src.Width;
			var ratioY = (float)maxHeight / src.Height;
			if( ratioY > ratioX) {
				maxHeight = (int)System.Math.Round( src.Height * ratioX );
			} else {
				maxWidth = (int)System.Math.Round( src.Width * ratioY );
			}
			src.Width = maxWidth;
			src.Height = maxHeight;
			return src;
		}
		public static HtmlImage ResizeProportionally( this HtmlImage src, int maxWidth, int maxHeight ) {
			var ratioX = (float)maxWidth / src.Width;
			var ratioY = (float)maxHeight / src.Height;
			if( ratioY > ratioX ) {
				maxHeight = (int)System.Math.Round( src.Height * ratioX );
			} else {
				maxWidth = (int)System.Math.Round( src.Width * ratioY );
			}
			src.Width = maxWidth;
			src.Height = maxHeight;
			return src;
		}
		/*
				public static Size ResizeProportionally( this Size src, int maxWidth, int maxHeight ) {
					// original dimensions
					int w = src.Width;
					int h = src.Height;
					// Longest and shortest dimension
					int longestDimension = ( w > h ) ? w : h;
					int shortestDimension = ( w < h ) ? w : h;
					// proportionality
					float factor = ( (float)longestDimension ) / shortestDimension;
					if( w < h ) {
						// height greater than width 
						src.Width = (int)( maxHeight / factor );
						src.Height = (int)maxHeight;
					} else {
						// width is greater than height
						src.Width = (int)maxWidth;
						src.Height=(int)( maxWidth / factor);
					}
					return src;
				}
		*/
#if integrated
		public string GetPropertyItems() {
			Image img = this;
			StringBuilder sb = new StringBuilder();
			foreach( PropertyItem i in img.PropertyItems ) {
				string s = "UnknownTag";
				try {
					PropertyTag Tag = (PropertyTag)i.Id;
					s = Tag.ToString() + " ";
				}
				catch {

				}
				sb.Append( s + "\r\n" );

				string t = "UnknownType";
				try {
					PropertyTagType Type = (PropertyTagType)i.Type;
					s = Type.ToString() + " ";
				}
				catch {

				}
				sb.Append( string.Format(
				  "\tType:{0}\r\n" +
				  "\tLength:{1}\r\n" +
				  "\tValues:\r\n{2}\r\n",
				  t, i.Len, DumpValues( i.Value )
				  ) );
			}
			return sb.ToString();
		}

		protected string DumpValues( byte[] v ) {
			StringBuilder sb = new StringBuilder();
			int index1 = 0;
			int index2 = 0;
			for( index1 = 0; index1 < v.Length; index1 += 16 ) {
				sb.Append( "\t" );
				for( index2 = 0; index2 < 16; index2++ ) {
					if( index1 + index2 < v.Length )
						sb.Append( v[index1 + index2].ToString( "X02" ) + " " );
					else
						sb.Append( "   " );
				}
				sb.Append( "\t" );
				for( index2 = 0; index2 < 16; index2++ ) {
					if( index1 + index2 < v.Length ) {
						char c = (char)v[index1 + index2];
						if( c < 0x20 )
							sb.Append( "." );
						else
							sb.Append( c );
					}
				}
				sb.Append( "\r\n" );
			}
			return sb.ToString();
		}

		public enum PropertyTagType {
			PropertyTagTypeByte = 1,
			PropertyTagTypeASCII = 2,
			PropertyTagTypeShort = 3,
			PropertyTagTypeLong = 4,
			PropertyTagTypeRational = 5,
			PropertyTagTypeUndefined = 7,
			PropertyTagTypeSLONG = 9,
			PropertyTagTypeSRational = 10
		}

		public enum PropertyTag {
			PropertyTagGpsVer = (int)0x0000,
			PropertyTagGpsLatitudeRef = (int)0x0001,
			PropertyTagGpsLatitude = (int)0x0002,
			PropertyTagGpsLongitudeRef = (int)0x0003,
			PropertyTagGpsLongitude = (int)0x0004,
			PropertyTagGpsAltitudeRef = (int)0x0005,
			PropertyTagGpsAltitude = (int)0x0006,
			PropertyTagGpsGpsTime = (int)0x0007,
			PropertyTagGpsGpsSatellites = (int)0x0008,
			PropertyTagGpsGpsStatus = (int)0x0009,
			PropertyTagGpsGpsMeasureMode = (int)0x000A,
			PropertyTagGpsGpsDop = (int)0x000B,
			PropertyTagGpsSpeedRef = (int)0x000C,
			PropertyTagGpsSpeed = (int)0x000D,
			PropertyTagGpsTrackRef = (int)0x000E,
			PropertyTagGpsTrack = (int)0x000F,
			PropertyTagGpsImgDirRef = (int)0x0010,
			PropertyTagGpsImgDir = (int)0x0011,
			PropertyTagGpsMapDatum = (int)0x0012,
			PropertyTagGpsDestLatRef = (int)0x0013,
			PropertyTagGpsDestLat = (int)0x0014,
			PropertyTagGpsDestLongRef = (int)0x0015,
			PropertyTagGpsDestLong = (int)0x0016,
			PropertyTagGpsDestBearRef = (int)0x0017,
			PropertyTagGpsDestBear = (int)0x0018,
			PropertyTagGpsDestDistRef = (int)0x0019,
			PropertyTagGpsDestDist = (int)0x001A,
			PropertyTagNewSubfileType = (int)0x00FE,
			PropertyTagSubfileType = (int)0x00FF,
			PropertyTagImageWidth = (int)0x0100,
			PropertyTagImageHeight = (int)0x0101,
			PropertyTagBitsPerSample = (int)0x0102,
			PropertyTagCompression = (int)0x0103,
			PropertyTagPhotometricInterp = (int)0x0106,
			PropertyTagThreshHolding = (int)0x0107,
			PropertyTagCellWidth = (int)0x0108,
			PropertyTagCellHeight = (int)0x0109,
			PropertyTagFillOrder = (int)0x010A,
			PropertyTagDocumentName = (int)0x010D,
			PropertyTagImageDescription = (int)0x010E,
			PropertyTagEquipMake = (int)0x010F,
			PropertyTagEquipModel = (int)0x0110,
			PropertyTagStripOffsets = (int)0x0111,
			PropertyTagOrientation = (int)0x0112,
			PropertyTagSamplesPerPixel = (int)0x0115,
			PropertyTagRowsPerStrip = (int)0x0116,
			PropertyTagStripBytesCount = (int)0x0117,
			PropertyTagMinSampleValue = (int)0x0118,
			PropertyTagMaxSampleValue = (int)0x0119,
			PropertyTagXResolution = (int)0x011A,
			PropertyTagYResolution = (int)0x011B,
			PropertyTagPlanarConfig = (int)0x011C,
			PropertyTagPageName = (int)0x011D,
			PropertyTagXPosition = (int)0x011E,
			PropertyTagYPosition = (int)0x011F,
			PropertyTagFreeOffset = (int)0x0120,
			PropertyTagFreeByteCounts = (int)0x0121,
			PropertyTagGrayResponseUnit = (int)0x0122,
			PropertyTagGrayResponseCurve = (int)0x0123,
			PropertyTagT4Option = (int)0x0124,
			PropertyTagT6Option = (int)0x0125,
			PropertyTagResolutionUnit = (int)0x0128,
			PropertyTagPageNumber = (int)0x0129,
			PropertyTagTransferFunction = (int)0x012D,
			PropertyTagSoftwareUsed = (int)0x0131,
			PropertyTagDateTime = (int)0x0132,
			PropertyTagArtist = (int)0x013B,
			PropertyTagHostComputer = (int)0x013C,
			PropertyTagPredictor = (int)0x013D,
			PropertyTagWhitePoint = (int)0x013E,
			PropertyTagPrimaryChromaticities = (int)0x013F,
			PropertyTagColorMap = (int)0x0140,
			PropertyTagHalftoneHints = (int)0x0141,
			PropertyTagTileWidth = (int)0x0142,
			PropertyTagTileLength = (int)0x0143,
			PropertyTagTileOffset = (int)0x0144,
			PropertyTagTileByteCounts = (int)0x0145,
			PropertyTagInkSet = (int)0x014C,
			PropertyTagInkNames = (int)0x014D,
			PropertyTagNumberOfInks = (int)0x014E,
			PropertyTagDotRange = (int)0x0150,
			PropertyTagTargetPrinter = (int)0x0151,
			PropertyTagExtraSamples = (int)0x0152,
			PropertyTagSampleFormat = (int)0x0153,
			PropertyTagSMinSampleValue = (int)0x0154,
			PropertyTagSMaxSampleValue = (int)0x0155,
			PropertyTagTransferRange = (int)0x0156,
			PropertyTagJPEGProc = (int)0x0200,
			PropertyTagJPEGInterFormat = (int)0x0201,
			PropertyTagJPEGInterLength = (int)0x0202,
			PropertyTagJPEGRestartInterval = (int)0x0203,
			PropertyTagJPEGLosslessPredictors = (int)0x0205,
			PropertyTagJPEGPointTransforms = (int)0x0206,
			PropertyTagJPEGQTables = (int)0x0207,
			PropertyTagJPEGDCTables = (int)0x0208,
			PropertyTagJPEGACTables = (int)0x0209,
			PropertyTagYCbCrCoefficients = (int)0x0211,
			PropertyTagYCbCrSubsampling = (int)0x0212,
			PropertyTagYCbCrPositioning = (int)0x0213,
			PropertyTagREFBlackWhite = (int)0x0214,
			PropertyTagGamma = (int)0x0301,
			PropertyTagICCProfileDescriptor = (int)0x0302,
			PropertyTagSRGBRenderingIntent = (int)0x0303,
			PropertyTagImageTitle = (int)0x0320,
			PropertyTagResolutionXUnit = (int)0x5001,
			PropertyTagResolutionYUnit = (int)0x5002,
			PropertyTagResolutionXLengthUnit = (int)0x5003,
			PropertyTagResolutionYLengthUnit = (int)0x5004,
			PropertyTagPrintFlags = (int)0x5005,
			PropertyTagPrintFlagsVersion = (int)0x5006,
			PropertyTagPrintFlagsCrop = (int)0x5007,
			PropertyTagPrintFlagsBleedWidth = (int)0x5008,
			PropertyTagPrintFlagsBleedWidthScale = (int)0x5009,
			PropertyTagHalftoneLPI = (int)0x500A,
			PropertyTagHalftoneLPIUnit = (int)0x500B,
			PropertyTagHalftoneDegree = (int)0x500C,
			PropertyTagHalftoneShape = (int)0x500D,
			PropertyTagHalftoneMisc = (int)0x500E,
			PropertyTagHalftoneScreen = (int)0x500F,
			PropertyTagJPEGQuality = (int)0x5010,
			PropertyTagGridSize = (int)0x5011,
			PropertyTagThumbnailFormat = (int)0x5012,
			PropertyTagThumbnailWidth = (int)0x5013,
			PropertyTagThumbnailHeight = (int)0x5014,
			PropertyTagThumbnailColorDepth = (int)0x5015,
			PropertyTagThumbnailPlanes = (int)0x5016,
			PropertyTagThumbnailRawBytes = (int)0x5017,
			PropertyTagThumbnailSize = (int)0x5018,
			PropertyTagThumbnailCompressedSize = (int)0x5019,
			PropertyTagThumbnailData = (int)0x501B,
			PropertyTagThumbnailImageWidth = (int)0x5020,
			PropertyTagThumbnailImageHeight = (int)0x5021,
			PropertyTagThumbnailBitsPerSample = (int)0x5022,
			PropertyTagThumbnailCompression = (int)0x5023,
			PropertyTagThumbnailPhotometricInterp = (int)0x5024,
			PropertyTagThumbnailImageDescription = (int)0x5025,
			PropertyTagThumbnailEquipMake = (int)0x5026,
			PropertyTagThumbnailEquipModel = (int)0x5027,
			PropertyTagThumbnailStripOffsets = (int)0x5028,
			PropertyTagThumbnailOrientation = (int)0x5029,
			PropertyTagThumbnailSamplesPerPixel = (int)0x502A,
			PropertyTagThumbnailRowsPerStrip = (int)0x502B,
			PropertyTagThumbnailStripBytesCount = (int)0x502C,
			PropertyTagThumbnailResolutionX = (int)0x502D,
			PropertyTagThumbnailResolutionY = (int)0x502E,
			PropertyTagThumbnailPlanarConfig = (int)0x502F,
			PropertyTagThumbnailResolutionUnit = (int)0x5030,
			PropertyTagThumbnailTransferFunction = (int)0x5031,
			PropertyTagThumbnailSoftwareUsed = (int)0x5032,
			PropertyTagThumbnailDateTime = (int)0x5033,
			PropertyTagThumbnailArtist = (int)0x5034,
			PropertyTagThumbnailWhitePoint = (int)0x5035,
			PropertyTagThumbnailPrimaryChromaticities = (int)0x5036,
			PropertyTagThumbnailYCbCrCoefficients = (int)0x5037,
			PropertyTagThumbnailYCbCrSubsampling = (int)0x5038,
			PropertyTagThumbnailYCbCrPositioning = (int)0x5039,
			PropertyTagThumbnailRefBlackWhite = (int)0x503A,
			PropertyTagThumbnailCopyRight = (int)0x503B,
			PropertyTagLuminanceTable = (int)0x5090,
			PropertyTagChrominanceTable = (int)0x5091,
			PropertyTagFrameDelay = (int)0x5100,
			PropertyTagLoopCount = (int)0x5101,
			PropertyTagGlobalPalette = (int)0x5102,
			PropertyTagIndexBackground = (int)0x5103,
			PropertyTagIndexTransparent = (int)0x5104,
			PropertyTagPixelUnit = (int)0x5110,
			PropertyTagPixelPerUnitX = (int)0x5111,
			PropertyTagPixelPerUnitY = (int)0x5112,
			PropertyTagPaletteHistogram = (int)0x5113,
			PropertyTagCopyright = (int)0x8298,
			PropertyTagExifExposureTime = (int)0x829A,
			PropertyTagExifFNumber = (int)0x829D,
			PropertyTagExifIFD = (int)0x8769,
			PropertyTagICCProfile = (int)0x8773,
			PropertyTagExifExposureProg = (int)0x8822,
			PropertyTagExifSpectralSense = (int)0x8824,
			PropertyTagGpsIFD = (int)0x8825,
			PropertyTagExifISOSpeed = (int)0x8827,
			PropertyTagExifOECF = (int)0x8828,
			PropertyTagExifVer = (int)0x9000,
			PropertyTagExifDTOrig = (int)0x9003,
			PropertyTagExifDTDigitized = (int)0x9004,
			PropertyTagExifCompConfig = (int)0x9101,
			PropertyTagExifCompBPP = (int)0x9102,
			PropertyTagExifShutterSpeed = (int)0x9201,
			PropertyTagExifAperture = (int)0x9202,
			PropertyTagExifBrightness = (int)0x9203,
			PropertyTagExifExposureBias = (int)0x9204,
			PropertyTagExifMaxAperture = (int)0x9205,
			PropertyTagExifSubjectDist = (int)0x9206,
			PropertyTagExifMeteringMode = (int)0x9207,
			PropertyTagExifLightSource = (int)0x9208,
			PropertyTagExifFlash = (int)0x9209,
			PropertyTagExifFocalLength = (int)0x920A,
			PropertyTagExifMakerNote = (int)0x927C,
			PropertyTagExifUserComment = (int)0x9286,
			PropertyTagExifDTSubsec = (int)0x9290,
			PropertyTagExifDTOrigSS = (int)0x9291,
			PropertyTagExifDTDigSS = (int)0x9292,
			PropertyTagExifFPXVer = (int)0xA000,
			PropertyTagExifColorSpace = (int)0xA001,
			PropertyTagExifPixXDim = (int)0xA002,
			PropertyTagExifPixYDim = (int)0xA003,
			PropertyTagExifRelatedWav = (int)0xA004,
			PropertyTagExifInterop = (int)0xA005,
			PropertyTagExifFlashEnergy = (int)0xA20B,
			PropertyTagExifSpatialFR = (int)0xA20C,
			PropertyTagExifFocalXRes = (int)0xA20E,
			PropertyTagExifFocalYRes = (int)0xA20F,
			PropertyTagExifFocalResUnit = (int)0xA210,
			PropertyTagExifSubjectLoc = (int)0xA214,
			PropertyTagExifExposureIndex = (int)0xA215,
			PropertyTagExifSensingMethod = (int)0xA217,
			PropertyTagExifFileSource = (int)0xA300,
			PropertyTagExifSceneType = (int)0xA301,
			PropertyTagExifCfaPattern = (int)0xA302
		}
#endif
	}

	//!! Test this function. It's supposed to significantly optimize file size without significant loss of quality
	/// <summary>
	/// Provides various image untilities, such as high quality resizing and the ability to save a JPEG.
	/// http://stackoverflow.com/questions/249587/high-quality-image-scaling-c-sharp
	/// //resize the image to the specified height and width
	/// using (var resized = ImageUtilities.ResizeImage(image, 50, 100)) {
    ///		// save the resized image as a jpeg with a quality of 90
	///		ImageUtilities.SaveJpeg(@"C:\myimage.jpeg", resized, 90);
	/// }
	/// </summary>
	public static class ImageUtilities {
		/// <summary>
		/// A quick lookup for getting image encoders
		/// </summary>
		private static Dictionary<string, ImageCodecInfo> encoders = null;

		/// <summary>
		/// A quick lookup for getting image encoders
		/// </summary>
		public static Dictionary<string, ImageCodecInfo> Encoders {
			//get accessor that creates the dictionary on demand
			get {
				//if the quick lookup isn't initialised, initialise it
				if( encoders == null ) {
					encoders = new Dictionary<string, ImageCodecInfo>();
				}

				//if there are no codecs, try loading them
				if( encoders.Count == 0 ) {
					//get all the codecs
					foreach( ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders() ) {
						//add each codec to the quick lookup
						encoders.Add( codec.MimeType.ToLower(), codec );
					}
				}

				//return the lookup
				return encoders;
			}
		}

		/// <summary>
		/// Resize the image to the specified width and height.
		/// </summary>
		/// <param name="image">The image to resize.</param>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		public static System.Drawing.Bitmap ResizeImage( System.Drawing.Image image, int width, int height ) {
			//a holder for the result
			Bitmap result = new Bitmap( width, height );
			//set the resolutions the same to avoid cropping due to resolution differences
			result.SetResolution( image.HorizontalResolution, image.VerticalResolution );

			//use a graphics object to draw the resized image into the bitmap
			using( Graphics graphics = Graphics.FromImage( result ) ) {
				//set the resize quality modes to high quality
				graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				//draw the image into the target bitmap
				graphics.DrawImage( image, 0, 0, result.Width, result.Height );
			}

			//return the resulting bitmap
			return result;
		}

		/// <summary> 
		/// Saves an image as a jpeg image, with the given quality 
		/// </summary> 
		/// <param name="path">Path to which the image would be saved.</param> 
		/// <param name="quality">An integer from 0 to 100, with 100 being the 
		/// highest quality</param> 
		/// <exception cref="ArgumentOutOfRangeException">
		/// An invalid value was entered for image quality.
		/// </exception>
		public static void SaveJpeg( string path, Image image, int quality ) {
			//ensure the quality is within the correct range
			if( ( quality < 0 ) || ( quality > 100 ) ) {
				//create the error message
				string error = string.Format( "Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality );
				//throw a helpful exception
				throw new ArgumentOutOfRangeException( error );
			}

			//create an encoder parameter for the image quality
			EncoderParameter qualityParam = new EncoderParameter( System.Drawing.Imaging.Encoder.Quality, quality );
			//get the jpeg codec
			ImageCodecInfo jpegCodec = GetEncoderInfo( "image/jpeg" );

			//create a collection of all parameters that we will pass to the encoder
			EncoderParameters encoderParams = new EncoderParameters( 1 );
			//set the quality parameter for the codec
			encoderParams.Param[0] = qualityParam;
			//save the image using the codec and the parameters
			image.Save( path, jpegCodec, encoderParams );
		}

		/// <summary> 
		/// Returns the image codec with the given mime type 
		/// </summary> 
		public static ImageCodecInfo GetEncoderInfo( string mimeType ) {
			//do a case insensitive search for the mime type
			string lookupKey = mimeType.ToLower();

			//the codec to return, default to null
			ImageCodecInfo foundCodec = null;

			//if we have the encoder, get it to return
			if( Encoders.ContainsKey( lookupKey ) ) {
				//pull the codec from the lookup
				foundCodec = Encoders[lookupKey];
			}

			return foundCodec;
		}

		/// <summary>
		/// Only process if shrink is required; resizes proportionally
		/// </summary>
		public static byte[] ResizeImage( String fullFilePath, int width, int height ) {
			byte[] data = null;
			using( System.Drawing.Image Img = System.Drawing.Image.FromFile( fullFilePath ) ) {
				// Check if Resize is not required if Image is smaller than limits
				if( Img.Size.IsSmaller( width, height ) )
					return IOHelper.BinaryFromFile( fullFilePath );
				// Resize Required
				var revsize = Img.Size.ResizeProportionally( width, height );
				//!! Consider using ImageProc.cs: ImageUtilities.ResizeImage to see if you get smaller files; also check performance
				using( Bitmap bitmap = new Bitmap( Img, revsize.Width, revsize.Height ) ) {
					using( System.IO.MemoryStream outStream = new System.IO.MemoryStream() ) {
						bitmap.Save( outStream, Img.RawFormat );
						data = outStream.ToArray();
					}
				}
			}
			return data;
		}

	}
}