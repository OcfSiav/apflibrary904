using OCF_Ws.Action;
using OCF_Ws.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace OCF_Ws
{
	// NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di classe "Service1" nel codice e nel file di configurazione contemporaneamente.
	public class Services : IServices
	{

/*
	1.	26/10/2018: Rilascio comprensivo di test dei servizi: setMainDoc; setAttachment; setInternalAttachment;
	2.	29/10/2018: Eventuale assistenza alla integrazione;
	3.	06/11/2018: Rilascio comprensivo di test dei servizi: getIndexConfigFromTypeDocument; getSignCheck; base64ToPdfA (*);
	4.	07/11/2018: Eventuale assistenza alla integrazione;
	5.	12/11/2018: Rilascio comprensivo di test dei servizi: createPdfAFromTemplate; setCard; getCard;
	6.	13/11/2018: eventuale assistenza alla integrazione;
	7.	14/11/2018: Test di integrazione complessivi;

	*Per i test di questi servizi e necessaria l’installazione della libreria Easy PDF 8 e del pacchetto Office 2016.

	Note generali
	•	E’ necessario creare una utenza di servizio (Archiflow), che verrà utilizzata dall’esterno per la login in AF e la fruizione dei servizi.
	•	Nel secondo rilascio (punto 3) sono incluse le modifiche ed i test ai processi massivi per l’utilizzo di easyPdf8 tramite servizio.
*/
		public Outcome getSignCheck(MainDocument mainDoc, string sCRC32, List<String> lNIN, bool bGetDocWithoutSign, out MainDocumentChecked mainDocOut)
		{
			#region Inizializzazione oggetti

			mainDocOut = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("SignCheck"))
			{
				esito = wsAction.getSignCheck(mainDoc, sCRC32, lNIN, bGetDocWithoutSign, out mainDocOut);
			}
			return esito;
		}

		public Outcome getIndexConfigFromTypeDocument(string username, string password, string sTipologiaDocumentale, out List<String> lstIndexFieldKey)
		{
			#region Inizializzazione oggetti

			lstIndexFieldKey = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("getIndexConfigFromTypeDocument"))
			{
				esito = wsAction.getNameFieldFromTypeDoc(username, password, sTipologiaDocumentale, out lstIndexFieldKey);
			}
			return esito;
		}

		public Outcome createPdfACRC32bFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocumentCRC32b mainDocOut)
		{
			#region Inizializzazione oggetti

			mainDocOut = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("createPdfACRC32bFromTemplate"))
			{
				esito = wsAction.createPdfACRC32bFromTemplate(aReplaceNameValue, sGuidCard, out mainDocOut);
			}
			return esito;
		}
		public Outcome getCRC32bFromFilePdf(MainDocument filePdf, out string CRC32b)
		{
			#region Inizializzazione oggetti

			CRC32b = "";
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("getCRC32bFromFilePdf"))
			{
				esito = wsAction.getCRC32bFromFilePdf(filePdf, out CRC32b);
			}
			return esito;
		}

		public Outcome createPdfAFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocument mainDocOut)
		{
			#region Inizializzazione oggetti

			mainDocOut = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("createPdfAFromTemplate"))
			{
				esito = wsAction.createPdfAFromTemplate(aReplaceNameValue, sGuidCard, out mainDocOut);
			}
			return esito;
		}
		public Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut)
		{
			#region Inizializzazione oggetti

			mainDocOut = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("base64ToPdfA"))
			{
				esito = wsAction.base64ToPdfA(mainDoc, out mainDocOut);
			}
			return esito;
		}
		public Outcome getCard(string usernName, string password, string sGuidCard, bool bGetMainDoc, bool bGetAttachment, out Card card)
		{
			#region Inizializzazione oggetti

			card = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("getCard"))
			{
				esito = wsAction.getCard(usernName, password, sGuidCard, bGetMainDoc, bGetAttachment, out card);
			}
			return esito;
		}
		
		public Outcome setXmlSignature(string sConnection, string sGuidCard, string ProfileName)
		{
			#region Inizializzazione oggetti
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("setXmlSignature"))
			{
				esito = wsAction.setXmlSignature(sConnection, sGuidCard, ProfileName);
			}
			return esito;
		}
		public Outcome setCard(string usernName, string sPassword, Card card, out string sGuidCard)
		{
			#region Inizializzazione oggetti
			sGuidCard = "";
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("setCard"))
			{
				//esito = wsAction.setMainDocument(sUserName, sPassword, oMainDoc, oMainDoc, sGuidCard);
			}
			return esito;
		}

		public Outcome setMainDocument(string usernName, string sPassword, MainDocument oMainDoc, string sGuidCard)
		{
			#region Inizializzazione oggetti
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("setMainDocument"))
			{
				esito = wsAction.setMainDocument(usernName, sPassword, oMainDoc, sGuidCard);
			}
			return esito;
		}
		public Outcome setAttachment(string usernName, string sPassword, Attachment Attachment, string sGuidCard)
		{
			#region Inizializzazione oggetti
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("setAttachment"))
			{
				esito = wsAction.setAttachment(usernName, sPassword, Attachment, sGuidCard);
			}
			return esito;
		}
		public Outcome setInternalAttachment(string usernName, string sPassword, string sGuidCardFrom, string sGuidCardTo, string Note, string sInternalNote, bool bBiunivocal)
		{
			#region Inizializzazione oggetti
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("setInternalAttachment"))
			{
				esito = wsAction.setInternalAttachment(usernName, sPassword, sGuidCardFrom, sGuidCardTo, Note, sInternalNote, bBiunivocal);
			}
			return esito;
		}

		

	}
}
