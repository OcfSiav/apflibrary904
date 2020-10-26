using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCF_Ws.Util;
using OCF_Ws.Manager;
using OCF_Ws.Model;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Collections.Specialized;
using SVAOLLib;
using System.Text.RegularExpressions;

namespace OCF_Ws.Action
{
	public class WsAction : IDisposable
	{
		string sConnection;

		bool mDisposed = false; public int lErr = 0;
		string LogId;
		LOLIB Logger;
		Rijndael oRijndael;
		string sWorkfolder;
		ResourceFileManager resourceFileManager;
		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool bDispoing)
		{
			if (mDisposed)
				return;

			if (bDispoing)
			{
				GC.SuppressFinalize(this);
			}
			mDisposed = true;
		}
		public WsAction(string sUser)
		{
			ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
			Logger = new LOLIB(); ;
			oRijndael = new Rijndael();
			if (LogId == null || LogId == "") LogId = LOLIB.CodeGen(sUser);
			string PathDirFolderConfiguration = @resourceFileManager.getConfigData("WORKFOLDER");
			sWorkfolder = PathDirFolderConfiguration + @"\" + LogId + @"\";
			System.IO.Directory.CreateDirectory(sWorkfolder);
		}
		private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
		{
			var certificate = (X509Certificate2)cert;
			return true;
		}
		public Outcome getCard(string usernName, string password, string sGuidCard, bool bGetMainDoc, bool bGetAttachment, out Model.Card oCard)
		{
			string strMessage = "";
			List<string> lIdDossiers = new List<string>();
			Outcome esito = new Outcome();
			oCard = new Model.Card();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Lavoro con l'utenza: " + usernName, 3);
			UtilSvCard oUtilSvCard = new UtilSvCard(Logger, LogId);

			try
			{
				if (!string.IsNullOrEmpty(sGuidCard.Trim()))
				{
					//string oUserName = oRijndael.Decrypt(usernName);
					//string oUserPwd = oRijndael.Decrypt(password);
					//sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);

					sConnection = oUtilSvCard.OpenConnection(usernName, password);
					Logger.WriteOnLog(LogId, "sConnection: " + sConnection, 3);
					Logger.WriteOnLog(LogId, "sGuidCard: " + sGuidCard, 3);
					Logger.WriteOnLog(LogId, "LogId: " + LogId, 3);
					SVAOLLib.Card oSvCard = new SVAOLLib.Card();
					oSvCard.GUIDconnect = sConnection;
					Guid oCardId;
					if (sGuidCard.Length > 12)                 // set the guid of the card
						oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
					else
						oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
					oSvCard.GuidCard = oCardId.ToString();

					oSvCard.LoadFromGuid();
					Visibility visibility = new Visibility();
					string sXML = oSvCard.GetCurrentVisibilityAsXML3(true, true);
					//Logger.WriteOnLog(LogId, "sXML: " + sXML, 3);
					using (var oCardManager = new CardManager(Logger, LogId))
					{
						oCard.Archivio = oCardManager.GetArchiveNameByCode(sConnection, oSvCard.ArchiveAsId);
					}
					Logger.WriteOnLog(LogId, "Archivio: " + oSvCard.ArchiveAsId, 3);



					//////////////////////////////////////////
					WsAnagrafica.AgrafServiceClient Anagrafica = new WsAnagrafica.AgrafServiceClient();
					short ArchiveId = oSvCard.ArchiveAsId;
					short DocumentID = oSvCard.DocTypeAsId;

					List<OCF_Ws.WsAnagrafica.IndexBookTagInfo> IndexBookTagInfo = new List<OCF_Ws.WsAnagrafica.IndexBookTagInfo>();
					IndexBookTagInfo=Anagrafica.ReadIndexBookTagInfoByArchDocType(ArchiveId, DocumentID);
					Logger.WriteOnLog(LogId, "IndexBookTagInfo: " + Logger.ToJson(IndexBookTagInfo), 3);

					//mi tiro fuori il tag dalla combinazione dell'archivio e tipodoc
					//Anagrafica.ReadEntityTypeByName
					/////////////////////////////////////
					List<SENDOBJECTSENDENTITIESSENDENTITY> getUsersInArchiveTypeDoc = UtilCardVisibility.getUsersInArchiveTypeDoc(sXML, LogId);

					visibility.UtentiUserId = UtilCardVisibility.getUsersVisibility(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "UtentiUserId: " + Logger.ToJson(visibility.UtentiUserId), 3);

					visibility.UtentiUserIdCC = UtilCardVisibility.getUsersVisibilityCC(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "UtentiUserIdCC: " + Logger.ToJson(visibility.UtentiUserIdCC), 3);

					visibility.UfficiCodiceUO = UtilCardVisibility.getUfficiCodiceUO(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "UfficiCodiceUO: " + Logger.ToJson(visibility.UfficiCodiceUO), 3);

					visibility.UfficiCodiceUOCC = UtilCardVisibility.getUfficiCodiceUOCC(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "UfficiCodiceUOCC: " + Logger.ToJson(visibility.UfficiCodiceUOCC), 3);

					visibility.GruppiDescrizione = UtilCardVisibility.getGruppiDescrizione(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "getGruppiDescrizione: " + Logger.ToJson(visibility.GruppiDescrizione), 3);

					visibility.GruppiDescrizioneCC = UtilCardVisibility.getGruppiDescrizioneCC(getUsersInArchiveTypeDoc, LogId);
					Logger.WriteOnLog(LogId, "LogId: " + Logger.ToJson(visibility.GruppiDescrizioneCC), 3);
					oCard.Visibilita = visibility;
					List<dynamic> allMetadati = new List<dynamic>();
					using (var oCardManager = new CardManager(Logger, LogId))
					{
						oCard.TipologiaDocumentale = oCardManager.GetTypeDocumentNameByCode(sConnection, oSvCard.DocTypeAsId);
						allMetadati = oCardManager.GetIndiciScheda(sConnection, sGuidCard);
					}
					//List<string> metadati = UtilSvCard.getMetadati(allMetadati);
					oCard.MetaDati = oUtilSvCard.getMetadati(allMetadati, LogId);
					//string objectDescription = UtilSvCard.getObjectDescription(allMetadati);
					oCard.Oggetto = oUtilSvCard.getObjectDescription(allMetadati);
					oCard.IdInterno = oUtilSvCard.getIdInterno(allMetadati);
					oCard.IdProtocollo = oUtilSvCard.getIdProtocollo(allMetadati);
					oCard.DataProtocollo = oUtilSvCard.getDataProtocollo(allMetadati);
					oCard.DataDocumento = oUtilSvCard.getDataDocumento(allMetadati);
					oCard.Classificazione = oUtilSvCard.getClassificaDocumento(out lIdDossiers, sGuidCard);
					Logger.WriteOnLog(LogId, "LogId: " + Logger.ToJson(oCard.Classificazione), 3);
					
					//oSvCard.GetVisibilityAsXML();
					//var oVisibility = CardVisibilityManager.();
					//sibility.GruppiDescrizione =
					oCard.Visibilita = visibility;
					// da fare oCard.Anagrafica

							 
					oCard.Guid = sGuidCard;

					if (bGetMainDoc)
					{
						MainDocument oMainDocument = new MainDocument();
						svMainDoc oSvMaindoc = new svMainDoc();
						using (var oDocManager = new DocManager(Logger, LogId))
						{
							oSvMaindoc = oDocManager.GetMainDoc(sConnection, sGuidCard);
						}
						oMainDocument.Filename = oSvMaindoc.Filename;
						oMainDocument.BinaryContent = Convert.ToBase64String(oSvMaindoc.oByte);
						oCard.MainDocument = oMainDocument;
					}
					if (bGetAttachment)
					{
						List<string> idAttachments = new List<string>();
						oCard.Attachments = oUtilSvCard.Attachment(sConnection, oSvCard, idAttachments, true);
					}


					//CardManager.SetInternalAttach(sConnection, sGuidCardFrom, sGuidCardTo, Note, sInternalNote, bBiunivocal, LogId);
				}
				else
				{
					// il numero di protocollo non è valorizzato
					strMessage = "Il valore GUID non è valorizzato.";
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				oUtilSvCard.CloseConnection(sConnection);
				oUtilSvCard.Dispose();
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCard + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCard + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome createPdfAFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocument mainDocOut)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			mainDocOut = new MainDocument();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			string sConnection = "";
			ConnectionManager oConnectionManager = new ConnectionManager();
			string sGuidCardForLog = sGuidCard;
			try
			{
				if (!string.IsNullOrEmpty(sGuidCard.Trim()))
				{
					string oUserName = oRijndael.Decrypt(resourceFileManager.getConfigData("UserFlux"));
					string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
					sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
					svMainDoc oSvMaindoc = new svMainDoc();
					using (var oDocManager = new DocManager(Logger, LogId))
					{
						oSvMaindoc = oDocManager.GetMainDoc(sConnection, sGuidCard);
					}

					Logger.WriteOnLog(LogId, "aReplaceNameValue: " + Logger.ToJson(aReplaceNameValue), 3);
					NameValueCollection addStringForReplace = new NameValueCollection();
					if (aReplaceNameValue != null)
					{
						if (aReplaceNameValue is Array && aReplaceNameValue[0] is Array)
						{
							// Array bidimensionale 1a ("i") dimensione indice 0 ho il nome del campo
							// 1a ("i") dimensione indice 1 ho il valore del campo

							for (int i = 0; i < aReplaceNameValue[0].GetLength(0); i++)
							{
								//								for (int j = 0; j < aReplaceNameValue[i].Length; j++)
								//								{
								//									if (i == 0)
								//									{
								//										if (aReplaceNameValue[i][j].Length > 0)
								//										{
								addStringForReplace.Add(aReplaceNameValue[0][i], aReplaceNameValue[1][i]);
								Logger.WriteOnLog(LogId, "Nome campo: " + aReplaceNameValue[0][i] + " -> " + aReplaceNameValue[1][i].ToString(), 3);
								//										}
								//									}
								//								}
							}
						}
						// CREAREZIONE del file DOCX definitivo
						DocxUtil oDocxUtil = new DocxUtil();
						oDocxUtil.WorkingFolder = sWorkfolder;
						string sPathFile = sWorkfolder + oSvMaindoc.Filename;
						oDocxUtil.FileMaterialize(sPathFile, oSvMaindoc.oByte);
						string sErrVar = "";
						string sPathFileCompleted = oDocxUtil.CreateDocx(addStringForReplace, sPathFile, Logger, LogId, out sErrVar);
						// Conversione del file generato in file pdf/a
						ConversionServices.ConversionServicesClient oConversionServices = new ConversionServices.ConversionServicesClient();
						ConversionServices.MainDocument oMainDocumentToConv = new ConversionServices.MainDocument();
						oMainDocumentToConv.Filename = oSvMaindoc.Filename;
						oMainDocumentToConv.BinaryContent = Convert.ToBase64String(File.ReadAllBytes(sPathFileCompleted));
						ConversionServices.MainDocument oMainDocumentPdf = new ConversionServices.MainDocument();
						var oOutcome = oConversionServices.base64ToPdfA(out oMainDocumentPdf, oMainDocumentToConv);
						if (oOutcome.iCode == 1)
						{
							// CONVERSIONE ESEGUITA
							mainDocOut.BinaryContent = oMainDocumentPdf.BinaryContent;
							mainDocOut.Filename = oMainDocumentPdf.Filename;
						}
						else
						{
							// CONVERSIONE NON ESEGUITA
							throw new ArgumentException("Problema durante la fase di conversione PDF: " + oOutcome.sDescription);
						}

					}
				}
				else
				{
					// La GUID non è valorizzata
					strMessage = "Il valore GUID della card non è valorizzata.";
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (!string.IsNullOrEmpty(sConnection))
				{
					oConnectionManager.CloseConnect();
				}
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCardForLog + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCardForLog + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome getCRC32bFromFilePdf(MainDocument filePdf, out string CRC32b)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			CRC32b = "";
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			try
			{
				if (filePdf.Validate(out strMessage))
				{
					var oPdfUtil = new PdfUtil();
					CRC32b= oPdfUtil.extractTextFromPdf(filePdf.BinaryContent, Logger, LogId);
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + LogId);
				}
			}
			return esito;
		}
		public Outcome createPdfACRC32bFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocumentCRC32b mainDocOut)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			mainDocOut = new MainDocumentCRC32b();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			string sConnection = "";
			ConnectionManager oConnectionManager = new ConnectionManager();
			string sGuidCardForLog = sGuidCard;
			try
			{
				if (!string.IsNullOrEmpty(sGuidCard.Trim()))
				{
					string oUserName = oRijndael.Decrypt(resourceFileManager.getConfigData("UserFlux"));
					string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
					sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
					svMainDoc oSvMaindoc = new svMainDoc();
					using (var oDocManager = new DocManager(Logger, LogId))
					{
						oSvMaindoc = oDocManager.GetMainDoc(sConnection, sGuidCard);
					}

					Logger.WriteOnLog(LogId, "aReplaceNameValue: " + Logger.ToJson(aReplaceNameValue), 3);
					NameValueCollection addStringForReplace = new NameValueCollection();
					if (aReplaceNameValue != null)
					{
						if (aReplaceNameValue is Array && aReplaceNameValue[0] is Array)
						{
							// Array bidimensionale 1a ("i") dimensione indice 0 ho il nome del campo
							// 1a ("i") dimensione indice 1 ho il valore del campo

							for (int i = 0; i < aReplaceNameValue[0].GetLength(0); i++)
							{
								addStringForReplace.Add(aReplaceNameValue[0][i], aReplaceNameValue[1][i]);
								Logger.WriteOnLog(LogId, "Nome campo: " + aReplaceNameValue[0][i] + " -> " + aReplaceNameValue[1][i].ToString(), 3);
							}
						}
						// CREAREZIONE del file DOCX definitivo
						DocxUtil oDocxUtil = new DocxUtil();
						oDocxUtil.WorkingFolder = sWorkfolder;
						string sPathFile = sWorkfolder + oSvMaindoc.Filename;
						oDocxUtil.FileMaterialize(sPathFile, oSvMaindoc.oByte);
						string sErrVar = "";
						string sPathFileCompleted = oDocxUtil.CreateDocx(addStringForReplace, sPathFile, Logger, LogId, out sErrVar);
						// Conversione del file generato in file pdf/a
						ConversionServices.ConversionServicesClient oConversionServices = new ConversionServices.ConversionServicesClient();
						ConversionServices.MainDocument oMainDocumentToConv = new ConversionServices.MainDocument();
						oMainDocumentToConv.Filename = oSvMaindoc.Filename;
						oMainDocumentToConv.BinaryContent = Convert.ToBase64String(File.ReadAllBytes(sPathFileCompleted));
						ConversionServices.MainDocument oMainDocumentPdf = new ConversionServices.MainDocument();
						var oOutcome = oConversionServices.base64ToPdfA(out oMainDocumentPdf, oMainDocumentToConv);
						if (oOutcome.iCode == 1)
						{
							// CONVERSIONE ESEGUITA
							mainDocOut.BinaryContent = oMainDocumentPdf.BinaryContent;
							mainDocOut.Filename = oMainDocumentPdf.Filename;
							var oPdfUtil = new PdfUtil();
							mainDocOut.CRC32b = oPdfUtil.extractTextFromPdf(oMainDocumentPdf.BinaryContent, Logger, LogId);
						}
						else
						{
							// CONVERSIONE NON ESEGUITA
							throw new ArgumentException("Problema durante la fase di conversione PDF: " + oOutcome.sDescription);
						}
					}
				}
				else
				{
					// La GUID non è valorizzata
					strMessage = "Il valore GUID della card non è valorizzata.";
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (!string.IsNullOrEmpty(sConnection))
				{
					oConnectionManager.CloseConnect();
				}
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCardForLog + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCardForLog + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut)
		{
			// Va usato AUTOMAPPER da IMPLEMENTARE
			string strMessage = "";
			mainDocOut = new MainDocument();
			Outcome esito = new Outcome();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Procedo alla conversione del file " + mainDoc.Filename, 3);
			ConversionServices.MainDocument oMainDocOut = new ConversionServices.MainDocument();
			ConversionServices.MainDocument oMainDocIn = new ConversionServices.MainDocument();
			ConversionServices.Outcome oOutcome = new ConversionServices.Outcome();
			oMainDocIn.BinaryContent = mainDoc.BinaryContent;
			oMainDocIn.Filename = mainDoc.Filename;
			try
			{
				if (mainDoc.Validate(out strMessage))
				{
					Logger.WriteOnLog(LogId, "Oggetto da convertire: " + mainDoc.ToXml(), 3);
					ConversionServices.ConversionServicesClient oConversionServices = new ConversionServices.ConversionServicesClient();
					oOutcome = oConversionServices.base64ToPdfA(out oMainDocOut, oMainDocIn);
					mainDocOut.Filename = oMainDocOut.Filename;
					mainDocOut.BinaryContent = oMainDocOut.BinaryContent;
					esito.iCode = oOutcome.iCode;
					esito.sDescription = oOutcome.sDescription;

				}
				else
				{
					Logger.WriteOnLog(LogId, "Oggetto NON Convertito: " + mainDoc.ToXml(), 3);
					esito.iCode = 0;
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}

			finally
			{
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + LogId);
				}
			}
			return esito;
		}
		public Outcome getSignCheck(MainDocument mainDoc, string sCRC32, List<String> lNIN, bool bGetDocWithoutSign, out MainDocumentChecked mainDocOut)
		{
			string strMessage = "";
			string sDescAnomMainDoc = "";
			Outcome esito = new Outcome();
			mainDocOut = new MainDocumentChecked();

			esito.iCode = 1;
			esito.sTransactionId = LogId;
			string sEsito = "";
			bool bCheckSign = false;
			bool bCheckHash = false;
			if (mainDoc == null)
			{
				throw new ArgumentException("Entità documento principale mancante.");
			}
			else if (mainDoc.Validate(out sDescAnomMainDoc) == false)
			{
				throw new ArgumentException(sDescAnomMainDoc);
			}

			try
			{
				sEsito = resourceFileManager.getConfigData("ProtOk");
				// Solo per test
				//Logger.WriteOnLog(LogId, "MainDOC: " + Logger.ToJson(mainDoc), 3);
				var sNameDirTemp = @resourceFileManager.getConfigData("LOGPATH") + @"\" + @LogId;
				System.IO.Directory.CreateDirectory(sNameDirTemp);
				string sPathMainDoc = sNameDirTemp + @"\" + mainDoc.Filename;
				Logger.WriteOnLog(LogId, "Path MAINDOC: " + sPathMainDoc, 3);
				FileUtil.MaterializeFile(sPathMainDoc, Convert.FromBase64String(mainDoc.BinaryContent));
				Manager.WcfSiavSignManager SvSignManager = new Manager.WcfSiavSignManager(Logger, LogId);
				// Verifica firma
				var FileToVerifiyWithoutSign = Path.GetDirectoryName(sPathMainDoc); //+ @"\" + Path.GetFileNameWithoutExtension(sPathMainDoc) + "_2" + Path.GetExtension(sPathMainDoc);
				Logger.WriteOnLog(LogId, "Path file da copiare: " + FileToVerifiyWithoutSign, 3);

				EsitoCheckFileSigned EsitoChk = SvSignManager.CheckSignTimeStampFileInfo2(sPathMainDoc, FileToVerifiyWithoutSign);
				if (EsitoChk.Check)
				{
					bCheckSign = true;
					Logger.WriteOnLog(LogId, "Controllo firma Esito: Positivo", 3);
				}
				else
				{
					sEsito = @resourceFileManager.getConfigData("FirmaNo");
					Logger.WriteOnLog(LogId, "Controllo firma Esito: Negativo", 3);
				}
				if (bCheckSign)
				{
					// Verifica HASH CRC32
					//uint crc = Crc32CAlgorithm.Compute(FileUtil.ReadFully(EsitoChk.FileNameDecrypt));

					//					string sFilecontent = "";
					//					Logger.WriteOnLog(LogId, "Leggo il file : " + EsitoChk.FileNameDecrypt + "<--fine", 3);
					//					sFilecontent = File.ReadAllText(EsitoChk.FileNameDecrypt);
					//					var arrayOfBytes = Encoding.ASCII.GetBytes(sFilecontent);
					var crc32 = new Crc32();
					//string sHashNow = crc32.Get(File.ReadAllBytes(EsitoChk.FileNameDecrypt)).ToString("X").ToUpper();
					// Modifica calcolo HASH (estratto da testo PDF)
					var oPdfUtil = new PdfUtil();
					string sHashNow = oPdfUtil.extractTextFromPdf(Convert.ToBase64String(File.ReadAllBytes(EsitoChk.FileNameDecrypt)), Logger, LogId);
					Logger.WriteOnLog(LogId, "Rosetta HASH Calcolato: " + sHashNow + " per il file: " + EsitoChk.FileNameDecrypt, 3); 
					Logger.WriteOnLog(LogId, "HASH ricevuto dal chiamante: " + sCRC32, 3);
					//FileUtil.ReadAllToString(EsitoChk.FileNameDecrypt,out sFilecontent);
					//					Logger.WriteOnLog(LogId, "File XML ove calcolo HASH -->" + sFilecontent + "<--fine", 3);
					// calcolo diverse varianti del file xml estrapolato
					//					string sHashNow = CrcFromString.CRC32OfString(sFilecontent);
					// senza 2 byte CR + Lf
					//					string sHashNow2ByteLess = CrcFromString.CRC32OfString(sFilecontent.Substring(0, sFilecontent.Length - 2));
					// senza 1 byte (CR o Lf)
					//					string sHashNow1ByteLess = CrcFromString.CRC32OfString(sFilecontent.Substring(0, sFilecontent.Length - 1));
					// Togliendo ogni carattere a capo
					//					string sHashNowCrLfLess = CrcFromString.CRC32OfString(sFilecontent.Replace(System.Environment.NewLine, ""));

					// senza 2 byte CR + Lf con TRIM
					//					string sHashNow2ByteLessTrim = CrcFromString.CRC32OfString(sFilecontent.Substring(0, sFilecontent.Length - 2).Trim());
					// senza 1 byte (CR o Lf) con TRIM
					//					string sHashNow1ByteLessTrim = CrcFromString.CRC32OfString(sFilecontent.Substring(0, sFilecontent.Length - 1).Trim());
					// Togliendo ogni carattere a capo con TRIM
					//					string sHashNowCrLfLessTrim = CrcFromString.CRC32OfString(sFilecontent.Replace(System.Environment.NewLine, "").Trim());

					//string sHashNow = crc32.Get(FileUtil.ReadFully(EsitoChk.FileNameDecrypt)).ToString("X");
					// Verifica Hash
					/*					Logger.WriteOnLog(LogId, "HASH Calcolato: " + sHashNow + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "sHashNow2ByteLess Calcolato: " + sHashNow2ByteLess + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "sHashNow1ByteLess Calcolato: " + sHashNow1ByteLess + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "HASH sHashNowCrLfLess: " + sHashNowCrLfLess + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "sHashNow2ByteLess con TRIM Calcolato: " + sHashNow2ByteLessTrim + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "sHashNow1ByteLess con TRIM Calcolato: " + sHashNow1ByteLessTrim + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "HASH sHashNowCrLfLess con TRIM: " + sHashNowCrLfLessTrim + " sul file " + EsitoChk.FileNameDecrypt, 3);
										Logger.WriteOnLog(LogId, "HASH inviato: " + sCRC32, 3);
										*/
					if (sCRC32 == sHashNow || string.IsNullOrEmpty(sCRC32))
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo HASH Esito: Positivo", 3);
					}
					/*
					else if (sCRC32 == sHashNow2ByteLess)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNow2ByteLess Esito: Positivo", 3);
					}
					else if (sCRC32 == sHashNow1ByteLess)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNow1ByteLess Esito: Positivo", 3);
					}
					else if (sCRC32 == sHashNowCrLfLess)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNowClRfLess Esito: Positivo", 3);
					}
					else if (sCRC32 == sHashNow2ByteLessTrim)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNow2ByteLessTrim Esito: Positivo", 3);
					}
					else if (sCRC32 == sHashNow1ByteLessTrim)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNow1ByteLessTrim Esito: Positivo", 3);
					}
					else if (sCRC32 == sHashNowCrLfLessTrim)
					{
						bCheckHash = true;
						Logger.WriteOnLog(LogId, "Controllo sHashNowCRLfLessTrim Esito: Positivo", 3);
					}
					*/
					else
					{
						sEsito = @resourceFileManager.getConfigData("HashNo");
						Logger.WriteOnLog(LogId, "Controllo HASH Esito: Negativo", 3);
					}
					// Fine Verifica HASH CRC32
					if (bCheckHash)
					{
						if (lNIN.Count > 0)
						{
							// Verifica certificati
							var p7m = System.IO.File.ReadAllBytes(@sPathMainDoc);
							string sCfVerify = "";
							foreach (var cert in EnumSigners(p7m))
							{
								sCfVerify += cert.SubjectName.Format(true);
								Logger.WriteOnLog(LogId, "CERT: " + Logger.ToJson(cert.SubjectName.Format(true)), 3);
							}
							string sValueNotFound = "Identificativi non trovati: ";
							bool bCheckCfVat = false;
							foreach (string singleCfCheck in lNIN)
							{
								Logger.WriteOnLog(LogId, "Controllo l'esitenza del codice fiscale: " + singleCfCheck, 3);
								if (sCfVerify.ToLower().IndexOf(singleCfCheck.ToLower()) == -1)
								{
									bCheckCfVat = true;
									sValueNotFound += " " + singleCfCheck + ",";
									sEsito = @resourceFileManager.getConfigData("CfKo");
								}
							}
							if (bCheckCfVat)
								strMessage = sValueNotFound;
						}
						else
						{
							Logger.WriteOnLog(LogId, "Nessun controllo sul codice fiscale", 3);
						}
					}
					// Fine verifica certificati

					if (bGetDocWithoutSign)
					{
						Logger.WriteOnLog(LogId, "Ritorno il contenuto del file.", 3);
						byte[] oArrByte = FileUtil.ReadFully(EsitoChk.FileNameDecrypt);
						mainDocOut.BinaryContent = Convert.ToBase64String(oArrByte);
					}
				}
				mainDocOut.Filename = mainDoc.Filename;
				mainDocOut.OutcomeCheck = sEsito;
				Logger.WriteOnLog(LogId, "MainDocOut: " + Logger.ToJson(mainDocOut), 3);
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
				Logger.WriteOnLog(LogId, Logger.ToJson(e), 3);
			}
			finally
			{
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + "_" + LogId);
				}
			}
			return esito;
		}
		static public IEnumerable<X509Certificate2> EnumSigners(byte[] pkcs7Envelope, bool recurse = true)
		{
			var cms = DecodePkcs7(pkcs7Envelope);

			var certs = cms.Certificates.Cast<X509Certificate2>();

			try
			{
				if (recurse && cms.ContentInfo.ContentType.FriendlyName == "PKCS 7 Data")
					certs = certs.Concat(EnumSigners(cms.ContentInfo.Content, recurse));
			}
			catch (System.Security.Cryptography.CryptographicException)
			{
			}

			return certs;
		}

		/// <summary>
		/// Possibili formati di trasporto della busta PKCS#7
		/// </summary>
		private enum Pkcs7TransportEncoding
		{
			/// <summary>
			/// Formato binario (nessun encoding applicato)
			/// </summary>
			DerBer = 1,

			/// <summary>
			/// Base64 
			/// </summary>
			Base64,

			/// <summary>
			/// Pem
			/// </summary>
			Pem,
		}

		private static SignedCms DecodePkcs7(byte[] pkcs7Envelope)
		{
			Pkcs7TransportEncoding p7mFormat = 0;

			// Si tratta di un ascii?

			string ascii = null;

			var asciiEncoding = Encoding.GetEncoding(
						  "us-ascii",
						  new EncoderExceptionFallback(),
						  new DecoderExceptionFallback());

			try
			{
				ascii = asciiEncoding.GetString(pkcs7Envelope);
			}
			catch (DecoderFallbackException)
			{
				// Se non è un ascii, per esclusione deve essere una busta binaria (DER o BER)
				p7mFormat = Pkcs7TransportEncoding.DerBer;
			}

			// Se è un ascii allora può essere una busta in Base64 o una busta Pem

			if (p7mFormat == 0)
			{
				const string PEMBegin = "-----BEGIN PKCS7-----";
				const string PEMEnd = "-----END PKCS7-----";

				// Se inizia e termina con i tag PEMBegin e PEMEnd allora è un PEM
				// e questi delimitano il base64 del firmato

				if (ascii.Substring(0, PEMBegin.Length) == PEMBegin
					&& ascii.Substring(ascii.Length - PEMEnd.Length) == PEMEnd)
				{
					p7mFormat = Pkcs7TransportEncoding.Pem;

					ascii = ascii.Substring(PEMBegin.Length, ascii.Length - PEMBegin.Length - PEMEnd.Length);
				}

				// Conversione da base64

				try
				{
					pkcs7Envelope = Convert.FromBase64String(ascii);

					if (p7mFormat == 0)
						p7mFormat = Pkcs7TransportEncoding.Base64;
				}
				catch (FormatException)
				{
					// Se si tratta di un PEM il contenuto obbligatoriamente deve essere un base64
					if (p7mFormat == Pkcs7TransportEncoding.Pem)
						throw;

					// altrimenti si fa un tentativo finale come DER/PEM
					p7mFormat = Pkcs7TransportEncoding.DerBer;
				}
			}

			// Decodifica la busta binaria ed estrae il contenuto

			var cms = new SignedCms();
			cms.Decode(pkcs7Envelope);

			return cms;
		}

		public Outcome setInternalAttachment(string usernName, string sPassword, string sGuidCardFrom, string sGuidCardTo, string Note, string sInternalNote, bool bBiunivocal)
		{
			string strMessage = "";
			Outcome esito = new Outcome();

			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Lavoro con l'utenza: " + usernName, 3);
			string sConnection = "";
			ConnectionManager oConnectionManager = new ConnectionManager();
			string sGuidCardForLog = sGuidCardFrom;
			try
			{
				if (!string.IsNullOrEmpty(sGuidCardFrom.Trim()))
				{
					if (!string.IsNullOrEmpty(sGuidCardTo.Trim()))
					{
						string oUserName = oRijndael.Decrypt(usernName);
						string oUserPwd = oRijndael.Decrypt(sPassword);
						sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
						using (var oCardManager = new CardManager(Logger, LogId))
						{
							oCardManager.SetInternalAttach(sConnection, sGuidCardFrom, sGuidCardTo, Note, sInternalNote, bBiunivocal);
						}
					}
					else
					{
						// il numero di protocollo non è valorizzato
						strMessage = "Il valore GUIDto non è valorizzato.";
					}
				}
				else
				{
					// il numero di protocollo non è valorizzato
					strMessage = "Il valore GUIDfrom non è valorizzato.";
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (!string.IsNullOrEmpty(sConnection))
				{
					oConnectionManager.CloseConnect();
				}
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCardForLog + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCardForLog + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome getNameFieldFromTypeDoc(string userName, string password, string sTipologiaDocumentale, out List<string> stIndexFieldKey)
		{
			#region Inizializzazione oggetti
			ConnectionManager connManager = new ConnectionManager();
			string strconnId = "";
			string strMessage = "";

			// Sezione inerente all'inizializzazione dei valori utii all'inserimento scheda
			stIndexFieldKey = new List<string>();
			Outcome esito = new Outcome();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			// fine sezione per inserimento scheda
			#endregion

			try
			{
				#region Mappatura entità in ingresso con i parametri del metodo di inserimento

				#endregion
				Logger.WriteOnLog(LogId, "sTipologiaDocumentale: " + sTipologiaDocumentale, 3);

				#region Login Archiflow
				string oUserName = oRijndael.Decrypt(userName);
				string oUserPwd = oRijndael.Decrypt(password);
				strconnId = connManager.OpenUserConnect(oUserName, oUserPwd);
				#endregion
				#region Inserimento scheda in Archiflow
				if (strconnId != null)
				{
					using (var oDocManager = new DocManager(Logger, LogId))
					{
						stIndexFieldKey = oDocManager.GetFieldsDocTypeByName(strconnId, sTipologiaDocumentale);
					}
					Logger.WriteOnLog(LogId, "stIndexFieldKey: " + stIndexFieldKey, 3);
					strMessage = "Azione eseguita con successo.";
				}
				#endregion
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (strconnId != "")
				{
					connManager.CloseConnect();
				}
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + userName + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + userName + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome setMainDocument(string usernName, string sPassword, MainDocument oMainDoc, string sGuidCard)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			string sConnection = "";
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Lavoro con l'utenza: " + usernName, 3);
			ConnectionManager oConnectionManager = new ConnectionManager();
			string sGuidCardForLog = sGuidCard;
			try
			{

				Logger.WriteOnLog(LogId, "Attachment: " + oMainDoc.ToXml(), 3);
				if (oMainDoc.Validate(out strMessage))
				{
					if (!string.IsNullOrEmpty(sGuidCard.Trim()))
					{
						string oUserName = oRijndael.Decrypt(usernName);
						string oUserPwd = oRijndael.Decrypt(sPassword);
						sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
						using (var oDocManager = new DocManager(Logger, LogId))
						{
							oDocManager.SetMainDocByteArr(sConnection, sGuidCard, System.Convert.FromBase64String(oMainDoc.BinaryContent), oMainDoc.Filename);
						}
					}
					else
					{
						// il numero di protocollo non è valorizzato
						strMessage = "Il valore GUID non è valorizzato.";
					}
				}
				else
				{
					// non ho trovato allegati
					throw new Exception(String.Format("{0} >> ", "ERRORE : setMainDocument Validation" + strMessage));
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (!string.IsNullOrEmpty(sConnection))
				{
					oConnectionManager.CloseConnect();
				}
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCardForLog + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCardForLog + "_" + LogId);
				}
			}
			return esito;
		}
		
			public Outcome setXmlSignature(string sConnection, string sGuidCard, string ProfileName)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Lavoro con la profilazione di segnatura: " + ProfileName, 3);
			Logger.WriteOnLog(LogId, "Progressivo assoluto scheda: " + sGuidCard, 3);
			try
			{
				if (string.IsNullOrEmpty(ProfileName))
				{
					throw new ArgumentException("Manca il nome profilazione della segnatura.");
				}
				else if (string.IsNullOrEmpty(sGuidCard))
				{
					throw new ArgumentException("Manca la valorizzazione del progressivo assoluto");
				}
				else if (string.IsNullOrEmpty(sConnection))
				{
					throw new ArgumentException("Manca la valorizzazione del parametro di connessione");
				}
				var sTemplateProfileSignXml = resourceFileManager.getConfigData(ProfileName);
				var sNameProfileSignXml = resourceFileManager.getConfigData(ProfileName + "_Nome");
				var sNoteProfileSignXml = resourceFileManager.getConfigData(ProfileName + "_Nota");
				var sArchiveProfileSignXml = resourceFileManager.getConfigData(ProfileName + "_Archivio");
				if (!string.IsNullOrEmpty(ProfileName))
				{	

					string sPathFile = sWorkfolder + sNameProfileSignXml;
					Logger.WriteOnLog(LogId, "File path: " + sPathFile, 3);
					using (var oWcfSiavCardManager = new WcfSiavCardManager(Logger, LogId))
					{
						OCF_Ws.WsCard.CardBundle oCardBundle;
						string sValueTextFile = sTemplateProfileSignXml;
						oWcfSiavCardManager.GetCard(sGuidCard, sConnection, out oCardBundle);
						NameValueCollection oIndexes;
						oWcfSiavCardManager.GetIndexes(oCardBundle,out oIndexes);
						var items = oIndexes.AllKeys.SelectMany(oIndexes.GetValues, (k, v) => new { key = k, value = v });
						foreach (var item in items)
						{
							if (item.key.ToUpper()== "NUMERO PROTOCOLLO")
							{
							   sValueTextFile = sValueTextFile.Replace("«" + item.key + "»", String.Format("{0:D7}", int.Parse(item.value.Substring(0, item.value.IndexOf("/")))));
							}
							if (item.key.ToUpper() == "UFFICIO MITTENTE")
							{
								sValueTextFile = sValueTextFile.Replace("«Mittente»", item.value);
							}
							if (item.key.ToUpper() == "MITTENTE")
							{
								sValueTextFile = sValueTextFile.Replace("«Mittente»", item.value);
							}
							if (item.key.ToUpper() == "UFFICIO DESTINATARIO")
							{
								sValueTextFile = sValueTextFile.Replace("«Destinatario»",item.value);
							}
							if (item.key.ToUpper() == "DESTINATARIO")
							{
								sValueTextFile = sValueTextFile.Replace("«Destinatario»", item.value);
							}
							else 
								sValueTextFile = sValueTextFile.Replace("«" + item.key + "»", item.value);
						}
						sValueTextFile = sValueTextFile.Replace("«ProgAssoluto»", String.Format("{0:D9}", int.Parse(sGuidCard)));
						sValueTextFile = sValueTextFile + System.Environment.NewLine;
						sNoteProfileSignXml = sNoteProfileSignXml.Replace("«DateNow»", DateTime.Now.ToString("dd/MM/yyyy") + " ore " + DateTime.Now.ToString("HH:mm:ss"));
						Logger.WriteOnLog(LogId, "Metadati: " + Logger.ToJson(oIndexes), 3);
						string pattern = @"«[^»]*?»(.*?)";
						Logger.WriteOnLog(LogId, "Testo segnatura: " + sValueTextFile, 3);

						Regex regex = new Regex(pattern);
						Match m = regex.Match(sValueTextFile);

						if (m.Success)
						{
							string sTagWithoutReplace = "";
							foreach (var group in m.Groups)
							{
								sTagWithoutReplace += group.ToString() + " ";
							}
							/*string id = m.Groups["id"].Value;
							int quantity = Int32.Parse(m.Groups["quantity"].Value);
							int points = Int32.Parse(m.Groups["points"].Value);
							*/
							Logger.WriteOnLog(LogId, "Sono stati trovati i seguenti tag non sostituiti: " + sTagWithoutReplace, 3);

							throw new ArgumentException("Sono stati trovati i seguenti tag non sostituiti: " + sTagWithoutReplace);
							//Console.WriteLine(id + ", " + quantity + ", " + points);
						}
						else
						{
							Encoding iso = Encoding.GetEncoding("ISO-8859-1");
							Encoding utf8 = Encoding.UTF8;
							byte[] utfBytes = utf8.GetBytes(sValueTextFile);
							byte[] isoBytes = Encoding.Convert(utf8, iso, utfBytes);
							//string msg = iso.GetString(isoBytes);
							//Util.FileUtil.MaterializeFile(sPathFile, Util.FileUtil.GetBytes(msg));
							oWcfSiavCardManager.SetAttachment(sGuidCard, sConnection, sNameProfileSignXml, isoBytes, sNoteProfileSignXml, false);
						}
					}
				}
				else
				{
					// il numero di protocollo non è valorizzato
					strMessage = "Il valore GUID non è valorizzato.";
				}
				
				// non ho trovato allegati
				//throw new Exception(String.Format("{0} >> ", "ERRORE : Attachment Validation" + strMessage));
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCard + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCard + "_" + LogId);
				}
			}
			return esito;
		}
		public Outcome setAttachment(string usernName, string sPassword, Model.Attachment Attachment, string sGuidCard)
		{
			string strMessage = "";
			Outcome esito = new Outcome();
			string sConnection = "";
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			Logger.WriteOnLog(LogId, "Lavoro con l'utenza: " + usernName, 3);
			ConnectionManager oConnectionManager = new ConnectionManager();
			string sGuidCardForLog = sGuidCard;
			try
			{
				Logger.WriteOnLog(LogId, "Attachment: " + Attachment.ToXml(), 3);
				if (Attachment.Validate(out strMessage))
				{
					if (!string.IsNullOrEmpty(sGuidCard.Trim()))
					{
						string oUserName = oRijndael.Decrypt(usernName);
						string oUserPwd = oRijndael.Decrypt(sPassword);
						sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
						using (var oCardManager = new CardManager(Logger, LogId))
						{
							oCardManager.SetExternalAttach(sConnection, sGuidCard, Attachment.Filename, Convert.FromBase64String(Attachment.BinaryContent), Attachment.Note); 
						}
					}
					else
					{
						// il numero di protocollo non è valorizzato
						strMessage = "Il valore GUID non è valorizzato.";
					}
				}
				else
				{
					// non ho trovato allegati
					throw new Exception(String.Format("{0} >> ", "ERRORE : Attachment Validation" + strMessage));
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				if (!string.IsNullOrEmpty(sConnection))
				{
					oConnectionManager.CloseConnect();
				}
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + sGuidCardForLog + "_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + sGuidCardForLog + "_" + LogId);
				}
			}
			return esito;
		}

	}
}