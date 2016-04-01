using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSShellExtContextMenuHandler
{
    /// <summary>
    /// Object rappresentation of an item in the xmlSetting.xml
    /// </summary>
    class ItemXML
    {
        public string sPathProg;
        public string sSeparatore; //separator between files
        public string sPrefixItem;
        public string sSuffixItem;
        
        public ItemXML(string sPathProg, string sSeparatore)
        {
            this.sPathProg = sPathProg;
            
            if (sSeparatore.Trim().Equals(""))
                this.sSeparatore = " ";
            else
                this.sSeparatore = sSeparatore;

            this.sPrefixItem = "\"";
            this.sSuffixItem = "\"";

        }
    }
}
