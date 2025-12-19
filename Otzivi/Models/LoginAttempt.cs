namespace Otzivi.Models
{
    public class LoginAttempt
    {
        public string IpAddress { get; set; }
        public int AttemptCount { get; set; }
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
        public bool IsBlocked => AttemptCount >= 5;

        public DateTime BlockUntil => LastAttempt.AddSeconds(30); // 👈 30 СЕКУНД
        public bool IsCurrentlyBlocked => IsBlocked && DateTime.Now < BlockUntil;

        public int RemainingAttempts => Math.Max(0, 5 - AttemptCount);
    }
}