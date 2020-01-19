namespace NT
{
    public struct idRandom {
        int seed;
        public const int MaxRand = 0x7fff;

        public idRandom(int _seed = 0) {
            seed = _seed;
        }

        public void SetSeed(int _seed) {
            seed = _seed;
        }

        public int NextInt() {
            seed = 69069 * seed + 1;
            return seed & MaxRand;
        }

        public int NextInt(int max) {
            return max == 0 ? 0 : (NextInt() % max);
        }

        public float NextFloat() {
            return (float)NextInt() / (float)(MaxRand + 1);
        }

        public float CNextFloat() {
            return 2.0f * (NextFloat() - 0.5f);
        }
    }
}