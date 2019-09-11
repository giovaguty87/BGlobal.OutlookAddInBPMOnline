using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGlobal.OutlookAddInBPMONLine.Core.Helper
{
    public static class Helper
    {
        public static String ValidateMail(string mail)
        {
            if (!string.IsNullOrWhiteSpace(mail))
            {
                if (mail.Substring(0, 1) == "'")
                    mail = mail.Substring(1);

                if (mail.Substring(mail.Length - 1, 1) == "'")
                    mail = mail.Substring(0, mail.Length - 1);
            }

            return mail;
        }
    }
}