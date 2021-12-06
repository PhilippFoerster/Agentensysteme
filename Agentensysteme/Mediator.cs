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
        }

        public Proposal PickBest(int times, Func<Proposal> negotioationFunction) //times mal verhandeln und Agenten auf eine der Lösungen auswählen
        {
            List<Proposal> proposals = new List<Proposal>();
            for (int i = 1; i <= times; i++)
            {
                agents.ForEach(x => x.Reset());
                this.proposal = new Proposal() { Order = Utils.GetRandomOrder(agents[0].Matrix.Count) };
                var proposal = negotioationFunction();
                Print(proposal, $"Added {i}/{times}");
                proposals.Add(proposal);
            }
            proposals = proposals.DistinctBy(x => string.Join("", x.Order)).ToList();
            Dictionary<Agent, List<Proposal>> list = new Dictionary<Agent, List<Proposal>>();
            foreach (var agent in agents)
            {
                list.Add(agent, agent.OrderProposals(proposals));
            }

            Proposal best = null;
            var oldIndex = int.MaxValue;
            for (int i = 0; i < proposals.Count || i < oldIndex; ++i)
            {
                foreach (var entry in list)
                {
                    var prop = entry.Value[i];
                    int count = 0;
                    foreach (var x in agents)
                    {
                        var proposalsOfOtherAgent = list[x];
                        var index = proposalsOfOtherAgent.IndexOf(prop);
                        count += index;
                    }
                    if (count < oldIndex)
                    {
                        best = entry.Value[i];
                        oldIndex = count;
                    }
                }
            }
            Console.WriteLine("Accpeted: " + string.Join(", ", agents.Select(x => x.Accepted)));
            agents.ForEach(x => x.Accepted = 0);
            return best;
        }

        public Proposal BuildProposal()
        {
            List<int> all = Enumerable.Range(0, agents[0].Matrix.Count).ToList();
            List<int> order = new List<int> { 0 };
            for (int i = 1; i < agents[0].Matrix.Count; ++i)
            {
                agents = agents.Shuffle().ToList();
                var same = agents[0].GetNextNumbers(order);
                List<int> allPossibilities = new List<int>().Concat(same).ToList();
                foreach (var agent in agents.Skip(1))
                {
                    var candidates = agent.GetNextNumbers(order);
                    allPossibilities.AddRange(candidates);
                    same = same.Intersect(candidates).ToList(); //Schnittmenge: Für beide (bzw. alle) Agenten ist die nächste Zahl gut
                }
                if (same.Any())
                    order.Add(same[0]); //Dadurch, dass die Combos in den Agenten sortiert werden und wir hier immer den ersten "Vorschlag" wählen, ist es gleichzeitig der beste für einen der Agenten
                else if (allPossibilities.Any()) //Falls es keine gemeinsamen Combos gibt, dann wenigstens die beste von einem Agenten nehmen. Dadurch, dass die Agenten jede Runde geshuffelt werden, wird keiner bevorzugt
                    order.Add(allPossibilities[0]);
                else order.Add(all.Except(order).ToList().TakeRandom());
            }
            return new Proposal { Order = order };
        }


        public Proposal RunNegotiation(int rounds, bool print = true)
        {
            double time = 0;
            double forceInc = (double)1 / 20;
            double step = (double)1 / rounds;
            int count = 0;
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
                            otherAgent.Force += forceInc; //Force "Zwang" erhöhen, je höher Force, desto wahrscheinlicher nimmt Agent an, obwohl es schlecht für ihn ist
                        }
                        else
                            otherAgent.Force -= 5 * forceInc;
                    }
                    if (allAccept)
                    {
                        Print(p, "Accpeted", print);
                        proposal = p;
                        count = 0;
                    }
                    else
                    {
                        Print(p, "Rejected", print);
                        count++;
                        if (count == 19)
                            Console.Write("");
                        if (count == 20)
                        {
                            Console.WriteLine("Stopped bc stuck at " + i);
                            i = rounds;
                        }
                    }
                }
                time += step;
            }
            return proposal;
        }

        private void Print(Proposal p, string prefix, bool print = true)
        {
            if (!print)
                return;
            var cost1 = Utils.CalculateCost(agents[0].Matrix, p.Order);
            var cost2 = Utils.CalculateCost(agents[1].Matrix, p.Order);
            var msg = $"{prefix}: {(false ? String.Join(",", p.Order) : "")} --- {cost1} - {cost2} - {cost1 + cost2}";
            Console.WriteLine(msg);
        }
    }
}
