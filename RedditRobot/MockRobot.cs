using System;

namespace RedditRobot
{
	public class MockRobot
	{
		public MockRobot ()
		{
		}

		public void drive(int direction, int rotation) 
		{
			Console.WriteLine ("Driving - direction: {0}, rotation: {1}", direction, rotation);
		}
	}
}

