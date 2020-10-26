using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace OCF_Ws.Model
{
	[DataContract]
	public class FileProcessed
	{
		[DataMember]
		public string Filename { get; set; }
		[DataMember]
		public string sNumProtocol { get; set; }
		[DataMember]
		public string sOutComeValidationFile { get; set; }

		[DataMember]
		public string BinaryContent  { get; set; }
		public string ToXml()
		{
			string sToXML = "<FileProcessed>" + System.Environment.NewLine;
			sToXML += "	" + "<Filename>" + (string.IsNullOrEmpty(this.Filename) ? "" : this.Filename) + "</Filename>" + System.Environment.NewLine;
			sToXML += "	" + "<sNumProtocol>" + (string.IsNullOrEmpty(this.sNumProtocol) ? "" : this.sNumProtocol) + "</sNumProtocol>" + System.Environment.NewLine;
			sToXML += "	" + "<sOutComeValidationFile>" + (string.IsNullOrEmpty(this.sOutComeValidationFile) ? "" : this.sOutComeValidationFile) + "</sOutComeValidationFile>" + System.Environment.NewLine;
			sToXML += "	" + "<BinaryContent >" + (string.IsNullOrEmpty(this.BinaryContent ) ? "" : this.BinaryContent ) + "</BinaryContent >" + System.Environment.NewLine;
			sToXML += "</FileProcessed>";
			return sToXML;
		}
	}
}