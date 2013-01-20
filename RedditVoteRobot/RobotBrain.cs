using RedditAPI;
using RobotControllerInterface;
using System;
using System.Threading;
using System.Timers;

namespace RedditVoteRobot
{
	public class RobotBrain {
		private Reddit reddit; 
		private IRobot robot;
		private Config config;
		private System.Timers.Timer timer;
		private readonly string robotSubreddit;
		private string postId;
		private string leftId;
		private string rightId;
		private string forwardId;
		private string backId;
		private int timesMoved = 0;
		private bool timerBusy = false;
		private object timerLock = new object();
		
		private static readonly string introTitle = @"You are driving the reddit alien robot. 
                                                       Where should I go next?";
		private static readonly string introText = @"weeeeeeee";

		public RobotBrain (IRobot robot,
		                   Reddit reddit,
		                   Config config)
		{
			this.robot = robot;
			this.reddit = reddit;
			this.config = config;
			this.robotSubreddit = config.subreddit;
		}
		
		public void start()
		{
			this.postId = reddit.postSelf (robotSubreddit, introTitle, introText);
			// TODO: if post failed, error message and stop
			timer = new System.Timers.Timer(30000); // 60 secs
			timer.Elapsed += new ElapsedEventHandler(TimerCallback_Move);
			timer.Enabled = true;
			timer.Start ();
		}
                         
        public void stop ()
		{
			if (timer != null) 
			{
				timer.Dispose ();
			}
        }

        static double robotWheelRadiusMm = 125;
        static double robotCircumferenceMm = 2 * Math.PI * robotWheelRadiusMm;

        private double getTurnDurationMillisecs(double turnVelocityMmPerSec, 
                                                double turnAngleDegrees)
        {
            if (turnVelocityMmPerSec == 0) return 0;
            double mmToTravel = (turnAngleDegrees / 360) * robotCircumferenceMm;
            return 1000 * mmToTravel / turnVelocityMmPerSec;
        }

        private double getDriveDurationMillisecs(double driveVelocityMmPerSec, double driveDistanceMm)
        {
            // note velocity is in mm / sec and duration should be msecs
            return driveDistanceMm * 1000 / driveVelocityMmPerSec;
        }

		private void TimerCallback_Move (object source, ElapsedEventArgs e)
		{
			// use lock to make sure moves don't overlap. (if a new move starts
			// before the current one ends, the new move will be ignored.)
			lock (timerLock) {
				if (timerBusy)
					return;
				else
					timerBusy = true;
			}

			if (forwardId != null) {
				// fetch most recent set of controls and read vote values
				Console.WriteLine ("fetching most recent control set");
				Comment forwardComment = reddit.getComment (robotSubreddit, postId, forwardId);
				Comment backComment = reddit.getComment (robotSubreddit, postId, backId);
				Comment leftComment = reddit.getComment (robotSubreddit, postId, leftId);
				Comment rightComment = reddit.getComment (robotSubreddit, postId, rightId);
				if (forwardComment != null && backComment != null && leftComment != null && rightComment != null) {
					// subtract self-votes
                    forwardComment.ups -= 1;
                    backComment.ups -= 1;
                    leftComment.ups -= 1;
                    rightComment.ups -= 1;
                    // translate votes into driving params
					Console.WriteLine ("reading controls...");
                    Console.WriteLine("forward:{0}, back:{1}, left:{2}, right:{3}",
                        forwardComment.ups, backComment.ups, 
                        leftComment.ups, rightComment.ups);
					int distance = this.config.driveDistanceCm;
                    int driveVelocity = 0;
                    if (forwardComment.ups > backComment.ups) driveVelocity = 200;
                    else if (forwardComment.ups < backComment.ups) driveVelocity = -200;

                    int rotateVotes = leftComment.ups - rightComment.ups;
                    int rotateVelocity = 0;
                    int rotateDuration = 0;

                    if (rotateVotes != 0)
                    {
                        // use negative velocity to reverse turn direction
                        rotateVelocity = (rotateVotes > 0) ? 300 : -300;
                        // calculate how long to turn for desired angle
                        double rotateAngleDegrees = turnDegrees(); // always > 0
                        Console.WriteLine("Turning {0} degrees", rotateAngleDegrees);
                        rotateDuration = Convert.ToInt32(
                            getTurnDurationMillisecs(Math.Abs(rotateVelocity),
                                                     rotateAngleDegrees));

                    }
                    // turn right or left
                    if (rotateVelocity != 0)
                    {
                        robot.DriveDirect(rotateVelocity, -1 * rotateVelocity); // first rotate
                        Thread.Sleep(Math.Abs(rotateDuration));
                        robot.DriveDirect(0, 0);
                    }
                    // pause for effect
                    Thread.Sleep(100);
                    // go forward or back
                    if (driveVelocity != 0)
                    {
                        double driveDistanceMm = 10 * config.driveDistanceCm;
                        double driveDuration = getDriveDurationMillisecs(driveVelocity,
                                                                         driveDistanceMm);
                        Console.WriteLine("Target distance: {0}cm", distance);
                        Console.WriteLine("Driving at {0}mm/sec for {1} msecs",
                                           driveVelocity, driveDuration);
                        robot.DriveDirect(driveVelocity, driveVelocity);
                        Thread.Sleep(Math.Abs(Convert.ToInt32(driveDuration)));
                        robot.DriveDirect(0, 0);
                    }

					// delete old direction controls
					Console.WriteLine ("deleting old comments");
					reddit.deleteComment (forwardId);
					reddit.deleteComment (backId);
					reddit.deleteComment (leftId);
					reddit.deleteComment (rightId);
				} else {
					Console.WriteLine ("There was a problem fetching comments. Ids: {0},{1},{2},{3}",
					                   forwardId, backId, leftId, rightId);
				}
				forwardId = backId = leftId = rightId = null;
			}
			// create a new set of direction controls
			Console.WriteLine ("create new controls");
			forwardId = reddit.postComment(postId, forwardString());
			backId = reddit.postComment(postId, backString()); 
			leftId = reddit.postComment(postId, leftString());
			rightId = reddit.postComment(postId, rightString());
            
            // stop if max number of moves has been reached
			if (++timesMoved > 20) {
				timer.Stop();
			}
			// free lock so function can repeat
			lock (timerLock) {
				timerBusy = false;
			}
		}

		private string rightString ()
		{
			return string.Format ("    {0}\n[](/right){1}",
                                 DateTime.Now, 
                                 config.rightStrings.pickRandom ());
		}

		private string leftString ()
		{
			return string.Format ("    {0}\n[](/left){1}",
                                  DateTime.Now,
                                  config.leftStrings.pickRandom ());
		}

		private string forwardString ()
		{
			return string.Format ("    {0}\n\n[](/forward){1}",
                                  DateTime.Now,
                                  config.forwardStrings.pickRandom ());
		}

		private string backString ()
		{
			return string.Format ("    {0}\n[](/back){1}",
                                  DateTime.Now,
                                  config.backStrings.pickRandom ());
		}

        private static Random rand = new Random();
        private double turnDegrees()
        {
            double degrees = rand.Next(config.rotateMinDegrees,
                                       config.rotateMaxDegrees);
            return degrees;
        }
	}
}

