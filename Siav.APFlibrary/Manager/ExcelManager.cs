using NPOI.HSSF.Model;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Manager
{
	class ExcelManager
	{
		public string CreateReportMassive(string path, string sNameSheet, string sNameFile, List<Dictionary<string, string>> lData)
		{
			NameValueCollection oResult = new NameValueCollection();
			string pathfilename = "";
			try
			{
				pathfilename = path + @"\" + sNameFile + ".xls";

				HSSFWorkbook wb;
				HSSFSheet sh;
				wb = HSSFWorkbook.Create(InternalWorkbook.CreateWorkbook());

				// create sheet
				sh = (HSSFSheet)wb.CreateSheet(sNameSheet.Replace("/", "_"));
				//string[] arrData = null;
				int iRowExc = 0;
				var headerCellStyle = wb.CreateCellStyle();
				var detailSubtotalFont = wb.CreateFont();
				detailSubtotalFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;
				//headerCellStyle.FillForegroundColor= NPOI.SS.Util.HSSFColor.AQUA.index;
				headerCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
				headerCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.SetFont(detailSubtotalFont);
				var detailSubtotalFontLight = wb.CreateFont();
				var DataCellStyle = wb.CreateCellStyle();
				detailSubtotalFontLight.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Normal;
				//                      DataCellStyle.FillForegroundColor = NPOI.SS.Util.HSSFColor.AQUA.index;
				DataCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
				DataCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

				DataCellStyle.SetFont(detailSubtotalFontLight);
				List<string> listKeys = new List<string>(lData[0].Keys);

				for (int iRow = 0; iRow < lData.Count; iRow++)
				{
					var r = sh.CreateRow(iRowExc);
					//string sData = lData[iRow];
					//arrData = sData.Split('|');

					if (iRow == 0)
					{
						int iHeader = 0;
						// Loop through list.
						foreach (string k in listKeys)
						{
							var newCell = r.CreateCell(iHeader);
							newCell.SetCellValue(k);
							newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
							newCell.CellStyle = headerCellStyle;
							iHeader++;
						}
						iRowExc++;
						r = sh.CreateRow(iRowExc);
					}
					for (int iColumn = 0; iColumn < listKeys.Count; iColumn++)
					{
						List<string> listValue = new List<string>(lData[iRowExc - 1].Values);
						var newCell = r.CreateCell(iColumn);
						newCell.SetCellValue(listValue[iColumn]);
						newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
						newCell.CellStyle = DataCellStyle;
					}
					iRowExc++;
				}
				using (var fs = new FileStream(pathfilename, FileMode.Create, FileAccess.Write))
				{
					for (int iColumn = 0; iColumn < listKeys.Count; iColumn++)
					{
						sh.AutoSizeColumn(iColumn);
					}
					wb.Write(fs);
					fs.Close();
				}
			}
			catch (Exception ex)
			{ throw new ArgumentException(ex.Message); }
			finally { }
			return pathfilename;
		}
		public string CreateReportMassive(string path, string sNameSheet, string sNameFile, List<Dictionary<string, string>> lData, string sReport)
		{
			NameValueCollection oResult = new NameValueCollection();
			string pathfilename = "";
			try
			{
				pathfilename = path + @"\" + sNameFile + ".xls";

				HSSFWorkbook wb;
				HSSFSheet sh;
				wb = HSSFWorkbook.Create(InternalWorkbook.CreateWorkbook());

				// create sheet
				sh = (HSSFSheet)wb.CreateSheet(sNameSheet.Replace("/", "_"));
				//string[] arrData = null;
				int iRowExc = 0;
				var headerCellStyle = wb.CreateCellStyle();
				var detailSubtotalFont = wb.CreateFont();
				detailSubtotalFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;
				//headerCellStyle.FillForegroundColor= NPOI.SS.Util.HSSFColor.AQUA.index;
				headerCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
				headerCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Medium;
				headerCellStyle.SetFont(detailSubtotalFont);
				var detailSubtotalFontLight = wb.CreateFont();
				var DataCellStyle = wb.CreateCellStyle();
				detailSubtotalFontLight.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Normal;
				//                      DataCellStyle.FillForegroundColor = NPOI.SS.Util.HSSFColor.AQUA.index;
				DataCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
				DataCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
				DataCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

				DataCellStyle.SetFont(detailSubtotalFontLight);
				List<string> listKeys = new List<string>(lData[0].Keys);

				for (int iRow = 0; iRow < lData.Count; iRow++)
				{
					var r = sh.CreateRow(iRowExc);
					//string sData = lData[iRow];
					//arrData = sData.Split('|');

					if (iRow == 0)
					{
						int iHeader = 0;
						// Loop through list.
						foreach (string k in listKeys)
						{
							var newCell = r.CreateCell(iHeader);
							newCell.SetCellValue(k);
							newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
							newCell.CellStyle = headerCellStyle;
							iHeader++;
						}
						iRowExc++;
						r = sh.CreateRow(iRowExc);
					}
					for (int iColumn = 0; iColumn < listKeys.Count; iColumn++)
					{
						List<string> listValue = new List<string>(lData[iRowExc - 1].Values);
						var newCell = r.CreateCell(iColumn);
						newCell.SetCellValue(PersonalizeDataForSpecificReport(listValue[iColumn], iColumn, sReport));
						newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
						newCell.CellStyle = DataCellStyle;
					}
					iRowExc++;
				}
				using (var fs = new FileStream(pathfilename, FileMode.OpenOrCreate, FileAccess.Write))
				{
					for (int iColumn = 0; iColumn < listKeys.Count; iColumn++)
					{
						sh.AutoSizeColumn(iColumn);
					}
					wb.Write(fs);
					fs.Close();
				}
			}
			catch (Exception ex)
			{ throw new ArgumentException(ex.Message); }
			finally { }
			return pathfilename;
		}
		static string TimeFromDays(string totalDay)
		{
			DateTime startDate = new DateTime(2010, 1, 1);
			DateTime endDate = startDate.AddDays(int.Parse(totalDay));
			var totalDays = (endDate - startDate).TotalDays;
			var totalYears = Math.Truncate(totalDays / 365);
			var totalMonths = Math.Truncate((totalDays % 365) / 30);
			var remainingDays = Math.Truncate((totalDays % 365) % 30);
			return string.Format("{0} anni, {1} mesi {2} giorni", totalYears, totalMonths, remainingDays);
		}
		static string PersonalizeDataForSpecificReport(string sValue, int iCol, string sReport)
		{
			string sRet = sValue;
			if (sReport.ToUpper() == "REPORT3")
			{
				if (iCol == 5)
				{
					sRet = TimeFromDays(sValue);
				}
			}
			return sRet;
		}
	}
}
