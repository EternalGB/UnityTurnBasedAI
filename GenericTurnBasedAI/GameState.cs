using System.Collections.Generic;

namespace GenericTurnBasedAI
{
	public abstract class GameState
	{
		
		public abstract bool IsTerminal();
		
		public abstract IEnumerable<Turn> GeneratePossibleTurns();
		
		public abstract GameState Clone();
		
	}
}



