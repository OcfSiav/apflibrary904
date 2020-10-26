using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TestWCFDPO
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
			ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);

			wcfDpo.ServicesClient owcfDpo = new wcfDpo.ServicesClient();
			wcfDpo.MainDocumentChecked oMainDocumentChecked = new wcfDpo.MainDocumentChecked();
			wcfDpo.MainDocument oMainDocument = new wcfDpo.MainDocument();
			Byte[] bytesDocx = File.ReadAllBytes(@"C:\temp\upload_pdf__1005626.pdf.p7m");
			String file = Convert.ToBase64String(bytesDocx);
			oMainDocument.BinaryContent = file;
			oMainDocument.Filename = "test.pdf.p7m";
			List<string> oListString = new List<string>();
			oListString.Add("");
			wcfDpo.Outcome oOutcome = new wcfDpo.Outcome();

			oOutcome = owcfDpo.getSignCheck(out oMainDocumentChecked, oMainDocument, "", oListString,false);

			Console.WriteLine("ATTIVITA' TERMINATA");
			Console.ReadKey();
		}
	}
}
