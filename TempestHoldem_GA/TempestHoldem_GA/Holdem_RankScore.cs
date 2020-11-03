using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TempestHoldem_GA
{
    public static class Holdem_RankScore
    {
        /* 
         * n枚の札(int[])を入力して役の点数(uint)に変換して出力
         * n<5の場合はストフラ,フルハウス,フラッシュ,ストレートを判定しない(例:AsKsQsJsはAのハイカードとする)
         * 
         * カードの表現は商でスート(スペード>ハート>ダイヤ>クラブ)
         * 余りでランク(例:ダイヤA=1*13+0=13)
         * 
         * 点数の表現は
         * 上位4bitで役(9つの役が0～8に対応)
         * 中位14bitでメインカードのランク
         * 下位14bitでサブカード(キッカー、フルハウスの2枚側、ツーペアの弱い側)のランク
         * ストフラ,フラッシュ,ストレート,ハイカードの場合は全てのカードをサブカードとして扱う
         * 
         */

        //役のスコア
        public const uint STFL = 0x80000000;
        public const uint QUADS = 0x70000000;
        public const uint FULL = 0x60000000;
        public const uint FLUSH = 0x50000000;
        public const uint STRAIGHT = 0x40000000;
        public const uint THREE = 0x30000000;
        public const uint TWO = 0x20000000;
        public const uint ONE = 0x10000000;
        public const uint HIGHCARD = 0x00000000;

        //役、メインカード、サブカードをスコアに変換するときのビットシフト数
        private const int shift_hand = 14 * 2;
        private const int shift_main = 14;
        private const int shift_sub = 0;

        public static uint Score(int[] cards)
        {
            //スコア
            uint score = 0;
            //ペア用のカウンタ
            int[] rankCount = CountRank(cards);
            //フラッシュ用のカウンタ
            int[] suitCount = CountSuit(cards);

            //ストレートのスコアの一時格納用
            uint straight = Straight(cards);
            //ストレート
            if (straight > 0)
            {
                score = UintMax(STRAIGHT | straight, score);
            }

            //フラッシュ＆ストレートフラッシュ
            for (int i = 0; i < suitCount.Length; i++)
            {
                //フラッシュの条件を満たす
                if (suitCount[i] >= 5)
                {
                    //条件を満たすスートのカードを抽出
                    int[] suited = cards.Where(id => id / 13 == i).ToArray();

                    //フラッシュとしてスコアを計算
                    score = UintMax(FLUSH | HighCard(suited,5), score);

                    //フラッシュのスートのカードでストレートのスコアを取得
                    straight = Straight(suited);

                    //ストレートが成立している
                    if (straight > 0)
                    {
                        //ストレートフラッシュとしてスコアを計算
                        score = UintMax(STFL | straight, score);
                    }
                }
            }

            //4カード
            for (int i = 0; i < rankCount.Length; i++)
            {
                //同じランクのカードが4枚以上
                if (rankCount[i] >= 4)
                {
                    //スコアのメインカード部分を計算
                    uint score_main = (uint)0x0001 << (shift_main + i);

                    //キッカーを取得
                    int[] kicker = cards.Where(id => id % 13 != i).ToArray();

                    //サブカード(キッカー)部分のスコアを計算
                    uint score_sub = HighCard(kicker,1);

                    //最終的なスコアを計算
                    score = UintMax(QUADS | score_main | score_sub, score);
                }
            }

            //3カード＆フルハウス
            for (int i = 0; i < rankCount.Length; i++)
            {
                //同じランクのカードが3枚以上
                if (rankCount[i] >= 3)
                {
                    //スコアのメインカード部分を計算
                    uint score_main = (uint)0x0001 << (shift_main + i);

                    //キッカーを取得
                    int[] kicker = cards.Where(id => id % 13 != i).ToArray();

                    //サブカード(キッカー)部分のスコアを計算
                    uint score_sub = HighCard(kicker,2);

                    //3カードのスコアを計算
                    score = UintMax(THREE | score_main | score_sub, score);

                    //フルハウス判定
                    for (int j = 0; j < rankCount.Length; j++)
                    {
                        //3カードと違うランクのカード
                        if (i != j)
                        {
                            //ペアが存在
                            if (rankCount[j] >= 2)
                            {
                                //スコアのサブカード部分を計算
                                score_sub = (uint)0x0001 << (shift_sub + j);

                                //フルハウスのスコアを計算
                                score = UintMax(FULL | score_main | score_sub, score);
                            }
                        }
                    }
                }
            }

            //1ペア＆2ペア
            for (int i = 0; i < rankCount.Length; i++)
            {
                //同じランクのカードが2枚以上
                if (rankCount[i] >= 2)
                {
                    //スコアのメインカード部分を計算
                    uint score_main = (uint)0x0001 << (shift_main + i);

                    //キッカーを取得
                    int[] kicker = cards.Where(id => id % 13 != i).ToArray();

                    //サブカード(キッカー)部分のスコアを計算
                    uint score_sub = HighCard(kicker,3);

                    //1ペアのスコアを計算
                    score = UintMax(ONE | score_main | score_sub, score);

                    //2ペア判定
                    for (int j = 0; j < rankCount.Length; j++)
                    {
                        //先に見つけた1ペアと違うランクのカード
                        if (i != j)
                        {
                            //ペアが存在
                            if (rankCount[j] >= 2)
                            {
                                //スコアのメインカード部分を更新
                                score_main |= (uint)0x0001 << (shift_main + j);

                                //キッカーを取得
                                kicker = cards.Where(id => (id % 13 != i && id % 13 != j)).ToArray();

                                //サブカード(キッカー)部分のスコアを計算
                                score_sub = HighCard(kicker,1);

                                //2ペアのスコアを計算
                                score = UintMax(TWO | score_main | score_sub, score);
                            }
                        }
                    }
                }
            }

            //ハイカード
            score = UintMax(HIGHCARD | HighCard(cards,5), score);

            //最終的なスコア
            return score;
        }

        /*
         * ストレート判定
         * 判定が真ならば上位5枚のランクのビットが立てられた下位14bit(サブカード)のスコアを返す
         * (メインカードの値は常に0)
         * 判定が偽の場合0を返す
         */
        private static uint Straight(int[] cards)
        {
            //存在する数字のフラグ
            uint flag = HighCard(cards,100);

            //ストレート判定用のマスク
            uint mask = 0x3E00;

            //マスクのbitが5つ立っている限りループ
            while (mask >= 0x001F)
            {
                //ストレート判定
                if ((flag & mask) == mask)
                {
                    return mask;
                }

                //右に1bitシフト
                mask = mask >> 1;
            }

            return 0;
        }

        /*
         * ハイカードとしてスコアに変換
         * 
         * ペアなどは考慮せず存在するカードのランクを列挙して28bitのスコアに変換して返す
         * Aは1と14どちらでもあるとみなす(両方のフラグが立つ)
         * サブカードとして扱うので中位14bitは常に0、下位14bitで表現する
         * 1が5bitを超えて存在する場合もある
         * 
         */
        public static uint HighCard(int[] cards, int effective_count)
        {
            //存在する数字のフラグ
            int flag = 0x0000;

            //存在確認
            foreach (int id in cards)
            {
                flag |= 0x0001 << (id % 13);
            }

            //A(1)のフラグが立っている
            if ((flag & 0x0001) != 0)
            {
                //A(14)のフラグを立てる
                flag |= 0x2000;
            }

            //枚数
            int count = 0;
            {
                //シフト回数
                int i = 0;
                //枚数カウント
                while (flag >> i != 0)
                {
                    if ((flag >> i & 1) == 1)
                    {
                        count++;
                    }
                    i++;
                }
            }
            {
                //有効枚数を超過しているなら有効枚数まで削る
                int i = 0;
                while (count > effective_count)
                {
                    if ((flag & 1) == 1)
                    {
                        count--;
                    }
                    flag = flag >> 1;
                    i++;
                }
                //ビットの位置を元に戻す
                flag = flag << i;
            }

            return (uint)flag;
        }

        public static int[] CountSuit(int[] cards)
        {
            //各スートの枚数
            int[] count = new int[4];

            //枚数を数える
            foreach (int id in cards)
            {
                count[id / 13]++;
            }

            return count;
        }

        private static int[] CountRank(int[] cards)
        {
            //各ランクの枚数
            int[] count = new int[14];

            //枚数を数える
            foreach (int id in cards)
            {
                count[id % 13]++;
            }

            //Aを14とする
            count[14 - 1] = count[1 - 1];
            //1は存在しないものとする
            count[0] = 0;

            return count;
        }

        //大きい方を返す(uint)
        public static uint UintMax(uint a, uint b)
        {
            if (a >= b)
            {
                return a;
            }

            return b;
        }
    }
}
