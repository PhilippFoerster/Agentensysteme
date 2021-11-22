using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public class Proposal
    {
        public List<int> Order { get; set; }

        public Proposal Clone()
        {
            var p = (Proposal)MemberwiseClone();
            p.Order = Order.Select(x => x).ToList();
            return p;
        }

        public void Swap(int x, int y)
        {
            try
            {
                var temp = Order[x];
                Order[x] = Order[y];
                Order[y] = temp;
            }
            catch(Exception ex)
            {

            }
        }
    }
}
