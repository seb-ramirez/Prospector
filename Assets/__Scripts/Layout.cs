﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//the slotdef class is not a subclass of monobehavior, so it doesnt need a 
//separate c# file
[System.Serializable] //this makes slotdefs visible in the unity inspector
public class SlotDef {
	public float	x;
	public float	y;
	public bool		faceUp=false;
	public string	layerName="Default";
	public int		layerID = 0;
	public int		id;
	public List<int>		hiddenBy = new List<int>();
	public string		type="slot";
	public Vector2		stagger;
}

public class Layout : MonoBehaviour {
	public PT_XMLReader		xmlr;
	public PT_XMLHashtable 	xml;
	public Vector2		multiplier;
	//slotdef references
	public List<SlotDef>  slotDefs;
	public SlotDef		drawPile;
	public SlotDef		discardPile;
	//this holds all of the possible names for the layers set by layerID

	public string[]		sortingLayerNames = new
	string[] {"Row0","Row1","Row2","Row3","Discard","Draw"};

	//this function is called to read in the layoutxml.xml file
	public void ReadLayout(string xmlText) {
		xmlr = new PT_XMLReader ();
		xmlr.Parse (xmlText);	//the xml is parsed
		xml = xmlr.xml ["xml"] [0]; //and xml is set as a shortcut to the xml

		//read in the multiplier, which sets card spacing
		multiplier.x = float.Parse (xml ["multiplier"] [0].att ("x"));
		multiplier.y = float.Parse (xml ["multiplier"] [0].att ("y"));

		//read in the slots
		SlotDef tSD;
		// slotsX is used as a shortcut to all the ,slot.s
		PT_XMLHashList slotsX = xml ["slot"];

		for (int i=0; i<slotsX.Count; i++) {
			tSD = new SlotDef (); // create new slotdef instance
			if (slotsX [i].HasAtt ("type")) {
				//if this <slot> has a type attribute parse it
				tSD.type = slotsX [i].att ("type");
			} else {
				// if not, set itss type to "slot"; its a tableau card
				tSD.type = "slot";
			}
			//various attributes are parsed into numerical values
			tSD.x = float.Parse (slotsX [i].att ("x"));
			tSD.y = float.Parse (slotsX [i].att ("y"));
			tSD.layerID = int.Parse (slotsX [i].att ("layer"));
			//this converts the number of the layerID into a text layername
			tSD.layerName = sortingLayerNames [tSD.layerID ];
			//the layers are used to make sure that the correct cards are
			// on top of the others. in unity 2d, all of our assets are
			//effectively at the same z depth, so the layer is used
			//to differentiate between them.

			switch (tSD.type) {
			//pull additional attributes based on the type of this <slot>
			case "slot":
				tSD.faceUp = (slotsX [i].att ("faceup") == "1");
				tSD.id = int.Parse (slotsX [i].att ("id"));
				if (slotsX [i].HasAtt ("hiddenby")) {
					string[] hiding = slotsX [i].att ("hiddenby").Split (',');
					foreach (string s in hiding) {
						tSD.hiddenBy.Add (int.Parse (s));
					}
				}
				slotDefs.Add (tSD);
				break;

			case "drawpile":
				tSD.stagger.x = float.Parse (slotsX [i].att ("xstagger"));
				drawPile = tSD;
				break;
			case "discardpile":
				discardPile = tSD;
				break;
			}
		}
	}



}
