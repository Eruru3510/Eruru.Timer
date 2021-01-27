using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using RawTimer = System.Timers.Timer;

namespace Eruru.Timer {

	public class Timer {

		public bool IgnoreSame { get; set; } = true;
		public event TimerAction<object, ElapsedEventArgs> Elapsed;
		public DateTime Next {

			get {
				ReaderWriterLock.AcquireReaderLock (Timeout.Infinite);
				try {
					return DateTimes.Count == 0 ? DateTime.MinValue : DateTimes[0];
				} finally {
					ReaderWriterLock.ReleaseReaderLock ();
				}
			}

		}

		readonly RawTimer RawTimer = new RawTimer ();
		readonly List<DateTime> DateTimes = new List<DateTime> ();
		readonly ReaderWriterLock ReaderWriterLock = new ReaderWriterLock ();

		public Timer () {
			RawTimer.Elapsed += RawTimer_Elapsed;
		}

		public void Add (DateTime dateTime) {
			if (dateTime <= DateTime.Now) {
				return;
			}
			ReaderWriterLock.AcquireWriterLock (Timeout.Infinite);
			try {
				int i = 0;
				for (; i < DateTimes.Count; i++) {
					if (IgnoreSame && dateTime == DateTimes[i]) {
						Update ();
						return;
					}
					if (dateTime < DateTimes[i]) {
						break;
					}
				}
				DateTimes.Insert (i, dateTime);
				Update ();
			} finally {
				ReaderWriterLock.ReleaseWriterLock ();
			}
		}

		public void ForEach (Action<DateTime> action) {
			if (action is null) {
				throw new ArgumentNullException (nameof (action));
			}
			ReaderWriterLock.AcquireReaderLock (Timeout.Infinite);
			try {
				DateTimes.ForEach (action);
			} finally {
				ReaderWriterLock.ReleaseReaderLock ();
			}
		}

		public int RemoveAll (Predicate<DateTime> match) {
			if (match is null) {
				throw new ArgumentNullException (nameof (match));
			}
			ReaderWriterLock.AcquireWriterLock (Timeout.Infinite);
			try {
				return DateTimes.RemoveAll (match);
			} finally {
				ReaderWriterLock.ReleaseWriterLock ();
			}
		}

		public void Clear () {
			ReaderWriterLock.AcquireWriterLock (Timeout.Infinite);
			try {
				RawTimer.Enabled = false;
				DateTimes.Clear ();
			} finally {
				ReaderWriterLock.ReleaseWriterLock ();
			}
		}

		private void RawTimer_Elapsed (object sender, ElapsedEventArgs e) {
			if (DateTimes.Count != 0) {
				ReaderWriterLock.AcquireWriterLock (Timeout.Infinite);
				try {
					DateTimes.RemoveAt (0);
				} finally {
					ReaderWriterLock.ReleaseWriterLock ();
				}
			}
			Update ();
			Elapsed?.Invoke (sender, e);
		}

		void Update () {
			if (DateTimes.Count == 0) {
				RawTimer.Enabled = false;
			} else {
				RawTimer.Enabled = true;
				RawTimer.Interval = Math.Max (1, (DateTimes[0] - DateTime.Now).TotalMilliseconds);
			}
		}

	}

}