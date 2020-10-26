using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OCF_Ws.Model
{
	[DataContract]
	public class Card 
	{
		[DataMember(Name = "Archivio", IsRequired = false)]
		public string Archivio { get; set; }
		[DataMember(Name = "TipologiaDocumentale", IsRequired = false)]	
		public string TipologiaDocumentale { get; set; }
		[DataMember(Name = "Oggetto", IsRequired = false)]
		public string Oggetto { get; set; }
		[DataMember(Name = "IdInterno", IsRequired = false)]
		public string IdInterno { get; set; }
		[DataMember(Name = "IdProtocollo", IsRequired = false)]
		public string IdProtocollo { get; set; }
		[DataMember(Name = "DataProtocollo", IsRequired = false)]
		public string DataProtocollo { get; set; }
		[DataMember(Name = "DataDocumento", IsRequired = false)]
		public string DataDocumento { get; set; }
		[DataMember(Name = "Guid", IsRequired = false)]
		public string Guid { get; set; }
		[DataMember(Name = "MetaDati", IsRequired = false)]
		public List<string> MetaDati { get; set; }
		[DataMember(Name = "Classificazione", IsRequired = false)]
		public List<string> Classificazione { get; set; }
		[DataMember(Name = "Anagrafica", IsRequired = false)]
		public List<Agraf> Anagrafica { get; set; }
		[DataMember(Name = "Visibilita", IsRequired = false)]
		public Visibility Visibilita { get; set; }
		[DataMember(Name = "MainDocument", IsRequired = false)]
		public MainDocument MainDocument { get; set; }
		[DataMember(Name = "Attachments", IsRequired = false)]
		public List<Attachment> Attachments { get; set; }
	  
		public string ToXml()
		{
			string sToXML = "<Card>" + System.Environment.NewLine;
			sToXML += "	" + "<Archivio>" + (string.IsNullOrEmpty(this.Archivio) ? "" : this.Archivio) + "</Archivio>" + System.Environment.NewLine;
			sToXML += "	" + "<TipologiaDocumentale>" + (string.IsNullOrEmpty(this.TipologiaDocumentale) ? "" : this.TipologiaDocumentale) + "</TipologiaDocumentale>" + System.Environment.NewLine;
			sToXML += "	" + "<IdInterno>" + (string.IsNullOrEmpty(this.IdInterno) ? "" : this.IdInterno) + "</IdInterno>" + System.Environment.NewLine;
			sToXML += "	" + "<DataDocumento>" + (string.IsNullOrEmpty(this.DataDocumento) ? "" : this.DataDocumento) + "</DataDocumento>" + System.Environment.NewLine;
			sToXML += "	" + "<DataProtocollo>" + (string.IsNullOrEmpty(this.DataProtocollo) ? "" : this.DataProtocollo) + "</DataProtocollo>" + System.Environment.NewLine;
			sToXML += "	" + "<IdProtocollo>" + (string.IsNullOrEmpty(this.IdProtocollo) ? "" : this.IdProtocollo) + "</IdProtocollo>" + System.Environment.NewLine;
			sToXML += "	" + "<Guid>" + (string.IsNullOrEmpty(this.Guid) ? "" : this.Guid) + "</Guid>" + System.Environment.NewLine;
			sToXML += "	" + "<MetaDati>" + string.Join<string>(System.Environment.NewLine, MetaDati) + "</MetaDati>" + System.Environment.NewLine;
			sToXML += "	" + "<Classificazione>" + string.Join<string>(System.Environment.NewLine, Classificazione) + "</Classificazione>" + System.Environment.NewLine;
			sToXML += "	" + string.Join<Agraf>(System.Environment.NewLine, Anagrafica);
			sToXML += "	" + Visibilita.ToXml();
			sToXML += "	" + MainDocument.ToXml();
			sToXML += "	" + string.Join<Attachment>(System.Environment.NewLine, Attachments);
			sToXML += "</Card>";
			return sToXML;
		}
		public bool Validate(out string sDescriptionValidate)
		{
			bool bResult = true;
			sDescriptionValidate = "";
			return bResult;
		}
	}
}
