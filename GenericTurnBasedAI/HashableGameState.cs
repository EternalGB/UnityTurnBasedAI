namespace GenericTurnBasedAI
{
	public abstract class HashableGameState : GameState
	{
		public abstract override int GetHashCode ();
	}
}

