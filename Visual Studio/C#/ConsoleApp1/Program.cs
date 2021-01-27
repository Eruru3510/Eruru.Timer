using System;
using System.Threading;

namespace ConsoleApp1 {

	class Program {

		static void Main (string[] args) {
			Console.Title = nameof (ConsoleApp1);
			Random random = new Random ();
			Eruru.Timer.Timer timer = new Eruru.Timer.Timer ();
			timer.Elapsed += (sender, e) => {
				//Console.WriteLine ($"触发{DateTime.Now}");
			};
			for (int i = 0; i < 10000; i++) {
				new Thread (() => {
					switch (random.Next (2)) {
						case 0:
							timer.Add (DateTime.Now.AddSeconds (1));
							break;
						case 1:
							//Console.WriteLine ($"下次{timer.Next}");
							break;
					}
				}) {
					IsBackground = true
				}.Start ();
			}
			Console.ReadLine ();
		}

	}

}