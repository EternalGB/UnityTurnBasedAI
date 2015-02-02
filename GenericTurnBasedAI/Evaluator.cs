using System.Collections;

namespace GenericTurnBasedAI
{
	public abstract class Evaluator 
	{
		
		public float minValue = float.MinValue;
		public float maxValue = float.MaxValue;
		
		public abstract float Evaluate(GameState state);

		public abstract Evaluator Clone();

	}
}



