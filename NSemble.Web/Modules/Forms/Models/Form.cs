using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Nancy.ViewEngines.Razor;

namespace NSemble.Modules.Forms.Models
{
    public abstract class Form<T> : IForm
        where T:new()
    {
        public string Id { get; set; }

        public abstract void HandleResponse(T rsp);

        public void HandleResponse(Object response)
        {
            HandleResponse((T) response);
        }

        public virtual IHtmlString AsHtml()
        {
            var sb = new StringBuilder();
            sb.AppendFormat(@"<form method=""post"" action=""{0}"">", "/" + Id);
            sb.AppendLine();

            var props = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                // We skip any property not marked with FormDataAttribute
                var att = (FormDataAttribute) prop.GetCustomAttributes(typeof (FormDataAttribute), false).FirstOrDefault();
                if (att == null) continue;

                sb.AppendLine(@"<div class=""form-group"">");
                sb.AppendFormat(@"<label for=""input{0}"">{1}</label>", prop.Name, att.Label ?? prop.Name);
                switch (att.Type)
                {
                    case "textarea":
                        sb.AppendFormat(@"<textarea class=""form-control"" id=""input{0}"" rows=""6"" name=""{0}"">{1}</textarea>",
                            prop.Name, att.DefaultValue);
                        break;
                    default:
                        sb.AppendFormat(@"<input type=""text"" name=""{0}"" class=""form-control"" id=""input{0}"" placeholder=""{1}"" value=""{2}"">"
                            , prop.Name, att.Placeholder, att.DefaultValue);
                        break;
                }
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</form>");
            return new NonEncodedHtmlString(sb.ToString());
        }

        public object GetResponseTypeInstance()
        {
            return new T();
        }
    }

    public interface IForm
    {
        object GetResponseTypeInstance();
        void HandleResponse(Object response);
    }
}