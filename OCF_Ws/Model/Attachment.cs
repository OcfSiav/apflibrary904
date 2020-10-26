using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OCF_Ws.Model
{
	[DataContract]
	public class Attachment 
	{
		[DataMember(Name = "BinaryContent", IsRequired = true)]
		public string BinaryContent { get; set; }
		[DataMember(Name = "Filename", IsRequired = true)]
		public string Filename { get; set; }
		[DataMember(Name = "Note", Order = 1, IsRequired = false, EmitDefaultValue = false)]
		public string Note { get; set; }
		public string ToXml()
		{
			string sToXML = "<Attachment>" + System.Environment.NewLine;
			sToXML += "	" + "<Filename>" + (string.IsNullOrEmpty(this.Filename) ? "" : this.Filename) + "</Filename>" + System.Environment.NewLine;
			sToXML += "	" + "<Note>" + (string.IsNullOrEmpty(this.Note) ? "" : this.Note) + "</Note>" + System.Environment.NewLine;
			sToXML += "	" + "<BinaryContent>" + (string.IsNullOrEmpty(this.BinaryContent) ? "" : this.BinaryContent) + "</BinaryContent>" + System.Environment.NewLine;
			sToXML += "</Attachment>";
			return sToXML;
		}
		public bool Validate(out string sDescriptionValidate)
		{
			bool bResult = true;
			sDescriptionValidate = "";
			if (string.IsNullOrEmpty(this.Filename))
			{
				bResult = false;
				sDescriptionValidate = "Attachment -> Nome file non valorizzato.";
			}
			else if (string.IsNullOrEmpty(this.BinaryContent))
			{
				bResult = false;
				sDescriptionValidate = "Attachment -> Contenuto file BASE64 non valorizzato.";
			}
			return bResult;
		}
	}
}
