using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Data;
using Siav.APFlibrary.Action;
using Siav.APFlibrary.Manager;

namespace Siav.APFlibrary
{
    [Guid("EBD66DDB-A5C1-4176-89B6-8B3E14B00B0B")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ProgId("Siav.APFlibrary")]
    [ComVisible(true)]
    public class Flux
    {
        // Risultati esito è sempre il ritorno della funzione, può avere i valori:
        // OK
        // KO : descrizione anomalia
        // i parametri di out sono i valori da restituire qualora la funzione lo richieda
        public object VerifyStep1Cancellazione(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.VerifyStep1Cancellazione(GUIdcard, out sOutput);
            return arrResult;
        }
        public object VerifyStep1Ingiunzione(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.Step1Ingiunzione(GUIdcard, out sOutput);
            return arrResult;
        }
        public object VerifyStep1(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.Step1(GUIdcard, out sOutput);
            return arrResult;
        }
        public object VerifyStep1ProvaValutativa(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.Step1Prova_Valutativa(GUIdcard, out sOutput);
            return arrResult;
        }
        public object getOfficesFromUserLauncherFlux(string idProcess, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.getUserOfficesFromFlux(idProcess, out sOutput);
            return arrResult;
        }
		public Boolean RemoveVisibility(string sGuidCard, string sUsers, string sOffices, string sGroups)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.RemoveVisibility(sGuidCard, sUsers, sOffices, sGroups);
			return bresult;
		}
		
		public Boolean AddVisibility(string sGuidCard, string sUsers, string sOffices, string sGroups)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.AddVisibility(sGuidCard, sUsers, sOffices, sGroups);
			return bresult;
		}
		public Boolean sendMessage(string sGuidCard, string sUsers, string sOffices, string sGroups, string sMessage)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.SendMessage(sGuidCard, sUsers, sOffices, sGroups, sMessage);
			return bresult;
		}
		public Boolean sendNotify(string GUIdcard, string sMessage, string sSubject, string emailTo,string sAuthor, Boolean CardAnnotation, string ArchEmailToUser, string ArchEmailToOffice, string ArchEmailToGroup)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.SendNotify(GUIdcard, sMessage, sSubject, sAuthor, emailTo, CardAnnotation, ArchEmailToUser, ArchEmailToOffice, ArchEmailToGroup);
            return bresult;
        }

        public Boolean SendEmailFromFlux(string sMessage, string sSubject)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.SendEmailFromFlux(sSubject, sMessage);
            return bresult;
        }
		public Boolean SendEmailFromCard(string sMitt,string sDest, string sMessage, string sSubject, string sBcc)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.SendEmailFromCard(sMitt, sDest, sSubject, sMessage, sBcc);
			return bresult;
		}

		public Boolean SetLinkAnag(string GUIdcard,string codFisc)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.SetLinkAnag(GUIdcard,codFisc);
            return bresult;
        }

        public Boolean CheckCardsInError(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardsInError(IdComMaxiva);
            return bresult;
        }
        public Boolean CheckCardsInErrorIngiunzione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardsInErrorIngiunzione(IdComMaxiva);
            return bresult;
        }
        public Boolean CheckCardsInErrorProvaValutativa(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardsInErrorProvaValutativa(IdComMaxiva);
            return bresult;
        }
        public Boolean CheckCardsInErrorCancellazione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardsInErrorCancellazione(IdComMaxiva);
            return bresult;
        }
        
        public Boolean CheckCardSigned(string GUIdcard)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardSigned(GUIdcard);
            return bresult;
        }

        public Boolean CheckCardSignedCancellazione(string GUIdcard)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckCardSignedCancellazione(GUIdcard);
            return bresult;
        }
        public Boolean CheckProtocolCard(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckProtocolCard(IdComMaxiva);
            return bresult;
        }
        public Boolean CheckProtocolCardIngiunzione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckProtocolCardIngiunzione(IdComMaxiva);
            return bresult;
        }

        public Boolean CheckProtocolCardCancellazione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            Boolean bresult = genComMassive.CheckProtocolCardCancellazione(IdComMaxiva);
            return bresult;
        }
        public bool ConvertMainDocument2Pdf(string GUIdcard)
        {
            GenComMassive genComMassive = new GenComMassive();
            bool sresult = genComMassive.ConvertMainDocument2Pdf(GUIdcard);
            return sresult;
        }
        public string GetLowerCardsReadyToProtocol(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetLowerCardsReadyToProtocol(IdComMaxiva);
            return sresult;
        }

        public string GetBiggerCardsReadyToProtocolCancellazione(string IdComMaxiva, bool bWithState)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetBiggerCardsReadyToProtocolCancellazione(IdComMaxiva, bWithState);
            return sresult;
        }
        public string GetBiggerCardsReadyToProtocolProvaValutativa(string IdComMaxiva, bool bWithState)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetBiggerCardsReadyToProtocolProvaValutativa(IdComMaxiva, bWithState);
            return sresult;
        }
        public string GetBiggerCardsReadyToProtocol(string IdComMaxiva, bool bWithState)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetBiggerCardsReadyToProtocol(IdComMaxiva, bWithState);
            return sresult;
        }
        public string GetBiggerCardsReadyToProtocolIngiunzione(string IdComMaxiva, bool bWithState)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetBiggerCardsReadyToProtocolIngiunzione(IdComMaxiva, bWithState);
            return sresult;
        }
        public string GetLowerCardsReadyToProtocolProvaValutativa(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetLowerCardsReadyToProtocolProvaValutativa(IdComMaxiva);
            return sresult;
        }
        public string GetLowerCardsReadyToProtocolCancellazione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.GetLowerCardsReadyToProtocolCancellazione(IdComMaxiva);
            return sresult;
        }
        
        public String InsertInProtocolArchiveProvaValutativa(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.InsertInProtocolArchiveProvaValutativa(IdComMaxiva);
            return sresult;
        }

        public String InsertInProtocolCancellazione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.InsertInProtocolArchiveCancellazione(IdComMaxiva);
            return sresult;
        }
        public String InsertInProtocolArchive(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.InsertInProtocolArchive(IdComMaxiva);
            return sresult;
        }
        public String InsertInProtocolArchiveIngiunzione(string IdComMaxiva)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.InsertInProtocolArchiveIngiunzione(IdComMaxiva);
            return sresult;
        }
        public String CreateReportMassiveIsc(string IdComMaxiva, string sGuidCard)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.CreateReportMassiveIsc(IdComMaxiva, sGuidCard);
            return sresult;
        }
        public String CreateReportMassiveProVal(string IdComMaxiva, string sGuidCard)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.CreateReportMassiveProVal(IdComMaxiva, sGuidCard);
            return sresult;
        }
        public String CreateReportMassiveIng(string IdComMaxiva, string sGuidCard)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.CreateReportMassiveIng(IdComMaxiva, sGuidCard);
            return sresult;
        }
        public String CreateReportMassiveCanc(string IdComMaxiva, string sGuidCard)
        {
            GenComMassive genComMassive = new GenComMassive();
            String sresult = genComMassive.CreateReportMassiveCanc(IdComMaxiva, sGuidCard);
            return sresult;
        }

        public object CasellarioCreator(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.CasellarioCreator(GUIdcard, out sOutput);
            return arrResult;
        }
		public Boolean IsExistMainDoc(string sGuidCard)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean sresult = genComMassive.IsExistMainDoc(sGuidCard);
			return sresult;
		}
		public Boolean AddMainDocToCardAttachInt(string sGuidCard)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean sresult = genComMassive.AddMainDocToCardAttachInt(sGuidCard);
			return sresult;
		}
		public Boolean SetCardDefaultVisibility(string sQuery)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean sresult = genComMassive.SetCardDefaultVisibility(sQuery);
			return sresult;
		}
		public int ProtocolBillFromPEC(string sGuidCard, ref string sOutput)
		{
			GenComMassive genComMassive = new GenComMassive();
			int iresult = genComMassive.ProtocolBillFromPEC(sGuidCard, out sOutput);
			return iresult;
		}

		public int GetNumberNotes(string sGuidCard)
		{
			GenComMassive genComMassive = new GenComMassive();
			int sresult = genComMassive.GetNumberNotes(sGuidCard);
			return sresult;
		}
		
		public string GetNumberSignsOnAttachment(string sGuidCard)
		{
			GenComMassive genComMassive = new GenComMassive();
			string sresult = genComMassive.GetNumberSignsOnAttachment(sGuidCard);
			return sresult;
		}
		public int GetNumberSigns(string sGuidCard)
		{
			GenComMassive genComMassive = new GenComMassive();
			int sresult = genComMassive.GetNumberSigns(sGuidCard);
			return sresult;
		}
		
		public object PredCasellarioCreator(string GUIdcard, ref string sOutput)
        {
            GenComMassive genComMassive = new GenComMassive();
            object arrResult = (object)genComMassive.PredCasellarioCreator(GUIdcard, out sOutput);
            return arrResult;
        }

		public object PredCreateReport(string GUIdcard)
		{
			GenComMassive genComMassive = new GenComMassive();
			object arrResult = (object)genComMassive.PredCreateReport(GUIdcard);
			return arrResult;
		}																				 
		public Boolean CreateCardReport(string sIndex, string sIndexValue)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.CreateCardReport(sIndex, sIndexValue);
			return bresult;
		}
		public Boolean CreateReportFromSQL(string sNameSqlResource, string sWhereValue, string sEmailDestinationTo, string sEmailDestinationBcc)
		{
			GenComMassive genComMassive = new GenComMassive();
			Boolean bresult = genComMassive.CreateReportFromSQL(sNameSqlResource, sWhereValue, sEmailDestinationTo, sEmailDestinationBcc);
			return bresult;
		}

		//public string VerifyAnag(string GUIdcard, out string sOutput)
		//{
		//    GenComMassive genComMassive = new GenComMassive();
		//    return genComMassive.VerAnag(GUIdcard, out sOutput);

		//}
		// creazione delle schede

		// creazione documento principale per ogni scheda
		// notifiche
		// gestione flussi

	}
}
