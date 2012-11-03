using RedditAPI;
using RobotControllerInterface;
using System;
using System.Timers;

namespace RedditVoteRobot
{
	public class RobotBrain {
		private Reddit reddit; 
		private IRobot robot;
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
		private static readonly string forwardText = "go ahead";
		private static readonly string backText = "go back!";
		private static readonly string leftText = "left";
		private static readonly string rightText = "right";
		
		public RobotBrain (IRobot robot,
		                   Reddit reddit,
		                   string robotSubreddit)
		{
			this.robot = robot;
			this.reddit = reddit;
			this.robotSubreddit = robotSubreddit;
		}
		
		public void start()
		{
			this.postId = reddit.postSelf (robotSubreddit, introTitle, introText);
			// TODO: if post failed, error message and stop
			timer = new System.Timers.Timer(60000); // 60 secs
			timer.Elapsed += new ElapsedEventHandler(TimerCallback_Move);
			timer.Enabled = true;
			timer.Start ();
			Console.WriteLine("press any key to stop");
			Console.ReadLine (); // wait for keypress
			timer.Dispose ();
		}
		
		private void TimerCallback_Move (object source, ElapsedEventArgs e)
		{
			// skip this move if the last one's still in progress
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
			forwardId = reddit.postComment(postId, forwardText + " " + DateTime.Now);
			backId = reddit.postComment(postId, backText + " " + DateTime.Now);
			leftId = reddit.postComment(postId, leftText + " " + DateTime.Now);
			rightId = reddit.postComment(postId, rightText + " " + DateTime.Now);
			if (++timesMoved > 20) {
				timer.Stop();
			}
			lock (timerLock) {
				timerBusy = false;
			}
		}
	}
}

