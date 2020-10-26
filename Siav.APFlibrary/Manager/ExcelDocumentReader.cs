using LinqToExcel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using LinqToExcel.Attributes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Manager { 

    public class ExcelDocumentReader: MarshalByRefObject
    {
        private ExcelQueryFactory Excel { get; set; }
        public string ExcelFileName { get; set; }
        public string SheetName { get; set; }
        public string SheetNameSelected = string.Empty;
        public string idTransaction = string.Empty;
        public string sDefaultWorkPath = string.Empty;
        public string sTransactionPath = string.Empty;
        private IEnumerable<string> columnNames;
        private IEnumerable<string> workSheet;
        public System.Linq.IQueryable<LinqToExcel.Row> getData  { get; set; }

       
    
        [Description("Initializes the ExcelDocumentReader with the FileName.")]
        public ExcelDocumentReader(string ExcelFileName, string sWorkingfolder, string sheet="")
        {
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;

                resourceFileManager.SetResources();
                sDefaultWorkPath = sWorkingfolder;
                sTransactionPath = sDefaultWorkPath + @"\";

                if ((ExcelFileName != null) && (ExcelFileName != ""))
                {
                    this.ExcelFileName = System.IO.Path.Combine(@sDefaultWorkPath, @ExcelFileName);
                    this.Excel = new ExcelQueryFactory(this.ExcelFileName);
                    this.workSheet = this.Excel.GetWorksheetNames();
                    if (sheet=="")
                        sheet = workSheet.First().ToString();
                    
                    columnNames = this.Excel.GetColumnNames(sheet);
                    getData = from a in this.Excel.Worksheet(sheet) select a;
                }
                else
                    throw new Exception("FileName is Null or Empty");
            }
            catch (Exception ex)
            { throw ex; }
            finally
            { }
        }

        [Description("Gets all the Worksheet names.")]
        public IEnumerable<string> GetWorkSheets()
        {
            try
            {
                return this.workSheet;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
        }

        [Description("Gets all the Worksheet names.")]
        public void SetWorkSheets()
        {
            try
            {
                workSheet = this.Excel.GetWorksheetNames();
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
        }

        [Description("Gets all the column names in the SheetName provided.")]
        public IEnumerable<string> GetColumnNames()
        {
            try
            {
                return columnNames;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
        }

   
    }
}