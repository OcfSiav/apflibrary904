using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using System.Web;
using NLog;
using System.Resources;
using System.ComponentModel;
using System.Net;

namespace Siav.APFlibrary.Manager
{
    public class SendMail 
    {
        public string Email = string.Empty;
        public string Password = string.Empty;
        public int Port = 587;
        public string Host = string.Empty;
        public Boolean isSsl = true;
		private static Logger logger = LogManager.GetCurrentClassLogger();
		public SendMail()
        {
            Rijndael oRijndael = new Rijndael();
            ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
            resourceFileManager.SetResources();
            Email = resourceFileManager.getConfigData("emailUserDefault");
            Password = oRijndael.Decrypt(resourceFileManager.getConfigData("emailPwdDefault"));
            Port = Convert.ToInt32(resourceFileManager.getConfigData("emailPortDefault"));
            Host = oRijndael.Decrypt(resourceFileManager.getConfigData("emailSmtpDefault"));
            isSsl = Convert.ToBoolean(Convert.ToInt32(resourceFileManager.getConfigData("emailIsSslDefault")));
        }

        public Boolean SENDMAIL(String receiverEmail, String senderName, String Subject, String emailBody, string Bcc,string[] Files=null)
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

			foreach (var address in receiverEmail.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
			{
				msg.To.Add(new MailAddress(address));
			}
			//msg.To.Add(new MailAddress(receiverEmail));
            msg.Subject = Subject;
            msg.IsBodyHtml = true;
            msg.Body = emailBody;
            if (Bcc != string.Empty)
				foreach (var address in Bcc.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
				{
					msg.To.Add(new MailAddress(address));
				}
			if (Files != null)
				foreach(string myFile in Files)
				{
					if (System.IO.File.Exists(myFile) == true)
					{
						FileStream fileStream = new FileStream(myFile, FileMode.Open, FileAccess.Read);
						string FileName = string.Empty;
						FileName = Path.GetFileName(myFile);
						Attachment mAttachment = new Attachment(fileStream, FileName);
						msg.Attachments.Add(mAttachment);
					}
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
		private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			// Get the message we sent
			MailMessage msg = (MailMessage)e.UserState;

			if (e.Cancelled)
			{
				// prompt user with "send cancelled" message 
			}
			if (e.Error != null)
			{
				// prompt user with error message 
			}
			else
			{
				// prompt user with message sent!
				// as we have the message object we can also display who the message
				// was sent to etc 
			}

			// finally dispose of the message
			if (msg != null)
				msg.Dispose();
		}
		public Boolean SENDMAIL2(String receiverEmail, String senderName, String Subject, String emailBody, string Bcc, bool isHtml = false)
		{
			logger.Debug("Richiamo il metodo: SENDMAIL");
			bool result = false;
			SmtpClient client = new SmtpClient();
			client.DeliveryMethod = SmtpDeliveryMethod.Network;
			client.EnableSsl = isSsl;
			client.Host = Host;
			client.Port = Port;
			client.Timeout = 10000;
			System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(Email, Password);

			if (!string.IsNullOrEmpty(Password))
			{
				client.UseDefaultCredentials = false;
				client.Credentials = credentials;
			}
			MailMessage msg = new MailMessage();
			logger.Debug("FROM: " + this.Email);
			logger.Debug("isSsl: " + isSsl);
			logger.Debug("Host: " + Host);
			logger.Debug("Port: " + Port);
			logger.Debug("Subject: " + Subject);
			logger.Debug("emailBody: " + emailBody);
			msg.From = new MailAddress(this.Email);
			List<string> lEmailTo = new List<string>();
			if (receiverEmail.IndexOf(";") > -1)
			{
				lEmailTo = receiverEmail.Split(';').ToList();

				foreach (string sSingleTo in lEmailTo)
				{
					logger.Debug("Invio la email a: " + sSingleTo);
					msg.To.Add(new MailAddress(sSingleTo));
				}
			}
			else
			{
				logger.Debug("Invio la email a (receiver): " + receiverEmail);
				msg.To.Add(new MailAddress(receiverEmail));
			}
			msg.Subject = Subject;
			msg.IsBodyHtml = isHtml;
			msg.Body = emailBody;
			if (Bcc != string.Empty)
			{
				if (Bcc.IndexOf(";") > -1)
				{
					lEmailTo = Bcc.Split(';').ToList();

					foreach (string sSingleTo in lEmailTo)
					{
						logger.Debug("Invio in BCC l'email a: " + sSingleTo);
						msg.Bcc.Add(new MailAddress(sSingleTo));
					}
				}
				else
				{
					logger.Debug("Invio in BCC l'email a (BCC): " + Bcc);
					msg.Bcc.Add(new MailAddress(Bcc));
				}

			}

			//string.Format("<html><head></head><body><b>Test HTML Email</b></body>");

			try
			{
				client.Send(msg);
				logger.Debug("Invio Email eseguito da:" + this.Email + " a: " + receiverEmail);
				result = true;
			}
			catch (Exception ex)
			{
				logger.Error("ERRORE: " + ex.Message + " - " + ex.StackTrace + " - " + ex.Source);
				//lblMsg.ForeColor = Color.Red;
				//lblMsg.Text = "Error occured while sending your message." + ex.Message;
			}
			return result;
		}
		public Boolean SENDMAILeATTACHMENTS(Stream FileContent, string fileName, string post, String Subject, string ToAddress)
		{
			logger.Debug("Richiamo il metodo: SENDMAILeATTACHMENTS");

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
			List<string> lEmailTo = new List<string>();
			if (ToAddress.IndexOf(";") > -1)
			{
				lEmailTo = ToAddress.Split(';').ToList();

				foreach (string sSingleTo in lEmailTo)
				{
					msg.To.Add(new MailAddress(sSingleTo));
				}
			}
			else
			{
				msg.To.Add(new MailAddress(ToAddress));
			}
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
				logger.Debug("Invio Email eseguito da:" + this.Email + " a: " + ToAddress);
			}

			catch (Exception ex)
			{
				logger.Error("ERRORE: " + ex.Message);
			}
			return result;
		}


		public Boolean SendEmail_WithAttach(string[] Files, string ToAddress, String senderName, string Subject, string Message, string Bcc = "", bool isHtml = false)
		{
			logger.Debug("Richiamo il metodo: SendEmail_WithAttach");

			string emailBody = Message;
			bool result = false;
			SmtpClient client = new SmtpClient();

			client.DeliveryMethod = SmtpDeliveryMethod.Network;
			client.Host = Host;
			client.Port = Convert.ToInt32(Port);
			System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(Email, Password);

			if (string.IsNullOrEmpty(Password))
			{
				client.UseDefaultCredentials = false;
				client.Credentials = credentials;
			}
			client.EnableSsl = isSsl;
			MailMessage msg = new MailMessage();
			msg.From = new MailAddress(this.Email);

			foreach (string myToAddress in ToAddress.Split(';'))
			{
				msg.To.Add(new MailAddress(myToAddress));
			}
			msg.Subject = Subject;
			msg.IsBodyHtml = isHtml;
			List<string> lEmailTo = new List<string>();

			if (Bcc != string.Empty)
			{
				if (Bcc.IndexOf(";") > -1)
				{
					lEmailTo = Bcc.Split(';').ToList();

					foreach (string sSingleTo in lEmailTo)
					{
						msg.Bcc.Add(new MailAddress(sSingleTo));
					}
				}
				else
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
					FileName = Path.GetFileName(myFile);
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
				logger.Debug("Invio Email eseguito da:" + this.Email + " a: " + ToAddress);
			}

			catch (Exception ex)
			{
				logger.Error("ERRORE: " + ex.Message);
			}

			return result;
		}
	}
}
