using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace ReplayDefaultVisibility
{
	class Program
	{
		static void Main(string[] args)
		{
			FatturaElettronica.Ordinaria.FatturaOrdinaria fattura = new FatturaElettronica.Ordinaria.FatturaOrdinaria();
			
			var s = new XmlReaderSettings { IgnoreWhitespace = true };
			var r = XmlReader.Create(@"C:\temp\IT01641790702_ag2mJ.xml", s);
			
			fattura.ReadXml(r);
			var a= fattura.FatturaElettronicaHeader.CedentePrestatore;
			var personsDump = ObjectDumper.Dump(a, DumpStyle.Console);
			Console.WriteLine(personsDump);
			string sNumFattura = "";
			foreach (var doc in fattura.FatturaElettronicaBody)
			{
				var datiDocumento = doc.DatiGenerali.DatiGeneraliDocumento;
				sNumFattura += $"Numero Fattura: {datiDocumento.Numero}" + " " + $"Data: {datiDocumento.Data.ToShortDateString()}" + System.Environment.NewLine;
			}
			sNumFattura = sNumFattura.Substring(0,sNumFattura.Length-2);
			Console.WriteLine(sNumFattura);
			string[] stringSeparators = new string[] { "\r\n" };
			string[] lines = personsDump.Split(stringSeparators, StringSplitOptions.None);
			foreach (string aaa in lines)
			{
				if(aaa.IndexOf(" null")==-1 && (aaa.IndexOf("{") == -1 && aaa.IndexOf("}") == -1))
					Console.WriteLine(aaa.Trim()); //But will print 3 lines in total.
			}
			// Creating XSLCompiled object    
			XslCompiledTransform objXSLTransform = new XslCompiledTransform();
			objXSLTransform.Load(@"c:\temp\FoglioStileAssoSoftware.xsl");

			// Creating StringBuilder object to hold html data and creates TextWriter object to hold data from XslCompiled.Transform method    
			StringBuilder htmlOutput = new StringBuilder();
			TextWriter htmlWriter = new StringWriter(htmlOutput);

			// Call Transform() method to create html string and write in TextWriter object.    
			objXSLTransform.Transform(r, null, htmlWriter);
			System.IO.File.WriteAllText(@"C:\temp\fattura.html", htmlOutput.ToString());
			XslTransform myXslTransform;
			myXslTransform = new XslTransform();
			myXslTransform.Load(@"c:\temp\FoglioStileAssoSoftware.xsl");
			myXslTransform.Transform(@"C:\temp\IT01641790702_ag2mJ.xml", @"C:\temp\ISBNBookList.xml");
			r.Close();



			string sQuery = "";
			if (args.Length > 0)
			{
				foreach (Object obj in args)
				{
					sQuery +=obj;
				}
				Siav.APFlibrary.Flux oFlux = new Siav.APFlibrary.Flux();
				oFlux.SetCardDefaultVisibility(sQuery);// "select progressivo from archivio where progressivo = 280929 OR progressivo = 280923");
			}
			else
			{
				Console.WriteLine("Nessuna query individuata come argomento.");
			}
			
		}
	}
}
