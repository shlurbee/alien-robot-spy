using System;

namespace RobotControllerInterface
{
	public class MockRobot : IRobot
	{
		public MockRobot ()
		{
		}

		public void Drive(int velocity, int angle)
		{
			Console.WriteLine("Drive - velocity:{0}, angle:{1}", velocity, angle);
		}

		public void DriveDirect(int left, int right)
		{
			Console.WriteLine ("DriveDirect - lef:{0}, right:{1}", left, right);
		}
	}
}

