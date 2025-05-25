using System.Text.RegularExpressions;

namespace GrabDataFromGametester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int five = 0;
            List<int> last_bets = new List<int>();
            List<int> last_plays = new List<int>();

            double last_bets_sum = 0;
            double last_plays_sum = 0;

            for (int i = 0; i<5;i++)
            {
                last_bets.Add(0);
                last_plays.Add(0);
            }

            one_field data = new one_field();
            string line = "start";
            int money = 0;
            bool in_progress = false;
            Console.WriteLine("Enter starting money: ");
            string? money_str = Console.ReadLine();
            if(string.IsNullOrEmpty(money_str))
            {
                money = ReadLastMoneyStatus();
                Console.WriteLine($"Starting money: {money} (from last status)");
            }
            else
            {
                money_str = money_str.Trim();
                Int32.TryParse(money_str, out money);
                Console.WriteLine($"Starting money: {money}");
            }


            while (line != "exit")
            {
                line = Console.ReadLine();

                if (line.Contains("reset".ToLower()) || line.Contains("flag".ToLower()))
                {
                    in_progress = false;
                }

                if (line.Contains("restart".ToLower()))
                {
                    Console.WriteLine("Enter starting money: ");
                    Int32.TryParse(Console.ReadLine(), out money);
                    Console.WriteLine($"Starting money: {money}");
                }

                if (line.Contains("deleteone".ToLower()))
                {
                    if (File.Exists("data.csv"))
                    {
                        var lines = File.ReadAllLines("data.csv").ToList();
                        if (lines.Count > 1) // Ensure there's more than just the header
                        {
                            lines.RemoveAt(lines.Count - 1); // Remove the last line
                            File.WriteAllLines("data.csv", lines); // Overwrite the file
                            Console.WriteLine("Last line deleted.");
                        }
                        else
                        {
                            Console.WriteLine("No data to delete.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("File does not exist.");
                    }
                }

                if (line.Contains("status".ToLower()))
                {
                    Console.WriteLine($"Current money: {money}");
                }

                if (line.Contains("statuscount".ToLower()))
                {
                    PrintStatusSummary(money,last_bets_sum,last_plays_sum);
                }

                if (line != null)
                {
                    var bet = Regex.Match(line, @"\btoxicshado bets ([\d,]+)");
                    if (bet.Success)
                    {
                        // Extract the number
                        if (Int64.TryParse(bet.Groups[1].Value.Replace(",", ""), out long number))
                        {
                            Console.WriteLine($"Extracted number: {number}");
                            data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            data.bet = (int)number;
                            in_progress = true;
                        }
                    }
                    var player = Regex.Match(line, @"\btoxicshado gets (\d+) and (\d+)");
                    if (player.Success && in_progress)
                    {
                        // Extract the numbers
                        if (Int64.TryParse(player.Groups[1].Value, out long number1) && Int64.TryParse(player.Groups[2].Value, out long number2))
                        {
                            Console.WriteLine($"Extracted numbers: {number1}, {number2}");
                            data.player_val1 = (int)number1;
                            data.player_val2 = (int)number2;

                        }
                    }
                    var opponent = Regex.Match(line, @"\btoxicshado, your opponent throws their dice\.\.\. and gets (\d+) and (\d+)");
                    if (opponent.Success && in_progress)
                    {
                        // Extract the opponent's numbers
                        if (Int64.TryParse(opponent.Groups[1].Value, out long opponentVal1) && Int64.TryParse(opponent.Groups[2].Value, out long opponentVal2))
                        {
                            Console.WriteLine($"Opponent's numbers: {opponentVal1}, {opponentVal2}");
                            data.opponent_val1 = (int)opponentVal1;
                            data.opponent_val2 = (int)opponentVal2;
                        }
                    }

                    var sixes = Regex.Match(line, @"\btoxicshado rolls two (\d+)s!.*?won ([\d,]+)");
                    if (sixes.Success && in_progress)
                    {
                        // Extract the rolled number and winning amount
                        if (Int64.TryParse(sixes.Groups[2].Value.Replace(",", ""), out long winningAmount))
                        {
                            data.player_val1 = 6;
                            data.player_val2 = 6;

                            data.opponent_val1 = -1;
                            data.opponent_val2 = -1;

                            money += (int)winningAmount;
                            data.total = (int)winningAmount;
                            data.status = 3; // two sixes
                            Console.WriteLine($"Rolled two sixes! Winning amount: {winningAmount}");
                        }
                    }
                    // toxicshado, you rolled a double and won twice your bet: 2,000 :GTface1:
                    var doubles = Regex.Match(line, @"\btoxicshado.+ double .+ bet: ([\d,]+)");
                    if (doubles.Success && in_progress)
                    {
                        if (Int64.TryParse(doubles.Groups[1].Value.Replace(",", ""), out long winningAmount))
                        {
                            data.total = (int)winningAmount;
                            money += (int)winningAmount;
                            data.status = 2; // doubles
                        }
                    }

                    var win = Regex.Match(line, @"\btoxicshado, you won ([\d,]+)");
                    if (win.Success && in_progress)
                    {
                        // Extract the winning amount
                        if (Int64.TryParse(win.Groups[1].Value.Replace(",", ""), out long winningAmount))
                        {
                            data.total = (int)winningAmount;
                            money += (int)winningAmount;
                            data.status = 1; // win
                        }
                    }

                    var lose = Regex.Match(line, @"\btoxicshado, you lost ([\d,]+)");
                    if (lose.Success && in_progress)
                    {
                        // Extract the winning amount
                        if (Int64.TryParse(lose.Groups[1].Value.Replace(",", ""), out long loseAmmount))
                        {
                            data.total = (int)loseAmmount * -1;
                            money = money + data.total;
                            data.status = -1; // lose
                        }
                    }

                    var draw = Regex.Match(line, @"\btoxicshado, it's a draw, you get back ([\d,]+)");
                    if (draw.Success && in_progress)
                    {
                        data.total = data.bet;
                        data.status = 0; // draw   
                    }
                    if (in_progress)
                        if (win.Success || draw.Success || lose.Success || sixes.Success || doubles.Success)
                        {
                            if (!File.Exists("data.csv"))
                            {
                                Console.WriteLine("File does not exist.");
                                using (var file = File.CreateText("data.csv"))
                                {
                                    file.WriteLine("current_money,timestamp,bet,player_val1,player_val2,opponent_val1,opponent_val2,total,status");
                                }
                            }
                            using (var file = File.AppendText("data.csv"))
                            {
                                file.WriteLine($"{money},{data.timestamp},{data.bet},{data.player_val1},{data.player_val2},{data.opponent_val1},{data.opponent_val2},{data.total},{data.status}");
                                Console.WriteLine($"Data written to file: {money},{data.timestamp},{data.bet},{data.player_val1},{data.player_val2},{data.opponent_val1},{data.opponent_val2},{data.total},{data.status}");
                            }


                            five = (five+1)%5;
                            last_bets[five % 5] = data.bet;
                            last_plays[five % 5] = data.total;

                            last_bets_sum = 0;
                            last_plays_sum = 0;
                            for (int i = 0; i < 5; i++)
                            {
                                last_bets_sum += last_bets[i];
                                last_plays_sum += last_plays[i];
                            }

                            last_bets_sum /= 5;
                            last_plays_sum /= 5;

                            PrintStatusSummary(money, last_bets_sum, last_plays_sum);
                            data = new one_field();
                            in_progress = false;
                        }
                }
            }
        }




        public static void PrintStatusSummary(int money, double last_bets, double last_plays)
        {
            const string fileName = "data.csv";
            if (!File.Exists(fileName))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("File does not exist.");
                Console.ResetColor();
                return;
            }

            var lines = File.ReadAllLines(fileName);
            if (lines.Length <= 1)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("No data to summarize.");
                Console.ResetColor();
                return;
            }

            int runningTotal = 0;
            int losses = 0, draws = 0, wins = 0, doubleWins = 0, twoSixes = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var parts = line.Split(',');
                if (parts.Length < 9)
                    continue; // skip malformed lines

                if (int.TryParse(parts[8], out int status))
                {
                    runningTotal += status;
                    switch (status)
                    {
                        case -1: losses++; break;
                        case 0: draws++; break;
                        case 1: wins++; break;
                        case 2: doubleWins++; break;
                        case 3: twoSixes++; break;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Current Money : {money}; With and average bet (over the last 5 bets) of {last_bets} and avg winings {last_plays}\nLosses:{losses}; Draws:{draws}; Wins:{wins}; Doubles:{doubleWins}; Sixes:{twoSixes}");
            Console.WriteLine($"");
            Console.ResetColor();
        }

        public static int ReadLastMoneyStatus()
        {
            const string fileName = "data.csv";
            if (!File.Exists(fileName))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("File does not exist.");
                Console.ResetColor();
                return 0;
            }

            var lines = File.ReadAllLines(fileName);
            if (lines.Length <= 1)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("No data in file.");
                Console.ResetColor();
                return 0;
            }

            // Find the last non-empty line (skip header)
            for (int i = lines.Length - 1; i > 0; i--)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    var parts = line.Split(',');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int lastMoney))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Last money status: {lastMoney}");
                        Console.ResetColor();
                        return lastMoney;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("No valid money status found.");
            Console.ResetColor();
            return 0;
        }

        struct one_field
        {
            public string timestamp;
            public int bet;
            public int player_val1;
            public int player_val2;
            public int opponent_val1;
            public int opponent_val2;
            public int total;
            public int status; // -1 = lose, 0 = draw, 1 = win, 2 = double numbers win, 3 two sixes
        }

    }
}
