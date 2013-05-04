using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
namespace ConsoleDiff
{
  public  class DiffRequest
    {
        public String[] original { get; set; }
        public String[] edited {get;set;}
        public int hash { get; set; }

        public DiffRequest(String[] original, String[] edited)
        {
            this.original = original;
            this.edited = edited;
            calculateHashCode();
            

            
           
        }

        public void calculateHashCode()
        {
            if (original == null || edited == null) return;
            StringBuilder sb = new StringBuilder();
            foreach (String s in original)
            {
                sb.Append(s);
            }
            foreach (String s in edited)
            {
                sb.Append(s);
            }
            String concat = sb.ToString();
            hash = concat.GetHashCode();


        }


        public DiffRequest() { }
    }
}
