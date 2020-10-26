using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using OCF_Ws.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OCF_Ws.Util
{
	public class PdfUtil
	{
		public string extractTextFromPdf(string sBinaryContent, Util.LOLIB logger, string LogId)
		{
			string sHashNow = "";
			string sTextContentPdf = string.Empty;
			try
			{
				byte[] temp_backToBytes = Convert.FromBase64String(sBinaryContent);
				logger.WriteOnLog(LogId, "Letti i byte del file da processare", 3);

				using (PdfReader reader = new PdfReader(temp_backToBytes))
				{
					logger.WriteOnLog(LogId, "Oggetto pdf generato", 3);
				//	ITextExtractionStrategy strategy = new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy();

					// 1. if pdf document has only one page
					//here second parameter is PDF Page number
					//ExtractedData = PdfTextExtractor.GetTextFromPage(reader, 1, strategy);


					/*// 2. if pdf ducument has more than one page
						// iterating through all pages
						*/



					for (int i = 1; i <= reader.NumberOfPages; i++)
					{
						logger.WriteOnLog(LogId, "Processo la pagina: " + i, 3);
						sTextContentPdf += PdfTextExtractor.GetTextFromPage(reader, i);
//						sTextContentPdf += PdfTextExtractor.GetTextFromPage(reader, i, strategy);
						logger.WriteOnLog(LogId, "Letti i byte del file da processare", 3);
					}

					sTextContentPdf = Regex.Replace(sTextContentPdf, @"\t|\n|\r", "");
					logger.WriteOnLog(LogId, "Elimino il carattere invio dal testo", 3);
					var crc32 = new Crc32();

					// test
					Encoding ascii = Encoding.ASCII;
					Encoding unicode = Encoding.Unicode;

					logger.WriteOnLog(LogId, "Dati Letti: " + sTextContentPdf, 3);
					sHashNow = crc32.Get(Encoding.UTF8.GetBytes(sTextContentPdf)).ToString("X").ToUpper();
					logger.WriteOnLog(LogId, "Hash CRC32b: " + sHashNow, 3);
				}
			}
			catch (Exception ex)
			{ throw ex; }
			finally
			{
			}
			return sHashNow;
		}
	}
}
