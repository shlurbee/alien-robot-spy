using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;


namespace RedditVoteRobot
{
	// add a "pickRandom" method to the List collection
	public static class Extensions 
	{
		private static Random random = new Random();

		public static T pickRandom<T>(this List<T> list)
		{
			if (list.Count == 0) 
				return default(T);
			return list[random.Next (0, list.Count)];
		}
	}

	[Serializable()]
	public class Config
	{
		Random random = new Random();

		public Config ()
		{
		}

		public String username { get; set; }
		public String password { get; set; }
		public String subreddit { get; set; }
		public String redditBaseUrl { get; set; }
		public String redditApiUrl { get; set; }
		public String linkPrefix { get; set; }
		public String commentPrefix { get; set; }
		public String cookieDomain { get; set; }
		public List<String> leftStrings { get; set; }
		public List<String> rightStrings { get; set; }
		public List<String> forwardStrings { get; set; }
		public List<String> backStrings { get; set; }
		public List<String> robotAdjectives { get; set; }
        public int rotateMinDegrees { get; set; } // should be > 0
        public int rotateMaxDegrees { get; set; } // should be > 0
        public int driveDistanceCm { get; set; }

		public static Config fromFile(string filename)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Config));
			FileStream stream = new FileStream(filename, 
			                                   FileMode.Open,
			                                   FileAccess.Read,
			                                   FileShare.Read);
			Config config = (Config)serializer.Deserialize(stream);
			return config;
		}

		public void toFile(string filename)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Config));
			TextWriter writer = new StreamWriter(filename);
			serializer.Serialize(writer, this);
			writer.Close ();
		}
		
		private string pickRandom (List<string> list)
		{
			return list[random.Next(0, list.Count)];
		}
	}
}

