using System.Collections.Generic;

namespace UniversalTurnBasedAI
{
	public abstract class Turn 
	{
		public abstract GameState ApplyTurn(GameState state);
	}
}



