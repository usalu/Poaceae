using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Poaceae
{
    public class PoaceaeInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Poaceae";
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
                return "This library stores templates for other plugins.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("ed822f26-d6b9-4912-9e46-a72f811ccd6a");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Ueli Saluz";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "usaluz@outlook.de";
            }
        }
    }
}
