
using Siav.APFlibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WcfSiav.APFlibrary.Model
{
	[DataContract]
	public class InputAgrafBiz: IValidation
	{
		[DataMember]
		public string Nome { get; set; }
		[DataMember]
		public string Telefono { get; set; }
		[DataMember]
		public string Cellulare { get; set; }
		[DataMember]
		public string EmailPEO { get; set; }
		[DataMember]
		public string EmailPEC { get; set; }
		[DataMember]
		public string Via { get; set; }
		[DataMember]
		public string Numero { get; set; }
		[DataMember]
		public string Cap { get; set; }
		[DataMember]
		public string Citta { get; set; }
		[DataMember]
		public string Provincia { get; set; }
		[DataMember]
		public string Nazione { get; set; }
		[DataMember]
		public string PartitaIva { get; set; }
		[DataMember]
		public string ID { get; set; }
		[DataMember]
		public string CodiceAAMS { get; set; }
		[DataMember]
		public string CodiceGestoreRicevitoria { get; set; }
		[DataMember]
		public string Annotazioni { get; set; }
		[DataMember]
		public string Disabilitato { get; set; }

		public bool Validate(out string description)
		{
			Boolean bCheck = true;
			description = "";
			if (string.IsNullOrEmpty(this.ID) && string.IsNullOrEmpty(this.CodiceAAMS) && string.IsNullOrEmpty(this.CodiceGestoreRicevitoria)) {
				description = "Non è stato rilevato alcun campo identificativo valorizzato.";
				bCheck = false;
			}
			return bCheck;
		}
		public string ToXml()
		{
			string sToXML = "<InputAgrafBiz>" + System.Environment.NewLine;
			sToXML += "	" + "<Nome>" + (string.IsNullOrEmpty(this.Nome) ? "" : this.Nome) + "</Nome>" + System.Environment.NewLine;
			sToXML += "	" + "<ID>" + (string.IsNullOrEmpty(this.ID) ? "" : this.Nome) + "</ID>" + System.Environment.NewLine;
			sToXML += "	" + "<CodiceAAMS>" + (string.IsNullOrEmpty(this.CodiceAAMS) ? "" : this.CodiceAAMS) + "</CodiceAAMS>" + System.Environment.NewLine;
			sToXML += "	" + "<CodiceGestoreRicevitoria>" + (string.IsNullOrEmpty(this.CodiceGestoreRicevitoria) ? "" : this.CodiceGestoreRicevitoria) + "</CodiceGestoreRicevitoria>" + System.Environment.NewLine;
			sToXML += "	" + "<PartitaIva>" + (string.IsNullOrEmpty(this.PartitaIva) ? "" : this.PartitaIva) + "</PartitaIva>" + System.Environment.NewLine;
			sToXML += "	" + "<Annotazioni>" + (string.IsNullOrEmpty(this.Annotazioni) ? "" : this.Annotazioni) + "</Annotazioni>" + System.Environment.NewLine;
			sToXML += "	" + "<Disabilitato>" + (string.IsNullOrEmpty(this.Disabilitato) ? "" : this.PartitaIva) + "</Disabilitato>" + System.Environment.NewLine;
			sToXML += "	" + "<ADDRESSES>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "<ADDRESS>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Via>" + (string.IsNullOrEmpty(this.Via) ? "" : this.Via) + "</Via>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Numero>" + (string.IsNullOrEmpty(this.Numero) ? "" : this.Numero) + "</Numero>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Citta>" + (string.IsNullOrEmpty(this.Citta) ? "" : this.Citta) + "</Citta>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Provincia>" + (string.IsNullOrEmpty(this.Provincia) ? "" : this.Provincia) + "</Provincia>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Cap>" + (string.IsNullOrEmpty(this.Cap) ? "" : this.Cap) + "</Cap>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<Nazione>" + (string.IsNullOrEmpty(this.Nazione) ? "" : this.Nazione) + "</Nazione>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "</ADDRESS>" + System.Environment.NewLine;
			sToXML += "	" + "</ADDRESSES>" + System.Environment.NewLine;
			sToXML += "	" + "<EMAILS>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "<EMAIL>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<PEO>" + (string.IsNullOrEmpty(this.EmailPEO) ? "" : this.EmailPEO) + "</PEO>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "</EMAIL>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "<EMAIL>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<PEC>" + (string.IsNullOrEmpty(this.EmailPEC) ? "" : this.EmailPEC) + "</PEC>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "</EMAIL>" + System.Environment.NewLine;
			sToXML += "	" + "</EMAILS>" + System.Environment.NewLine;
			sToXML += "	" + "<PHONES>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "<PHONE>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<OFFICE>" + (string.IsNullOrEmpty(this.Telefono) ? "" : this.Telefono) + "</OFFICE>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "</PHONE>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "<PHONE>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "	" + "<MOBILE>" + (string.IsNullOrEmpty(this.Cellulare) ? "" : this.Cellulare) + "</MOBILE>" + System.Environment.NewLine;
			sToXML += "	" + "	" + "</PHONE>" + System.Environment.NewLine;
			sToXML += "	" + "</PHONES>" + System.Environment.NewLine;
			sToXML += "</InputAgrafBiz>";
			return sToXML;
		}
	}
}
