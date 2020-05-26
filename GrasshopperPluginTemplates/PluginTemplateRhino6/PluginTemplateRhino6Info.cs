using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GrasshopperPluginTemplateRhino6
{
    public class PluginTemplateRhino6Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "GrasshopperPluginTemplateRhino6";
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
                return new Guid("3df1bd47-db96-4626-b085-2af3aea83fc8");
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
