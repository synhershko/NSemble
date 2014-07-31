using Nancy;
using Nancy.ModelBinding;
using NSemble.Core.Nancy;
using NSemble.Modules.Forms.Models;
using Raven.Client;

namespace NSemble.Modules.Forms
{
    public class FormsModule : NSembleModule
    {
        public FormsModule(IDocumentSession session)
            : base("Forms")
        {
            Post["/{formId}"] = p =>
            {
                IForm form = session.Load<IForm>("forms/" + p.formId);
                
                if (form == null)
                    return 404;

                var response = this.BindTo(form.GetResponseTypeInstance());
                form.HandleResponse(response);

                // TODO
                return Response.AsJson(new {Status = "ok"});
            };
        }
    }
}