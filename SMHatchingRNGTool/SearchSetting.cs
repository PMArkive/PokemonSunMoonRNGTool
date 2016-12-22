namespace SMHatchingRNGTool
{
    public class SearchSetting
    {
        public int Ability = -1;
        public int Gender = -1;
        public int Ball = -1;
        public int HPType = -1;
        public int[] IVup, IVlow;
        public bool Skip;

        public bool validIVs(int[] IV)
        {
            for (int i = 0; i < 6; i++)
                if (IVlow[i] > IV[i] || IV[i] > IVup[i])
                    return false;

            return true;
        }

        public bool mezapa_check(int[] IV)
        {
            if (HPType == -1)
                return true;
            var val = 15 * ((IV[0] & 1) + 2 * (IV[1] & 1) + 4 * (IV[2] & 1) + 8 * (IV[5] & 1) + 16 * (IV[3] & 1) + 32 * (IV[4] & 1)) / 63;
            return val == HPType;
        }
    }
}
