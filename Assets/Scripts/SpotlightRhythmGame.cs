using System;
using UnityEngine;

namespace GameLab.Rhythm
{
    [DisallowMultipleComponent]
    public sealed class SpotlightRhythmGame : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SpotLightMovement spotLightMovement;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private SoundManager soundManager;

        [Header("Song")]
        [Tooltip("SoundManager의 Resources/Sound 클립 이름. 비워두면 Music Source의 클립을 사용합니다.")]
        [SerializeField] private string bgmName;
        [SerializeField, Min(1f)] private float bpm = 120f;
        [Tooltip("첫 A 판정이 발생하는 곡 시작 후 시간(초)")]
        [SerializeField, Min(0f)] private float firstHitTime = 2f;
        [Tooltip("A와 D 판정 사이의 박자 수")]
        [SerializeField, Min(0.125f)] private float hitIntervalBeats = 1f;
        [Tooltip("스포트라이트가 시작점에서 중앙까지 이동하는 데 걸리는 박자 수")]
        [SerializeField, Min(0.125f)] private float approachBeats = 2f;
        [SerializeField, Min(0f)] private float audioStartDelay = 0.5f;
        [SerializeField] private bool playOnStart = true;

        [Header("Judgement Windows (seconds)")]
        [SerializeField, Min(0.001f)] private float perfectWindow = 0.045f;
        [SerializeField, Min(0.001f)] private float greatWindow = 0.09f;
        [SerializeField, Min(0.001f)] private float goodWindow = 0.14f;

        private double songStartDspTime;
        private int currentNoteIndex;
        private RhythmLane? lastPressedLane;

        public event Action<RhythmJudgementResult> Judged;

        public bool IsRunning { get; private set; }
        public double SongTime => IsRunning ? AudioSettings.dspTime - songStartDspTime : 0d;
        public RhythmLane ExpectedLane => (currentNoteIndex & 1) == 0
            ? RhythmLane.Left
            : RhythmLane.Right;
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        private double SecondsPerBeat => 60d / bpm;
        private double HitInterval => SecondsPerBeat * hitIntervalBeats;
        private double ApproachDuration => SecondsPerBeat * approachBeats;
        private double CurrentHitTime => firstHitTime + currentNoteIndex * HitInterval;

        private void Awake()
        {
            if (spotLightMovement == null)
            {
                spotLightMovement = FindFirstObjectByType<SpotLightMovement>();
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            UpdateSpotLightPositions();
            ProcessExpiredNotes();

            if (musicSource != null && musicSource.clip != null &&
                SongTime > musicSource.clip.length)
            {
                StopGame();
            }
        }

        public void StartGame()
        {
            if (spotLightMovement == null || !spotLightMovement.IsConfigured)
            {
                Debug.LogError(
                    "SpotlightRhythmGame에 설정이 완료된 SpotLightMovement를 연결하세요.",
                    this);
                return;
            }

            ResetGameState();
            songStartDspTime = AudioSettings.dspTime + audioStartDelay;

            if (!TryScheduleMusic(songStartDspTime))
            {
                Debug.LogWarning(
                    "재생할 BGM이 없습니다. 음악 없이 DSP 시계만 사용해 게임을 시작합니다.",
                    this);
            }

            IsRunning = true;
            UpdateSpotLightPositions();
        }

        public void StopGame()
        {
            IsRunning = false;

            if (musicSource != null)
            {
                musicSource.Stop();
            }

            spotLightMovement?.ResetToStart();
        }

        // PlayerController의 Input System 콜백에서 호출한다.
        public void PressLeft()
        {
            TryJudge(RhythmLane.Left);
        }

        public void PressRight()
        {
            TryJudge(RhythmLane.Right);
        }

        private void TryJudge(RhythmLane pressedLane)
        {
            if (!IsRunning || SongTime < 0d)
            {
                return;
            }

            RhythmLane expectedLane = ExpectedLane;
            double timingError = SongTime - CurrentHitTime;

            if (lastPressedLane == pressedLane)
            {
                lastPressedLane = pressedLane;
                PublishFailure(
                    RhythmJudgement.RepeatedKey,
                    expectedLane,
                    pressedLane,
                    timingError);
                return;
            }

            lastPressedLane = pressedLane;

            if (pressedLane != expectedLane)
            {
                PublishFailure(
                    RhythmJudgement.WrongKey,
                    expectedLane,
                    pressedLane,
                    timingError);
                return;
            }

            double absoluteError = Math.Abs(timingError);
            if (absoluteError <= perfectWindow)
            {
                PublishHit(RhythmJudgement.Perfect, pressedLane, timingError, 1000);
            }
            else if (absoluteError <= greatWindow)
            {
                PublishHit(RhythmJudgement.Great, pressedLane, timingError, 700);
            }
            else if (absoluteError <= goodWindow)
            {
                PublishHit(RhythmJudgement.Good, pressedLane, timingError, 400);
            }
            else if (timingError < 0d)
            {
                PublishFailure(
                    RhythmJudgement.TooEarly,
                    expectedLane,
                    pressedLane,
                    timingError);
            }
            else
            {
                PublishMiss(expectedLane, pressedLane, timingError);
                AdvanceNote();
            }
        }

        private void ProcessExpiredNotes()
        {
            while (IsRunning && SongTime - CurrentHitTime > goodWindow)
            {
                RhythmLane missedLane = ExpectedLane;
                PublishMiss(missedLane, null, SongTime - CurrentHitTime);
                AdvanceNote();
            }
        }

        private void PublishHit(
            RhythmJudgement judgement,
            RhythmLane pressedLane,
            double timingError,
            int baseScore)
        {
            Combo++;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            Score += baseScore;

            Judged?.Invoke(new RhythmJudgementResult(
                judgement,
                ExpectedLane,
                pressedLane,
                timingError,
                Score,
                Combo));

            AdvanceNote();
        }

        private void PublishMiss(
            RhythmLane expectedLane,
            RhythmLane? pressedLane,
            double timingError)
        {
            PublishFailure(
                RhythmJudgement.Miss,
                expectedLane,
                pressedLane,
                timingError);
        }

        private void PublishFailure(
            RhythmJudgement judgement,
            RhythmLane expectedLane,
            RhythmLane? pressedLane,
            double timingError)
        {
            Combo = 0;
            Judged?.Invoke(new RhythmJudgementResult(
                judgement,
                expectedLane,
                pressedLane,
                timingError,
                Score,
                Combo));
        }

        private void AdvanceNote()
        {
            currentNoteIndex++;
            UpdateSpotLightPositions();
        }

        private void UpdateSpotLightPositions()
        {
            if (spotLightMovement == null)
            {
                return;
            }

            spotLightMovement.UpdatePositions(
                SongTime,
                GetUpcomingHitTime(RhythmLane.Left),
                GetUpcomingHitTime(RhythmLane.Right),
                ApproachDuration);
        }

        private double GetUpcomingHitTime(RhythmLane lane)
        {
            int noteIndex = currentNoteIndex;

            if ((noteIndex & 1) != (int)lane)
            {
                noteIndex++;
            }

            return firstHitTime + noteIndex * HitInterval;
        }

        private bool TryScheduleMusic(double dspStartTime)
        {
            if (soundManager != null && !string.IsNullOrWhiteSpace(bgmName) &&
                soundManager.TryScheduleBgm(bgmName, dspStartTime))
            {
                musicSource = soundManager.BgmSource;
                return true;
            }

            if (musicSource == null || musicSource.clip == null)
            {
                return false;
            }

            musicSource.Stop();
            musicSource.time = 0f;
            musicSource.PlayScheduled(dspStartTime);
            return true;
        }

        private void ResetGameState()
        {
            currentNoteIndex = 0;
            lastPressedLane = null;
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            spotLightMovement.ResetToStart();
        }

        private void OnValidate()
        {
            bpm = Mathf.Max(1f, bpm);
            hitIntervalBeats = Mathf.Max(0.125f, hitIntervalBeats);
            approachBeats = Mathf.Max(0.125f, approachBeats);
            perfectWindow = Mathf.Max(0.001f, perfectWindow);
            greatWindow = Mathf.Max(perfectWindow, greatWindow);
            goodWindow = Mathf.Max(greatWindow, goodWindow);
        }
    }
}
