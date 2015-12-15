using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// this is an enum, which defines a type of variable that only has a few
//possible named values. the cardstate variable type has one of four values
//drawpile, tableau, target, & discard
public enum CardState{
	drawpile,
	tableau,
	target,
	discard
}

public class CardProspector : Card { // Make sure CardProspector extends card
	// this is how you use the enum cardstate
	public CardState		state = CardState.drawpile;
	// the hiddenby list stores which other cards will keep this one face down
	public List<CardProspector> hiddenBy = new List<CardProspector>();
	//layoutID matches this card to a layout xml id if its a tableau card
	public int 		layoutID;
	// The SlotDef class stores information pulled in from the layoutxml <slot>
	public SlotDef	slotDef;

	//this allows the card to react to being clicked
	override public void OnMouseUpAsButton(){
		//call the cardclicked method on the prospector singleton
		Prospector.S.CardClicked (this);
		//also call the base class (card.cs) version of this method
		base.OnMouseUpAsButton ();
	}
}
	
