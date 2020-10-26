using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace OCF_Ws.Model
{
	[DataContract]
	public class MainDocumentCRC32b
	{
		[DataMember(Name = "Filename", IsRequired = true)]
		public string Filename { get; set; }
		[DataMember(Name = "BinaryContent",IsRequired = true)]
		public string BinaryContent { get; set; }
		[DataMember(Name = "CRC32b", IsRequired = true)]
		public string CRC32b { get; set; }

		public string ToXml()
		{
			string sToXML = "<MainDoc>" + System.Environment.NewLine;
			sToXML += "	" + "<Filename>" + (string.IsNullOrEmpty(this.Filename) ? "" : this.Filename) + "</Filename>" + System.Environment.NewLine;
			sToXML += "	" + "<BinaryContent>" + (string.IsNullOrEmpty(this.BinaryContent) ? "" : this.BinaryContent) + "</BinaryContent>" + System.Environment.NewLine;
			sToXML += "	" + "<CRC32b>" + (string.IsNullOrEmpty(this.CRC32b) ? "" : this.CRC32b) + "</CRC32b>" + System.Environment.NewLine;
			sToXML += "</MainDoc>";
			return sToXML;
		}
		public bool Validate(out string sDescriptionValidate)
		{
			bool bResult = true;
			sDescriptionValidate = "";
			if (string.IsNullOrEmpty(this.Filename))
			{
				bResult = false;
				sDescriptionValidate = "MainDoc -> Nome file non valorizzato.";
			}
			else if (string.IsNullOrEmpty(this.BinaryContent))
			{
				bResult = false;
				sDescriptionValidate = "MainDoc -> Contenuto file BASE64 non valorizzato.";
			}
			return bResult;
		}
	}

}