namespace SimplexNoise
{
    static class Interpolation
    {
        public static double sumOctave(int num_iterations, long x, long y, float persistence, float scale, int low, int high)
        {
            var freq = scale;
            //float freq = 1.0f;
            double noise = 0;
            double maxAmp = 0;
            double amp = 1;

            for (int i = 0; i < num_iterations; i++)
            {
                noise += Noise.Generate(x * freq, y * freq) * amp;
                maxAmp += amp;
                amp *= persistence;
                freq *= 2;
            }

            noise /= maxAmp;
            noise = (noise + 1) / 2;
            noise = noise * (high - low) + low;
            return noise;
        }
    }
}