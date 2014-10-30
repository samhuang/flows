using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServiceTest
{
    public class ConversionResult
    {
        public ConversionStatus Status
        {
            get;
            set;
        }

        public int NumberOfPages
        {
            get;
            set;
        }

        public string[] Urls
        {
            get;
            set;
        }
    }
}
