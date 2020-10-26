using ConversionServices.Action;
using ConversionServices.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ConversionServices
{
	// NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di classe "Service1" nel codice, nel file svc e nel file di configurazione contemporaneamente.
	// NOTA: per avviare il client di prova WCF per testare il servizio, selezionare Service1.svc o Service1.svc.cs in Esplora soluzioni e avviare il debug.
	public class ConversionServices : IConversionServices
	{
		public Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut)
		{
			#region Inizializzazione oggetti

			mainDocOut = null;
			Outcome esito = new Outcome();
			#endregion
			using (var wsAction = new WsAction("CONVERSION"))
			{
				esito = wsAction.base64ToPdfA(mainDoc,out mainDocOut);
			}
			return esito;
		}
	}
}
