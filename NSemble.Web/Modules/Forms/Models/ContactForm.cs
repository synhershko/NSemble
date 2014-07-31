using System;
using System.Collections.Generic;
using System.Net.Mail;
using NSemble.Core.Extensions;
using NSemble.Core.Models;

namespace NSemble.Modules.Forms.Models
{
    public class ContactForm : Form<ContactForm.Response>
    {
        public class Response : IFormResponse
        {
            [FormData(Label = "Name", Placeholder = "Your name")]
            public string FromName { get; set; }
            [FormData(Label = "E-mail", Placeholder = "example@example.com")]
            public string FromEmail { get; set; }
            [FormData(Label = "Subject")]
            public string Subject { get; set; }
            [FormData(Label = "Message", Type = "textarea")]
            public string Body { get; set; }
        }

        public string EmailSubjectPrefix { get; set; }
        public List<MailAddress> SendResponsesTo { get; set; }

        public override void HandleResponse(Response response)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            if (SendResponsesTo == null)
                throw new ArgumentNullException("SendResponsesTo");

            var message = new MailMessage
            {
                From = new MailAddress(response.FromEmail, response.FromName),
                Subject = string.Join(" ", EmailSubjectPrefix, response.Subject),
                Body = DynamicContentHelpers.CompiledStringContent(response.Body, DynamicContentType.Markdown).ToHtmlString(),
                IsBodyHtml = true,
            };
            SendResponsesTo.ForEach(message.To.Add);

            var client = new SmtpClient();
            client.Send(message);
        }
    }
}