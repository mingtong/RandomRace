using System;
using System.Collections.Generic;

namespace RandomRace
{
    public struct Cell : IEquatable<Cell>
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode() { return X * 397 ^ Y; }
    }
    public enum Terrain { Road, Water, Obstacle }
    public enum Card { Energy, Slow }
    public sealed class Racer
    {
        public string Name; public Cell Position; public int Stamina = 3;
        public bool SkillUsed; public readonly List<Card> Hand = new List<Card>();
        public Racer(string name, Cell position) { Name = name; Position = position; }
    }
    public sealed class RaceGame
    {
        public const int Width = 10, Height = 8, MaxStamina = 8;
        public readonly Terrain[,] Map = new Terrain[Width, Height];
        public readonly List<Racer> Players = new List<Racer>();
        public readonly Cell Finish = new Cell(Width - 1, Height / 2);
        public int Turn { get; private set; }
        public int Winner { get; private set; } = -1;
        public int LastBotEnergyCards { get; private set; }
        public bool LastBotUsedSkill { get; private set; }
        public int LastBotSlowTarget { get; private set; } = -1;
        public int LastBotSlowAmount { get; private set; }
        readonly Random cardRandom;
        static readonly Cell[] Directions = { new Cell(1,0), new Cell(-1,0), new Cell(0,1), new Cell(0,-1) };
        public RaceGame(int seed)
        {
            var random = new Random(seed); cardRandom = new Random(seed ^ 0x49a53);
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++)
            { int roll = random.Next(100); Map[x,y] = roll < 13 ? Terrain.Water : roll < 25 ? Terrain.Obstacle : Terrain.Road; }
            // Keep a winding, connected road so every generated board is playable.
            int row = Height / 2;
            for (int x = 0; x < Width; x++)
            { Map[x,row] = Terrain.Road; int next = Math.Max(1, Math.Min(Height - 2, row + random.Next(-1, 2))); Map[x,next] = Terrain.Road; row = next; }
            for (int y = 0; y < Height; y++) { Map[0,y] = Terrain.Road; Map[Width-1,y] = Terrain.Road; }
            for (int i = 0; i < 4; i++) { var player = new Racer(i == 0 ? "Player" : "Bot " + i, new Cell(0, Height / 2)); Players.Add(player); Draw(player); }
        }
        public bool Walkable(Cell c) { return c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height && Map[c.X,c.Y] == Terrain.Road; }
        public List<Cell> Path(Cell start, Cell end)
        {
            if (!Walkable(start) || !Walkable(end)) return null;
            var queue = new Queue<Cell>(); var previous = new Dictionary<Cell,Cell>();
            queue.Enqueue(start); previous[start] = start;
            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue(); if (current.Equals(end)) break;
                foreach (Cell d in Directions) { var n = new Cell(current.X+d.X,current.Y+d.Y); if (Walkable(n) && !previous.ContainsKey(n)) { previous[n] = current; queue.Enqueue(n); } }
            }
            if (!previous.ContainsKey(end)) return null;
            var path = new List<Cell>(); for (Cell c = end; !c.Equals(start); c = previous[c]) path.Add(c);
            path.Reverse(); return path;
        }
        public bool Move(Cell destination)
        {
            if (Winner >= 0) return false;
            var racer = Players[Turn]; var path = Path(racer.Position, destination);
            if (path == null || path.Count == 0) return false;
            int finishIndex = path.FindIndex(c => c.Equals(Finish));
            int steps = finishIndex >= 0 ? finishIndex + 1 : path.Count;
            if (steps > racer.Stamina) return false;
            racer.Position = path[steps - 1]; racer.Stamina -= steps;
            if (finishIndex >= 0) Winner = Turn;
            return true;
        }
        void Draw(Racer p) { if (p.Hand.Count < 3) p.Hand.Add((Card)cardRandom.Next(2)); }
        public bool UseSkill()
        {
            if (Winner >= 0) return false; var p = Players[Turn];
            if (p.SkillUsed || p.Stamina >= MaxStamina) return false;
            p.SkillUsed = true; p.Stamina = Math.Min(MaxStamina, p.Stamina + 2); return true;
        }
        public bool UseCard(int index)
        {
            if (Winner >= 0) return false; var p = Players[Turn];
            if (index < 0 || index >= p.Hand.Count) return false;
            Card card = p.Hand[index];
            if (card == Card.Energy) { if (p.Stamina >= MaxStamina) return false; p.Stamina = Math.Min(MaxStamina, p.Stamina + 2); }
            else
            {
                Racer target = LeadingOpponent(p);
                target.Stamina = Math.Max(0, target.Stamina - 2);
            }
            p.Hand.RemoveAt(index); return true;
        }
        public Racer LeadingOpponent(Racer player)
        {
            Racer target = null; int best = int.MaxValue;
            foreach (var other in Players)
            {
                if (other == player) continue;
                var route = Path(other.Position, Finish); int distance = route == null ? int.MaxValue : route.Count;
                if (target == null || distance < best) { target = other; best = distance; }
            }
            return target;
        }
        public void EndTurn()
        {
            if (Winner >= 0) return;
            Turn = (Turn + 1) % Players.Count;
            var p = Players[Turn]; p.Stamina = Math.Min(MaxStamina, p.Stamina + 2); Draw(p);
        }
        public void BotMove()
        {
            if (Winner >= 0 || Turn == 0) return;
            LastBotEnergyCards = 0; LastBotUsedSkill = false; LastBotSlowTarget = -1; LastBotSlowAmount = 0;
            var p = Players[Turn]; var path = Path(p.Position, Finish);
            if (path != null && path.Count > p.Stamina)
            {
                // Prefer renewable cards to the once-per-game skill, and stop once the finish is reachable.
                for (int i = p.Hand.Count - 1; i >= 0 && path.Count > p.Stamina; i--)
                    if (p.Hand[i] == Card.Energy && p.Stamina <= MaxStamina - 2 && UseCard(i)) LastBotEnergyCards++;
                if (path.Count > p.Stamina && p.Stamina <= MaxStamina - 2) LastBotUsedSkill = UseSkill();
                if (path.Count > p.Stamina && LeadingOpponent(p).Stamina > 0)
                    for (int i = p.Hand.Count - 1; i >= 0; i--) if (p.Hand[i] == Card.Slow)
                    {
                        var target = LeadingOpponent(p); int before = target.Stamina;
                        if (UseCard(i)) { LastBotSlowTarget = Players.IndexOf(target); LastBotSlowAmount = before - target.Stamina; }
                        break;
                    }
            }
            if (path != null && path.Count > 0 && p.Stamina > 0) Move(path[Math.Min(path.Count, p.Stamina)-1]);
            if (Winner < 0) EndTurn();
        }
    }
}
