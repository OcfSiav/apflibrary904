using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Siav.APFlibrary.Model;
using Siav.APFlibrary.SiavWsAgraf;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Specialized;
using Siav.APFlibrary.SiavWsCard;
using Siav.APFlibrary.SiavWsChart;

namespace Siav.APFlibrary.Manager
{
    public class WcfSiavChartManager
    {
        public ChartServiceContractClient siavWsChart;
        private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var certificate = (X509Certificate2)cert;
            return true;
        }
        public WcfSiavChartManager()
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
            this.siavWsChart = new SiavWsChart.ChartServiceContractClient();
        }

        public Siav.APFlibrary.SiavWsChart.User getUser(WcfSiavLoginManager wcfSiavLoginManager, string sUser)
        {
            Siav.APFlibrary.SiavWsChart.User oUser;
            oUser = null;
            try
            {
                Siav.APFlibrary.SiavWsChart.ResultInfo oResult = Siav.APFlibrary.SiavWsChart.ResultInfo.OK;
                // call the WCF service contract to get an user information (the user with ID=0)
                oResult = siavWsChart.GetUser(wcfSiavLoginManager.oSessionInfo.SessionId, sUser.ToUpper(), true, true,out oUser);

                if (oResult == Siav.APFlibrary.SiavWsChart.ResultInfo.OK)
                {
                    // OK
                }
                else
                    Console.WriteLine("GetUser call error; contact WCF administrator for details");
            }
            catch (FaultException<Siav.APFlibrary.SiavWsChart.ArchiflowServiceExceptionDetail> fex)
            {
                throw new ArgumentException(fex.Detail.Message);
            }
            catch (FaultException<RegistryServiceExceptionDetail> fex)
            {
                throw new ArgumentException(fex.Detail.Message);
            }
            catch (Exception  ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return oUser;
        }

        public Siav.APFlibrary.SiavWsChart.SendObject getUserOffices(WcfSiavLoginManager wcfSiavLoginManager, int iUserId)
        {
            Siav.APFlibrary.SiavWsChart.SendObject sendObject;
            sendObject = null;
            try
            {
                Siav.APFlibrary.SiavWsChart.ResultInfo oResult = Siav.APFlibrary.SiavWsChart.ResultInfo.OK;
                // call the WCF service contract to get an user information (the user with ID=0)

                oResult = siavWsChart.GetUserOfficesGroups(wcfSiavLoginManager.oSessionInfo.SessionId, iUserId, out sendObject);

                if (oResult == Siav.APFlibrary.SiavWsChart.ResultInfo.OK)
                {
                    if (sendObject != null)
                    {
                        if (sendObject.SendEntities != null && sendObject.SendEntities.Count > 0)
                        {
                            Console.WriteLine("\nSendEntities:\n");

                            foreach (Siav.APFlibrary.SiavWsChart.SendEntity oSendEntity in sendObject.SendEntities)
                            {
                                Console.WriteLine(string.Format("SendEntity : Description={0}; EntityType={1}; Id={2}; Level={3}, SendingType={4}, Status={5}",
                                    oSendEntity.Description, oSendEntity.EntityType, oSendEntity.Id, oSendEntity.Level, oSendEntity.SendingType, oSendEntity.Status));
                            }
                        }

                        if (sendObject.SendOptions != null)
                            Console.WriteLine(string.Format("\nSendOptions : IsAutomaticSending={0}; IsDisabledReceivers={1}; IsVisibleOnlyDoc={2}",
                                    sendObject.SendOptions.IsAutomaticSending, sendObject.SendOptions.IsDisabledReceivers, sendObject.SendOptions.IsVisibleOnlyDoc));

                        Console.WriteLine();
                    }

                }
                else
                    Console.WriteLine("GetUser call error; contact WCF administrator for details");
            }
            catch (FaultException<RegistryServiceExceptionDetail> fex)
            {
                throw new ArgumentException(fex.Detail.Message);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return sendObject;
        }
    }

}

