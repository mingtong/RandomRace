using System;

namespace RandomRace
{
    // Pure C# checks, callable from the editor or a standalone test runner.
    public static class RaceChecks
    {
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        public static void Run()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                var game = new RaceGame(seed); var start = game.Players[0].Position;
                var route = game.Path(start, game.Finish);
                Check(route != null && route.Count > 3, "Map is unreachable: " + seed);
                Check(!game.Move(game.Finish) && game.Players[0].Stamina == 3, "Movement overspent stamina");
                Check(game.Move(route[0]) && game.Players[0].Stamina == 2, "Movement cost incorrect");
                game.EndTurn(); Check(game.Turn == 1 && game.Players[1].Stamina == 5, "Turn recovery incorrect");
                int actions = 0;
                while (game.Winner < 0 && actions++ < 100)
                {
                    if (game.Turn != 0) game.BotMove();
                    else
                    {
                        var player = game.Players[0]; var path = game.Path(player.Position, game.Finish);
                        if (player.Stamina > 0 && path.Count > 0) game.Move(path[Math.Min(player.Stamina, path.Count) - 1]);
                        if (game.Winner < 0) game.EndTurn();
                    }
                }
                Check(game.Winner >= 0, "Game did not finish: " + seed);
                int winner = game.Winner; game.EndTurn(); Check(game.Winner == winner && !game.UseSkill(), "Game continued after victory");
            }
            var cards = new RaceGame(8); var racer = cards.Players[0];
            Check(racer.Hand.Count == 1 && cards.UseSkill() && racer.Stamina == 5 && !cards.UseSkill(), "Skill rules incorrect");
            racer.Hand.Clear(); racer.Hand.Add(Card.Energy); Check(cards.UseCard(0) && racer.Stamina == 7, "Energy card incorrect");
            racer.Hand.Add(Card.Slow); cards.Players[1].Position = new Cell(9, 3);
            Check(cards.UseCard(0) && cards.Players[1].Stamina == 1 && !cards.UseCard(0), "Slow card incorrect");
            var caps = new RaceGame(2); for (int i = 0; i < 20; i++) caps.EndTurn();
            Check(caps.Players[0].Stamina == 8 && caps.Players[0].Hand.Count == 3, "Stamina or hand cap incorrect");
            var botGame = new RaceGame(3); botGame.EndTurn(); var bot = botGame.Players[1];
            bot.Position = new Cell(9, 3); bot.Stamina = 1; bot.Hand.Clear(); bot.Hand.Add(Card.Energy); bot.Hand.Add(Card.Slow);
            botGame.BotMove(); Check(botGame.Winner == 1 && !bot.SkillUsed && bot.Hand.Count == 2, "Bot wasted ability before winning");
            var feedback = new RaceGame(11); feedback.EndTurn(); var actor = feedback.Players[1];
            for (int x = 0; x < RaceGame.Width; x++) feedback.Map[x, 4] = Terrain.Road;
            feedback.Players[0].Position = new Cell(8, 4); feedback.Players[0].Stamina = 3;
            actor.Position = new Cell(0, 4); actor.Stamina = 1; actor.SkillUsed = true; actor.Hand.Clear(); actor.Hand.Add(Card.Energy); actor.Hand.Add(Card.Slow);
            feedback.BotMove();
            Check(feedback.LastBotEnergyCards == 1 && !feedback.LastBotUsedSkill && feedback.LastBotSlowTarget == 0 && feedback.LastBotSlowAmount == 2, "Bot action feedback incorrect");
        }
    }
}
