using System;

namespace TempestHoldem_GA
{
    class Program
    {
        static void Main(string[] args)
        {
            //初期化
            Random random = new Random();

            //個体数
            int biont;
            Console.WriteLine("input biont...");
            int.TryParse(Console.ReadLine(), out biont);
            if (biont <= 0)
            {
                biont = 10;
            }

            //プレイヤー数
            int players;
            /*Console.WriteLine("input players...");
            int.TryParse(Console.ReadLine(), out players);
            if (players <= 0)*/
            {
                players = 6;
            }

            //世代内試行回数
            int count;
            Console.WriteLine("input count...");
            int.TryParse(Console.ReadLine(), out count);
            if (count <= 0)
            {
                count = 100000;
            }
            
            //同一ハンド試行回数
            int hand;
            Console.WriteLine("input hand...");
            int.TryParse(Console.ReadLine(), out hand);
            if (hand <= 0)
            {
                hand = 1;
            }

            //試行世代数
            int gen;
            Console.WriteLine("input gen...");
            int.TryParse(Console.ReadLine(), out gen);
            if (gen <= 0)
            {
                gen = 100;
            }


            /*
             * 個体群を生成
             */
            PlayerSet[][] playerSets = new PlayerSet[players][];

            int elements = 1;
            for (int i = 0; i < playerSets.Length; i++)
            {
                playerSets[i] = new PlayerSet[elements];

                for (int j = 0; j < playerSets[i].Length; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        playerSets[i][j] = new PlayerSet(biont, random);
                    }
                    else
                    {
                        playerSets[i][j] = new PlayerSet(biont,false);
                    }
                }

                elements *= 2;
            }

            //訓練開始
            Game game = new Game(random, playerSets, biont, players);

            while (true)
            {
                for (int g = 0; g < gen; g++)
                {
                    for (int c = 0; c < count; c++)
                    {
                        game.Play(hand);
                    }

                    playerSets[0][0].NextGen_Rank(random);

                    for (int i = 0; i < playerSets.Length; i++)
                    {
                        for (int j = 0; j < playerSets[i].Length; j++)
                        {
                            //playerSets[i][j].NextGen_Rank(random);
                        }
                    }
                }

                for(int i = 0; i < biont; i++)
                {
                    playerSets[0][0].players[i].ShowHandRange();
                }

                //Console.ReadKey();
            }
        }
    }
}
