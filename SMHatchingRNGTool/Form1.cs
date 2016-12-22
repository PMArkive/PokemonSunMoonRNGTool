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


        private readonly string[] mezapa =
        {
            "指定なし",
            "格闘", "飛行", "毒", "地面", "岩",
            "虫", "ゴースト", "鋼", "炎", "水",
            "草", "電気", "エスパー", "氷", "ドラゴン",
            "悪",
        };
        #endregion
        
        private int[] other_tsv = new int[0];
        private readonly string[] row_iden = { "H", "A", "B", "C", "D", "S" };
        private readonly string[] genders = { "♂", "♀", "-" };
        private readonly string[] abilities = { "1", "2", "H" };

        public Form1()
        {
            InitializeComponent();
        }
        
        private SearchSetting getSettings()
        {
            int[] IVup = { (int)IVup1.Value, (int)IVup2.Value, (int)IVup3.Value, (int)IVup4.Value, (int)IVup5.Value, (int)IVup6.Value, };
            int[] IVlow = { (int)IVlow1.Value, (int)IVlow2.Value, (int)IVlow3.Value, (int)IVlow4.Value, (int)IVlow5.Value, (int)IVlow6.Value, };
            return new SearchSetting
            {
                Ability = ability.SelectedIndex - 1,
                Gender = sex.SelectedIndex - 1,
                HPType = mezapaType.SelectedIndex - 1,
                IVlow = IVlow,
                IVup = IVup,
                Ball = ball.SelectedIndex - 1,
                Skip = Invalid_Refine.Checked,
            };
        }
        private EggRNGSearch getRNGSettings()
        {
            int[] pre_parent = { (int)pre_parent1.Value, (int)pre_parent2.Value, (int)pre_parent3.Value, (int)pre_parent4.Value, (int)pre_parent5.Value, (int)pre_parent6.Value, };
            int[] post_parent = { (int)post_parent1.Value, (int)post_parent2.Value, (int)post_parent3.Value, (int)post_parent4.Value, (int)post_parent5.Value, (int)post_parent6.Value, };
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

            var rng = new EggRNGSearch
            {
                GenderRatio = sex_threshold,
                GenderRandom = sex_ratio.SelectedIndex < 4,
                GenderMale = sex_ratio.SelectedIndex == 4,
                GenderFemale = sex_ratio.SelectedIndex == 5,
                International = International.Checked,
                ShinyCharm = omamori.Checked,
                Heterogeneous = pre_ditto.Checked || post_ditto.Checked || Heterogeneity.Checked,
                Everstone = pre_Items.SelectedIndex == 1 || post_Items.SelectedIndex == 1,
                DestinyKnot = pre_Items.SelectedIndex == 2 || post_Items.SelectedIndex == 2,
                ParentAbility = (!post_ditto.Checked ? post_ability : pre_ability).SelectedIndex,

                TSV = (int)TSV.Value,
                pre_parent = pre_parent,
                post_parent = post_parent,
            };
            rng.Initialize();
            return rng;
        }
        private bool frameMatch(EggRNGSearch.EggRNGResult result, SearchSetting setting)
        {
            //ここで弾く
            if (setting.Skip)
                return true;

            if (!(International.Checked || omamori.Checked) && shiny.Checked)
                return false;

            if (!other_TSV.Checked)
            {
                if (shiny.Checked && !result.Shiny)
                    return false;
            }
            else
            {
                if (International.Checked || omamori.Checked)
                    result.Shiny = other_tsv.Any(item => result.PSV == item);
                if (!result.Shiny)
                    return false;
            }
            if (!setting.validIVs(result.IVs))
                return false;
            if (!setting.mezapa_check(result.IVs))
                return false;
            if (setting.Ability != -1 && setting.Ability != result.Ability)
                return false;
            if (setting.Gender != -1 && setting.Gender != result.Gender)
                return false;
            if (setting.Ball != -1 && setting.Ball != result.Ball)
                return false;
            return true;
        }
        private DataGridViewRow getRow(int i, EggRNGSearch rng, EggRNGSearch.EggRNGResult result, DataGridView dgv)
        {
            var true_psv = rng.PIDRerolls > 0 ? result.PSV.ToString("d") : "-";
            string true_pid = International.Checked || omamori.Checked ? result.PID.ToString("X8") : "仮性格値";

            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dgv);
            row.SetValues(
                i, result.FramesUsed, result.Seed128, 
                result.IVs[0], result.IVs[1], result.IVs[2], result.IVs[3], result.IVs[4], result.IVs[5],
                genders[result.Gender], rng.Everstone ? "変わらず" : abilities[result.Ability], natures[result.Nature], 
                true_pid, true_psv, result.EC.ToString("X8")
                );

            for (int k = 0; k < result.InheritStats.Length; k++)
            {
                var color = result.InheritParents[k] == 1 ? Color.Red : Color.DodgerBlue;
                row.Cells[3 + (int)result.InheritStats[k]].Style.ForeColor = color;
            }
            if (result.Shiny)
            {
                row.DefaultCellStyle.BackColor = Color.LightCyan;
            }
            return row;
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
            int min = (int)s_min.Value;
            int max = (int)s_max.Value;

            uint[] st =
            {
                (uint)status0.Value,
                (uint)status1.Value,
                (uint)status2.Value,
                (uint)status3.Value,
            };

            uint[] status = { st[0], st[1], st[2], st[3] };
            var tiny = new TinyMT(status, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));

            List<DataGridViewRow> list = new List<DataGridViewRow>();
            k_dataGridView.Rows.Clear();

            var setting = getSettings();
            var rng = getRNGSettings();

            for (int i = 1; i < min; i++)
                tiny.nextState();
            for (int i = min; i <= max; i++, tiny.nextState())
            {
                //statusの更新
                tiny.status.CopyTo(st, 0);
                EggRNGSearch.EggRNGResult result = rng.Generate(st);

                if (!frameMatch(result, setting))
                    continue;
                list.Add(getRow(i, rng, result, k_dataGridView));
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
            int min = (int)s_min.Value;
            int max = (int)s_max.Value;

            uint[] st =
            {
                (uint)L_status0a.Value,
                (uint)L_status1a.Value,
                (uint)L_status2a.Value,
                (uint)L_status3a.Value,
            };

            uint[] status = { st[0], st[1], st[2], st[3] };
            var tiny = new TinyMT(status, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));

            List<DataGridViewRow> list = new List<DataGridViewRow>();
            L_dataGridView.Rows.Clear();
            
            var rng = getRNGSettings();
            int frameCount = 0;
            for (int i = 1; i <= max; i++)
            {
                //statusの更新
                tiny.status.CopyTo(st, 0);
                EggRNGSearch.EggRNGResult result = rng.Generate(st);
                int ctr = result.FramesUsed;
                result.FramesUsed = frameCount;
                frameCount += ctr;

                if (i >= min)
                {
                    var row = getRow(i, rng, result, L_dataGridView);
                    list.Add(row);
                }
                // Continue adjacents
                rng.tiny.status.CopyTo(tiny.status, 0);
            }

            L_dataGridView.Rows.AddRange(list.ToArray());
            L_dataGridView.CurrentCell = null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            k_dataGridView.DefaultCellStyle.Font = new Font("Consolas", 9);
            L_dataGridView.DefaultCellStyle.Font = new Font("Consolas", 9);
            k_dataGridView.Columns[9].DefaultCellStyle.Font = new Font("ＭＳ ゴシック", 9);
            L_dataGridView.Columns[9].DefaultCellStyle.Font = new Font("ＭＳ ゴシック", 9);

            Type dgvtype = typeof(DataGridView);
            System.Reflection.PropertyInfo dgvPropertyInfo = dgvtype.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            dgvPropertyInfo.SetValue(k_dataGridView, true, null);
            dgvPropertyInfo.SetValue(L_dataGridView, true, null);

            foreach (string t in mezapa)
                mezapaType.Items.Add(t);

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

        private void B_SaveConfig_Click(object sender, EventArgs e)
        {
            string[] lines =
            {
                status3.Text,
                status2.Text,
                status1.Text,
                status0.Text,
                TSV.Text,
            };
            try
            {
                File.WriteAllLines("config.txt", lines);
            }
            catch
            {
                Error("Unable to save config file.");
            }
        }
    }
}
