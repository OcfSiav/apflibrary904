using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using AspNet.Identity.Oracle;

namespace Siav.APFlibrary.Manager
{
    public class WorkFlowManager
    {
        private OracleDatabase _database;

        /// <summary>
        /// Constructor that takes a Oracle Database instance 
        /// </summary>
        /// <param name="database"></param>
        public WorkFlowManager(OracleDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Deletes a login from a user in the UserLogins table
        /// </summary>
        /// <param name="user">User to have login deleted</param>
        /// <param name="login">Login to be deleted from user</param>
        /// <returns></returns>
        /*public int Delete(IdentityUser user, UserLoginInfo login)
        {
            const string commandText = @"DELETE FROM PEC2WEBUSERLOGINS WHERE USERID = :USERID AND LOGINPROVIDER = :LOGINPROVIDER AND PROVIDERKEY = :PROVIDERKEY";
            var parameters = new List<OracleParameter>
            {
                new OracleParameter{ ParameterName = "USERID", Value = user.Id, OracleDbType = OracleDbType.Varchar2 },
                new OracleParameter{ ParameterName = "LOGINPROVIDER", Value = login.LoginProvider, OracleDbType = OracleDbType.Varchar2 },
                new OracleParameter{ ParameterName = "PROVIDERKEY", Value = login.ProviderKey, OracleDbType = OracleDbType.Varchar2 },
            };

            return _database.Execute(commandText, parameters);
        }*/

        /// <summary>
        /// Deletes all Logins from a user in the UserLogins table
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public int Delete(string userId)
        {
            const string commandText = @"DELETE FROM PEC2WEBUSERLOGINS WHERE USERID = :USERID";
            var parameters = new List<OracleParameter>
            {
                new OracleParameter{ ParameterName = "USERID", Value = userId, OracleDbType = OracleDbType.Varchar2 },
            };

            return _database.Execute(commandText, parameters);
        }

        /// <summary>
        /// Inserts a new login in the UserLogins table
        /// </summary>
        /// <param name="user">User to have new login added</param>
        /// <param name="login">Login to be added</param>
        /// <returns></returns>
        /*public int Insert(IdentityUser user, UserLoginInfo login)
        {
            const string commandText = @"INSERT INTO PEC2WEBUSERLOGINS (LOGINPROVIDER, PROVIDERKEY, USERID) VALUES (:LOGINPROVIDER, :PROVIDERKEY, :USERID)";
            var parameters = new List<OracleParameter>
            {
                new OracleParameter{ ParameterName = "USERID", Value = user.Id, OracleDbType = OracleDbType.Varchar2 },
                new OracleParameter{ ParameterName = "LOGINPROVIDER", Value = login.LoginProvider, OracleDbType = OracleDbType.Varchar2 },
                new OracleParameter{ ParameterName = "PROVIDERKEY", Value = login.ProviderKey, OracleDbType = OracleDbType.Varchar2 },
            };

            return _database.Execute(commandText, parameters);
        }*/

        /// <summary>
        /// Return a userId given a user's login
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public string FindUserNameFromProcess(string sProcessId)
        {
            // SELECT i.extern_name_id, n.extern_name, n.extern_id FROM INSTANCES i, extern_name N " + "WHERE ProcessInstanceID='" + strInstID + "' AND i.extern_name_id=n.extern_name_id
            const string commandText = @"SELECT n.extern_name FROM INSTANCES i, extern_name N " + "WHERE ProcessInstanceID= :PROCESSID AND i.extern_name_id=n.extern_name_id";
            var parameters = new List<OracleParameter>
            {
                new OracleParameter{ ParameterName = "PROCESSID", Value = sProcessId, OracleDbType = OracleDbType.Varchar2 },
            };

            return _database.GetStrValue(commandText, parameters);
        }

        /// <summary>
        /// Returns a list of user's logins
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        /*public List<UserLoginInfo> FindByUserId(string userId)
        {
            const string commandText = @"SELECT * FROM PEC2WEBUSERLOGINS WHERE USERID = :USERID";
            var parameters = new List<OracleParameter>
            {
                new OracleParameter{ ParameterName = "USERID", Value = userId, OracleDbType = OracleDbType.Varchar2 },
            };

            var rows = _database.Query(commandText, parameters);

            return rows.Select(row => new UserLoginInfo(row["LOGINPROVIDER"], row["PROVIDERKEY"])).ToList();
        }*/
    }
}
