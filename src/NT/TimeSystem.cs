using System;
using System.Diagnostics;

namespace NT
{   
    internal static class TimeSystem
    {
        public static uint frameCount;
        public static float time;
        public static float deltaTime;
        static float lastFrameTime;
        static Stopwatch stopwatch;

        static TimeSystem() {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public static void Update() {
            //frameCount++;
            stopwatch.Stop();
            float thisFrameTime = stopwatch.ElapsedMilliseconds * 0.001f;
            deltaTime = thisFrameTime - lastFrameTime;
            time += deltaTime;
            lastFrameTime = thisFrameTime;
            stopwatch.Start();
        }
    }
}