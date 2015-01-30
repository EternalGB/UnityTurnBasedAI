using System.Collections.Generic;

namespace GenericTurnBasedAI
{
	public abstract class Turn 
	{
		public abstract GameState ApplyTurn(GameState state);
	}
}



