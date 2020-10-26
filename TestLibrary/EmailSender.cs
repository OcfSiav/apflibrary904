using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using System.Web;
using System.Resources;

namespace TestLibrary
{
    public class SendMail 
    {
        public string Email = string.Empty;
        public string Password = string.Empty;
        public int Port = 587;
        public string Host = string.Empty;
        public Boolean isSsl = true;
        public SendMail()
        {
            ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
            resourceFileManager.SetResources();
            Email = resourceFileManager._resourceManager.GetString("emailUserDefault");
            Password = resourceFileManager._resourceManager.GetString("emailPwdDefault");
            Port = Convert.ToInt32(resourceFileManager._resourceManager.GetString("emailPortDefault"));
            Host = resourceFileManager._resourceManager.GetString("emailSmtpDefault");
            isSsl = Convert.ToBoolean(Convert.ToInt32(resourceFileManager._resourceManager.GetString("emailIsSslDefault")));
        }

        public Boolean SENDMAIL(String receiverEmail, String senderName, String Subject, String emailBody, string Bcc)
        {
            bool result = false;
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = isSsl;
            client.Host = Host;
            client.Port = Port;
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(Email, Password);
            client.UseDefaultCredentials = false;
            client.Credentials = credentials;
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(this.Email);
            msg.To.Add(new MailAddress(receiverEmail));
            msg.Subject = Subject;
            msg.IsBodyHtml = true;
            msg.Body = emailBody;
            if (Bcc != string.Empty)
            {
                msg.Bcc.Add(new MailAddress(Bcc));
            }

            //string.Format("<html><head></head><body><b>Test HTML Email</b></body>");

            try
            {
                client.Send(msg);
                result = true;
            }
            catch (Exception ex)
            {
                //lblMsg.ForeColor = Color.Red;
                //lblMsg.Text = "Error occured while sending your message." + ex.Message;
            }
            return result;
        }
        public Boolean SENDMAILeATTACHMENTS(Stream FileContent, string fileName, string post, String Subject, string ToAddress)
        {
            string emailBody = "";
            bool result = false;
            SmtpClient client = new SmtpClient();

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = Host;
            client.Port = Convert.ToInt32(Port);
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(Email, Password);
            client.UseDefaultCredentials = false;
            client.EnableSsl = false;
            client.Credentials = credentials;
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(this.Email);
            msg.To.Add(new MailAddress(ToAddress));
            msg.Subject = Subject;
            msg.IsBodyHtml = true;
            Attachment mAttachment = new Attachment(FileContent, fileName);
            msg.Attachments.Add(mAttachment);
            msg.Body = emailBody;
            //string.Format("<html><head></head><body><b>Test HTML Email</b></body>");

            try
            {
                client.Send(msg);
                result = true;
            }

            catch (Exception ex)
            {
                //lblMsg.ForeColor = Color.Red;
                //lblMsg.Text = "Error occured while sending your message." + ex.Message;
            }
            return result;
        }

        public Boolean SendEmail_WithAttach(string[] Files, string ToAddress, string Subject, string Message, string Bcc = "")
        {

            string emailBody = Message;
            bool result = false;
            SmtpClient client = new SmtpClient();

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = Host;
            client.Port = Convert.ToInt32(Port);
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(Email, Password);
            client.UseDefaultCredentials = false;
            client.EnableSsl = isSsl;
            client.Credentials = credentials;
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(this.Email);

            foreach (string myToAddress in ToAddress.Split(';'))
            {
                msg.To.Add(new MailAddress(myToAddress));
            }
            msg.Subject = Subject;
            msg.IsBodyHtml = true;
            if (Bcc != "")
            {
                foreach (string myBcc in Bcc.Split(';'))
                {
                    msg.Bcc.Add(new MailAddress(Bcc));
                }
            }

            //Attachment mAttachment = new Attachment(FileContent, fileName);
            foreach (string myFile in Files)
            {
                if (System.IO.File.Exists(myFile) == true)
                {
                    FileStream fileStream = new FileStream(myFile, FileMode.Open, FileAccess.Read);
                    string FileName = string.Empty;
                    FileName = myFile.Substring(myFile.LastIndexOf("\\"));
                    Attachment mAttachment = new Attachment(fileStream, FileName);
                    msg.Attachments.Add(mAttachment);
                }
            }
            msg.Body = emailBody;
            //string.Format("<html><head></head><body><b>Test HTML Email</b></body>");

            try
            {
                client.Send(msg);
                result = true;
            }

            catch (Exception ex)
            {
                //lblMsg.ForeColor = Color.Red;
                //lblMsg.Text = "Error occured while sending your message." + ex.Message;
            }

            return result;
        }
    }
}
