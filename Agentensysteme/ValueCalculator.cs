using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public class ValueCalculator
    {

        List<(double acceptRate, double average)> acceptRates = new();
        public void TryBestAcceptRate(int triesPerRound, int negatiationRounds, double start, double end, List<Agent> agents)
        {
            for (double i = start; i < end; i += 1)
            {
                Mediator m = new Mediator(agents, agents[0].Matrix.Count);
                long total = 0;
                agents.ForEach(x =>
                {
                    x.Reset();
                    x.AcceptRate = i;
                });
                for (int j = 0; j < triesPerRound; j++)
                {
                    var proposal = m.RunNegotiation(negatiationRounds);
                    total += agents.Sum(x => Utils.CalculateCost(x.Matrix, proposal.Order));
                }
                acceptRates.Add((i, total / (double)triesPerRound));
                Console.WriteLine($"AcceptRate {i}: {acceptRates[^1].average}");
            }
        }

        public void PrintResult()
        {
            var best = acceptRates.MinBy(x => x.average);
            var ordered = acceptRates.OrderBy(x => x.average);
            Console.WriteLine($"AcceptRate {best.acceptRate}: Average {best.average}");
        }

        public List<Action> GetActions(int triesPerRound, int negatiationRounds, int count, int length, List<Agent> agents)
        {
            List<Action> actions = new List<Action>();
            for (int i = 0; i < count; i++)
            {
                var start = (double)length * i;
                Action action = () => TryBestAcceptRate(triesPerRound, negatiationRounds, start, start += length, agents.Select(x => new Agent(x.Matrix)).ToList());
                actions.Add(action);
            }
            return actions;
        }
    }
}
