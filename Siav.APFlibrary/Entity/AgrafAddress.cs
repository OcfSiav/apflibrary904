using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Siav.APFlibrary.Entity
{
	public class AgrafAddress
	{
		public string Cap { get; set; }
		public string Citta { get; set; }
		public string Numero { get; set; }
		public string Provincia { get; set; }
		public string Stato { get; set; }
		public string Via { get; set; }
		public bool isMain { get; set; }
	}
}