using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    public enum Grade
    {
        None,    // no action was needed (e.g. a bar in another lane): no points, streak unaffected
        Perfect, // within the perfect window of the obstacle's beat
        Good,    // within the good window
        Ok,      // passed, but the answer was outside the good window: no points, streak resets
        Miss,    // collided: the run ends
    }

    /// <summary>Grades an obstacle from its outcome and answer timing (playbook 1.4). Docs/SCORING.md.</summary>
    public static class Judge
    {
        public static Grade GradeOf(Resolution r, double targetTime, double perfectSeconds, double goodSeconds)
        {
            if (r.Outcome == ObstacleOutcome.Hit) return Grade.Miss;
            if (!r.HasAnswer) return Grade.None;
            double d = Math.Abs(r.AnswerTime - targetTime);
            if (d <= perfectSeconds) return Grade.Perfect;
            if (d <= goodSeconds) return Grade.Good;
            return Grade.Ok;
        }
    }

    /// <summary>Score for one run: perfects x 100 + goods x 50, times a streak multiplier (x2 at 10, x3 at 25, x4 at 50).</summary>
    public sealed class ScoreKeeper
    {
        public const int PerfectPoints = 100;
        public const int GoodPoints = 50;

        public long Points { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int Perfects { get; private set; }
        public int Goods { get; private set; }
        public int Oks { get; private set; }
        public int Misses { get; private set; }

        public int Multiplier => Streak >= 50 ? 4 : Streak >= 25 ? 3 : Streak >= 10 ? 2 : 1;
        public int Graded => Perfects + Goods + Oks + Misses;
        public double PerfectPercent => Graded == 0 ? 0 : 100.0 * Perfects / Graded;

        public void Add(Grade g)
        {
            switch (g)
            {
                case Grade.Perfect: Hit(PerfectPoints); Perfects++; break;
                case Grade.Good: Hit(GoodPoints); Goods++; break;
                case Grade.Ok: Oks++; Streak = 0; break;
                case Grade.Miss: Misses++; Streak = 0; break;
            }
        }

        private void Hit(int basePoints)
        {
            Streak++;
            if (Streak > BestStreak) BestStreak = Streak;
            Points += basePoints * Multiplier; // the 10th hit in a row already scores x2
        }
    }

    /// <summary>Latency calibration maths: how far taps land from the clicks they were aimed at.</summary>
    public static class Calibration
    {
        public readonly struct Result
        {
            public readonly int Count;
            public readonly double MeanMs;
            public readonly double SpreadMs; // standard deviation

            public Result(int count, double meanMs, double spreadMs)
            {
                Count = count;
                MeanMs = meanMs;
                SpreadMs = spreadMs;
            }
        }

        /// <summary>
        /// Pairs each tap with its nearest click and averages (tap - click). Taps further than
        /// <paramref name="maxDistance"/> from any click (stray presses) are ignored. Positive = taps land after the
        /// clicks are scheduled, i.e. the sound (or the player) is late; the Conductor delays the game by this much.
        /// </summary>
        public static Result Compute(IList<double> clickTimes, IList<double> tapTimes, double maxDistance)
        {
            var offsets = new List<double>();
            foreach (double tap in tapTimes)
            {
                double best = double.MaxValue;
                foreach (double click in clickTimes)
                {
                    double d = tap - click;
                    if (Math.Abs(d) < Math.Abs(best)) best = d;
                }
                if (Math.Abs(best) <= maxDistance) offsets.Add(best);
            }
            if (offsets.Count == 0) return new Result(0, 0, 0);

            double mean = 0;
            foreach (double o in offsets) mean += o;
            mean /= offsets.Count;
            double variance = 0;
            foreach (double o in offsets) variance += (o - mean) * (o - mean);
            double spread = Math.Sqrt(variance / offsets.Count);
            return new Result(offsets.Count, mean * 1000.0, spread * 1000.0);
        }
    }
}
