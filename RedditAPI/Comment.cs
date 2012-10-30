using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditAPI
{
	public class Comment
	{
		public string id { get; set; }
		public int ups { get; set; }

		public Comment ()
		{
		}

		public Comment (JObject data)
		{
			this.id = (string)data["id"];
			this.ups = (int)data["ups"];
		}
	}
}

