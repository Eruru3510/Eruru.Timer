using System;
using System.Collections.Generic;
using System.Timers;
using Eruru.ReaderWriterLockHelper;
using RawTimer = System.Timers.Timer;

namespace Eruru.Timer {

	public class Timer {

		public bool IgnoreSame { get; set; } = true;
		public event TimerAction<object, ElapsedEventArgs> Elapsed;
		public DateTime Next {

			get {
				DateTime dateTime = DateTime.MinValue;
				ReaderWriterLockHelper.Read ((ref List<DateTime> dateTimes) => {
					dateTime = dateTimes.Count == 0 ? DateTime.MinValue : dateTimes[0];
				});
				return dateTime;
			}

		}

		readonly RawTimer RawTimer = new RawTimer ();
		readonly ReaderWriterLockHelper<List<DateTime>> ReaderWriterLockHelper = new ReaderWriterLockHelper<List<DateTime>> (new List<DateTime> ());

		public Timer () {
			RawTimer.Elapsed += RawTimer_Elapsed;
		}

		public void Add (DateTime dateTime) {
			if (dateTime <= DateTime.Now) {
				return;
			}
			ReaderWriterLockHelper.Write ((ref List<DateTime> dateTimes) => {
				int i = 0;
				for (; i < dateTimes.Count; i++) {
					if (IgnoreSame && dateTime == dateTimes[i]) {
						Update ();
						return;
					}
					if (dateTime < dateTimes[i]) {
						break;
					}
				}
				dateTimes.Insert (i, dateTime);
				Update ();
			});
		}

		public void ForEach (Action<DateTime> action) {
			if (action is null) {
				throw new ArgumentNullException (nameof (action));
			}
			ReaderWriterLockHelper.Read ((ref List<DateTime> dateTimes) => {
				dateTimes.ForEach (action);
			});
		}

		public int RemoveAll (Predicate<DateTime> match) {
			if (match is null) {
				throw new ArgumentNullException (nameof (match));
			}
			int count = 0;
			ReaderWriterLockHelper.Write ((ref List<DateTime> dateTimes) => {
				count = dateTimes.RemoveAll (match);
			});
			return count;
		}

		public void Clear () {
			ReaderWriterLockHelper.Write ((ref List<DateTime> dateTimes) => {
				RawTimer.Enabled = false;
				dateTimes.Clear ();
			});
		}

		private void RawTimer_Elapsed (object sender, ElapsedEventArgs e) {
			ReaderWriterLockHelper.Read ((ref List<DateTime> dateTimes) => {
				if (dateTimes.Count != 0) {
					ReaderWriterLockHelper.Write ((ref List<DateTime> subDateTimes) => {
						subDateTimes.RemoveAt (0);
					});
				}
			});
			Update ();
			Elapsed?.Invoke (sender, e);
		}

		void Update () {
			ReaderWriterLockHelper.Read ((ref List<DateTime> dateTimes) => {
				if (dateTimes.Count == 0) {
					RawTimer.Enabled = false;
					return;
				}
				RawTimer.Enabled = true;
				RawTimer.Interval = Math.Max (1, (dateTimes[0] - DateTime.Now).TotalMilliseconds);
			});
		}

	}

}