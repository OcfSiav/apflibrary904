using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SendReportFromSQL
{
	class Program
	{
		private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
		{
			var certificate = (X509Certificate2)cert;
			return true;
		}
		static void Main(string[] args)
		{
			Siav.APFlibrary.Flux apfLibrary2 = new Siav.APFlibrary.Flux();
			string sTipo = string.Empty;
			string sIdComMax = string.Empty;
			try
			{
				apfLibrary2.CreateReportFromSQL("E_ComunicazionePecUacfMI§E_ComunicazionePecUacfRM§E_VariazioniComPecUacfMI§E_VariazioniComPecUacfRM§E_VariazioniVarPecUacfMI§E_VariazioniVarPecUacfRM", 
					"",
					"cecilia.macaluso@organismocf.it;daniela.albasini@organismocf.it;marzia.fattorini@organismocf.it;valeria.spadaro@organismocf.it"
					,"valeria.santocchi@organismocf.it;giorgio.furiani@organismocf.it;documentale.wf@organismocf.it");
			}
			catch (Exception exc)
			{
				Console.Write(exc.ToString());
			}
//			Console.ReadLine();
		}
	}
}
