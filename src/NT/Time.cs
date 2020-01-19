using System;

namespace NT
{
    public static class Time {
        public static uint frameCount {get {return TimeSystem.frameCount;}}
        public static float delteTime {get {return TimeSystem.deltaTime;}}
        public static float time {get {return TimeSystem.time;}}
    }
}