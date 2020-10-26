using Siav.APFlibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
namespace TestLibrary
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
				Siav.APFlibrary.Action.GenComMassive test = new Siav.APFlibrary.Action.GenComMassive();
				test.ProtocolBillFromPEC("", out string outputTest);
				if (args.Length > 0)
				{
					foreach (Object obj in args)
					{
						if (obj.ToString().ToUpper().Trim() == "ISCRIZIONE")
						{
							sTipo = obj.ToString().ToUpper().Trim();
						}
						else if (obj.ToString().ToUpper().Trim() == "CANCELLAZIONE")
						{
							sTipo = obj.ToString().ToUpper().Trim();
						}
						else if (obj.ToString().ToUpper().Trim() == "INGIUNZIONE")
						{
							sTipo = obj.ToString().ToUpper().Trim();
						}
						else if (obj.ToString().ToUpper().Trim() == "PROVAVALUTATIVA")
						{
							sTipo = obj.ToString().ToUpper().Trim();
						}
						else
							sIdComMax = obj.ToString().ToUpper().Trim();
					}
				}
				else
				{
					Console.WriteLine("Nessuna query individuata come argomento.");
				}
				if (!string.IsNullOrEmpty(sTipo) && !string.IsNullOrEmpty(sIdComMax))
				{
					if (sTipo == "ISCRIZIONE")
					{
						Console.WriteLine("Avvio processo di iscrizione avente ID COMUMINCAZIONE MASSIVA MAX: " + sIdComMax);
						apfLibrary2.InsertInProtocolArchive(sIdComMax);
						Console.WriteLine("FINE");
					}
					else if (sTipo == "CANCELLAZIONE")
					{
						Console.WriteLine("Avvio processo di cancellazione avente ID COMUMINCAZIONE MASSIVA MAX: " + sIdComMax);
						apfLibrary2.InsertInProtocolCancellazione(sIdComMax);
						Console.WriteLine("FINE");

					}
					else if (sTipo == "INGIUNZIONE")
					{
						Console.WriteLine("Avvio processo di ingiunzione avente ID COMUMINCAZIONE MASSIVA MAX: " + sIdComMax);
						apfLibrary2.InsertInProtocolArchiveIngiunzione(sIdComMax);
						Console.WriteLine("FINE");
					}
					else if (sTipo == "PROVAVALUTATIVA")
					{
						Console.WriteLine("Avvio processo di prova valutativa avente ID COMUMINCAZIONE MASSIVA MAX: " + sIdComMax);
						apfLibrary2.InsertInProtocolArchiveProvaValutativa(sIdComMax);
						Console.WriteLine("FINE");
					}
				}
				Console.ReadLine();
				//string sOutput = "";
				//apfLibrary2.VerifyStep1Ingiunzione("317443", ref sOutput);
				//				apfLibrary2.CreateCardReport("Date", "09/11/2019");
				//apfLibrary2.InsertInProtocolArchive("918/19");
				/*
		Guid g = new Guid("57F67098-00A9-4F78-A729-000000012345");
		int pos = g.ToString().LastIndexOf('-');
		string part = g.ToString().Substring(pos + 1);
		long aresult = long.Parse(part);
		Console.WriteLine(aresult.ToString());
		string sF="";//57F67098-00A9-4F78-A729-0000000
					 // bool testconvert = apfLibrary2.ConvertMainDocument2Pdf("65886");
		apfLibrary2.VerifyStep1("66938",ref sF);

		string outputString2 = "";
		Guid oCardId2;
		apfLibrary2.CasellarioCreator("66214", ref sF);

		string sGuidCard2 = "65658"; //Cancellazione a domanda //"1249"; // Cancellazzione OMPAG avvio procedimento // "965";// Prova valutativa;
		if (sGuidCard2.Length > 12)                 // set the guid of the card
			oCardId2 = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard2.Substring(24, 12)).ToString("000000000000"));
		else
			oCardId2 = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard2).ToString("000000000000"));
		Siav.APFlibrary.Flux apfLibrary = new Siav.APFlibrary.Flux();//14140

		apfLibrary.InsertInProtocolArchiveProvaValutativa("3893/17");

		apfLibrary.InsertInProtocolArchiveProvaValutativa("1579/16");
		apfLibrary2.InsertInProtocolCancellazione("3838/17");

		apfLibrary2.VerifyStep1Cancellazione(oCardId2.ToString(), ref outputString2);

		string sout = "";
		apfLibrary2.CasellarioCreator("40920", ref sout);
		apfLibrary2.InsertInProtocolCancellazione("195/17");

		//apfLibrary2.InsertInProtocolCancellazione("6171/16");
		// ATTENZIONE implementare l'allegato circolare con la tipologia in entrata.
		string sTest = "000000001 POSIZIONE GRANDI/MAURIZIO";
		string parteNome = sTest.Substring(sTest.IndexOf("POSIZIONE") + "POSIZIONE".Length +1, sTest.Length - sTest.IndexOf("POSIZIONE") - "POSIZIONE".Length -1);
		string partePosizione = sTest.Substring(0, sTest.IndexOf("POSIZIONE") -1);

		string sResult = "";
		apfLibrary2.PredCasellarioCreator("40675", ref sResult);
		apfLibrary2.CasellarioCreator("40593", ref sResult);


		apfLibrary2.CreateReportMassiveCanc("6171/16", "33557");

		Rijndael aaaa = new Rijndael();
		string s = aaaa.Encrypt("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.29.59)(PORT=1521))(CONNECT_DATA=(FAILOVER_MODE=(TYPE=select)(METHOD=basic))(SERVER=dedicated)(SERVICE_NAME=archpro)));User Id=ARCHIFLOW;Password=ARCHIFLOW;");
		string c = aaaa.Decrypt(s);
		// Location and name of the XML file
		string filePathName = @"C:\Users\ERusso\documents\visual studio 2013\Projects\TestAPFLibrary\Siav.APFlibrary\Siav.APFlibrary\App_GlobalResources\ApfResource.resx";
		// Load the XML into memory
		XDocument xdoc = XDocument.Load(filePathName);
		string keyToCheck = "emailPortDefault";
		// Query the document
		XElement result = xdoc.Root.Descendants("data")
							  .Where(k => k.Attribute("name").Value== keyToCheck)
							  .Select(k => k)
							  .FirstOrDefault();
		result.Element("value").FirstNode.ToString();



		ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
		//HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://app01-docu-test.apf/SvCoverterWeb/Converter_xml.asp");
		// byte[] bytes;
		/*string sPathDocx=@"C:\Temp\1.docx";
		Byte[] bytesDocx = File.ReadAllBytes(sPathDocx);
		String file = Convert.ToBase64String(bytesDocx);
		string sBodyXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CONVERT_DOCUMENT xmlns:dt=\"urn:schemas-microsoft-com:datatypes\"><EXT dt:dt=\"string\">DOCX</EXT><DOCUMENT dt:dt=\"bin.base64\">" + file +
							"</DOCUMENT></CONVERT_DOCUMENT>";
		//
		byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(sBodyXml);
		request.Method = "POST";
		request.ContentType = "text/xml;charset=utf-8";
		request.ContentLength = requestBytes.Length;
		Stream requestStream = request.GetRequestStream();
		requestStream.Write(requestBytes, 0, requestBytes.Length);
		requestStream.Close();

		HttpWebResponse res = (HttpWebResponse)request.GetResponse();
		StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
		string backstr = sr.ReadToEnd();
		XmlDocument doc = new XmlDocument();
		doc.LoadXml(backstr);
		XmlNode node = doc.DocumentElement.SelectSingleNode("/CONVERTED_DOCUMENT");

		File.WriteAllBytes(@"c:\temp\example.pdf", Convert.FromBase64String(node.InnerText));
		//

		Guid oCardId;*/
				/*              https://app01-docu-test.apf/SvCoverterWeb       */
				/*
							 string outputString = "";
								Guid oCardId;

								string sGuidCard = "26950"; //Cancellazione a domanda //"1249"; // Cancellazzione OMPAG avvio procedimento // "965";// Prova valutativa;
								if (sGuidCard.Length > 12)                 // set the guid of the card
									oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
								else
									oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
							   // Siav.APFlibrary.Flux apfLibrary = new Siav.APFlibrary.Flux();//14140
								apfLibrary.CreateReportMassiveIng("3208/16", "15658");
								apfLibrary.VerifyStep1Cancellazione(oCardId.ToString(), ref outputString);


								apfLibrary.VerifyStep1ProvaValutativa(oCardId.ToString(), ref outputString);
								apfLibrary.InsertInProtocolArchiveProvaValutativa("2467/16");
								apfLibrary.VerifyStep1Cancellazione(oCardId.ToString(), ref outputString);
								apfLibrary.CreateReportMassiveIng("3208/16", "15658");
								apfLibrary.InsertInProtocolArchiveIngiunzione("3208/16");

								apfLibrary.InsertInProtocolCancellazione("2827/16");
								//apfLibrary.GetBiggerCardsReadyToProtocolProvaValutativa("1579/16",false);
								apfLibrary.VerifyStep1Ingiunzione(oCardId.ToString(), ref outputString);

								apfLibrary.InsertInProtocolArchiveProvaValutativa("1579/16");
								apfLibrary.CreateReportMassiveProVal("604/16", "7221");
								//apfLibrary.InsertInProtocolArchive("366/16");

								var res = (object[])apfLibrary.VerifyStep1ProvaValutativa(oCardId.ToString(), ref outputString);
							   // string outputString = "";
								//var res = (object[])apfLibrary.VerifyStep1Cancellazione(oCardId.ToString(), ref outputString);
								//var res = (object[])apfLibrary.VerifyStep1(oCardId.ToString(), ref outputString);
							   // var reso = (object[])apfLibrary.VerifyStep1ProvaValutativa(oCardId.ToString(), ref outputString);
								bool BrESULT = false;
								//apfLibrary.GetLowerCardsReadyToProtocol("343/16");
								apfLibrary.InsertInProtocolArchive("1434/16");
								//BrESULT = apfLibrary.CheckCardSigned(sGuidCard);
								//BrESULT = apfLibrary.CheckCardsInError("287/16");
								//apfLibrary.SetLinkAnag(oCardId.ToString(), "BLLMRA64E16D829U");
								//var res = (object[])apfLibrary.getOfficesFromUserLauncherFlux("6e01ef3d-5783-47cb-87ab-b0f146110cf6", ref outputString);

								//var res = (object[])apfLibrary.VerifyStep1("167", ref outputString);

								//Console.WriteLine(outputString);
								//Console.WriteLine(res.Length);
								apfLibrary = null;*/

			}
			catch (Exception exc)
			{
				Console.Write(exc.ToString());
			}
			Console.ReadLine();
            //string test = apfLibrary.VerifyStep1("19", out outputString);
            //string pathDocx = @"COMUNICAZIONE ISCRIZIONE ALBO.docx";
            //FluxHelper fluxHelper = new FluxHelper();
            //string errorTags="";
            //if (fluxHelper.CreateDocxMassive(pathDocx, @"Copia di deliberate_2015_12_21_9204720643708286156.xls", out errorTags))
            //{
            //    string ok = "";
            //}
        }
    }
}
