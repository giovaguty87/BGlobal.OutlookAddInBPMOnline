using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGlobal.OutlookAddInBPMONLine.Core.Model
{
    public class Contact
    {
        public string Id { set; get; }
        public string AccountId { set; get; }
        public string Email { set; get; }
        public string AccountName { set; get; }
        public string Name { set; get; }
        public byte[] Image { set; get; }
    }
}