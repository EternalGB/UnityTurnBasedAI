namespace GenericTurnBasedAI
{
	public abstract class HashableGameState : GameState
	{
		public abstract override int GetHashCode ();

		public override bool Equals (object obj)
		{
			HashableGameState other = obj as HashableGameState;
			if(other != null)
				return other.GetHashCode() == GetHashCode();
			else
				return false;
		}
	}
}

