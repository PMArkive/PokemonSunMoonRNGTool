using System.Linq;

namespace SMHatchingRNGTool
{
    public class EggRNGSearch
    {
        public TinyMT tiny;

        // Search Settings
        public int GenderRatio;
        public bool GenderRandom, GenderMale, GenderFemale;
        public bool DestinyKnot;
        public bool International;
        public bool ShinyCharm;

        public bool Heterogeneous;
        public int ParentAbility;
        public bool Everstone;
        public int TSV;
        private bool Homogeneous;
        private int InheritIVs;
        public int PIDRerolls;

        private int FramesUsed;
        public int[] pre_parent;
        public int[] post_parent;

        public void Initialize()
        {
            InheritIVs = DestinyKnot ? 5 : 3;
            if (International)
                PIDRerolls += 6;
            if (ShinyCharm)
                PIDRerolls += 2;

            Homogeneous = !Heterogeneous;
        }

        public class EggRNGResult
        {
            public readonly uint[] Seed = new uint[4];
            public readonly int[] BaseIV = new int[6];
            public uint[] InheritStats;
            public uint[] InheritParents;
            public int Gender, Ability, Nature, Ball;
            public uint PID, EC;
            public bool Shiny;
            public int FramesUsed;
            public uint PSV;

            public string Seed128 => string.Join(",", Seed.Select(v => v.ToString("X8")).Reverse());
            public int[] IVs;
            public void InheritIVs(int[] pre_parent, int[] post_parent)
            {
                IVs = (int[])BaseIV.Clone();
                for (int j = 0; j < InheritStats.Length; j++)
                {
                    var stat = InheritStats[j];
                    var parent = InheritParents[j];
                    IVs[stat] = parent == 0 ? pre_parent[stat] : post_parent[stat];
                }
            }
        }

        public EggRNGResult Generate(uint[] seed)
        {
            tiny = new TinyMT(seed, new TinyMTParameter(0x8f7011ee, 0xfc78ff1f, 0x3793fdff));
            EggRNGResult egg = new EggRNGResult();
            seed.CopyTo(egg.Seed, 0);
            FramesUsed = 0; // Reset Frame Counter

            //最初の消費 Initial Consumption
            getRand();

            //性別 Gender
            if (GenderRandom)
                egg.Gender = getRand() % 252 >= GenderRatio ? 0 : 1;
            else if (GenderMale)
                egg.Gender = 0;
            else if (GenderFemale)
                egg.Gender = 1;
            else
                egg.Gender = 2;

            //性格 -- Nature
            egg.Nature = (int)(getRand() % 25);

            //両親変わらず -- Everstone
            //Chooses which parent if necessary; users should not intermix with Power items either.
            if (Everstone)
                getRand();

            //特性 -- Ability
            egg.Ability = getRandomAbility(ParentAbility, getRand() % 100);

            //最初の遺伝箇所 -- IV Inheritance
            egg.InheritStats = new uint[InheritIVs];
            egg.InheritParents = new uint[InheritIVs];
            for (int i = 0; i < InheritIVs; i++)
            {
                repeat:
                egg.InheritStats[i] = getRand() % 6;

                // Scan for duplicate IV
                for (int k = 0; k < i; k++)
                    if (egg.InheritStats[k] == egg.InheritStats[i])
                        goto repeat;

                egg.InheritParents[i] = getRand() % 2;
            }

            //基礎個体値 -- Base IVs
            for (int j = 0; j < 6; j++)
                egg.BaseIV[j] = (int)(getRand() & 0x1F);
            egg.InheritIVs(pre_parent, post_parent);

            //暗号化定数 -- Encryption Constant
            egg.EC = getRand();

            //性格値判定 -- PID Rerolls
            for (int i = PIDRerolls; i > 0; i--)
            {
                egg.PID = getRand();
                egg.PSV = ((egg.PID >> 16) ^ (egg.PID & 0xFFFF)) >> 4;
                if (egg.PSV != TSV)
                    continue;

                egg.Shiny = true;
                break;
            }

            //ボール消費 -- Ball Inheritance
            if (Homogeneous) // Same Species (no Ditto)
                egg.Ball = getRand() % 100 >= 50 ? 1 : 2; // else 0

            //something
            getRand();

            egg.FramesUsed = FramesUsed;
            return egg;
        }
        private uint getRand()
        {
            var r = tiny.temper();
            tiny.nextState();
            ++FramesUsed;
            return r;
        }

        private static int getRandomAbility(int ability, uint value)
        {
            switch (ability)
            {
                case 0: // Ability 0
                    return value < 80 ? 0 : 1;
                case 1:
                    return value < 20 ? 0 : 1;
                case 2:
                    if (value < 20) return 0;
                    if (value < 40) return 1;
                    return 2;
            }
            return 0;
        }
    }
}
