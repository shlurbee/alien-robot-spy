using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditAPI
{
	
	public class Reddit
	{
		private Cookie sessionCookie;
		private string modhash;

		public Reddit (string username, string password)
		{
			if (this.modhash == null) {
				JArray errors = login (username, password);
				Console.WriteLine("cookie: " + this.sessionCookie.ToString());
				Console.WriteLine ("errors: " + errors.ToString());
			}
		}

		// TODO: get urls from config file?
		//private string redditBaseUrl = "http://reddit.local:8081";
		//private string redditApiUrl = "http://reddit.local:8081/api"; 
		//private string redditCookieDomain = "reddit.local"; // ".reddit.com"
		//private string linkPrefix = "t6";
		//private string commentPrefix = "t1";

		private string redditBaseUrl = "http://www.reddit.com";
		private string redditApiUrl = "http://www.reddit.com/api"; 
		private string redditCookieDomain = ".reddit.com"; // ".reddit.com"
		private string linkPrefix = "t3";
		private string commentPrefix = "t1";

		/* Helper methods */
		private string commentFullname (string commentId)
		{
			return commentPrefix + "_" + commentId;
		}

		private string commentUrl (string commentId)
		{
			return string.Format ("{0}/comments/{1}", commentId);
		}

		private string linkFullname (string linkId)
		{
			return linkPrefix + "_" + linkId;
		}

		/* API endpoints */
		private string loginUrl (string username)
		{
			return string.Format ("{0}/login/{1}", redditApiUrl, username);
		}

		private string postSelfUrl ()
		{
			return string.Format ("{0}/submit", redditApiUrl);
		}

		private string getPostUrl (string postId)
		{
			return string.Format ("{0}/comments/{1}.json", redditBaseUrl, postId);
		}

		private string postCommentUrl ()
		{
			return string.Format ("{0}/comment", redditApiUrl);
		}

		private string getCommentUrl(string subreddit, string parentPostId, string commentId)
		{
			return string.Format ("{0}/comments/{1}/0/{2}.json", 
			                      redditBaseUrl, parentPostId, commentId);
	    }

		private string refreshCommentsUrl (string subreddit, string parentPostId)
		{
			return string.Format ("{0}/r/{1}/comments/{2}.json", 
			                      redditBaseUrl, subreddit, parentPostId);
		}

		private string deleteCommentUrl ()
		{   
			return string.Format ("{0}/del/", redditApiUrl); 
		}

		/* API methods */
		public JArray login (string username, string password)
		{
			this.modhash = null;
			this.sessionCookie = null;
			JObject o = postRequest (loginUrl (username),
			  string.Format ("api_type=json&user={0}&passwd={1}",
			                 username, password));
			JObject json = (JObject)o["json"];
			JArray errors = (JArray)json["errors"];
			this.modhash = (string)json["data"]["modhash"];
			string cookie = System.Web.HttpUtility.UrlEncode((string)json["data"]["cookie"]);
			this.sessionCookie = new Cookie("reddit_session", cookie, "/", redditCookieDomain);
			return errors;
			// TODO: error handling
		}

		public string postSelf (string subreddit, string title, string text)
		{
			string queryString = string.Format ("title={0}&text={1}&sr={2}&kind={3}&uh={4}",
			                                    title, text, subreddit, "self", this.modhash);
			JObject o = postRequest (postSelfUrl (), queryString);
			// TODO: error handling (look at jquery[7][3][1] for errors?) (test that index exists?)
			string postUrl = (string)o["jquery"] [10] [3] [0];
			// www.reddit.com/r/mysubreddit/comments/asdf/this_is_my_title
			Match match = Regex.Match (postUrl, @"/comments/([a-z0-9]+)/");
			if (match.Success) {
				return match.Groups[1].Value;
			} else {
				return "";
				// TODO: handle error
			}
		}

		public string postComment (string parentId, string comment)
		{
			string queryString = string.Format ("parent={0}&text={1}&uh={2}",
			                                    linkFullname(parentId), 
			                                    comment, 
			                                    this.modhash);
			JObject o = postRequest (postCommentUrl (), queryString);
			string fullname = (string)o["jquery"][18][3][0][0]["data"]["id"];
			// TODO: error handling
			string id36 = fullname.Split ('_')[1];
			return id36;
		}

		public void deleteComment (string commentId)
		{
			string queryString = string.Format("id={0}_{1}&uh={2}", 
			                                   this.commentPrefix,
			                                   commentId,
			                                   this.modhash);
			postRequest (deleteCommentUrl(), queryString);
			// TODO: error handling
		}

		public Comment getComment (string subreddit, string parentId, string commentId)
		{
			Comment comment = null;
			string json = getRequest (getCommentUrl (subreddit, parentId, commentId));
			JArray items = JArray.Parse (json);
			foreach (JObject item in items) {
				if (((string)item["data"]["children"][0]["data"]["id"]).Equals (commentId)) {
					comment = new Comment((JObject)item["data"]["children"][0]["data"]);
					break;
				}
			}
			return comment;
		}

		/* Request Methods */
		private string getRequest (string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
			if (this.sessionCookie != null) {
				request.CookieContainer = new CookieContainer ();
				request.CookieContainer.Add (this.sessionCookie);
			}
			using (var response = (HttpWebResponse)request.GetResponse()) {
				using (var responseStream = response.GetResponseStream()) {
					using (var streamReader = new StreamReader(responseStream, Encoding.UTF8)) {
						string json = streamReader.ReadToEnd ();
						Console.WriteLine(url);
						Console.WriteLine(json.Substring(0,Math.Min (50, json.Length)) + "...");
						return json;
					}
				}
			}
		}

		private JObject postRequest (string url, string queryString)
		{
			byte[] buffer = Encoding.UTF8.GetBytes (queryString);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			if (this.sessionCookie != null) {
				request.CookieContainer = new CookieContainer ();
				request.CookieContainer.Add (this.sessionCookie);
			}
			using (Stream data = request.GetRequestStream()) {
				data.Write (buffer, 0, buffer.Length);
			}
			using (var response = (HttpWebResponse)request.GetResponse()) {
				using (var responseStream = response.GetResponseStream()) {
					using (var streamReader = new StreamReader(responseStream, Encoding.UTF8)) {
						string json = streamReader.ReadToEnd();
						Console.WriteLine(url + "?" + queryString);
						Console.WriteLine(json.Substring(0,Math.Min(50, json.Length)) + "...");
						JObject o = JObject.Parse(json);
						return o;
					}
				}
			}
		}

	}
}

