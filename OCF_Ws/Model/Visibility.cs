using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OCF_Ws.Model
{
	[DataContract]
	public class Visibility 
	{
		[DataMember(Name = "UtentiUserId", IsRequired = false)]
		public List<string> UtentiUserId { get; set; }
		[DataMember(Name = "UtentiUserIdCC", IsRequired = false)]
		public List<string> UtentiUserIdCC { get; set; }
		[DataMember(Name = "UfficiCodiceUO", IsRequired = false)]
		public List<string> UfficiCodiceUO { get; set; }
		[DataMember(Name = "UfficiCodiceUOCC", IsRequired = false)]
		public List<string> UfficiCodiceUOCC { get; set; }
		[DataMember(Name = "GruppiDescrizione", IsRequired = false)]
		public List<string> GruppiDescrizione { get; set; }
		[DataMember(Name = "GruppiDescrizioneCC", IsRequired = false)]
		public List<string> GruppiDescrizioneCC { get; set; }
		public string ToXml()
		{
			string sToXML = "<Visibility>" + System.Environment.NewLine;
			sToXML += "	" + "<UtentiUserId>" + string.Join<string>(System.Environment.NewLine, UtentiUserId)  + "</UtentiUserId>" + System.Environment.NewLine;
			sToXML += "	" + "<UtentiUserIdCC>" + string.Join<string>(System.Environment.NewLine, UtentiUserIdCC) + "</UtentiUserIdCC>" + System.Environment.NewLine;
			sToXML += "	" + "<UfficiCodiceUO>" + string.Join<string>(System.Environment.NewLine, UfficiCodiceUO) + "</UfficiCodiceUO>" + System.Environment.NewLine;
			sToXML += "	" + "<UfficiCodiceUOCC>" + string.Join<string>(System.Environment.NewLine, UfficiCodiceUOCC) + "</UfficiCodiceUOCC>" + System.Environment.NewLine;
			sToXML += "	" + "<GruppiDescrizione>" + string.Join<string>(System.Environment.NewLine, GruppiDescrizione) + "</GruppiDescrizione>" + System.Environment.NewLine;
			sToXML += "	" + "<GruppiDescrizioneCC>" + string.Join<string>(System.Environment.NewLine, GruppiDescrizioneCC) + "</GruppiDescrizioneCC>" + System.Environment.NewLine;
			sToXML += "</Visibility>";
			return sToXML;
		}
		public bool Validate(out string sDescriptionValidate)
		{
			bool bResult = true;
			sDescriptionValidate = "";
			if (UtentiUserId != null || UtentiUserId.All(x => string.IsNullOrWhiteSpace(x)))
			{
				if (UtentiUserIdCC != null || UtentiUserIdCC.All(x => string.IsNullOrWhiteSpace(x)))
				{
					if (UfficiCodiceUO != null || UfficiCodiceUO.All(x => string.IsNullOrWhiteSpace(x)))
					{
						if (UfficiCodiceUOCC != null || UfficiCodiceUOCC.All(x => string.IsNullOrWhiteSpace(x)))
						{
							if (GruppiDescrizione != null || GruppiDescrizione.All(x => string.IsNullOrWhiteSpace(x)))
							{
								if (GruppiDescrizioneCC != null || GruppiDescrizioneCC.All(x => string.IsNullOrWhiteSpace(x)))
								{
									bResult = false;
								}
							}
						}
					}
				}
			}

			return bResult;
		}
	}
}
