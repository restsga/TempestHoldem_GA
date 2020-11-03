using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TempestHoldem_GA
{
    class Game
    {
        const uint UINT_ONE = 1;

        static int[] BLINDS = { 100, 100, 100, 200+100, 500+100, 1000+100 };
        const int CUP = 10000;

        Random random;
        PlayerSet[][] playerSets;
        int BIONT;
        int PLAYERS;

        public Game(Random random,PlayerSet[][] playerSets,int biont,int players)
        {
            this.random = random;
            this.playerSets = playerSets;
            BIONT = biont;
            PLAYERS = players;
        }

        public void Play(int loop)
        {
            //山札
            List<int> deck = Enumerable.Range(0, 52).OrderBy(i => Guid.NewGuid()).ToList();

            //手札
            int[] hands = deck.Take(PLAYERS * 2).ToArray();
            //山札から除去
            deck.RemoveRange(0, PLAYERS * 2);

            //オールインプレイヤーのフラグ
            uint allin = 0b0000;
            //勝負する個体のID
            int[] id_list = Enumerable.Repeat(random.Next(BIONT), PLAYERS).ToArray();
            //各プレイヤーのアクション
            for (int i = 0; i < PLAYERS; i++)
            {
                //アクションを記録
                allin |= PushOrFold(playerSets[i][allin].players[id_list[i]], hands.Skip(i * 2).Take(2).ToArray()) << i;
            }

            //Console.WriteLine("allin:" + Convert.ToString(allin,2));

            for (int c = 0; c < loop; c++)
            {

                //ポット
                int pot = 0;
                //ポットにチップを投入
                for (int i = 0; i < PLAYERS; i++)
                {
                    //キャッシュ
                    Player player = playerSets[i][allin & ~(uint.MaxValue << i)].players[id_list[i]];

                    if ((allin >> i & UINT_ONE) == 0)
                    {
                        /*
                         * フォールド
                         */
                        //ポットにアンティとブラインドを投入
                        pot += BLINDS[i];
                        //利得を減らす
                        player.Play(BLINDS[i]);
                    }
                    else
                    {
                        /*
                         * オールイン
                         */
                        //ポットに限界までチップを投入
                        pot += CUP;
                        //利得を減らす
                        player.Play(CUP);
                    }

                    //Console.WriteLine("pot:" + pot);
                }

                //オールインしたプレイヤー数
                int player_allin = 0;
                //オールインフラグをコピー
                uint flag = allin;
                //プレイヤー数を数える
                while (flag != 0)
                {
                    if ((flag & UINT_ONE) != 0)
                    {
                        player_allin++;
                    }
                    flag = flag >> 1;
                }

                //リシャッフル
                deck.OrderBy(i => Guid.NewGuid()).ToList();

                int index;
                switch (player_allin)
                {
                    case 0:
                        //GBがポットを獲得
                        index = PLAYERS - 1;
                        playerSets[index][allin & ~(uint.MaxValue << index)].players[id_list[index]].Win(pot);

                        //Console.WriteLine("win:" + index + ",pot:" + pot);
                        break;
                    case 1:
                        index = 0;
                        while (true)
                        {
                            if ((allin >> index & 1) == 1)
                            {
                                //オープンレイズプレイヤーがポットを獲得
                                playerSets[index][allin & ~(uint.MaxValue << index)].players[id_list[index]].Win(pot);

                                //Console.WriteLine("win:" + index + ",pot:" + pot);
                                break;
                            }
                            index++;
                        }
                        break;
                    default:
                        //コミュニティカードを開く
                        int[] board = deck.Take(5).ToArray();
                        //山札から除去
                        //deck.RemoveRange(0, 5);

                        string message = "board:";
                        foreach (int card in board)
                        {
                            message += CardToString(card);
                        }
                        //Console.WriteLine(message);

                        //ハンドのスコア(役)
                        uint[] scores = new uint[PLAYERS];
                        //スコア計算(役判定)
                        for (int i = 0; i < scores.Length; i++)
                        {
                            if ((allin >> i & 1) == 0)
                            {
                                //フォールド
                                scores[i] = 0;
                            }
                            else
                            {
                                /* オールイン
                                 * ハンドとコミュニティカードを結合して
                                 * Aを0に変換、他のランクをそれに合わせてずらして
                                 * (役判定関数の仕様)
                                 * スコアを計算
                                 */
                                scores[i] = Holdem_RankScore.Score(
                                    hands.Skip(i * 2).Take(2).Union(board).
                                    Select(id => id % 13 == 13 - 1 ? id - (13 - 1) : id + 1).ToArray());
                            }
                        }

                        //勝者のスコア
                        uint win_score = scores.Max();
                        //勝者ID
                        int[] indexes = scores.Select((n, i) => new { value = n, index = i }).
                            Where(e => e.value == win_score).Select(e => e.index).ToArray();
                        //ポットを分割
                        pot /= indexes.Length;
                        //ポットを獲得
                        foreach (int i in indexes)
                        {
                            playerSets[i][allin & ~(uint.MaxValue << i)].players[id_list[i]].Win(pot);

                            //Console.WriteLine("win:" + i + ",pot:" + pot);
                        }

                        break;
                }
            }
        }

        private uint PushOrFold(Player player,int[] hand)
        {
            //player.ShowHandRange();

            if (player.actions[CardToRange(hand)])
            {
                //Console.WriteLine("Push");
                return 1;
            }
            //Console.WriteLine("Fold");
            return 0;
        }

        private int CardToRange(int[] hand)
        {
            int large, small;
            
            //ランクの大小に応じて値を格納
            if (hand[0] % 13 > hand[1] % 13)
            {
                large = hand[0]%13;
                small = hand[1]%13;
            }
            else
            {
                large = hand[1]%13;
                small = hand[0]%13;
            }

            string message =
                "hand:"+CardToString(hand[0])+CardToString(hand[1])+
                "(" + RankToString(large) + RankToString(small);

            if (hand[0] / 13 == hand[1] / 13)
            {
                //スーテッド
                //Console.WriteLine(message+"s)");
                return (13 - 1 - large) * 13 + 13 - 1 - small;
            }
            else
            {
                //オフスート
                //Console.WriteLine(message + "o)");
                return (13 - 1 - small) * 13 + 13 - 1 - large;
            }
        }

        private string RankToString(int rank)
        {
            rank += 2;

            switch (rank)
            {
                case 14:
                    return "A";
                case 13:
                    return "K";
                case 12:
                    return "Q";
                case 11:
                    return "J";
                case 10:
                    return "T";
                default:
                    return rank.ToString();
            }
        }

        private string CardToSuit(int card)
        {
            switch (card / 13)
            {
                case 0:
                    return "c";
                case 1:
                    return "d";
                case 2:
                    return "h";
                case 3:
                    return "s";
                default:
                    return "-";
            }
        }

        private string CardToString(int card)
        {
            return RankToString(card % 13) + CardToSuit(card);
        }
    }
}
