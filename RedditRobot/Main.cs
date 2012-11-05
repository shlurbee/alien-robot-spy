using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Xml.Serialization;

using RedditAPI;
using RobotControllerInterface;
using RedditVoteRobot;


namespace RedditRobot
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// TODO: error handling
			Config config = Config.fromFile("redditRobotConfig.xml");
			IRobot robot = (IRobot)new MockRobot();
			Reddit reddit = new Reddit(config.username, config.password);
			RobotBrain brain = new RobotBrain(robot,
			                                  reddit,
			                                  config);
			brain.start();
			Console.ReadKey();
			brain.stop();
		}

		private static void writeDefaultConfig()
		{
			Config config = new Config();
			config.username = "valree";
			config.password = "shpluh";
			config.subreddit = "alienrobotspy";
			config.leftStrings = new List<string>() {
				"Left seems appropriate at this juncture",
				"90 degrees please GoldBot",
				"Hard a'port!"
			};
			config.rightStrings = new List<string>() {
				"Starboard!"
			};
			config.forwardStrings = new List<string>() {
				"Faster GoldBot, Kill! Kill!",
				"Forward, Ho!",
				"Proceed forth",
				"Mosey onward",
				"Continue",
				"On",
				"Ever Progress! Forward.",
				"Trundle forth",
				"Always forward, never straight"
			};
			config.backStrings = new List<string>() {
				"Retreat!",
				"Fall back",
				"Reverse, Ho!",
				"Go Backwards",
				"180 degrees seems appropriate"
			};
			config.robotAdjectives = new List<string>() {
				"Brave",
				"Mighty",
				"Valiant",
				"Dear",
				"Beloved",
				"Supple",
				"Gracious",
				"Low-Slung",
				"Gallant",
				"Plucky",
				"Intrepid",
				"Trundling",
				"Spiffy",
				"Technologically-resplendent"
			};
			config.toFile ("redditRobotConfig.xml");
		}
	}
}
