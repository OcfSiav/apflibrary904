using AspNet.Identity.Oracle;
using Siav.APFlibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Manager
{
	public class QueryDataForReport
	{
		public OracleDatabase Database { get; private set; }
		private ReadDataForReport recordAnagrafico;
		public QueryDataForReport()
		{
			recordAnagrafico = new ReadDataForReport(new OracleDatabase());
		}
		public QueryDataForReport(OracleDatabase database)
		{
			Database = database;
			recordAnagrafico = new ReadDataForReport(database);
		}
		public List<Dictionary<string, string>> getDataForReport(string sNameQuery, List<Oracle.ManagedDataAccess.Client.OracleParameter> oParameter)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetDataForReport(sNameQuery, oParameter);
			return x != null ? x : null;
		}

		public List<Dictionary<string, string>> getDataForReport(List<Oracle.ManagedDataAccess.Client.OracleParameter> oParameter)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetDataForReport(oParameter);
			return x != null ? x : null;
		}
		public List<Dictionary<string, string>> getDataForReport(List<Oracle.ManagedDataAccess.Client.OracleParameter> oParameter, string sSql)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetDataForReport(oParameter, sSql);
			return x != null ? x : null;
		}

	}
	class ReportManager
	{
	}
	public class QueryCondition
	{
		public string Name { get; set; }
		public ICollection<string> PossibleValues { get; set; }
	}

	public class Variant
	{
		public IDictionary<QueryCondition, string> AttributeValues { get; set; }
	}
}
