using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Specialized;
using OCF_Ws.WsSign;
using OCF_Ws.Model;
using System.IO;
using OCF_Ws.Util;

namespace OCF_Ws.Manager
{
	public class WcfSiavSignManager
	{
		public LOLIB _Logger;
		public string _sLogId;
		public SignServiceContractClient Sign;
		private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
		{
			var certificate = (System.Security.Cryptography.X509Certificates.X509Certificate2)cert;
			return true;
		}
		public WcfSiavSignManager(LOLIB Logger, string sLogId)
		{
			_sLogId = sLogId;
			_Logger = Logger;
			//ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
			this.Sign = new OCF_Ws.WsSign.SignServiceContractClient();
		}


		
		public EsitoCheckFileSigned CheckSignTimeStampFileInfo2(string FileToVerifiy, string FileToVerifiyWithoutSign)
	{
		EsitoCheckFileSigned oEsitoCheckFileSigned = new EsitoCheckFileSigned();
		try
		{
				//				const string FileToVerifiy = @"C:\Users\GTorelli\Desktop\Firmati Marcati\Notifica_6789_20040312110300.pdf.p7m";
			SC.FileIni = @"arcsql35.ini";
			_Logger.WriteOnLog(_sLogId, "Verifico la firma", 3);
				using (var signSC = new SC(true))
			{
				_Logger.WriteOnLog(_sLogId, "Eseguo la verifica", 3);
				var checkResult = signSC.Verify(FileToVerifiy,VerifyDegree.StandardVerify,SaveOption.SaveSignContent, @FileToVerifiyWithoutSign );
				_Logger.WriteOnLog(_sLogId, _Logger.ToJson(checkResult), 3);
				var sPathFileDecrypted = @checkResult.ContentFileName;
				if (File.Exists(sPathFileDecrypted))
				{
					oEsitoCheckFileSigned.FileNameDecrypt = checkResult.ContentFileName;
					oEsitoCheckFileSigned.byteContentDecrypt = FileUtil.ReadFully(sPathFileDecrypted);
				}
				if (checkResult.SignaturesValid == true)
				{
					oEsitoCheckFileSigned.Check = true;
					oEsitoCheckFileSigned.Descrizione = "Il file di tipo P7M è stato correttamente validato.";
				} // true
				else if (checkResult.SignaturesValid == false)
				{
					oEsitoCheckFileSigned.Check = false;
					oEsitoCheckFileSigned.Descrizione = "Il file di tipo PDF NON è valido.";
				} // false
				else
				{
					oEsitoCheckFileSigned.Check = false;
					oEsitoCheckFileSigned.Descrizione = "Il file non è firmato.";
				} // null
			}
		}
		catch (Exception ex)
		{
				_Logger.WriteOnLog(_sLogId, ex.StackTrace + " - " + ex.Message + " - " + ex.Source, 1);
				throw new ArgumentException(ex.Message + " - " + ex.StackTrace);
		}
		return oEsitoCheckFileSigned;
	}



public EsitoCheckFileSigned CheckSignTimeStampFileInfo(WcfSiavLoginManager wcfSiavLoginManager, string Filename, byte[] aFileBin, bool mustDecrypted)
		{
			OCF_Ws.WsSign.SignTimeStampFileInfo oSignTimeStampFileInfo;
			EsitoCheckFileSigned oEsitoCheckFileSigned = new EsitoCheckFileSigned();
			oSignTimeStampFileInfo = null;
			try
			{

				OCF_Ws.WsSign.ResultInfo oResult = OCF_Ws.WsSign.ResultInfo.OK;
				// call the WCF service contract to get an user information (the user with ID=0)
				OCF_Ws.WsSign.SessionInfo oSessionInfo = new OCF_Ws.WsSign.SessionInfo();
				oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
				oResult = Sign.GetSignTimeStampFileInfo(out oSignTimeStampFileInfo, oSessionInfo, Filename, aFileBin, mustDecrypted);
				
				if (oResult == OCF_Ws.WsSign.ResultInfo.OK)
				{
					if (oSignTimeStampFileInfo.IsSignedP7M)
					{
						oEsitoCheckFileSigned.FileNameDecrypt = Filename;
						oEsitoCheckFileSigned.byteContentDecrypt = oSignTimeStampFileInfo.DecryptedFile;

						if (oSignTimeStampFileInfo.IsSignedP7MValid)
						{
							oEsitoCheckFileSigned.Check = true;
							oEsitoCheckFileSigned.Descrizione = "Il file di tipo P7M è stato correttamente validato.";
						}
						else
						{
							oEsitoCheckFileSigned.Check = false;
							oEsitoCheckFileSigned.Descrizione = "2 – Documento con firma NON valida.";
						}

					}
					else if (oSignTimeStampFileInfo.IsSignedPdf)
					{
						oEsitoCheckFileSigned.FileNameDecrypt = Filename;
						oEsitoCheckFileSigned.byteContentDecrypt = oSignTimeStampFileInfo.DecryptedFile;

						if (oSignTimeStampFileInfo.IsSignedPdfValid)
						{
							oEsitoCheckFileSigned.Check = true;
							oEsitoCheckFileSigned.Descrizione = "Il file di tipo PDF è stato correttamente validato.";
						}
						else
						{
							oEsitoCheckFileSigned.Check = false;
							oEsitoCheckFileSigned.Descrizione = "2 – Documento con firma NON valida.";
						}
					}
					else if (oSignTimeStampFileInfo.IsSignedXml)
					{
						oEsitoCheckFileSigned.FileNameDecrypt = Filename;
						oEsitoCheckFileSigned.byteContentDecrypt = oSignTimeStampFileInfo.DecryptedFile;

						if (oSignTimeStampFileInfo.IsSignedXmlValid)
						{
							oEsitoCheckFileSigned.Check = true;
							oEsitoCheckFileSigned.Descrizione = "Il file di tipo XML è stato correttamente validato.";
						}
						else
						{
							oEsitoCheckFileSigned.Check = false;
							oEsitoCheckFileSigned.Descrizione = "2 – Documento con firma NON valida.";
						}
					}
					else if (oSignTimeStampFileInfo.IsTimeStamp)
					{
						oEsitoCheckFileSigned.FileNameDecrypt = Filename;
						oEsitoCheckFileSigned.byteContentDecrypt = oSignTimeStampFileInfo.DecryptedFile;

						if (oSignTimeStampFileInfo.IsTimeStampValid)
						{
							oEsitoCheckFileSigned.Check = true;
							oEsitoCheckFileSigned.Descrizione = "Il file ha la marcatura temporale correttamente validata.";
						}
						else
						{
							oEsitoCheckFileSigned.Check = false;
							oEsitoCheckFileSigned.Descrizione = "Il file ha la marcatura temporale non valida.";
						}
					}
				}
				else
					throw new ArgumentException("Errore ricezione dati file");
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> fex)
			{
				throw new ArgumentException(fex.Detail.Message);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
			return oEsitoCheckFileSigned;
		}
	}
}

