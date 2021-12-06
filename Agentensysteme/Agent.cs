using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public class Agent
    {
        private double force = 1; //Dadurch nimmt man Anfang fast alles an
        public double Force
        {
            get => force;
            set
            {
                if (value < 0.05)
                    force = 0.05;
                else if (value > 1) //Maximalwert
                    force = 1;
                else 
                    force = value;
            }
        }

        private int maxCost;
        private int minCost;
        public readonly List<List<int>> Matrix;
        private List<(int x, int y)> combos;
        public int Accepted = 0;

        public Agent(List<List<int>> matrix)
        {
            this.Matrix = matrix;
            combos = GetGoodCombos();
        }

        public void Reset()
        {
            maxCost = 0;
            minCost = 0;
            force = 1;
        }

        private void InitMinMax(Proposal p)
        {
            if (maxCost == 0)
            {
                maxCost = Utils.CalculateCost(Matrix, p.Order);
                minCost = maxCost - 1;
            }
        }

        double changeCount = 1;

        public List<int> GetNextNumbers(List<int> order)
        {
            return combos.Where(x => x.x == order[^1] && !order.Contains(x.y)).Select(x => x.y).ToList(); //Combo fängt mit letztem Element der Order an und der y-Wert ist noch nicht in der Order verwendet
        }

        public List<Proposal> OrderProposals(List<Proposal> proposals)
        {
            return proposals.OrderBy(x => Utils.CalculateCost(Matrix, x.Order)).ToList();
        }

        public Proposal Propose(Proposal old, double time, bool prevRejected)
        {
            InitMinMax(old);
            var p = old.Clone();
            changeCount = Math.Ceiling(p.Order.Count * (1 - time) / 10); //Am Anfang mehr Änderungen als am Ende zulassen, damit man schnell in Richtung einer guten Lösung kommt
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
            else if (newCost < minCost)
                minCost = newCost;
            bool accept = newCost <= oldCost || SmallNegtiveImpact(oldCost, newCost, time);
            if (accept)
                Accepted++;
            return accept;
        }

        private bool SmallNegtiveImpact(double oldCost, double newCost, double time)
        {
            if (newCost > maxCost)
                return false;
            var percentFromStart = maxCost / newCost;
            var range = newCost - oldCost;
            var forceFactor = 100 * percentFromStart * Force * changeCount;
            if (range < forceFactor) //Je höher Force, desto öfter trifft diese Bedingung zu --> Man wird dazu gedrängt, anzunehmen
                return true;
            return false;
        }

        private List<(int x, int y)> GetGoodCombos()
        {
            List<(int value, int x, int y)> temp = new List<(int, int, int)>();
            var count = Matrix.Count - 1;
            for (int i = 0; i < count; i++) //Alle Combos und deren Werte "generieren"
            {
                for (int j = 0; j < count; j++)
                {
                    temp.Add((Matrix[j][i], j, i));
                }
            }
            var max = temp.Max(x => x.value);
            var min = temp.Min(x => x.value);
            var range = max - min;
            //Sortieren nach Wert und Combos filtern, sodass nur gute behalten werden
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

        public List<(int start, int end)> GetComboRanges(List<int> order) //Welche Combos existieren aktuell?
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
            return combos.OrderBy(x => order[x.x]).ToList();
        }

        public (List<int> occupied, List<int> free) GetPlaces(List<(int start, int end)> comboRanges) //Welche Plätze sind frei / belegt (wo kann man noch was tauschen, ohne ne Combo zu zerstören)
        {
            if (comboRanges.Count == 0)
                return (new List<int>(), Enumerable.Range(0, Matrix.Count).ToList());
            var occupied = comboRanges.Select(RangeToEnumerable).SelectMany(x => x).ToList();
            return (occupied, Enumerable.Range(0, Matrix.Count).Except(occupied).ToList());
        }

        private IEnumerable<int> RangeToEnumerable((int start, int end) range) //Bsp: (0, 3) zu Liste: 0,1,2,3
            => Enumerable.Range(range.start, range.end - range.start + 1);


        public (int pos1, int pos2) GetSwapPair(List<(int start, int end)> comboRanges, (List<int> occupied, List<int> free) places, Proposal p)
        {
            var order = p.Order;
            if (comboRanges.Count == 0) //Wenn es noch keine Combo gibt, eine zufällige, neue anfangen
            {
                return GetRandomGoodSwap(order, places);
            }
            else
            {
                if (places.free.Count > 0)
                {
                    //Combos weiterbauen
                    var swap = GetSwapFromCurrentCombos(order, places, comboRanges);
                    if (swap is not (0, 0))
                        return swap;
                    else
                    {
                        //Neue rand combo machen
                        swap = GetRandomGoodSwap(order, places);
    
                        if (swap is not (0, 0))
                            return swap;

                        var order2 = p.Order.Select(x => x).ToList();
                        var prop = new Proposal { Order = order2 };
                        Random rand = new Random();
                        swap = (rand.Next(0, Matrix.Count), rand.Next(0, Matrix.Count));
                        prop.Swap(swap.pos1, swap.pos2);

                        var oldCost = Utils.CalculateCost(Matrix, p.Order);
                        var neWCost = Utils.CalculateCost(Matrix, prop.Order);
                        if (neWCost - oldCost < 80) //Nur wenn der zufällige Swap nicht zu schlecht ist, vorschlagen
                            return swap;
                    }
                }
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

        private (int pos1, int pos2) ComboToSwap(List<int> order, (int x, int y) combo, bool appendRight) //Combo in Swap-Koordinaten umwandeln
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

        private (int pos1, int pos2) GetSwapFromAvailablePositions(List<int> order, (List<int> occupied, List<int> free) places, List<(int pos1, int pos2)> positions)
        {
            var positionsRandom = positions.Distinct().Shuffle();
            foreach (var pos in positionsRandom)
            {
                (int x, int y) swap = (0,0);
                if (Utils.Get5050())
                {
                    var combo = combos.FirstOrDefault(x => x.x == order[pos.pos1] && places.free.Contains(x.y));
                    if (combo is (0, 0))
                        combo = combos.FirstOrDefault(x => x.y == order[pos.pos2] && places.free.Contains(x.x));
                    swap = ComboToSwap(order, combo, true);
                }
                if(swap is (0, 0))
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

        private (int pos1, int pos2) GetSwapFromCurrentCombos(List<int> order, (List<int> occupied, List<int> free) places, List<(int start, int end)> comboRanges)
        {
            comboRanges = comboRanges.Shuffle().ToList();
            var starts = comboRanges.Take(5).Select(x => x.start).Except(new List<int>() { 0 }).ToList(); //Nur 5 Combos beachten aus Performancegründen
            var ends = comboRanges.Take(5).Select(x => x.end).Except(new List<int>() { Matrix.Count }).ToList();
            (int x, int y) swap = (0, 0);
            if (Utils.Get5050()) //Zufall: vorne oder hinten weiterbauen
            {
                foreach (var start in starts)
                {
                    var value = order[start];
                    //FirstOrDefault ebenfalls wegen Performance
                    var combo = combos.FirstOrDefault(x => value == x.y && places.free.Contains(order.IndexOf(x.x)) && places.free.Contains(start - 1));
                    swap = ComboToSwap(order, combo, false);
                    if (swap is not (0, 0))
                        return swap;
                }
                foreach (var end in ends)
                {
                    var value = order[end];
                    var combo = combos.FirstOrDefault(x => value == x.x && places.free.Contains(order.IndexOf(x.y)) && places.free.Contains(end + 1));
                    swap = ComboToSwap(order, combo, true);
                    if (swap is not (0, 0))
                        return swap;
                }
            }
            else
            {
                foreach (var end in ends)
                {
                    var value = order[end];
                    var combo = combos.FirstOrDefault(x => value == x.x && places.free.Contains(order.IndexOf(x.y)) && places.free.Contains(end + 1));
                    swap = ComboToSwap(order, combo, true);
                    if (swap is not (0, 0))
                        return swap;
                }
                foreach (var start in starts)
                {
                    var value = order[start];
                    var combo = combos.FirstOrDefault(x => value == x.y && places.free.Contains(order.IndexOf(x.x)) && places.free.Contains(start - 1));
                    swap = ComboToSwap(order, combo, false);
                    if (swap is not (0, 0))
                        return swap;
                }
            }
            return swap;
        }
    }
}
