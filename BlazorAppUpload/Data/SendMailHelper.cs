using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

namespace BlazorAppUpload.Data
{
    public class SendMailHelper
    {
        public bool MailIsValid { get; set; }

        public string EmailTo { get; set; }

        public string Message { get; set; }

        public void ValidateEmail()
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(EmailTo);
            MailIsValid = match.Success;
        }

        public void SendMail()
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("mariia.duz@nure.ua");
                    mail.To.Add(EmailTo);
                    mail.Subject = "Test";
                    mail.Body = "<h1>File was successfully uploaded to azure</h1>";
                    mail.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential("gmail", "password");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                        Message = "Mail Sent";
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }
    }
}
