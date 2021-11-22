using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public class Agent
    {
        private double force;
        public double Force
        {
            get => force;
            set => force = value < 0 ? 0 : value;
        }

        private int maxCost;
        private int minCost;
        public readonly List<List<int>> Matrix;
        private List<(int x, int y)> combos;
        private int count;
        public double AcceptRate { get; set; } = 14;

        public Agent(List<List<int>> matrix)
        {
            this.Matrix = matrix;
            combos = GetGoodCombos();
        }

        public void Reset()
        {
            maxCost = 0;
            minCost = 0;
            count = 0;
            force = 0;
            combos = combos.Shuffle().ToList();
        }

        private void InitMinMax(Proposal p)
        {
            if (maxCost == 0)
            {
                maxCost = Utils.CalculateCost(Matrix, p.Order);
                minCost = maxCost - 1;
            }
        }

        public bool ReNegotiate(Proposal p)
        {
            var cost = Utils.CalculateCost(Matrix, p.Order);
            return cost * 1.15 > maxCost || maxCost - cost < 150 && Matrix.Count >= 10;
        }

        public Proposal Propose(Proposal old, double time, bool prevRejected)
        {
            InitMinMax(old);
            count++;
            var p = old.Clone();
            var changeCount = Math.Ceiling(p.Order.Count * (1 - time) / 10);
            if (count * changeCount > Matrix.Count / 10)
            {
                combos = combos.Shuffle().ToList();
                count = 0;
            }
            for (int i = 0; i < changeCount; i++)
                ApplyCombo(p);
            return p;
        }

        public bool Accept(Proposal oldProp, Proposal newProp, double time)
        {
            InitMinMax(oldProp);
            var oldCost = Utils.CalculateCost(Matrix, oldProp.Order);
            var newCost = Utils.CalculateCost(Matrix, newProp.Order);
            if (newCost > maxCost)
                maxCost = newCost;
            else if(newCost < minCost)
                minCost = newCost;
            bool accept = newCost <= oldCost || CalculateNegativeImpact(oldCost, newCost, time) > AcceptRate;
            return accept;
        }

        private double CalculateNegativeImpact(double oldCost, double newCost, double time)
        {
            time = time - 1;
            var force = Force - 1;
            var loss = newCost - oldCost;
            var lossPercent = loss / newCost;

            var totalRange = maxCost - minCost;
            var minMaxRatio = (((maxCost - newCost) / totalRange) - ((newCost - minCost) / totalRange) + 1) / 2;

            var l1 = Math.Pow(2, lossPercent);
            var l2 = 1.414 - Math.Pow(Math.E, -lossPercent + 0.88);
            var t1 = Math.Pow(8, time);
            var t2 = 1.414 - Math.Pow(Math.E, -time + 0.88);
            var f1 = Math.Pow(16, force);
            var f2 = 1.414 - Math.Pow(Math.E, -force + 0.88);

            var acceptRate = (f1 * minMaxRatio) / (t1 * l1);

            return acceptRate;
        }

        private List<(int x, int y)> GetGoodCombos()
        {
            List<(int value, int x, int y)> temp = new List<(int, int, int)>();
            var count = Matrix.Count - 1;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    temp.Add((Matrix[j][i], j, i));
                }
            }
            var max = temp.Max(x => x.value);
            var min = temp.Min(x => x.value);
            var range = max - min;
            return temp.OrderBy(x => x.value).Where(x => (double)x.value / range < 0.2 && x.x != x.y).Select(x => (x.x, x.y)).ToList();
        }

        private void ApplyCombo(Proposal p)
        {
            var comboRanges = GetComboRanges(p.Order);
            var places = GetPlaces(comboRanges);
            if (places.free.Count > 0)
            {
                var swap = GetSwapPair(comboRanges, places, p);
                if (swap is not (0, 0))
                    p.Swap(swap.pos1, swap.pos2);
            }
        }

        public List<(int start, int end)> GetComboRanges(List<int> order)
        {
            List<(int x, int y)> combos = new List<(int, int)>();
            for (int i = 1; i < order.Count; i++)
            {
                var current = (order[i - 1], order[i]);
                if (this.combos.Contains(current))
                {
                    var existing = combos.Where(x => x.y == i - 1).FirstOrDefault();
                    if (existing is (0, 0))
                        combos.Add((i - 1, i));
                    else
                        existing.y = i;
                }
            }
            return combos.OrderBy(x => order[x.x]).ToList(); //TODO
        }

        public (List<int> occupied, List<int> free) GetPlaces(List<(int start, int end)> comboRanges)
        {
            if (comboRanges.Count == 0)
                return (new List<int>(), Enumerable.Range(0, Matrix.Count).ToList());
            var occupied = comboRanges.Select(RangeToEnumerable).SelectMany(x => x).ToList();
            return (occupied, Enumerable.Range(0, Matrix.Count).Except(occupied).ToList());
        }

        private IEnumerable<int> RangeToEnumerable((int start, int end) range)
            => Enumerable.Range(range.start, range.end - range.start + 1);


        public (int pos1, int pos2) GetSwapPair(List<(int start, int end)> comboRanges, (List<int> occupied, List<int> free) places, Proposal p)
        {
            var order = p.Order;
            if (comboRanges.Count == 0)
            {
                return GetRandomGoodSwap(order, places);
            }
            else
            {
                if (places.free.Count > 0)
                {
                    var swap = GetSwapFromCurrentCombos(order, places, comboRanges);
                    //if (Utils.Get5050())
                    //    Reorder(p, comboRanges);
                    if (swap is not (0, 0))
                        return swap;
                    else
                    {
                        swap = GetRandomGoodSwap(order, places);
                        //if (Utils.Get5050())
                        //    Reorder(p, comboRanges);
                        if (swap is not (0, 0))
                            return swap;
                    }
                }
                //Reorder(p, comboRanges);
                return (0, 0);
            }
        }

        private (int pos1, int pos2) GetRandomGoodSwap(List<int> order, (List<int> occupied, List<int> free) places)
        {
            List<(int pos1, int pos2)> positions = new();
            if (places.free.Count < 2)
                return (0, 0);
            for (int i = 0; i < places.free.Count - 1; i++)
            {
                if (places.free[i + 1] - places.free[i] == 1)
                {
                    positions.Add((places.free[i], places.free[i + 1]));
                }
            }
            return GetSwapFromAvailablePositions(order, places, positions);
        }

        private (int x, int y) ComboToSwap(List<int> order, (int x, int y) combo, bool appendRight)
        {
            if (combo is (0, 0))
                return combo;
            if (appendRight) //x stays
            {
                return (order.IndexOf(combo.y), order.IndexOf(combo.x) + 1);
            }
            else
            {
                return (order.IndexOf(combo.x), order.IndexOf(combo.y) - 1);
            }
        }

        private (int x, int y) GetSwapFromAvailablePositions(List<int> order, (List<int> occupied, List<int> free) places, List<(int pos1, int pos2)> positions)
        {
            var positionsRandom = positions.Distinct().Shuffle();
            foreach (var pos in positionsRandom)
            {
                (int x, int y) swap;
                if (Utils.Get5050())
                {
                    var combo = combos.FirstOrDefault(x => x.x == order[pos.pos1] && places.free.Contains(x.y));
                    if (combo is (0, 0))
                        combo = combos.FirstOrDefault(x => x.y == order[pos.pos2] && places.free.Contains(x.x));
                    swap = ComboToSwap(order, combo, true);
                }
                else
                {
                    var combo = combos.FirstOrDefault(x => x.y == order[pos.pos2] && places.free.Contains(x.x));
                    if (combo is (0, 0))
                        combo = combos.FirstOrDefault(x => x.x == order[pos.pos1] && places.free.Contains(x.y));
                    swap = ComboToSwap(order, combo, false);
                }
                if (swap is not (0, 0))
                    return swap;
            }
            return (0, 0);
        }

        private (int x, int y) GetSwapFromCurrentCombos(List<int> order, (List<int> occupied, List<int> free) places, List<(int start, int end)> comboRanges)
        {
            comboRanges = comboRanges.Shuffle().ToList();
            var starts = comboRanges.Take(5).Select(x => x.start).Except(new List<int>() { 0 }).ToList();
            var ends = comboRanges.Take(5).Select(x => x.end).Except(new List<int>() { Matrix.Count }).ToList();
            (int x, int y) swap = (0, 0);
            if (Utils.Get5050())
                foreach (var start in starts)
                {
                    var value = order[start];
                    var combo = combos.FirstOrDefault(x => value == x.y && places.free.Contains(order.IndexOf(x.x)) && places.free.Contains(start - 1));
                    swap = ComboToSwap(order, combo, false);
                    if (swap is not (0, 0))
                        return swap;
                }
            else
                foreach (var end in ends)
                {
                    var value = order[end];
                    var combo = combos.FirstOrDefault(x => value == x.x && places.free.Contains(order.IndexOf(x.y)) && places.free.Contains(end + 1));
                    swap = ComboToSwap(order, combo, true);
                    if (swap is not (0, 0))
                        return swap;
                }
            return swap;
        }

        private void Reorder(Proposal p, List<(int start, int end)> comboRanges)
        {
            var grouped = comboRanges.GroupBy(x => x.end - x.start);
            var ranges = GetComboRangesFromGroup( grouped);
            if (ranges.length != 0)
            {
                var r1 = RangeToEnumerable(ranges.ranges[0]);
                var r2 = RangeToEnumerable(ranges.ranges[1]);
                var swaps = r1.Zip(r2);
                foreach (var swap in swaps)
                {
                    p.Swap(swap.First, swap.Second);
                }
            }
        }

        private (int length, List<(int pos1, int pos2)> ranges) GetComboRangesFromGroup(IEnumerable<IGrouping<int, (int start, int end)>> grouped)
        {
            Random random = new Random();
            foreach (var group in grouped)
            {
                var count = group.Count();
                if (count > 1)
                {
                    var rand = random.Next(1, count);
                    return (group.Key, new List<(int pos1, int pos2)> { group.ElementAt(random.Next(0, rand)), group.ElementAt(random.Next(rand, count)) });
                }
            }
            return (0, new List<(int pos1, int pos2)>());
        }

        private (int length, List<(int pos1, int pos2)> ranges) GetComboRangesFromGroup2(List<int> order, IEnumerable<IGrouping<int, (int start, int end)>> grouped)
        {
            foreach (var group in grouped)
            {
                var count = group.Count();
                if (count > 1)
                {
                    var starts = group.Select(x => order[x.start]);
                    var ends = group.Select(x => order[x.end]);
                    var combo = combos.FirstOrDefault(x => starts.Contains(x.y) && ends.Contains(x.x));
                    if (combo is not (0, 0))
                    {
                        (int pos1, int pos2) comboIndex = (order.IndexOf(combo.x), order.IndexOf(combo.y));
                        var range1 = (comboIndex.pos1 - group.Key, comboIndex.pos1);
                        var range2 = (comboIndex.pos2, comboIndex.pos2 + group.Key);
                        return (group.Key, new() { range1, range2 });
                    }
                }
            }
            return (0, new List<(int pos1, int pos2)>());
        }
    }
}
