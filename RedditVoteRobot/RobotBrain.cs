using RedditAPI;
using RobotControllerInterface;
using System;
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
			timer = new System.Timers.Timer(30000); // 30 secs
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
					// translate votes into driving params
					Console.WriteLine ("reading controls");
					// TODO: check for null
					int velocity = forwardComment.ups - backComment.ups;
					int angle = leftComment.ups - rightComment.ups;
					// go! go! go!
					robot.Drive (velocity, angle);
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
	}
}

