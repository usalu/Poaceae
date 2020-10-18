using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace PluginTemplateRhino5
{
    public class PluginTemplateRhino5Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "PluginTemplateRhino5";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("b34b880a-8a87-47ac-97ae-4c4c6de7c9ec");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
