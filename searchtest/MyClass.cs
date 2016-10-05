using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

namespace searchtest
{

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class SearchTest : MonoBehaviour
	{
		private Rect window_pos = new Rect(200,200,400,5);

		private Dictionary<string, Dictionary<string, int>> part_map = new Dictionary<string, Dictionary<string, int>>();
		private Dictionary<string, AvailablePart> part_lookup = new Dictionary<string, AvailablePart>();

		private List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();
		private string search_str = "";

		private bool use_tag_attributes = true;

		//populate the Dictionary "part_map" of part names to tags with various scores.
		//for each part it creates 'tags' from individual words in various attributes and assigns each tag a score (based on which attribute they came from)
		//tags created from the parts name have the highest scores, while those created from the description have the lowest scores.
		//all tags are created a trimmed lowercase strings.
		private void create_tags(){
			foreach(AvailablePart part in PartLoader.LoadedPartsList){
				part_lookup.Add(part.name, part);
				Dictionary<string, int> tags = new Dictionary<string, int>();
				//create tags from part's title (highest scoring tags)
				foreach(string tag in Regex.Split(part.title, @"/\W/")){
					if(!string.IsNullOrEmpty(tag)){
						tags.Add(tag.ToLower().Trim(), 12);                    
					}
				}
				//create tags from part's name
				foreach(string tag in Regex.Split(part.name, @"/\W/")){
					if(!string.IsNullOrEmpty(tag)){
						tags.Add(tag.ToLower().Trim(), 6);                    
					}
				}
				//create tags from part's...erm..tags, ignoring tags starting with ? cos I'm not sure what that's about.
				if(use_tag_attributes){
					foreach(string tag in Regex.Split(part.tags, @"/\W/")){
						if(!string.IsNullOrEmpty(tag) && !tag.StartsWith("?")){
							tags.Add(tag.ToLower().Trim(), 4);                    
						}
					}
				}
				//create tags from part's description (lowest scoring tags.
				foreach(string tag in Regex.Split(part.description, @"/\W/")){
					if(!string.IsNullOrEmpty(tag)){
						tags.Add(tag.ToLower().Trim(), 2);                    
					}
				}
				part_map.Add(part.name, tags); //add dictionation of (string)tag to (int) score to outter dictionary - part.name -> dictionary of tags-to-scores.
			}
		}

		private void rebuild_tags(){
			part_map = new Dictionary<string, Dictionary<string, int>>();
			part_lookup = new Dictionary<string, AvailablePart>();
			create_tags();
		}

		//search looks at the tags for each part and generates a total score for each part.  
		//If a tag matches the whole search str then the score is incremented by the tags point value, 
		//if the tag contans the whole search string score gets incremented by half the tag's points
		//Search string is then split into individual words and if a tag matches one of the words entirely then score is incremented by tags points
		//and again, if tag contains the word score gets incremented by half the tag's points.
		//The resultant score for each part is added to part_scores against the part's name and this is finally returned as
		//a List of key values pairs<string, int> sorted by the int score.
		private List<KeyValuePair<string, int>> search(string search_str){
			search_str = search_str.ToLower().Trim();
			Dictionary<string, int> part_scores = new Dictionary<string, int>();

			foreach(KeyValuePair<string, Dictionary<string, int>> part_tags  in part_map){
				int score = 0;
				foreach(KeyValuePair<string, int> tag in part_tags.Value){
					if(tag.Key == search_str){ 
						score += tag.Value;			//tag matches whole search string, score incremented by tag's points
					}else if(tag.Key.Contains(search_str)){ 
						score += tag.Value / 2;		//tag contains the search string, score incremented by half tag's points
					}
					foreach(string search_word in Regex.Split(search_str, @"/\W/")){
						if(tag.Key == search_word){
							score += tag.Value;		//tag matches single word from search string, score incremented by tag's points
						}else if(tag.Key.Contains(search_word)){
							score += tag.Value / 2;	//tag contains single word from search stirng, score incremented by half tag's points.
						}
					}
				}
				part_scores.Add(part_tags.Key, score);
			}
			//sort and return list of part.name -> search score.
			List<KeyValuePair<string, int>> sorted_list = part_scores.ToList();
			sorted_list.Sort((pair1,pair2) => pair2.Value.CompareTo(pair1.Value));
			return sorted_list;
		}


		//Start calls map_parts to create the tags for each part
		private void Start(){
			create_tags();
		}

		//basic GUI stuff
		private void OnGUI(){
			window_pos = GUILayout.Window(42, window_pos, DrawWindow, "Search Test", GUILayout.Width(window_pos.width), GUILayout.MaxWidth(window_pos.width), GUILayout.ExpandHeight(true));
		}

		private void DrawWindow(int window_id){
			search_str = GUILayout.TextField(search_str);
			if(GUI.changed){
				window_pos.height = 5;
				results = search(search_str);
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("use tag attrs")){
				use_tag_attributes = true;
				rebuild_tags();
				results = search(search_str);
			}
			if (GUILayout.Button("Ignore tag attrs")){
				use_tag_attributes = false;
				rebuild_tags();
				results = search(search_str);
			}	
			GUILayout.EndHorizontal();
			GUILayout.Label("Using tag attributes: " + use_tag_attributes.ToString());
			GUILayout.Space(10);
			foreach(KeyValuePair<string, int> r in results){
				if(r.Value > 0){
					GUILayout.Label(part_lookup[r.Key].title + " - " + r.Value);
				}
			}
			GUI.DragWindow();
		}
	}
}

