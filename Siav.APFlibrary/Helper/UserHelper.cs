using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Helper
{
    class UserHelper
    {
        public List<String> getUserOffices(Siav.APFlibrary.SiavWsChart.SendObject oSendObject)
        {
            List<String> lResult = new List<string>();
            try
            {
                if (oSendObject != null)
                {
                    if (oSendObject.SendEntities != null && oSendObject.SendEntities.Count > 0)
                    {
                        foreach (Siav.APFlibrary.SiavWsChart.SendEntity oSendEntity in oSendObject.SendEntities)
                        {
                            if (oSendEntity.EntityType == SiavWsChart.EntityType.OfficeEntity)
                                lResult.Add(oSendEntity.Description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return lResult;
        }
    }
}
