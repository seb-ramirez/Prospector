using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {

	public string    suit;
	public int       rank;
	public Color     color = Color.black;
	public string    colS = "Black";  // or "Red"
	
	public List<GameObject> decoGOs = new List<GameObject>();
	public List<GameObject> pipGOs = new List<GameObject>();
	
	public GameObject back;  // back of card;
	public CardDefinition def; // from DeckXML.xml	
	// list of the spriterenderer components of this gameobject and its children
	public SpriteRenderer[]		spriteRenderers;

	void Start() {
		SetSortOrder (0); //ensures that the card starts properly depth sorted
	}

	public bool faceUp {
		get {
			return(!back.activeSelf);
		}
		set {
			back.SetActive (!value);
		}
	}
	// if spriterenderers is not yet defined, this function defines it
	public void PopulateSpriteRenderers(){
		//if spriterenderers is null or empty
		if (spriteRenderers == null | spriteRenderers.Length == 0) {
			//get spriterender components of this gameobject and its children
			spriteRenderers = GetComponentsInChildren<SpriteRenderer> ();
		}
	}

	//sets the sortinglayername on all spriterenderer components
	public void SetSortingLayerName(string tSLN){
		PopulateSpriteRenderers ();

		foreach (SpriteRenderer tSR in spriteRenderers){
			tSR.sortingLayerName = tSLN;
		}
	}

	//sets the sortingorder of all spriterenderer components
	public void SetSortOrder(int sOrd){
		PopulateSpriteRenderers ();

		//the white background of the card is on bottom (sOrd)
		//on top of that are all the pips, decorators, face, etc. (sOrd +1)
		//the back is on top so that when visible, it covers the rest (sOrd+2)

		//iterate through all the spriterenderers as tSR
		foreach (SpriteRenderer tSR in spriteRenderers) {
			if (tSR.gameObject == this.gameObject) {
				//if the gameobject is this.gameobject, its the background
				tSR.sortingOrder = sOrd; //set its order to dOrd
				continue; //and continue to the next iteration of the loop

			}
			//each of the children of this gameobject are named
			// switch based on the names
			switch (tSR.gameObject.name) {
			case "back": //if the name is "back"
				tSR.sortingOrder = sOrd + 2;
				//^set it to the highest layer to cover everything else
				break;
			case "face":
			default:
				tSR.sortingOrder = sOrd + 1;
				break;
			}
		}
	}

	//virtual methods can be overridden by subclass methods with the same name
	virtual public void OnMouseUpAsButton() {
		print (name); //when clicked, this outputs the card name
	}
} // class Card

[System.Serializable]
public class Decorator{
	public string	type;			// For card pips, tyhpe = "pip"
	public Vector3	loc;			// location of sprite on the card
	public bool		flip = false;	//whether to flip vertically
	public float 	scale = 1.0f;
}

[System.Serializable]
public class CardDefinition{
	public string	face;	//sprite to use for face cart
	public int		rank;	// value from 1-13 (Ace-King)
	public List<Decorator>	
					pips = new List<Decorator>();  // Pips Used
}
