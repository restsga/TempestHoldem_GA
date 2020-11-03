using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TempestHoldem_GA
{
    class Player
    {
        private long gain;
        private int hands;
        private long ev;
        public bool[] actions;

        public long Gain { get => gain;}
        public int Hands { get => hands;}
        public long Ev { get => ev;}

        internal Player(Random random)
        {
            gain = 0;
            hands = 0;
            ev = 0;
            actions = new bool[169];

            for(int i = 0; i < actions.Length; i++)
            {
                if (random.NextDouble() < 0.5)
                {
                    actions[i] = true;
                }
                else
                {
                    actions[i] = false;
                }
            }
        }
        internal Player(bool[] actions)
        {
            gain = 0;
            hands = 0;
            ev = 0;
            this.actions = new bool[169];

            for (int i = 0; i < actions.Length; i++)
            {
                this.actions[i] = actions[i];
            }
        }
        internal Player(bool flag)
        {
            gain = 0;
            hands = 0;
            ev = 0;
            this.actions = new bool[169];

            for (int i = 0; i < actions.Length; i++)
            {
                this.actions[i] = flag;
            }
        }

        public void Play(int value)
        {
            gain -= value;
            hands++;
        }

        public void Win(int value)
        {
            gain += value;
        }

        public void ShowHandRange()
        {
            string message = "handrange:";
            for (int i = 0; i < 13; i++)
            {
                message += Environment.NewLine;
                for (int j = 0; j < 13; j++)
                {
                    message += Convert.ToInt32(actions[i * 13 + j]);
                }
            }
            Console.WriteLine(message);
        }

        internal void CalcEv()
        {
            if (hands == 0)
            {
                ev = 0;
                return;
            }
            ev = gain / hands;
        }

        internal void Cross(Random random,Player player)
        {
            /*int r1 = random.Next(actions.Length);
            int r2 = random.Next(actions.Length-r1)+r1;
            actions = this.actions.Take(r1).
                Concat(player.actions.Skip(r1).Take(r2 - r1)).
                Concat(actions.Skip(r2).TakeWhile(b => true)).ToArray();*/

            for(int i = 0; i < actions.Length; i++)
            {
                if (random.NextDouble() < 0.5)
                {
                    actions[i] = player.actions[i];
                }
            }
        }

        internal void Mutate(Random random)
        {
            for(int i = 0; i < actions.Length; i++)
            {
                if (random.NextDouble() < 0.05)
                {
                    actions[i] = actions[i] ? false : true;
                }
            }
        }
    }

    class PlayerSet
    {
        public Player[] players;

        public PlayerSet(int count,bool flag)
        {
            players = new Player[count];

            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new Player(flag);
            }
        }
        public PlayerSet(int count,Random random)
        {
            players = new Player[count];

            for(int i = 0; i < players.Length; i++)
            {
                players[i] = new Player(random);
            }
        }

        public void NextGen_Rank(Random random)
        {
            Enumerable.Range(0, players.Length).ToList().ForEach(i => players[i].CalcEv());

            players.ToList().Sort((a, b) => (int)(b.Ev - a.Ev));

            Console.WriteLine("ev:" + players[0].Ev);

            List<Player> next = new List<Player>();

            for (int i = 0; i < players.Length-1; i++)
            {
                int[] array = Enumerable.Range(1, players.Length).Select(n=>n*n/10+1).ToArray();
                int r = random.Next(array.Sum());
                int sum = 0;
                for (int a=0;a<array.Length;a++)
                {
                    sum += array[a];

                    if (r < sum)
                    {
                        next.Add(new Player(players[a].actions));
                        break;
                    }
                }
            }
            next.Add(new Player(random));

            for(int i = 0; i < players.Length; i++)
            {
                if (random.NextDouble() > 0.02)
                {
                    next[i].Cross(random,
                        next[Enumerable.Range(0, players.Length).Where(e => e != i).
                        OrderBy(i => Guid.NewGuid()).ToList().First()]);
                }
                else
                {
                    next[i].Mutate(random);
                }
            }

            players = next.ToArray();
        }
    }
}
