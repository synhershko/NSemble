using System.Collections.Generic;
using System.Net.Mail;
using Raven.Abstractions.Extensions;

namespace NSemble.Core.Tasks
{
    public class SendEmailTask : ExecutableTask
    {
        private readonly string replyTo;
        private readonly string subject;
        private readonly string view;
        private readonly dynamic model;
        private readonly HashSet<string> sendTo;
        private string html;

        public const string basicEmailHtml = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"">
<html>
<head>
<meta http-equiv=""content-type"" content=""text/html; charset=UTF-8"">
</head>
<body bgcolor=""#ffffff"" text=""#000000"">{0}</body>
</html>";

        public SendEmailTask(
            string replyTo,
            string subject,
            HashSet<string> sendTo,
            string view,
            dynamic model)
        {
            this.replyTo = replyTo;
            this.subject = subject;
            this.view = view;
            this.model = model;
            this.sendTo = sendTo;
        }

        public SendEmailTask(
            string replyTo,
            string subject,
            HashSet<string> sendTo,
            string html)
        {
            this.replyTo = replyTo;
            this.subject = subject;
            this.sendTo = sendTo;
            this.html = html;
        }

        public override void Execute()
        {
            if (html == null)
            {
                // TODO
                //IViewFactory vf;
                //var response = vf.RenderView(view, model, new ViewLocationContext());
                //html = response.ToString();
                html = string.Empty;
            }

            var mailMessage = new MailMessage
                                  {
                                      IsBodyHtml = true,
                                      Body = html,
                                      Subject = subject,
                                  };

            if (string.IsNullOrEmpty(replyTo) == false)
            {
                try
                {
                    mailMessage.ReplyToList.Add(new MailAddress(replyTo));
                }
                catch
                {
                    // we explicitly ignore bad reply to emails
                }
            }

            sendTo.ForEach(email => mailMessage.To.Add(email));

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Send(mailMessage);
            }

        }
    }
}
