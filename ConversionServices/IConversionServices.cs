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
	// NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
	[ServiceContract]
	public interface IConversionServices
	{
		[OperationContract]
		Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut);

	}
	
}
