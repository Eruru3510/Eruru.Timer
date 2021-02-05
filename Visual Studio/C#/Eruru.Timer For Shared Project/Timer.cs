using System;
using System.Collections.Generic;
using System.Timers;
using RawTimer = System.Timers.Timer;

namespace Eruru.Timer {

	public class Timer {

		public bool IgnoreSame { get; set; } = true;
		public event TimerAction<object, ElapsedEventArgs> Elapsed;
		public DateTime Next {

			get {
				DateTime dateTime = DateTime.MinValue;
				ReaderWriterLockHelper.Read (() => {
					dateTime = DateTimes.Count == 0 ? DateTime.MinValue : DateTimes[0];
				});
				return dateTime;
			}

		}

		readonly RawTimer RawTimer = new RawTimer ();
		readonly List<DateTime> DateTimes = new List<DateTime> ();
		readonly ReaderWriterLockHelper.ReaderWriterLockHelper ReaderWriterLockHelper = new ReaderWriterLockHelper.ReaderWriterLockHelper ();

		public Timer () {
			RawTimer.Elapsed += RawTimer_Elapsed;
		}

		public void Add (DateTime dateTime) {
			if (dateTime <= DateTime.Now) {
				return;
			}
			ReaderWriterLockHelper.Write (() => {
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
			});
		}

		public void ForEach (Action<DateTime> action) {
			if (action is null) {
				throw new ArgumentNullException (nameof (action));
			}
			ReaderWriterLockHelper.Read (() => {
				DateTimes.ForEach (action);
			});
		}

		public int RemoveAll (Predicate<DateTime> match) {
			if (match is null) {
				throw new ArgumentNullException (nameof (match));
			}
			int count = 0;
			ReaderWriterLockHelper.Write (() => {
				count = DateTimes.RemoveAll (match);
			});
			return count;
		}

		public void Clear () {
			ReaderWriterLockHelper.Write (() => {
				RawTimer.Enabled = false;
				DateTimes.Clear ();
			});
		}

		private void RawTimer_Elapsed (object sender, ElapsedEventArgs e) {
			if (DateTimes.Count != 0) {
				ReaderWriterLockHelper.Write (() => {
					DateTimes.RemoveAt (0);
				});
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