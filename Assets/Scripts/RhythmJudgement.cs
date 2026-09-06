using System;

namespace GameLab.Rhythm
{
    public enum RhythmLane
    {
        Left = 0,
        Right = 1
    }

    public enum RhythmJudgement
    {
        Perfect,
        Great,
        Good,
        Miss,
        WrongKey,
        RepeatedKey,
        TooEarly
    }

    public readonly struct RhythmJudgementResult
    {
        public RhythmJudgementResult(
            RhythmJudgement judgement,
            RhythmLane expectedLane,
            RhythmLane? pressedLane,
            double timingError,
            int score,
            int combo)
        {
            Judgement = judgement;
            ExpectedLane = expectedLane;
            PressedLane = pressedLane;
            TimingError = timingError;
            Score = score;
            Combo = combo;
        }

        public RhythmJudgement Judgement { get; }
        public RhythmLane ExpectedLane { get; }
        public RhythmLane? PressedLane { get; }

        // 음수면 빠른 입력, 양수면 늦은 입력이다.
        public double TimingError { get; }
        public int Score { get; }
        public int Combo { get; }
    }
}
