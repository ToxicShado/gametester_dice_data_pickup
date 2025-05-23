using System.Text.RegularExpressions;

namespace GrabDataFromGametester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            one_field data = new one_field();
            string line = "start";
            int money = 0;
            Console.WriteLine("Enter starting money: ");    
            Int32.TryParse(Console.ReadLine(), out money);
            Console.WriteLine($"Starting money: {money}");  
            while (line != "exit")
            {
                line = Console.ReadLine();

                if (line.Contains("status".ToLower())) { 
                Console.WriteLine($"Current money: {money}");
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
                        }
                    }
                    var player = Regex.Match(line, @"\btoxicshado gets (\d+) and (\d+)");
                    if (player.Success)
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
                    if (opponent.Success)
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
                    if (sixes.Success)
                    {
                        // Extract the rolled number and winning amount
                        if (Int64.TryParse(sixes.Groups[2].Value.Replace(",", ""), out long winningAmount))
                        {
                            data.player_val1 = 6;
                            data.player_val2 = 6;

                            data.opponent_val1 = -1;
                            data.opponent_val2 = -1;

                            money += (int)winningAmount;
                            data.total = (int)winningAmount + data.bet;
                            data.status = 3; // two sixes
                            Console.WriteLine($"Rolled two sixes! Winning amount: {winningAmount}");
                        }
                    }

                    var win = Regex.Match(line, @"\btoxicshado, you won ([\d,]+)");
                    if (win.Success)
                    {
                        // Extract the winning amount
                        if (Int64.TryParse(win.Groups[1].Value.Replace(",", ""), out long winningAmount))
                        {
                            data.total = (int)winningAmount + data.bet;
                            money += (int)winningAmount;
                            data.status = 1; // win
                        }
                    }

                    var lose = Regex.Match(line, @"\btoxicshado, you lost ([\d,]+)");
                    if (lose.Success)
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
                    if (draw.Success)
                    {
                        // Extract the winning amount
                        if (Int64.TryParse(draw.Groups[1].Value.Replace(",", ""), out long drawAmmount))
                        {
                            data.total = (int)drawAmmount;
                            data.status = 0; // draw
                        }
                    }

                    if(win.Success || draw.Success || lose.Success || sixes.Success)
                    {
                        if(!File.Exists("data.txt"))
                        {
                            Console.WriteLine("File does not exist.");
                            using (var file = File.CreateText("data.txt"))
                            {
                                file.WriteLine("current_money,timestamp,bet,player_val1,player_val2,opponent_val1,opponent_val2,total,status");
                            }
                        }
                        using (var file = File.AppendText("data.txt"))
                        {
                            file.WriteLine($"{money} {data.timestamp},{data.bet},{data.player_val1},{data.player_val2},{data.opponent_val1},{data.opponent_val2},{data.total},{data.status}");
                            Console.WriteLine($"Data written to file: {money},{data.timestamp},{data.bet},{data.player_val1},{data.player_val2},{data.opponent_val1},{data.opponent_val2},{data.total},{data.status}");
                        }
                        data = new one_field();
                    }
                }
            }
        }
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
