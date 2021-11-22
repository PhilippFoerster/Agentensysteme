using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public class Mediator
    {
        private Proposal proposal;
        internal List<Agent> agents;


        public Mediator(List<Agent> agents, int orderLength)
        {
            this.agents = agents;
            proposal = new Proposal() { Order = Utils.GetRandomOrder(orderLength) };
            //proposal = new Proposal() { Order = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 } };
            //proposal = new Proposal() { Order = new List<int> { 3, 8, 5, 2, 0, 9, 7, 6, 4 ,1 } };
        }


        public Proposal RunNegotiation(int rounds)
        {
            double time = 0;
            double forceInc = (double)1 / 10;
            double step = (double)1 / rounds;
            for (int i = 0; i < rounds; i++)
            {
                //agents = agents.Shuffle().ToList();
                foreach (var agent in agents)
                {
                    var otherAgents = agents.Except(new List<Agent> { agent });
                    var p = agent.Propose(proposal, time, true);
                    bool allAccept = true;
                    foreach (var otherAgent in otherAgents)
                    {
                        bool accept = otherAgent.Accept(proposal, p, time);
                        if (!accept)
                        {
                            allAccept = false;
                            otherAgent.Force += forceInc;
                        }
                        else
                            otherAgent.Force -= 5 * forceInc;
                    }
                    if (allAccept)
                        proposal = p;
                }
                time += step;
            }
            return proposal;
        }
    }
}
