using System;

namespace NSemble.Modules.Forms
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FormDataAttribute : Attribute
    {
        public string Label { get; set; }
        public string Placeholder { get; set; }
        public string DefaultValue { get; set; }
        public string Type { get; set; }
    }
}