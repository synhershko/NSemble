using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NSemble.Core.Models;
using Nancy.ViewEngines.Razor;

namespace NSemble.Core.Extensions
{
    public static class HtmlExtensions
    {
        public static MemberInfo GetTargetMemberInfo(this Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Convert:
                    return GetTargetMemberInfo(((UnaryExpression)expression).Operand);
                case ExpressionType.Lambda:
                    return GetTargetMemberInfo(((LambdaExpression)expression).Body);
                case ExpressionType.Call:
                    return ((MethodCallExpression)expression).Method;
                case ExpressionType.MemberAccess:
                    return ((MemberExpression)expression).Member;
                default:
                    return null;
            }
        }

        private static IDictionary<string, object> DictionaryFromAnonymousObject(object values)
        {
            var ret = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (values != null)
            {
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(values);
                foreach (PropertyDescriptor prop in props)
                {
                    object val = prop.GetValue(values);
                    ret.Add(prop.Name, val);
                }
            }
            return ret;
        }

        public static IHtmlString LabelFor<TModel, TValue>(this HtmlHelpers<TModel> html, Expression<Func<TModel, TValue>> expression)
        {
            return LabelFor(html, expression, null);
        }

        public static IHtmlString LabelFor<TModel, TValue>(this HtmlHelpers<TModel> html, Expression<Func<TModel, TValue>> expression, object htmlAttributes)
        {
            return LabelFor(html, expression, DictionaryFromAnonymousObject(htmlAttributes));
        }

        public static IHtmlString LabelFor<TModel, TValue>(this HtmlHelpers<TModel> html, Expression<Func<TModel, TValue>> expression, IDictionary<string, object> htmlAttributes)
        {
            // get the html field name; probably need to run this through convention first
            var mi = expression.GetTargetMemberInfo();
            string htmlFieldName = mi.Name;

            // TODO: support getting DisplayName from Model metadata
            //ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
            string labelText = /*metadata.DisplayName ?? metadata.PropertyName ??*/ htmlFieldName.Split('.').Last();
            if (String.IsNullOrEmpty(labelText))
            {
                return NonEncodedHtmlString.Empty;
            }


            var sb = new StringBuilder();
            sb.AppendFormat(@"<label for=""{0}""", htmlFieldName /* TODO: normalize, conventions */);

            if (htmlAttributes != null)
                foreach (var htmlAttribute in htmlAttributes)
                {
                    sb.AppendFormat(@" {0}=""{1}""", htmlAttribute.Key, htmlAttribute.Value);
                }

            sb.AppendFormat(">{0}</label>", labelText);
            return new NonEncodedHtmlString(sb.ToString());
        }

        public static IHtmlString CheckBox<T>(this HtmlHelpers<T> helper, string Name, dynamic ModelProperty)
        {
            string input = String.Empty;
            bool checkedState = false;

            if (!bool.TryParse(ModelProperty.ToString(), out checkedState))
            {
                input = "<input name=\"" + Name + "\" type=\"checkbox\" value=\"true\" />";
            }
            else
            {
                if (checkedState)
                    input = "<input name=\"" + Name + "\" type=\"checkbox\" value=\"true\" checked />";
                else
                    input = "<input name=\"" + Name + "\" type=\"checkbox\" value=\"true\" />";
            }

            return new NonEncodedHtmlString(input);
        }

        public static IHtmlString ValidationSummary<T>(this HtmlHelpers<T> helper, List<ErrorModel> Errors)
        {

            if (!Errors.Any())
                return new NonEncodedHtmlString("");

            string div = "<div class=\"validation-summary-errors\"><span>Account creation was unsuccessful. Please correct the errors and try again.</span><ul>";

            foreach (var item in Errors)
            {

                div += "<li>" + item.ErrorMessage + "</li>";

            }

            div += "</ul></div>";

            return new NonEncodedHtmlString(div);
        }

        public static IHtmlString ValidationMessageFor<T>(this HtmlHelpers<T> helper, List<ErrorModel> Errors, string PropertyName)
        {
            if (!Errors.Any())
                return new NonEncodedHtmlString("");

            string span = String.Empty;

            foreach (var item in Errors)
            {
                if (item.Name == PropertyName)
                {
                    span += "<span class=\"field-validation-error\">" + item.ErrorMessage + "</span>";
                    break;
                }

            }

            return new NonEncodedHtmlString(span);
        }
    }
}