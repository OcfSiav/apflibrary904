
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateReportExcel
{
	class Program
	{
		static void Main(string[] args)
		{
			//bool bOK = true;
			//string LogId = LOLIB.CodeGen("");
			//LOLIB Logger = new LOLIB();
			//try
			//{
			//	string path = "";
			//	string sPathResource= "";
			//	if (args.Count()>0) 
			//		path = args[0];
			//	Console.WriteLine("trying path: " + path);
			//	if (File.Exists(path))
			//	{
			//		sPathResource = path;
			//	}
			//	else
			//	{
			//		Console.WriteLine("path not found");
			//		sPathResource = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\GlobalResources\GlobalResource.resx";
			//	}
			//	ResourceFileManager resourceFileManager = null;
			//	resourceFileManager = ResourceFileManager.Instance;
			//	resourceFileManager.SetResources(sPathResource);
			//	Logger.WriteOnLog(LogId, "Avvio applicazione", 3);
			//	Logger.WriteOnLog(LogId, "Path file di configurazione individuato: " + sPathResource, 3);

			//	string sDateFrom = "";
			//	string sDateTo = "";
			//	string sReportType = resourceFileManager.getConfigData("ReportPeriod").ToUpper();
			//	System.Globalization.CultureInfo MyCultureInfo = new System.Globalization.CultureInfo("it-IT");
			//	Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");
			//	if (sReportType == "C")
			//	{
			//		DateTime dtFrom = DateTime.Parse(resourceFileManager.getConfigData("CustomPeriodFrom"), MyCultureInfo);
			//		DateTime dtTo = DateTime.Parse(resourceFileManager.getConfigData("CustomPeriodTill"), MyCultureInfo);

			//		sDateFrom = dtFrom.ToString();
			//		sDateTo = dtTo.ToString();
			//	}
			//	else if(sReportType == "M")
			//	{
			//		// configurazione ultimo mese
			//		var today = DateTime.Today;
			//		var month = new DateTime(today.Year, today.Month, 1);

			//		sDateFrom = month.AddMonths(-1).ToString(); 
			//		sDateTo = month.AddDays(-1).ToString();
			//	}
			//	else if (sReportType == "D")
			//	{
			//		// configurazione ultimo giorno
			//		var today = DateTime.Today;
			//		sDateFrom = today.AddDays(-1).ToString();
			//		sDateTo = today.AddDays(-1).ToString();
			//	}
			//	else if (sReportType == "Y")
			//	{
			//		//configurazione ultimo anno
			//		var today = DateTime.Today;
			//		var firstDay = new DateTime(today.Year, 1, 1);
			//		var lastDay = new DateTime(today.Year, 12, 31);
			//		// configurazione ultimo giorno
			//		sDateFrom = firstDay.AddYears(-1).ToString();
			//		sDateTo = lastDay.AddYears(-1).ToString();
			//	}
			//	var sPathRelease = resourceFileManager.getConfigData("PathReleaseFileXls");
			//	var sNamePageXls = resourceFileManager.getConfigData("NamePageXls");
			//	Logger.WriteOnLog(LogId, "Tipo di ricerca: " + sReportType, 3);
			//	Logger.WriteOnLog(LogId, "A partire da: " + sDateFrom, 3);
			//	Logger.WriteOnLog(LogId, "Fino a: " + sDateTo, 3);
			//	QueryDataForReport oQueryDataForReport = new QueryDataForReport();
			//	ExcelManager oExcelManager = new ExcelManager();
			//	TimeSpan ts = new TimeSpan(23, 59, 59);
			//	DateTime dtDateFrom = DateTime.Parse(sDateFrom, MyCultureInfo);
			//	DateTime dtDateTo = DateTime.Parse(sDateTo, MyCultureInfo);
			//	dtDateTo = dtDateTo.Date + ts;
			//	Logger.WriteOnLog(LogId, "fino a: " + dtDateTo.ToString(), 3);

			//	var parameters = new List<OracleParameter>
			//	{
			//		new OracleParameter{ ParameterName = "DATA1", Value = dtDateFrom, OracleDbType = OracleDbType.Date},
			//		new OracleParameter{ ParameterName = "DATA2", Value = dtDateTo, OracleDbType = OracleDbType.Date},
			//	};	
			//	var result = oQueryDataForReport.getDataForReport(parameters);
			//	if (result != null)
			//	{
			//		Logger.WriteOnLog(LogId, "Individuati:" + result.Count + " record.", 3);
			//		if (result.Count>0)
			//		{
			//			sNamePageXls = dtDateFrom.ToString().Substring(1,10) + "-" + dtDateTo.ToString().Substring(1, 10);
			//			var sPathFile = oExcelManager.CreateReportMassive(@sPathRelease, sNamePageXls, "REPORT_" + sNamePageXls.Replace('/','.') + "_" + LogId, result);
						Siav.APFlibrary.Flux oApfLib = new Siav.APFlibrary.Flux();
						oApfLib.CreateCardReport("DATA",DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy"));
			//			Logger.WriteOnLog(LogId, "Report creato nel path:" + sPathFile, 3);
			//		}
			//		else
			//		{
			//			Logger.WriteOnLog(LogId, "Report NON creato perchè non sono stati individuati record da esportare.", 3);
			//		}
			//	}
			//}
			//catch (Exception e)
			//{
				
			//	bOK= false;
			//	string strMessage = e.Source + " -> " + e.StackTrace + " -> " + e.Message;
			//	Logger.WriteOnLog(LogId, strMessage, 3);
			//}
			//finally
			//{
			//	if (bOK)
			//	{
			//		Logger.RenameFileLog(LogId, "OK_" + LogId);
			//	}
			//	else
			//	{
			//		Logger.RenameFileLog(LogId, "KO_" + LogId);
			//	}
			//}
		}
	}
}
