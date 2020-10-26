using Siav.APFlibrary.Helper;
using Siav.APFlibrary.Manager;
using Siav.APFlibrary.Model;
using Siav.APFlibrary.SiavWsCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Action
{
    class CardAction
    {
        public void MainDocMaterializeFromCard(string path, WcfSiavLoginManager siavLogin,FluxHelper fluxHelper, WcfSiavCardManager siavCardManager, string sGuidCard, out string sPathFileCreated)
        {
            try
            {
                CardBundle oCardModelBundle;
                MainDoc oMainDocModel;
                // Recupera la scheda ove è stato avviato il processo
                siavCardManager.GetCard(sGuidCard, siavLogin, out oCardModelBundle);
                // Recupera il documento principale della scheda 
                siavCardManager.GetMainDoc(oCardModelBundle, out oMainDocModel);
                // Materializza il file sul filesystem
                string sModelDocX = path + @"\" + oMainDocModel.Filename + "_ModelSource." + oMainDocModel.Extension;
                fluxHelper.FileMaterialize(sModelDocX, oMainDocModel.oByte);
                sPathFileCreated = sModelDocX;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        /*
        public void AttachmentMaterializeFromCard(string path, WcfSiavLoginManager siavLogin, FluxHelper fluxHelper, WcfSiavCardManager siavCardManager, string sGuidCard, out string sPathFileCreated)
        {
            try
            {
                CardBundle oCardModelBundle;
                MainDoc oMainDocModel;
                // Recupera la scheda ove è stato avviato il processo
                siavCardManager.GetCard(sGuidCard, siavLogin, out oCardModelBundle);
                // Recupera il documento principale della scheda 
                siavCardManager.GetMainDoc(oCardModelBundle, out oMainDocModel);
                // Materializza il file sul filesystem
                string sModelDocX = path + @"\" + oMainDocModel.Filename + "_ModelSource." + oMainDocModel.Extension;
                fluxHelper.FileMaterialize(sModelDocX, oMainDocModel.oByte);
                sPathFileCreated = sModelDocX;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        */
    }
}
