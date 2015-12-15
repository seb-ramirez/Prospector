using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ScoreEvent{
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}


public class Prospector : MonoBehaviour {

	static public Prospector 	S;
	static public int 	SCORE_FROM_PREV_ROUND = 0;
	static public int	HIGH_SCORE = 0;

	public float		reloadDelay = 1f;

	public Vector3		fsPosMid = new Vector3 (0.5f, 0.90f, 0);
	public Vector3		fsPosRun = new Vector3 (0.5f, 0.75f, 0);
	public Vector3		fsPosMid2 = new Vector3 (0.5f, 0.5f, 0);
	public Vector3		fsPosEnd = new Vector3 (1.0f, 0.65f, 0);


	public Deck					deck;
	public TextAsset			deckXML;

	public Layout	layout;
	public TextAsset	layoutXML;
	public Vector3		layoutCenter;
	public float	xOffset = 3;
	public float	yOffset = -2.5f;
	public Transform	layoutAnchor;

	public CardProspector 	target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;


	public FloatingScore	fsRun;
	
	public GUIText		GTGameOver;
	public GUIText		GTRoundResult;

	void Awake(){
		S = this;
		if (PlayerPrefs.HasKey ("ProspectorHighScore")) {
			HIGH_SCORE = PlayerPrefs.GetInt ("ProspectorHighScore");
		}
		score += SCORE_FROM_PREV_ROUND;
		SCORE_FROM_PREV_ROUND = 0;

		GameObject go = GameObject.Find ("GameOver");
		if (go != null) {
			GTRoundResult = go.GetComponent<GUIText> ();
		}
		go = GameObject.Find ("RoundResult");
		if (go != null) {
			GTRoundResult = go.GetComponent<GUIText> ();
		}

		ShowResultGTs (false);

		go = GameObject.Find ("HighScore");
		string hScore = "High Score: " + Utils.AddCommasToNumber (HIGH_SCORE);
		go.GetComponent<GUIText> ().text = hScore;

	}
	void ShowResultGTs(bool show){
		GTGameOver.gameObject.SetActive (show);
		GTRoundResult.gameObject.SetActive (show);
	}

	public List<CardProspector> drawPile;


	//fields to track score info
	public int		chain = 0;
	public int		scoreRun = 0;
	public int 		score = 0;




	void Start() {
		Scoreboard.S.score = score;
		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);

		Deck.Shuffle (ref deck.cards); //this shuffles the deck
	//the ref keyword passes a reference to deck.cards, which allows 
		//deck.cards to be modified by deck.shuffle()

		layout = GetComponent<Layout>(); // get the layout
		layout.ReadLayout(layoutXML.text); //pass layoutxml to it

		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame ();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card>ICD) {
		List<CardProspector> ICP = new List<CardProspector> ();
		CardProspector tCP;
		foreach (Card tCD in ICD) {
			tCP = tCD as CardProspector;
			ICP.Add (tCP);
		}
		return(ICP);
	}

	//the draw function will pull a single card from the drawpile and return it
	CardProspector Draw(){
	CardProspector cd = drawPile[0]; //pull the 0th cardprospector
	drawPile.RemoveAt(0); //then remove it from list<> drawpile
	return(cd);  //and return it
}

	//convert from the layoutid int to the cardprospector with that id
	CardProspector FindCardByLayoutID(int layoutID){
		foreach (CardProspector tCP in tableau) {
			//search through all cards in the tableau list<>
			if (tCP.layoutID == layoutID) {
				return(tCP);
			}
		}
		//if its not found, return null
		return(null);
	}


	//layoutgame() positions the initial tableau of cards, aka the "mine"
	void LayoutGame (){
		//create an empty gameobject to serve as an anchor for the tableau //
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject ("_LayoutAnchor");
			//^creates an empty gameobject named _layoutanchor in the hierarchy
			layoutAnchor = tGO.transform;	//grab its transform
			layoutAnchor.transform.position = layoutCenter;	// position it
		}

		CardProspector cp;
		//follow the layout
		foreach (SlotDef tSD in layout.slotDefs) {
			// ^iterate through all the slotdefs in the layout.slotdefs as tsd
			cp = Draw (); // pull a card from the top (beginning) of the drawpile
			cp.faceUp = tSD.faceUp; // set its faceup to the value in slotdef
			cp.transform.parent = layoutAnchor; // make its parent layoutanchor
			//this replaces the previous parent:deck.deckanchor, which appears 
			//as_deck in the hierarchy when the scene is playing.
			cp.transform.localPosition = new Vector3 (
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID);
			//^set the localposition of the card based on slotdef
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.tableau;
			//cardprospectors in the tableau have the state cardstate.tableau

			cp.SetSortingLayerName(tSD.layerName); //set the sorting layers

			tableau.Add (cp); //add this cardprospector to the list<> tableau
		}

		//set which cards are hiding others
		foreach (CardProspector tCP in tableau) {
			foreach (int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutID (hid);
				tCP.hiddenBy.Add (cp);
			}
		}



		MoveToTarget (Draw ());

		UpdateDrawPile ();
	}

	//cardclicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd){
		//the reaction is determined by the state of the clicked card
		switch (cd.state){
			case CardState.target:
			//clicking the target card does nothing
			break;
			case CardState.drawpile:
			MoveToDiscard(target);
			MoveToTarget(Draw());
			UpdateDrawPile();

			ScoreManager(ScoreEvent.draw);
			break;

			case CardState.tableau:
			bool validMatch = true;
			if(!cd.faceUp){
				validMatch = false;
			}
			if (!AdjacentRank(cd, target)){
				validMatch = false;
			}
			if(!validMatch) return;
			tableau.Remove(cd);
			MoveToTarget(cd);
			SetTableauFaces();

			ScoreManager(ScoreEvent.mine);

			break;
		}


		//check to see whether the game is over or not
		CheckForGameOver ();
	}

	//moves the current target to the discardpile
	void MoveToDiscard(CardProspector cd){
		cd.state = CardState.discard;
		discardPile.Add (cd);
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID + 0.5f);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (-100 + discardPile.Count);
	}

	//make cd the new target card
	void MoveToTarget(CardProspector cd) {
		//if there is currently a target card, move it to discardpile
		if (target != null)
			MoveToDiscard (target);
		target = cd;
		cd.state = CardState.target;
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (0);
	}

	//arranges all the cards of the drawpile to show how many are left
	void UpdateDrawPile(){
		CardProspector cd;
		for (int i=0; i<drawPile.Count; i++) {
			cd = drawPile [i];
			cd.transform.parent = layoutAnchor;
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3 (
				layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
				-layout.drawPile.layerID + 0.1f * i);
			cd.faceUp = false;
			cd.state = CardState.drawpile;

			cd.SetSortingLayerName (layout.drawPile.layerName);
			cd.SetSortOrder (-10 * i);
		}
	}


	//return true if the two cards are adjacent in rank (A & K wrap around)
	public bool AdjacentRank(CardProspector c0, CardProspector c1){
		if (!c0.faceUp | !c1.faceUp)
			return(false);
		// if they are 1 apart, they are adjacent
		if (Mathf.Abs (c0.rank - c1.rank) == 1) {
			return(true);
		}
		if (c0.rank == 1 && c1.rank == 13)
			return(true);
		if (c0.rank == 13 && c1.rank == 1)
			return(true);

		return (false);
	}

	// this turns cards in the mine face-up or face-down
	void SetTableauFaces(){
		foreach (CardProspector cd in tableau) {
			bool fup = true;
			foreach (CardProspector cover in cd.hiddenBy) {
				// if either of the covering cards are in tableau
				if (cover.state == CardState.tableau) {
					fup = false;
				}
			}
			cd.faceUp = fup;
		}
	}

	//test whether the game is over
	void CheckForGameOver(){
		if (tableau.Count == 0) {
			GameOver (true);
			return;
		}
		if (drawPile.Count > 0) {
			return;
		}
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank (cd, target)) {
				return;
			}
		}
		//since there are no valid plays, the game is over
		GameOver (false);
	}
	//called when the game is over. simple for now but expandable
	void GameOver(bool won){
		if (won) {
			ScoreManager (ScoreEvent.gameWin);
		} else {
			ScoreManager (ScoreEvent.gameLoss);

		}

		Invoke ("ReloadLevel", reloadDelay);
	}

		void ReloadLevel(){
		//reload the scene, resetting the game
		Application.LoadLevel ("__Prospector_Scene_0");
	
	}

	//scoremanager handles all of the scoring
	void ScoreManager(ScoreEvent sEvt){
		List<Vector3> fsPts;
		switch (sEvt) {
		case ScoreEvent.draw:
		case ScoreEvent.gameWin:
		case ScoreEvent.gameLoss:
			chain = 0;
			score += scoreRun;
			scoreRun = 0;

			if (fsRun != null){
				fsPts = new List<Vector3>();
				fsPts.Add( fsPosRun);
				fsPts.Add( fsPosMid2);
				fsPts.Add( fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init (fsPts, 0, 1);
				fsRun.fontSizes = new List<float>(new float[]{28,36,4});
				fsRun = null;
			}

			break;
		case ScoreEvent.mine:
			chain++;
			scoreRun += chain;

			FloatingScore fs;
			Vector3 p0 = Input.mousePosition;
			p0.x /= Screen.width;
			p0.y /= Screen.height;
			fsPts = new List<Vector3>();
			fsPts.Add( p0 );
			fsPts.Add( fsPosMid );
			fsPts.Add (fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore(chain,fsPts);
			fs.fontSizes = new List<float>(new float[]{4,50,28});
			if (fsRun == null){
				fsRun = fs;
				fsRun.reportFinishTo = null;
			}else{
				fs.reportFinishTo = fsRun.gameObject;
			}
			break;
		}

		//this second switch statement handles round wins and losses
		switch (sEvt) {
		case ScoreEvent.gameWin:
			GTGameOver.text = "Round Over";
			Prospector.SCORE_FROM_PREV_ROUND = score;
			print ("You won this round! Round score:" + score);
			GTRoundResult.text = "You won this round!\nRound Score:"+score;
			ShowResultGTs (true);
			break;
		case ScoreEvent.gameLoss:
			GTGameOver.text = "Game Over";
			if (Prospector.HIGH_SCORE <= score) {
				print ("You got the high score! high score:" + score);
				string sRR = "You got the high score!\nHigh Score:"+score;
				GTRoundResult.text = sRR;
				Prospector.HIGH_SCORE = score;
				PlayerPrefs.SetInt ("ProspectorHighScore", score);
			} else {
				print ("Your final score for the game was:" + score);
				GTRoundResult.text = "Your final score was:" +score;
			}
			ShowResultGTs(true);
			break;
		default:
			print ("score:" + score + " scoreRun:" + scoreRun + " chain:" + chain);
			break;
		}
	}




}
