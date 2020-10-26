using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using BCL.easyPDF.Printer;
using ConversionServices.Model;
using iTextSharp.text.pdf.parser;
using System.Text.RegularExpressions;
using System.Text;

namespace ConversionServices.Manager
{
	public class Convert2PdfManager
	{
		public readonly byte[] USER = System.Text.ASCIIEncoding.UTF8.GetBytes("Hello");
		/** Owner password. */
		public readonly byte[] OWNER = System.Text.ASCIIEncoding.UTF8.GetBytes("World");

		public string getPdfA(Stream fileContents, string nomeFile)
		{
			string sFileContentPdf = string.Empty;

			try
			{
				sFileContentPdf = string.Empty;
				Byte[] sResult;

				using (Printer oPrinter = new Printer())
				{
					string sFileExtension = Path.GetExtension(nomeFile).ToUpper();
					if ("BMP,GIF,JPEG,PNG,TIF,TIFF,WMF,EMF".IndexOf("sFileExtension") > 0)
					{
						ImagePrintJob oPrintJob = oPrinter.ImagePrintJob;
						//oPrintJob.NativeOfficePDF = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						sResult = oPrintJob.PrintOut3(ReadFully(fileContents), sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					if ("PPTX".IndexOf("sFileExtension") > -1)
					{
						PowerPointPrintJobEx oPrintJob = oPrinter.PowerPointPrintJobEx;
						oPrintJob.NativeOfficePDF = true;
						oPrintJob.NativeOfficeStandardPDFA = true;
						sResult = oPrintJob.PrintOut3(ReadFully(fileContents), sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					if ("DOCX".IndexOf("sFileExtension") > -1)
					{
						WordPrintJobEx oPrintJob = oPrinter.WordPrintJobEx;
						oPrintJob.NativeOfficePDF = true;
						oPrintJob.NativeOfficeStandardPDFA = true;
						sResult = oPrintJob.PrintOut3(ReadFully(fileContents), sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					if ("XSLX,CSV".IndexOf("sFileExtension") > -1)
					{
						ExcelPrintJobEx oPrintJob = oPrinter.ExcelPrintJobEx;
						oPrintJob.PrintAllSheets = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						sResult = oPrintJob.PrintOut3(ReadFully(fileContents), sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else
					{
						GenericPrintJob oPrintJob = oPrinter.GenericPrintJob;
						//oPrintJob.NativeOfficePDF = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						sResult = oPrintJob.PrintOut3(ReadFully(fileContents), sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
				}
			}
			catch (Exception ex)
			{ throw ex; }
			finally
			{
			}
			return sFileContentPdf;
		}
		public void ResizeForm(string sourcefile, string outFile)
		{
			var document = new Document();
			var writer = new PdfCopy(document, new FileStream(outFile, FileMode.Create));
			document.Open();
			var reader = new PdfReader(Path.Combine(sourcefile));
			for (var i = 1; i <= reader.NumberOfPages; i++)
			{
				var page = writer.GetImportedPage(reader, i);
				writer.AddPage(page);
			}
			reader.Close();
			writer.Close();
			document.Close();
		}

		public static void InsertPressMarkOnPdf(string pathIn, string pathOut, string valuePressMark, string fontName, int fontSize, int numPage = 1, int coordinateX = 1, int coordinateY = 1)
		{
			try
			{
				ResourceFileManager resourceFileManager;
				resourceFileManager = ResourceFileManager.Instance;
				resourceFileManager.SetResources();
				string sPathPfx = resourceFileManager.getConfigData("PathPfx");
				string sPasswordPfx = resourceFileManager.getConfigData("PasswordPfx");
				Siav.Archiflow.Documents.Pdf.PdfDoc pdfDoc = new Siav.Archiflow.Documents.Pdf.PdfDoc();
				//pdfDoc.ChangePdfVersion(pathIn, pathOut, Siav.Archiflow.Documents.Pdf.PdfDoc.PdfVersion.VERSION_1_7);
				Siav.Archiflow.Documents.Pdf.PdfSignature pdfSignature = new Siav.Archiflow.Documents.Pdf.PdfSignature();
				Siav.Archiflow.Documents.Pdf.PdfSignature.PdfFont pdfFont = new Siav.Archiflow.Documents.Pdf.PdfSignature.PdfFont();
				pdfFont.Size = fontSize;
				pdfFont.Name = fontName;
				pdfSignature.AddTextLine(valuePressMark);
				pdfSignature.Font = pdfFont;
				pdfSignature.Rectangle.Origin = Siav.Archiflow.Documents.Pdf.PdfSignature.PdfRectangle.RectangleOrigin.TopLeft;
				pdfSignature.Rectangle.Page = numPage;
				pdfSignature.Rectangle.X = coordinateX;
				pdfSignature.Rectangle.Y = coordinateY;
				Siav.Archiflow.Documents.Pdf.PdfSignature.PdfPfxFile pfxFile = new Siav.Archiflow.Documents.Pdf.PdfSignature.PdfPfxFile();
				pfxFile.File = File.ReadAllBytes(@sPathPfx); //@"C:\Siav\Certificato\Garante.pfx"
				pfxFile.FileName = Path.GetFileName(sPathPfx); //"Garante.pfx";
				pfxFile.Password = sPasswordPfx;// "Garante";
				pdfSignature.PfxFile = pfxFile;

				pdfDoc.SignPades(pathIn, pathOut, pdfSignature);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
		public byte[] EncryptPdf(byte[] src)
		{
			PdfReader reader = new PdfReader(src);
			using (MemoryStream ms = new MemoryStream())
			{
				using (PdfStamper stamper = new PdfStamper(reader, ms))
				{
					stamper.SetEncryption(
					  USER, OWNER,
					  PdfWriter.ALLOW_PRINTING,
					  PdfWriter.ENCRYPTION_AES_128 | PdfWriter.DO_NOT_ENCRYPT_METADATA
					);
				}
				return ms.ToArray();
			}
		}

		
		public string ConvertMainDoc(MainDocument oMainDoc, Util.LOLIB logger, string sWorkingFolder, string LogId, out MainDocument oMainDocPdf)
		{
			string sFileContentPdf = string.Empty;
			oMainDocPdf = new MainDocument();
			try
			{
				sFileContentPdf = string.Empty;
				Byte[] sResult;
				using (Printer oPrinter = new Printer())
				{
					string sFileExtension = Path.GetExtension(oMainDoc.Filename).ToUpper();
					logger.WriteOnLog(LogId, "Estensione: " + sFileExtension, 3);
					if ("BMP,GIF,JPEG,PNG,TIFF,TIF,WMF,EMF".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione image: " + sFileExtension, 3);
						//ImagePrintJob oPrintJob = oPrinter.ImagePrintJob;
						//oPrintJob.NativeOfficePDF = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						//sResult = oPrintJob.PrintOut3(oMainDoc.oByte, sFileExtension);
						var oPDFSetting = oPrinter.PrinterSetting;
						oPDFSetting.LayoutPaperSize = (int)prnPaperSize.PRN_PAPER_A4;
						
						//oPDFSetting.LayoutPaperOrientation = prnPaperOrientation.PRN_PAPER_ORIENT_LANDSCAPE;
						oPDFSetting.FontEmbedding = prnFontEmbedding.PRN_FONT_EMBED_NONE;
						oPDFSetting.Save();
						oPrinter.PrintJob.PDFSetting.StandardPdfAConformance = prnPdfAConformance.PRN_PDFA_CONFORM_1B_TC1;
						oPrinter.PrintJob.PDFSetting.StandardPdfXConformance = prnPdfXConformance.PRN_PDFX_CONFORM_NONE;
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrinter.PrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("PPTX,PPS".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione power point: " + sFileExtension, 3);
						PowerPointPrintJobEx oPrintJob = oPrinter.PowerPointPrintJobEx;
						//oPrintJob.NativeOfficePDF = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						oPrintJob.FrameSlides = true;
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("HTML,HTM,XHTML,XML".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione HTML: " + sFileExtension, 3);
						IEExtendedPrintJob oPrintJob = oPrinter.IEExtendedPrintJob;
						IEExtendedSetting oIESetting = oPrintJob.IEExtendedSetting;

						oIESetting.DisableScriptDebugger = true;
						oIESetting.DisplayErrorDialogOnEveryError = false;
						oIESetting.Save();

						oPrintJob.PageWidth = 11.93;
						oPrintJob.PageHeight = 15.98;
						//oPrintJob.ContentOrientation = prnContentOrientation.;
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("EML".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione eml: " + sFileExtension, 3);
						PrintJob oPrintJob = oPrinter.PrintJob;
						//oPrintJob.QueueWaitTimeout = 100000;
						//sResult = oPrintJob.PrintOut3(oMainDoc.oByte, sFileExtension);
						//sFileContentPdf = Convert.ToBase64String(sResult);

						string sPathFileTemp = sWorkingFolder + "\\test." + sFileExtension;
						logger.WriteOnLog(LogId, "path file di input: " + sPathFileTemp, 3);
						string sPathPDFTemp = sWorkingFolder + "\\test.pdf";
						logger.WriteOnLog(LogId, "path file di output: " + sPathPDFTemp, 3);
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						File.WriteAllBytes(@sPathFileTemp, temp_backToBytes);
						logger.WriteOnLog(LogId, "File di input creato : " + sPathFileTemp, 3);
						System.Threading.Thread.Sleep(5000);
						oPrintJob.PrintOut(@sPathFileTemp, @sPathPDFTemp);
						logger.WriteOnLog(LogId, "File di output creato : " + sPathPDFTemp, 3);
						sResult = File.ReadAllBytes(@sPathPDFTemp);
						//sResult = oPrintJob.PrintOut3(oMainDoc.oByte, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("DOCX,RTF".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione word: " + sFileExtension, 3);
						WordPrintJobEx oPrintJob = oPrinter.WordPrintJobEx;
						oPrintJob.NativeOfficePDF = true;
						oPrintJob.NativeOfficeStandardPDFA = true;
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("ODT,SWX,WPD,ODS,SXC,ODP,SXI,ODG,SXD".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione Open Offices: " + sFileExtension, 3);
						OpenOfficePrintJob oPrintJob = oPrinter.OpenOfficePrintJob;
						oPrintJob.ConvertBookmarks = true;
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else if ("XLSX,CSV".IndexOf(sFileExtension) > -1)
					{
						logger.WriteOnLog(LogId, "Entro conversione excel: " + sFileExtension, 3);
						ExcelPrintJobEx oPrintJob = oPrinter.ExcelPrintJobEx;
						//oPrintJob.NativeOfficePDF = true;
						//oPrintJob.NativeOfficeStandardPDFA = true;
						oPrintJob.PrintAllSheets = true;
						oPrintJob.NativeOfficePDF = true;
						string sPathFileTemp = sWorkingFolder + "\\test." + sFileExtension;
						logger.WriteOnLog(LogId, "path file di input: " + sPathFileTemp, 3);
						string sPathPDFTemp = sWorkingFolder + "\\test.pdf";
						logger.WriteOnLog(LogId, "path file di output: " + sPathPDFTemp, 3);
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						File.WriteAllBytes(@sPathFileTemp, temp_backToBytes);
						logger.WriteOnLog(LogId, "File di input creato : " + sPathPDFTemp, 3);
						oPrintJob.PrintOut(@sPathFileTemp, @sPathPDFTemp);
						logger.WriteOnLog(LogId, "File di output creato : " + sPathPDFTemp, 3);
						sResult = File.ReadAllBytes(@sPathPDFTemp);
						//sResult = oPrintJob.PrintOut3(oMainDoc.oByte, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
						//oPrinter.PrintJob.PDFSetting.StandardPdfAConformance = prnPdfAConformance.PRN_PDFA_CONFORM_1B_TC1;
						//oPrinter.PrintJob.PDFSetting.Save();
						//sResult = oPrinter.PrintJob.PrintOut3(oMainDoc.oByte, sFileExtension);
						//sFileContentPdf = Convert.ToBase64String(sResult);
					}
					else
					{
						logger.WriteOnLog(LogId, "Entro conversione generica: " + sFileExtension, 3);
						var oPrintJob = oPrinter.PrintJob;
						var oPDFSetting = oPrintJob.PDFSetting;
						oPDFSetting.FontEmbedding = prnFontEmbedding.PRN_FONT_EMBED_NONE;
						oPDFSetting.StandardPdfAConformance = prnPdfAConformance.PRN_PDFA_CONFORM_NONE;
						oPDFSetting.StandardPdfXConformance = prnPdfXConformance.PRN_PDFX_CONFORM_NONE;
						oPDFSetting.Save();
						byte[] temp_backToBytes = Convert.FromBase64String(oMainDoc.BinaryContent);
						sResult = oPrintJob.PrintOut3(temp_backToBytes, sFileExtension);
						sFileContentPdf = Convert.ToBase64String(sResult);
					}
					oMainDocPdf.Filename = Path.GetFileNameWithoutExtension(oMainDoc.Filename) + ".pdf";
					oMainDocPdf.BinaryContent = Convert.ToBase64String(sResult);
				}
			}
			catch (Exception ex)
			{ throw ex; }
			finally
			{
			}
			return sFileContentPdf;
		}

		public static byte[] ReadFully(Stream input)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}
	}
}