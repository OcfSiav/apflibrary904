using System.Linq;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Text;
using AspNet.Identity.Oracle;
using Siav.APFlibrary.Manager;

namespace Siav.APFlibrary.Model
{

	public class ReadDataForReport
	{
		private OracleDatabase _database;
		private ResourceFileManager resourceFileManager;
		/// <summary>
		/// Constructor that takes a Oracle Database instance 
		/// </summary>
		/// <param name="database"></param>
		public ReadDataForReport(OracleDatabase database)
		{
			_database = database;
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
		}
		public List<Dictionary<string, string>> GetDataForReport(string sNameQuery, List<OracleParameter> parameters)
		{
			List<System.Dynamic.DynamicObject> oResult = new List<System.Dynamic.DynamicObject>();
			string sIdAgraf = string.Empty;
			try
			{
				string commandText = "";
				commandText = @resourceFileManager.getConfigData(sNameQuery);
				/*var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "NOMEBATCH", Value = sNomeBatch, OracleDbType = OracleDbType.Varchar2 },
				};*/
				var rows = _database.Query(commandText, parameters);
				//aF_PROG, absolute_path, source_type
				return rows;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}

		public List<Dictionary<string, string>> GetDataForReport(List<OracleParameter> parameters)
		{
			List<System.Dynamic.DynamicObject> oResult = new List<System.Dynamic.DynamicObject>();
			string sIdAgraf = string.Empty;
			try
			{
				string commandText = "";
				commandText = @resourceFileManager.getConfigData("GetQuery");
				/*var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "NOMEBATCH", Value = sNomeBatch, OracleDbType = OracleDbType.Varchar2 },
				};*/
				var rows = _database.Query(commandText, parameters);
				//aF_PROG, absolute_path, source_type
				return rows;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
		public List<Dictionary<string, string>> GetDataForReport(List<OracleParameter> parameters, string sql)
		{
			List<System.Dynamic.DynamicObject> oResult = new List<System.Dynamic.DynamicObject>();
			string sIdAgraf = string.Empty;
			try
			{
				string commandText = "";
				commandText = sql;// @resourceFileManager.getConfigData("GetQuery");
								  /*var parameters = new List<OracleParameter>
								  {
									  new OracleParameter{ ParameterName = "NOMEBATCH", Value = sNomeBatch, OracleDbType = OracleDbType.Varchar2 },
								  };								   */
				var rows = _database.Query(commandText, parameters);
				//aF_PROG, absolute_path, source_type
				return rows;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
		static string DotNetToOracle(string text)
		{
			Guid guid = new Guid(text);
			return BitConverter.ToString(guid.ToByteArray()).Replace("-", "");
		}
		static byte[] ParseHex(string text)
		{
			// Not the most efficient code in the world, but
			// it works...
			byte[] ret = new byte[text.Length / 2];
			for (int i = 0; i < ret.Length; i++)
			{
				ret[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
			}
			return ret;
		}
		static string OracleToDotNet(string text)
		{
			byte[] bytes = ParseHex(text);
			Guid guid = new Guid(bytes);
			return guid.ToString("N").ToUpperInvariant();
		}
	}
}
