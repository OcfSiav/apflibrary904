using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCF_Ws.Model
{
	public class EsitoCheckFileSigned
	{
		public bool Check { get; set; }
		public string Descrizione { get; set; }
		public byte[] byteContentDecrypt { get; set; }
		public string FileNameDecrypt { get; set; }
	}
}