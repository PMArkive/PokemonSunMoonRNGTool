using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SMHatchingRNGTool
{
    public partial class Form1 : Form
    {

        #region deta
        private readonly string[] natures =
        {
            "がんばりや", "さみしがり", "ゆうかん", "いじっぱり",
            "やんちゃ", "ずぶとい", "すなお", "のんき", "わんぱく",
            "のうてんき", "おくびょう", "せっかち", "まじめ", "ようき",
            "むじゃき", "ひかえめ", "おっとり", "れいせい", "てれや",
            "うっかりや", "おだやか", "おとなしい",
            "なまいき", "しんちょう", "きまぐれ"
        };


        private readonly object[,] mezapa =
        {
            {25, "指定なし"},
            {0, "格闘"},
            {1, "飛行"},
            {2, "毒"},
            {3, "地面"},
            {4, "岩"},
            {5, "虫"},
            {6, "ゴースト"},
            {7, "鋼"},
            {8, "炎"},
            {9, "水"},
            {10, "草"},
            {11, "電気"},
            {12, "エスパー"},
            {13, "氷"},
            {14, "ドラゴン"},
            {15, "悪"},
        };
        #endregion
        
        private int[] other_tsv = new int[0];
        private readonly string[] row_iden = { "H", "A", "B", "C", "D", "S" };

        public Form1()
        {
            InitializeComponent();
        }

        private static bool IVcheck(int[] IV, int[] IVup, int[] IVlow)
        {
            for (int i = 0; i < 6; i++)
            {
                if (IVlow[i] > IV[i] || IV[i] > IVup[i]) return false;
            }
            return true;
        }

        private void k_search_Click(object sender, EventArgs e)
        {
            if (s_min.Value > s_max.Value)
                Error("消費数が 下限 ＞上限 になっています。");
            else if (IVlow1.Value > IVup1.Value)
                Error("Hの個体値が 下限 ＞上限 になっています。");
            else if (IVlow2.Value > IVup2.Value)
                Error("Aの個体値が 下限 ＞上限 になっています。");
            else if (IVlow3.Value > IVup3.Value)
                Error("Bの個体値が 下限 ＞上限 になっています。");
            else if (IVlow4.Value > IVup4.Value)
                Error("Cの個体値が 下限 ＞上限 になっています。");
            else if (IVlow5.Value > IVup5.Value)
                Error("Dの個体値が 下限 ＞上限 になっています。");
            else if (IVlow6.Value > IVup6.Value)
                Error("Sの個体値が 下限 ＞上限 になっています。");
            else if (0 > TSV.Value || TSV.Value > 4095)
                Error("TSVの上限下限が閾値を超えています。");
            else if (sex_ratio.SelectedIndex == 6 && !(post_ditto.Checked || pre_ditto.Checked))
                Error("無性別ポケモンに対し、メタモンが選択されていません。");
            else
                kotai_search();
        }

        private void kotai_search()
        {
            #region 宣言

            int min = (int)s_min.Value;
            int max = (int)s_max.Value;
            int u_Type = (int)mezapa[mezapaType.SelectedIndex, 0];
            string u_ability = ability.Text;
            string u_sex = sex.Text;
            string u_ball = ball.Text;
            uint psv;

            #endregion

            #region 遺伝

            var iden_loop = pre_Items.Text == "赤い糸" || post_Items.Text == "赤い糸" ? 5 : 3;

            string[] iden_oya_box = new string[iden_loop];
            uint[] iden_box = new uint[iden_loop];

            #endregion

            #region stats

            int[] IVup = { (int)IVup1.Value, (int)IVup2.Value, (int)IVup3.Value, (int)IVup4.Value, (int)IVup5.Value, (int)IVup6.Value, };
            int[] IVlow = { (int)IVlow1.Value, (int)IVlow2.Value, (int)IVlow3.Value, (int)IVlow4.Value, (int)IVlow5.Value, (int)IVlow6.Value, };
            int[] pre_parent = { (int)pre_parent1.Value, (int)pre_parent2.Value, (int)pre_parent3.Value, (int)pre_parent4.Value, (int)pre_parent5.Value, (int)pre_parent6.Value, };
            int[] post_parent = { (int)post_parent1.Value, (int)post_parent2.Value, (int)post_parent3.Value, (int)post_parent4.Value, (int)post_parent5.Value, (int)post_parent6.Value, };
            uint[] st =
            {
                (uint)status0.Value,
                (uint)status1.Value,
                (uint)status2.Value,
                (uint)status3.Value,
            };

            uint tsv = (uint)TSV.Value;
            #endregion

            #region 性別閾値
            int sex_threshold = 0;
            switch (sex_ratio.SelectedIndex)
            {
                case 0: sex_threshold = 126; break;
                case 1: sex_threshold = 31; break;
                case 2: sex_threshold = 63; break;
                case 3: sex_threshold = 189; break;
                case 4: sex_threshold = 0; break;
                case 5: sex_threshold = 252; break;
            }
            #endregion

            uint[] status = { st[0], st[1], st[2], st[3] };
            var tiny = new TinyMT(status, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));

            List<DataGridViewRow> list = new List<DataGridViewRow>();
            k_dataGridView.Rows.Clear();

            for (int i = 0; i <= max; i++)
            {
                var shiny_flag = false;
                //statusの更新
                for (int j = 0; j <= 3; j++) st[j] = tiny.status[j];

                var r = tiny.temper();
                var seed = string.Join(",", tiny.status.Select(v => v.ToString("X8")).Reverse());
                //生の乱数列からの性別と遺伝箇所
                var row_sex = r % 252 < sex_threshold ? "♀" : "♂";
                var row_iden_oya = r % 2 == 0 ? "先" : "後";

                //計算
                int count;
                uint pid;
                uint encryption_key;
                string p_ability;
                string p_sex;
                string p_nature;
                string p_ball;
                int[] IV;
                cal(st, out IV, out iden_box, out iden_oya_box, out p_sex, out p_ability, out p_nature, out pid, out encryption_key, out count, out p_ball);

                for (int j = 0; j < iden_loop; j++)
                {
                    int value = (int)iden_box[j];
                    IV[value] = iden_oya_box[j] == "先" ? pre_parent[value] : post_parent[value];
                }

                var HID = pid >> 16;
                var LID = pid & 0xFFFF;
                psv = (HID ^ LID) / 0x10;
                var true_psv = International.Checked || omamori.Checked ? psv.ToString("d") : "-";
                if (!(International.Checked || omamori.Checked) && shiny.Checked) goto ExitIF;
                //ここで弾く
                if (!Invalid_Refine.Checked)
                {
                    if (!other_TSV.Checked)
                    {
                        if (shiny.Checked && psv != tsv) goto ExitIF;
                        if (psv == tsv) shiny_flag = true;
                    }
                    else
                    {
                        var for_flag = false;
                        if (International.Checked || omamori.Checked)
                        {
                            if (other_tsv.Any(item => psv == item))
                            {
                                for_flag = true;
                                shiny_flag = true;
                            }
                        }
                        if(!for_flag) goto ExitIF;
                    }
                    if (!IVcheck(IV, IVup, IVlow)) goto ExitIF;
                    if (u_Type != 25)
                    {
                        if (!mezapa_check(IV, u_Type)) goto ExitIF;
                    }
                    if (u_ability != "指定なし")
                    {
                        if (u_ability != p_ability) goto ExitIF;
                    }
                    if (u_sex != "指定なし")
                    {
                        if (u_sex != p_sex) goto ExitIF;
                    }
                    if (u_ball != "指定なし")
                    {
                        if (u_ball != p_ball) goto ExitIF;
                    }
                }

                if (pre_Items.Text == "変わらず" || post_Items.Text == "変わらず") p_nature = "変わらず";
                string true_pid = International.Checked || omamori.Checked ? pid.ToString("X8") : "仮性格値";

                if (i >= min)
                {
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(k_dataGridView);
                    row.SetValues(i, seed, IV[0], IV[1], IV[2], IV[3], IV[4], IV[5], p_sex, p_ability, p_nature, true_pid, encryption_key.ToString("X8"), count, true_psv, r.ToString("X8"), (r % 32).ToString("d"), row_iden[r % 6], row_iden_oya, natures[r % 25], row_sex);

                    for (int k = 0; k < iden_loop; k++)
                    {
                        if (pre.ForeColor == Color.DodgerBlue)
                        {
                            row.Cells[2 + (int)iden_box[k]].Style.ForeColor = iden_oya_box[k] == "先" ? Color.DodgerBlue : Color.Red;
                        }
                        else
                        {
                            row.Cells[2 + (int)iden_box[k]].Style.ForeColor = iden_oya_box[k] == "先" ? Color.Red : Color.DodgerBlue;
                        }

                    }
                    if (shiny_flag)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCyan;
                    }
                    list.Add(row);
                }
            ExitIF:;

                tiny.nextState();
            }

            k_dataGridView.Rows.AddRange(list.ToArray());
            k_dataGridView.CurrentCell = null;
        }

        private void List_search_Click(object sender, EventArgs e)
        {
            if (s_min.Value > s_max.Value)
                Error("消費数が 下限 ＞上限 になっています。");
            else if (IVlow1.Value > IVup1.Value)
                Error("Hの個体値が 下限 ＞上限 になっています。");
            else if (IVlow2.Value > IVup2.Value)
                Error("Aの個体値が 下限 ＞上限 になっています。");
            else if (IVlow3.Value > IVup3.Value)
                Error("Bの個体値が 下限 ＞上限 になっています。");
            else if (IVlow4.Value > IVup4.Value)
                Error("Cの個体値が 下限 ＞上限 になっています。");
            else if (IVlow5.Value > IVup5.Value)
                Error("Dの個体値が 下限 ＞上限 になっています。");
            else if (IVlow6.Value > IVup6.Value)
                Error("Sの個体値が 下限 ＞上限 になっています。");
            else if (0 > TSV.Value || TSV.Value > 4095)
                Error("TSVの上限下限が閾値を超えています。");
            else if (sex_ratio.SelectedIndex == 6 && !(post_ditto.Checked || pre_ditto.Checked))
                Error("無性別ポケモンに対し、メタモンが選択されていません。");
            else
                FukaList_search();
        }

        private void FukaList_search()
        {
            #region 宣言
            int min = (int)n_min.Value;
            int max = (int)n_max.Value;
            uint pid = 0x0;
            int count = 0, pre_count = 0;

            int International_loop = 0;
            int omamori_loop = 0;
            if (International.Checked) International_loop = 6;
            if (omamori.Checked) omamori_loop = 2;
            #endregion

            #region 遺伝
            var iden_loop = pre_Items.Text == "赤い糸" || post_Items.Text == "赤い糸" ? 5 : 3;

            string[] iden_oya_box = new string[iden_loop];
            uint[] iden_box = new uint[iden_loop];
            string p_ability = "";
            string p_sex = "", p_ball = "";

            #endregion

            #region stats
            int[] IV = new int[6];
            int[] IVup = new int[6];
            int[] IVlow = new int[6];
            int[] pre_parent = new int[6];
            int[] post_parent = new int[6];
            uint[] st = new uint[4];

            IVup[0] = Convert.ToInt32(IVup1.Text);
            IVup[1] = Convert.ToInt32(IVup2.Text);
            IVup[2] = Convert.ToInt32(IVup3.Text);
            IVup[3] = Convert.ToInt32(IVup4.Text);
            IVup[4] = Convert.ToInt32(IVup5.Text);
            IVup[5] = Convert.ToInt32(IVup6.Text);
            IVlow[0] = Convert.ToInt32(IVlow1.Text);
            IVlow[1] = Convert.ToInt32(IVlow2.Text);
            IVlow[2] = Convert.ToInt32(IVlow3.Text);
            IVlow[3] = Convert.ToInt32(IVlow4.Text);
            IVlow[4] = Convert.ToInt32(IVlow5.Text);
            IVlow[5] = Convert.ToInt32(IVlow6.Text);

            pre_parent[0] = Convert.ToInt32(pre_parent1.Text);
            pre_parent[1] = Convert.ToInt32(pre_parent2.Text);
            pre_parent[2] = Convert.ToInt32(pre_parent3.Text);
            pre_parent[3] = Convert.ToInt32(pre_parent4.Text);
            pre_parent[4] = Convert.ToInt32(pre_parent5.Text);
            pre_parent[5] = Convert.ToInt32(pre_parent6.Text);
            post_parent[0] = Convert.ToInt32(post_parent1.Text);
            post_parent[1] = Convert.ToInt32(post_parent2.Text);
            post_parent[2] = Convert.ToInt32(post_parent3.Text);
            post_parent[3] = Convert.ToInt32(post_parent4.Text);
            post_parent[4] = Convert.ToInt32(post_parent5.Text);
            post_parent[5] = Convert.ToInt32(post_parent6.Text);

            st[0] = (uint)L_status0a.Value;
            st[1] = (uint)L_status1a.Value;
            st[2] = (uint)L_status2a.Value;
            st[3] = (uint)L_status3a.Value;

            uint tsv = (uint)TSV.Value;
            #endregion

            #region 性別閾値
            int sex_threshold = 0;
            switch (sex_ratio.SelectedIndex)
            {
                case 0: sex_threshold = 126; break;
                case 1: sex_threshold = 31; break;
                case 2: sex_threshold = 63; break;
                case 3: sex_threshold = 189; break;
                case 4: sex_threshold = 0; break;
                case 5: sex_threshold = 252; break;
                case 6: sex_threshold = 300; break;
            }
            #endregion

            List<DataGridViewRow> list = new List<DataGridViewRow>();
            L_dataGridView.Rows.Clear();

            uint[] status = { st[0], st[1], st[2], st[3] };
            TinyMT tiny = new TinyMT(status, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));

            for (int i = 1; i <= max; i++)
            {
                var shiny_flag = false;
                var seed = string.Join(",", tiny.status.Select(v => v.ToString("X8")).Reverse());
                //最初の消費
                var r = tiny.temper();
                tiny.nextState();
                count++;

                //性別
                if (sex_ratio.SelectedIndex < 4)
                {
                    r = tiny.temper();
                    p_sex = r % 252 < sex_threshold ? "♀" : "♂";
                    tiny.nextState();
                    count++;
                }
                if (sex_ratio.SelectedIndex > 3)
                {
                    p_sex = r % 252 < sex_threshold ? "♀" : "♂";
                }
                if (sex_threshold == 300) p_sex = "-";

                //性格
                r = tiny.temper();
                var p_nature = natures[r % 25];
                tiny.nextState();
                count++;

                //両親変わらず
                if (pre_Items.Text == "変わらず" & post_Items.Text == "変わらず")
                {
                    r = tiny.temper();
                    tiny.nextState();
                    count++;
                }

                //特性
                r = tiny.temper();
                int value = (int)(r % 100);
                if (!(post_ditto.Checked || pre_ditto.Checked))
                {
                    if (post_ability.Text == "1")
                    {
                        p_ability = value < 80 ? "1" : "2";
                    }
                    if (post_ability.Text == "2")
                    {
                        p_ability = value < 20 ? "1" : "2";
                    }
                    if (post_ability.Text == "夢")
                    {
                        if (value < 20) p_ability = "1";
                        else if (value < 40) p_ability = "2";
                        else p_ability = "夢";
                    }
                }
                else
                {
                    if (pre_ditto.Checked)
                    {
                        if (post_ability.Text == "1")
                        {
                            p_ability = value < 80 ? "1" : "2";
                        }
                        if (post_ability.Text == "2")
                        {
                            p_ability = value < 20 ? "1" : "2";
                        }
                        if (post_ability.Text == "夢")
                        {
                            if (value < 20) p_ability = "1";
                            else if (value < 40) p_ability = "2";
                            else p_ability = "夢";
                        }
                    }
                    else
                    {
                        if (pre_ability.Text == "1")
                        {
                            p_ability = value < 80 ? "1" : "2";
                        }
                        if (pre_ability.Text == "2")
                        {
                            p_ability = value < 20 ? "1" : "2";
                        }
                        if (pre_ability.Text == "夢")
                        {
                            if (value < 20) p_ability = "1";
                            else if (value < 40) p_ability = "2";
                            else p_ability = "夢";
                        }
                    }
                }
                tiny.nextState();
                count++;

                //最初の遺伝箇所
                int iden_count = 0;

                while (true)
                {
                    var flag = true;
                    r = tiny.temper();
                    for (int k = 0; k < iden_count; k++)
                    {
                        if (iden_box[k] != r%6)
                            continue;
                        r = tiny.temper();
                        tiny.nextState();
                        count++;
                        flag = false;
                        break;
                    }
                    if (flag)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            r = tiny.temper();
                            string iden_oya = r % 2 == 0 ? "先" : "後";
                            tiny.nextState();

                            if (j == 0)
                            {
                                iden_box[iden_count] = r % 6;
                            }
                            else
                            {
                                iden_oya_box[iden_count] = iden_oya;
                                iden_count++;
                            }
                            count++;
                        }
                    }

                    if (iden_count == iden_loop) break;
                }

                //基礎個体値
                for (int j = 0; j < 6; j++)
                {
                    r = tiny.temper();
                    IV[j] = (int)(r % 32);
                    tiny.nextState();
                    count++;
                }

                //暗号化定数
                r = tiny.temper();
                var encryption_key = r;
                tiny.nextState();
                count++;

                //性格値判定    
                uint LID;
                uint HID;

                if (!(International.Checked || omamori.Checked))
                {
                    r = tiny.temper();
                    pid = r;
                }
                else
                {
                    for (int j = 0; j < (omamori_loop + International_loop); j++)
                    {
                        r = tiny.temper();
                        pid = r;

                        HID = pid >> 16;
                        LID = pid & 0xFFFF;
                        tiny.nextState();
                        count++;
                        if (!(L_TSV_shiny.Checked & ((HID ^ LID)>>4 == tsv)))
                            continue;
                        shiny_flag = true;
                        break;
                    }
                }

                //ボール消費
                
                if (!(post_ditto.Checked || pre_ditto.Checked || Heterogeneity.Checked))
                {
                    r = tiny.temper();
                    p_ball = r % 100 >= 50 ? "先親" : "後親";
                    tiny.nextState();
                    count++;
                }

                //何かの消費
                tiny.nextState();
                count++;

                if (i >= min)
                {
                    for (int j = 0; j < iden_loop; j++)
                    {
                        value = (int)iden_box[j];
                        IV[value] = iden_oya_box[j] == "先" ? pre_parent[value] : post_parent[value];
                    }

                    HID = pid >> 16;
                    LID = pid & 0xFFFF;
                    var psv = (HID ^ LID) >> 4;
                    var true_psv = International.Checked || omamori.Checked ? psv.ToString("d") : "-";
                    var true_pid = International.Checked || omamori.Checked ? pid.ToString("X8") : "仮性格値";

                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(L_dataGridView);
                    row.SetValues(i, pre_count, seed, IV[0], IV[1], IV[2], IV[3], IV[4], IV[5], p_sex, p_ability, p_nature, true_pid, true_psv, encryption_key.ToString("X8"));

                    for (int k = 0; k < iden_loop; k++)
                    {
                        if (pre.ForeColor == Color.DodgerBlue)
                        {
                            row.Cells[3 + (int)iden_box[k]].Style.ForeColor = iden_oya_box[k] == "先" ? Color.DodgerBlue : Color.Red;
                        }
                        else
                        {
                            row.Cells[3 + (int)iden_box[k]].Style.ForeColor = iden_oya_box[k] == "先" ? Color.Red : Color.DodgerBlue;
                        }

                    }
                    if (shiny_flag)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCyan;
                    }
                    list.Add(row);
                }
                pre_count = count;
            }

            L_dataGridView.Rows.AddRange(list.ToArray());
            L_dataGridView.CurrentCell = null;
        }

        private void cal(uint[] st, out int[] IV, out uint[] iden_box, out string[] iden_oya_box, out string p_sex, out string p_ability, out string p_nature, out uint pid, out uint encryption_key, out int count, out string p_ball)
        {
            #region 宣言その他もろもろ

            count = 0;
            p_ability = "";
            p_nature = "";
            p_sex = "";
            p_ball = "";
            pid = 0x0;
            encryption_key = 0x0;
            IV = new int[] { 0, 0, 0, 0, 0, 0 };

            int International_loop = 0;
            int omamori_loop = 0;
            if (International.Checked) International_loop = 6;
            if (omamori.Checked) omamori_loop = 2;
            #endregion

            uint[] status = { st[0], st[1], st[2], st[3] };
            TinyMT tiny = new TinyMT(status, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));

            #region 性別閾値
            int sex_threshold = 0;
            if (sex_ratio.SelectedIndex == 0)
            {
                sex_threshold = 126;
            }
            else if (sex_ratio.SelectedIndex == 1)
            {
                sex_threshold = 32;
            }
            else if (sex_ratio.SelectedIndex == 2)
            {
                sex_threshold = 63;
            }
            else if (sex_ratio.SelectedIndex == 3)
            {
                sex_threshold = 189;
            }
            else if (sex_ratio.SelectedIndex == 4)
            {
                sex_threshold = 0;
            }
            else if (sex_ratio.SelectedIndex == 5)
            {
                sex_threshold = 252;
            }
            else if (sex_ratio.SelectedIndex == 6)
            {
                sex_threshold = 300;
            }
            #endregion

            #region 遺伝箇所
            int iden_loop = 0;
            if (pre_Items.Text == "赤い糸" || post_Items.Text == "赤い糸") iden_loop = 5;
            else iden_loop = 3;

            if (iden_loop == 3)
            {
                iden_box = new uint[] { 0, 0, 0 };
                iden_oya_box = new string[] { "", "", "" };
            }
            else
            {
                iden_box = new uint[] { 0, 0, 0, 0, 0 };
                iden_oya_box = new string[] { "", "", "", "", "" };
            }
            #endregion

            //最初の消費
            var r = tiny.temper();
            tiny.nextState();
            count++;

            //性別
            if (sex_ratio.SelectedIndex < 4) 
            {
                r = tiny.temper();
                p_sex = (r % 252 < sex_threshold) ? "♀" : "♂";
                tiny.nextState();
                count++;
            }
            if (sex_ratio.SelectedIndex > 3) 
            {
                p_sex = (r % 252 < sex_threshold) ? "♀" : "♂";
            }
            if (sex_threshold == 300) p_sex = "-";

            //性格
            r = tiny.temper();
            p_nature = natures[r % 25];
            tiny.nextState();
            count++;

            //両親変わらず
            if (pre_Items.Text == "変わらず" & post_Items.Text == "変わらず")
            {
                r = tiny.temper();
                tiny.nextState();
                count++;
            }


            //特性
            r = tiny.temper();
            int value = (int)(r % 100);
            if (!(post_ditto.Checked || pre_ditto.Checked))
            {
                if (post_ability.Text == "1")
                {
                    p_ability = value < 80 ? "1" : "2";
                }
                if (post_ability.Text == "2")
                {
                    p_ability = value < 20 ? "1" : "2";
                }
                if (post_ability.Text == "夢")
                {
                    if (value < 20) p_ability = "1";
                    else if (value < 40) p_ability = "2";
                    else p_ability = "夢";
                }
            }
            else
            {
                if (pre_ditto.Checked)
                {
                    if (post_ability.Text == "1")
                    {
                        p_ability = value < 80 ? "1" : "2";
                    }
                    if (post_ability.Text == "2")
                    {
                        p_ability = value < 20 ? "1" : "2";
                    }
                    if (post_ability.Text == "夢")
                    {
                        if (value < 20) p_ability = "1";
                        else if (value < 40) p_ability = "2";
                        else p_ability = "夢";
                    }
                }
                else
                {
                    if (pre_ability.Text == "1")
                    {
                        p_ability = value < 80 ? "1" : "2";
                    }
                    if (pre_ability.Text == "2")
                    {
                        p_ability = value < 20 ? "1" : "2";
                    }
                    if (pre_ability.Text == "夢")
                    {
                        if (value < 20) p_ability = "1";
                        else if (value < 40) p_ability = "2";
                        else p_ability = "夢";
                    }
                }
            }
            tiny.nextState();
            count++;

            //最初の遺伝箇所
            int iden_count = 0;

            while (true)
            {
                var flag = true;
                r = tiny.temper();
                for (int k = 0; k < iden_count; k++)
                {
                    if (iden_box[k] != r%6)
                        continue;

                    r = tiny.temper();
                    tiny.nextState();
                    count++;
                    flag = false;
                    break;
                }
                if (flag)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        r = tiny.temper();
                        string iden_oya = (r % 2 == 0) ? "先" : "後";
                        tiny.nextState();

                        if (j == 0)
                        {
                            iden_box[iden_count] = r % 6;
                        }
                        else
                        {
                            iden_oya_box[iden_count] = iden_oya;
                            iden_count++;
                        }
                        count++;
                    }
                }

                if (iden_count == iden_loop) break;
            }

            //基礎個体値
            for (int j = 0; j < 6; j++)
            {
                r = tiny.temper();
                IV[j] = (int)(r % 32);
                tiny.nextState();
                count++;
            }
            //暗号化定数

            r = tiny.temper();
            encryption_key = r;
            tiny.nextState();
            count++;

            //性格値判定
            uint tsv = (uint)TSV.Value;

            if (!(International.Checked || omamori.Checked))
            {
                r = tiny.temper();
                pid = r;
            }
            else
            {
                int loopCount = omamori_loop + International_loop;
                for (int j = 0; j < loopCount; j++)
                {
                    r = tiny.temper();
                    pid = r;

                    var HID = pid >> 16;
                    var LID = pid & 0xFFFF;
                    tiny.nextState();
                    count++;
                    if (other_TSV.Checked)
                        continue;

                    if ((shiny.Checked || k_TSV_shiny.Checked) && (HID ^ LID) >> 4 == tsv)
                        break;
                }
            }
            //ボール消費
            if (!(post_ditto.Checked || pre_ditto.Checked　|| Heterogeneity.Checked))
            {
                r = tiny.temper();
                p_ball = r % 100 >= 50 ? "先親" : "後親";
                tiny.nextState();
                count++;
            }

            //something
            r = tiny.temper();
            tiny.nextState();
            count++;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            k_dataGridView.DefaultCellStyle.Font = new Font("Consolas", 9);
            k_dataGridView.Columns[20].DefaultCellStyle.Font = new Font("ＭＳ ゴシック", 9);
            k_dataGridView.Columns[8].DefaultCellStyle.Font = new Font("ＭＳ ゴシック", 9);
            L_dataGridView.DefaultCellStyle.Font = new Font("Consolas", 9);
            L_dataGridView.Columns[9].DefaultCellStyle.Font = new Font("ＭＳ ゴシック", 9);

            Type dgvtype = typeof(DataGridView);
            System.Reflection.PropertyInfo dgvPropertyInfo = dgvtype.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            dgvPropertyInfo.SetValue(k_dataGridView, true, null);
            dgvPropertyInfo.SetValue(L_dataGridView, true, null);

            for (int i = 0; i < mezapa.GetLength(0); i++)
            {
                mezapaType.Items.Add(mezapa[i, 1]);
            }

            for (int co = 15; co < 20; co++)
            {
                k_dataGridView.Columns[co].DefaultCellStyle.BackColor = Color.Gainsboro;
            }

            pre_Items.SelectedIndex = 0;
            post_Items.SelectedIndex = 0;
            mezapaType.SelectedIndex = 0;
            ability.SelectedIndex = 0;
            pre_ability.SelectedIndex = 0;
            post_ability.SelectedIndex = 0;
            sex.SelectedIndex = 0;
            sex_ratio.SelectedIndex = 0;
            ball.SelectedIndex = 0;
            
            loadConfig();
            other_TSV.Enabled = loadTSV();
        }

        private void loadConfig()
        {
            if (File.Exists("config.txt"))
            {
                string[] list = File.ReadAllLines("config.txt");
                if (list.Length != 5)
                    return;

                string st3 = list[0];
                string st2 = list[1];
                string st1 = list[2];
                string st0 = list[3];
                string tsvstr = list[4];
                ushort tsv;
                uint s3, s2, s1, s0;

                
                if (!uint.TryParse(st0, out s0))
                    Error("status[0]に不正な値が含まれています。");
                else if (!uint.TryParse(st1, out s1))
                    Error("status[1]に不正な値が含まれています。");
                else if (!uint.TryParse(st2, out s2))
                    Error("status[2]に不正な値が含まれています。");
                else if (!uint.TryParse(st3, out s3))
                    Error("status[3]に不正な値が含まれています。");
                else if (!ushort.TryParse(tsvstr, out tsv))
                    Error("TSVに不正な値が含まれています。");
                else if (tsv > 4095)
                    Error("TSVの上限下限が閾値を超えています。");
                else
                {
                    status3.Value = L_status3a.Value = s3;
                    status2.Value = L_status2a.Value = s2;
                    status1.Value = L_status1a.Value = s1;
                    status0.Value = L_status0a.Value = s0;
                    TSV.Value = tsv;
                }
            }
            else
            {
                Error("config.txtが存在しません。\nデフォルトの設定を読み込みます。");
            }
        }
        private bool loadTSV()
        {
            if (!File.Exists("TSV.txt"))
                return false;

            //test.txtを1行ずつ読み込んでいき、末端(何もない行)までwhile文で繰り返す
            string[] list = File.ReadAllLines("TSV.txt");
            int[] tsvs = new int[list.Length];

            for (int i = 0; i < list.Length; i++)
            {
                var v = list[i];
                int val;
                if (!int.TryParse(v, out val)) // not number
                {
                    string message = $"{i + 1}番目のTSV:{v}に不正な値が含まれています。";
                    Error(message);
                    return false;
                }
                if (0 > val || val > 4095)
                {
                    string message = $"{i + 1}番目のTSV:{v}が上限下限が閾値を超えています。";
                    Error(message);
                    return false;
                }
                tsvs[i] = val;
            }

            other_tsv = tsvs;
            return true;
        }

        private static bool mezapa_check(int[] IV, int u_Type)
        {
            var val = 15 * ((IV[0] & 1) + 2 * (IV[1] & 1) + 4 * (IV[2] & 1) + 8 * (IV[5] & 1) + 16 * (IV[3] & 1) + 32 * (IV[4] & 1)) / 63;
            return u_Type == val;
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            k_dataGridView.SelectAll();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(k_dataGridView.GetClipboardContent());
            }
            catch (ArgumentNullException)
            {
                Error("選択されていません");
            }
        }

        private void NumericUpDown_Enter(object sender, EventArgs e)
        {
            NumericUpDown NumericUpDown = sender as NumericUpDown;
            NumericUpDown.Select(0, NumericUpDown.Text.Length);
        }

        private void NumericUpDown_Check(object sender, CancelEventArgs e)
        {
            NumericUpDown NumericUpDown = sender as NumericUpDown;
            Control ctrl = NumericUpDown;
            if (ctrl == null)
                return;
            if (!string.IsNullOrEmpty(NumericUpDown.Text))
                return;
            foreach (var box in ((NumericUpDown)ctrl).Controls.OfType<TextBox>())
            {
                // クリップボードへコピー
                box.Undo();
                break;
            }
        }
        private void Send2List(object sender, EventArgs e)
        {
            try
            {
                var seed = (string)k_dataGridView.CurrentRow.Cells[1].Value;
                string[] Data = seed.Split(',');
                L_status3a.Value = Convert.ToUInt32(Data[0], 16);
                L_status2a.Value = Convert.ToUInt32(Data[1], 16);
                L_status1a.Value = Convert.ToUInt32(Data[2], 16);
                L_status0a.Value = Convert.ToUInt32(Data[3], 16);
            }
            catch (NullReferenceException)
            {
                Error("検索結果からseedを選択して下さい");
            }
        }

        private void L_copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(L_dataGridView.GetClipboardContent());
            }
            catch (ArgumentNullException)
            {
                Error("選択されていません");
            }
        }

        private void L_SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            L_dataGridView.SelectAll();
        }

        private void Change_ditto(object sender, EventArgs e)
        {
            if ((sender as CheckBox)?.Checked ?? false)
                (sender == post_ditto ? pre_ditto : post_ditto).Checked = false;
        }

        private void Change_color(object sender, EventArgs e)
        {
            // Invert Colors
            if (pre.ForeColor == Color.Red)
            {
                pre.ForeColor = Color.DodgerBlue;
                post.ForeColor = Color.Red;
            }
            else
            {
                pre.ForeColor = Color.Red;
                post.ForeColor = Color.DodgerBlue;
            }
            pre_parent1.ForeColor = pre.ForeColor;
            pre_parent2.ForeColor = pre.ForeColor;
            pre_parent3.ForeColor = pre.ForeColor;
            pre_parent4.ForeColor = pre.ForeColor;
            pre_parent5.ForeColor = pre.ForeColor;
            pre_parent6.ForeColor = pre.ForeColor;
            
            post_parent1.ForeColor = post.ForeColor;
            post_parent2.ForeColor = post.ForeColor;
            post_parent3.ForeColor = post.ForeColor;
            post_parent4.ForeColor = post.ForeColor;
            post_parent5.ForeColor = post.ForeColor;
            post_parent6.ForeColor = post.ForeColor;
        }

        private static void Error(string msg)
        {
            System.Media.SystemSounds.Exclamation.Play();
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
