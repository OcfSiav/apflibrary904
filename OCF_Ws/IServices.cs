using OCF_Ws.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace OCF_Ws
{
	// NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
	[ServiceContract]
	public interface IServices
	{
		[OperationContract]
		Outcome setMainDocument(string sUserName, string sPassword, MainDocument oMainDoc, string sGuidCard);
		[OperationContract]
		Outcome setXmlSignature(string sConnection, string sGuidCard, string ProfileName);
		[OperationContract]
		Outcome setAttachment(string sUserName, string sPassword, Attachment Attachment, string sGuidCard);
		[OperationContract]
		Outcome setInternalAttachment(string sUserName, string sPassword, string sGuidCardFrom, string sGuidCardTo, string Note, string sInternalNote, bool bBiunivocal);
		[OperationContract]
		Outcome setCard(string usernName, string password, Model.Card card, out string sGuidCard);
		[OperationContract]
		Outcome getCard(string usernName, string password, string sGuidCard, bool bGetMainDoc, bool bGetAttachment, out Model.Card card);
		[OperationContract]
		Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut);
		[OperationContract]
		Outcome createPdfAFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocument mainDocOut);
		[OperationContract]
		Outcome createPdfACRC32bFromTemplate(string[][] aReplaceNameValue, string sGuidCard, out MainDocumentCRC32b mainDocOut);
		[OperationContract]
		Outcome getCRC32bFromFilePdf(MainDocument filePdf, out string CRC32b);
		[OperationContract]
		Outcome getIndexConfigFromTypeDocument(string username, string password, string sTipologiaDocumentale, out List<String> lstIndexFieldKey);
		[OperationContract]
		Outcome getSignCheck(MainDocument mainDoc, string sCRC32, List<String> lNIN, bool bGetDocWithoutSign, out MainDocumentChecked mainDocOut);

	}

	// Per aggiungere tipi compositi alle operazioni del servizio utilizzare un contratto di dati come descritto nell'esempio seguente.
	// È possibile aggiungere file XSD nel progetto. Dopo la compilazione del progetto è possibile utilizzare direttamente i tipi di dati definiti qui con lo spazio dei nomi "OCF_Ws.ContractType".

}
