using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OCF_Ws.Model
{
	[DataContract]
	public class Agraf 
	{
		[DataMember(Name = "AddressBook", IsRequired = true)]
		public string AddressBook { get; set; }
		[DataMember(Name = "Id1", IsRequired = true)]
		public string Id1 { get; set; }
		[DataMember(Name = "Id2", IsRequired = true)]
		public string Id2 { get; set; }
		[DataMember(Name = "Id3", IsRequired = true)]
		public string Id3 { get; set; }
		[DataMember(Name = "TypeEntity", IsRequired = true)]
		public string TypeEntity { get; set; }

		public string ToXml()
		{
			string sToXML = "<Agraf>" + System.Environment.NewLine;
			sToXML += "	" + "<AddressBook>" + (string.IsNullOrEmpty(this.AddressBook) ? "" : this.AddressBook) + "</AddressBook>" + System.Environment.NewLine;
			sToXML += "	" + "<Id1>" + (string.IsNullOrEmpty(this.Id1) ? "" : this.Id1) + "</Id1>" + System.Environment.NewLine;
			sToXML += "	" + "<Id2>" + (string.IsNullOrEmpty(this.Id2) ? "" : this.Id2) + "</Id2>" + System.Environment.NewLine;
			sToXML += "	" + "<Id3>" + (string.IsNullOrEmpty(this.Id3) ? "" : this.Id3) + "</Id3>" + System.Environment.NewLine;
			sToXML += "	" + "<TypeEntity>" + (string.IsNullOrEmpty(this.TypeEntity) ? "" : this.TypeEntity) + "</TypeEntity>" + System.Environment.NewLine;
			sToXML += "</Agraf>";
			return sToXML;
		}
		public bool Validate(out string sDescriptionValidate)
		{
			bool bResult = true;
			sDescriptionValidate = "";
			if (string.IsNullOrEmpty(this.AddressBook))
			{
				bResult = false;
				sDescriptionValidate = "Agraf -> AddressBook non valorizzato.";
			}
			else if (string.IsNullOrEmpty(this.Id1) && string.IsNullOrEmpty(this.Id2) && string.IsNullOrEmpty(this.Id3))
			{
				bResult = false;
				sDescriptionValidate = "Agraf -> Nessun identificativo valorizzato.";
			}
			else if (string.IsNullOrEmpty(this.TypeEntity))
			{
				bResult = false;
				sDescriptionValidate = "Agraf -> Nessuna TypeEntity valorizzata.";
			}
			return bResult;
		}
	}
}
