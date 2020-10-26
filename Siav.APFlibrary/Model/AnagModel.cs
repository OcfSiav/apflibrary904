
using System.Linq;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System;
using Siav.APFlibrary.Manager;
using WcfSiav.APFlibrary.Model;
using AspNet.Identity.Oracle;
using Siav.APFlibrary.Entity;
using NLog;

namespace Siav.APFlibrary.Model
{
	 
	public class AnagModel
    {
		Logger nLog;
		private InputAgrafBiz oAgraf { get; set; }
		private OracleDatabase _database;
		private ResourceFileManager resourceFileManager;
		/// <summary>
		/// Constructor that takes a Oracle Database instance 
		/// </summary>
		/// <param name="database"></param>
		public AnagModel(OracleDatabase database, Logger log)
        {
			nLog = log;

            _database = database;
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
		}
		/*
public List<InputAgrafBiz> Add(string IdCard)
{
	string commandText = @resourceFileManager.getConfigData("GetProtocollatore");
	var parameters = new List<OracleParameter>
	{
		new OracleParameter{ ParameterName = "idScheda", Value = IdCard, OracleDbType = OracleDbType.Varchar2 },
	};
	var rows = _database.Query(commandText, parameters);
	List<InputAgrafBiz> list = (from row in rows.AsEnumerable()
								select new InputAgrafBiz
								{
									UserId = row["USERID"]
								}).ToList();

	//List<string> 
	//var list = (from row in rows.AsEnumerable() select userid = row["userid"] ).ToList();
	return list;
}
*/
		/*
		public List<InputAgrafBiz> Modify(string IdCard)
		{
			string commandText = @resourceFileManager.getConfigData("GetProtocollatore");
			var parameters = new List<OracleParameter>
			{
				new OracleParameter{ ParameterName = "idScheda", Value = IdCard, OracleDbType = OracleDbType.Varchar2 },
			};
			var rows = _database.Query(commandText, parameters);
			List<InputAgrafBiz> list = (from row in rows.AsEnumerable()
										select new InputAgrafBiz
										{
											UserId = row["USERID"]
										}).ToList();

			//List<string> 
			//var list = (from row in rows.AsEnumerable() select userid = row["userid"] ).ToList();
			return list;
		}
		*/
		/*
		public List<InputAgrafBiz> Disable(string IdCard)
		{
			string commandText = @resourceFileManager.getConfigData("GetProtocollatore");
			var parameters = new List<OracleParameter>
			{
				new OracleParameter{ ParameterName = "idScheda", Value = IdCard, OracleDbType = OracleDbType.Varchar2 },
			};
			var rows = _database.Query(commandText, parameters);
			List<InputAgrafBiz> list = (from row in rows.AsEnumerable()
										select new InputAgrafBiz
										{
											UserId = row["USERID"]
										}).ToList();

			//List<string> 
			//var list = (from row in rows.AsEnumerable() select userid = row["userid"] ).ToList();
			return list;
		}
		  */
		public int SetEnableDisableAgrafEntity(string sId, bool isActive)
		{
			string commandText = @resourceFileManager.getConfigData("SetIdAgrafEnableDisable");
			int iValue = 0;
			if (isActive)
			{
				iValue = 1;
			}
			else
			{
				iValue = 2; 
			}
			var parameters = new List<OracleParameter>
			{
				new OracleParameter{ ParameterName = "STATUS", Value = iValue, OracleDbType = OracleDbType.Int16 },
				new OracleParameter{ ParameterName = "ID", Value = DotNetToOracle(sId), OracleDbType = OracleDbType.Varchar2 }
			};

			return _database.Execute(commandText, parameters);
		}
		public List<AgrafIndexbook> GetIdIndexBook(string sRubricaName)
		{
			List<AgrafIndexbook> list = new List<AgrafIndexbook>();
			string sIdAgraf = string.Empty;
			try
			{
				string commandText = "";
				commandText = @resourceFileManager.getConfigData("GetIdAgrafIndexbook");
				var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "rubrica", Value = sRubricaName, OracleDbType = OracleDbType.Varchar2 },
				};
				var rows = _database.Query(commandText, parameters);

				list = (from row in rows.AsEnumerable()
						select new AgrafIndexbook
						{
							Id= OracleToDotNet(row["INDEXBOOK_ID"]),
							Nome= row["DESCRIPTION"]
						}).ToList();
				return list;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
		public List<IdentityAgraf> GetLastVersion(string sIdAgraf)
		{
			List<IdentityAgraf> list = new List<IdentityAgraf>();
			try
			{
				string commandText = "";
				commandText = @resourceFileManager.getConfigData("GetLastVersion");

				var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "IDAGRAF", Value =DotNetToOracle(sIdAgraf), OracleDbType = OracleDbType.Varchar2 },
				};
				var rows = _database.Query(commandText, parameters);

				list = (from row in rows.AsEnumerable()
						select new IdentityAgraf
						{
							Version = row["VER"]
						}).ToList();
				return list;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
	
		public List<AgrafTag> GetTagIFromIndexbool(string sId)
		{
			List<AgrafTag> list = new List<AgrafTag>();
			string sIdAgraf = string.Empty;
			try
			{
				string  commandText = @resourceFileManager.getConfigData("GetIdTagFromIndexBook");
				
				var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "idIndexBook", Value = sId, OracleDbType = OracleDbType.Varchar2 }
				};
				var rows = _database.Query(commandText, parameters);

				list = (from row in rows.AsEnumerable()
						select new AgrafTag
						{
							id = OracleToDotNet(row["INDEXBOOKTAG_ID"]),
							name = row["NAME"]
						}).ToList();
				return list;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message + " - SOURCE: " + ex.Source + " - STACKTRACE: " + ex.StackTrace);
			}
		}

		public List<IdentityAgraf> GetIdAgraf(string sId, string sRubricaName)
		{
			List<IdentityAgraf> list = new List<IdentityAgraf>();
			string sIdAgraf = string.Empty;
			try
			{
				string commandText = "";
				if (sRubricaName.ToUpper() == @resourceFileManager.getConfigData("ESERCENTI").ToUpper())
				{
					commandText = @resourceFileManager.getConfigData("GetIdAgrafId");
				}
				else if(sRubricaName.ToUpper() == @resourceFileManager.getConfigData("ESERCIZI").ToUpper())
				{
					commandText = @resourceFileManager.getConfigData("GetIdAgrafId2");
				}
				else if (sRubricaName.ToUpper() == @resourceFileManager.getConfigData("GESTORI").ToUpper())
				{
					commandText = @resourceFileManager.getConfigData("GetIdAgrafId3");
				}
				else
				{
					commandText = @resourceFileManager.getConfigData("GetIdAgrafId");
				}
				var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "rubrica", Value = sRubricaName, OracleDbType = OracleDbType.Varchar2 },
					new OracleParameter{ ParameterName = "id", Value = sId, OracleDbType = OracleDbType.Varchar2 }
				};
				var rows = _database.Query(commandText, parameters);
				
				list = (from row in rows.AsEnumerable()
						select new IdentityAgraf
						{
							id = OracleToDotNet(row["GENERICENTITY_ID"]),
							idRubrica = OracleToDotNet(row["INDEXBOOK_ID"]),
							Stato = row["STATUS"],
							Version = row["GENERICENTITY_VERSION"]
						}).ToList();
				return list;
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
