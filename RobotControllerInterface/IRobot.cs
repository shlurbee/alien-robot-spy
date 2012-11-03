using System;

namespace RobotControllerInterface
{
	public interface IRobot
	{
		void Drive(int velocity, int angle);
		void DriveDirect(int left, int right);
	}
}

