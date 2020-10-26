using OCF_Ws.ContractTypes;
using OCF_Ws.Manager;
using OCF_Ws.WsClassifica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCF_Ws.Util
{
	public class UtilSvCard : IDisposable
	{
		bool mDisposed = false; public int lErr = 0;
		public ConnectionManager oConnectionManager;
		public LOLIB _Logger;
		public string _sLogId;

		public UtilSvCard(LOLIB Logger, string sLogId)
		{
			oConnectionManager = new ConnectionManager();
			_sLogId = sLogId;
			_Logger = Logger;
		}
		public List<string> getClassificaDocumento(out List<string> lIdDossiers, string id)
		{
			ResourceFileManager resourceFileManager;
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
			lIdDossiers = new List<string>();
			ResultInfo ri = ResultInfo.NULL;
			WsClassifica.ClassificaDocumentoSearcher Searcher = new WsClassifica.ClassificaDocumentoSearcher();
			_Logger.WriteOnLog(_sLogId, "getGruppiDescrizione: " + id, 3);
			_Logger.WriteOnLog(_sLogId, "getGruppiDescrizione: " + _Logger.ToJson(lIdDossiers), 3);
			Searcher.DocumentId = new DocumentIdentifier();
			Searcher.DocumentId.Id = Int32.Parse(id);
			Searcher.TitolarioItemId = new TitolarioItemIdentifier();
			Searcher.TitolarioItemId.Id = Int32.Parse( resourceFileManager.getConfigData("IdTitolario"));
			_Logger.WriteOnLog(_sLogId, "getGruppiDescrizione: " + _Logger.ToJson(Searcher.TitolarioItemId.Id), 3);
			List<WsClassifica.ClassificaDocumentoEntity> ReturnValue = new List<WsClassifica.ClassificaDocumentoEntity>();
			//oEsito.Codice = "1";
			 
			WsClassifica.iClassificaServiceContractClient oWsClassifica = new WsClassifica.iClassificaServiceContractClient();
			//WsClassifica.ResultInfo oResultInfoClassifica = new WsClassifica.ResultInfo();
			try
			{
				ri = oWsClassifica.ReadClassificaDocumento(out ReturnValue, Searcher);
				_Logger.WriteOnLog(_sLogId, "getGruppiDescrizione: " + _Logger.ToJson(ri), 3);
			}
			catch(Exception e)
			{ 

			oWsClassifica.Close();
				throw new Exception("Errore durante la lettura della classifica.", e);

			}
			oWsClassifica.Close();
			if (ri == WsClassifica.ResultInfo.OK)
			{
				foreach (var value in ReturnValue)
				{
					int Id = Convert.ToInt32(value.Id.Id);
					lIdDossiers.Add(Id.ToString());
				}
			}
			else
			{
				//oEsito.Codice = "0";
				throw new Exception("Errore durante la lettura della classifica.");
			}
			return lIdDossiers;
			//return oEsito;
		}
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
				Console.WriteLine("sto chiamando il metodo Dispose per la classe ConnectionManager...");
				//Supressing the Finalization method call
				GC.SuppressFinalize(this);

			}
			mDisposed = true;
		}
		public string OpenConnection(string usernName, string password)
		{
			string sConnection = "";
			Rijndael oRijndael;
			
			oRijndael = new Rijndael();
			string oUserName = oRijndael.Decrypt(usernName);
			string oUserPwd = oRijndael.Decrypt(password);
			sConnection = oConnectionManager.OpenUserConnect(oUserName, oUserPwd);
			return sConnection;
		}
		
		public void CloseConnection(string sConnection)
		{
			if (!string.IsNullOrEmpty(sConnection))
			{
				oConnectionManager.CloseConnect();
			}
		}
		public string getIdInterno(List<dynamic> allMetadati)
		{
			string IdInterno = "";
			//var metadati = Manager.CardManager.GetIndiciScheda(stGuidConnect, sGuidCard, LogId, false);

			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.id) == 2)
				{
					IdInterno = x.value;
					break;
				}
			}
			return IdInterno;
		}

		public string getIdProtocollo(List<dynamic> allMetadati)
		{
			string IdInterno = "";
			//var metadati = Manager.CardManager.GetIndiciScheda(stGuidConnect, sGuidCard, LogId, false);

			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.id) == 0)
				{
					IdInterno = x.value;
					break;
				}
			}
			return IdInterno;
		}

		public string getDataProtocollo(List<dynamic> allMetadati)
		{
			string dataProtocollo = "";
			//var metadati = Manager.CardManager.GetIndiciScheda(stGuidConnect, sGuidCard, LogId, false);

			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.id) == 1)
				{
					dataProtocollo = x.value;
					break;
				}
			}
			return dataProtocollo;
		}

		public string getDataDocumento(List<dynamic> allMetadati)
		{
			string dataDocumento = "";
			//var metadati = Manager.CardManager.GetIndiciScheda(stGuidConnect, sGuidCard, LogId, false);

			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.id) == 1)
				{
					dataDocumento = x.value;
					break;
				}
			}
			return dataDocumento;
		}

		public List<string> getMetadati(List<dynamic> allMetadati,string LogId)
		{
			List<string> listaMetadati = new List<string>();
			LOLIB Logger;
			Logger = new LOLIB();
			Logger.WriteOnLog(LogId, "allMetadati: " + Logger.ToJson(allMetadati), 3);
			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.value) && Int32.Parse(x.value) !=24)
				{
					listaMetadati.Add(x.value);
					Logger.WriteOnLog(LogId, "x.value: " + Logger.ToJson(x.value), 3);
				}
			}
			return listaMetadati;
		}
		public List<Model.Attachment> Attachment(string stGuidConnect, SVAOLLib.Card gCard, List<string> idAttachments, Boolean addBinary = true)
		{
			//var att = UtilSvCard.attchment(sConnection, oSvCard, LogId, idAttachments, true);
			List<dynamic> lAttachmentExt = new List<dynamic>();
			using (var oCardManager = new CardManager(_Logger, _sLogId))
			{
				lAttachmentExt = oCardManager.GetAttachmentExt(stGuidConnect, gCard, idAttachments, true);
			}
			List<Model.Attachment> oAttachments = new List<Model.Attachment>();
			foreach (var singleAttach in lAttachmentExt)
			{
				Model.Attachment oAttachment = new Model.Attachment();
				oAttachment.BinaryContent = singleAttach.binarycontent;
				oAttachment.Filename = singleAttach.nomefile;
				oAttachment.Note = singleAttach.note;
				oAttachments.Add(oAttachment);
			}
			return oAttachments;

		}
		public string getObjectDescription(List<dynamic> allMetadati)
		{
			string objectDescription = "";
			//var metadati = Manager.CardManager.GetIndiciScheda(stGuidConnect, sGuidCard, LogId, false);

			foreach (var x in allMetadati)
			{
				if (Int32.Parse(x.id) == 24)
				{
					objectDescription = x.value;
				}
			}
			return objectDescription;
		}
	}
}