using System;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// An Evaluator that returns random values between <see cref="min/> and <see cref="max"/> for
	/// every state. Can be useful to test other evaluation functions. Any evaluation function
	/// should be at least as good as selecting moves randomly.
	/// </summary>
	public class EvaluatorRandom : Evaluator
	{
		
		float min, max;
		Random rando;
		
		public EvaluatorRandom(float min, float max)
		{
			this.min = min;
			this.max = max;
			rando = new Random((int)DateTime.Now.Ticks);
		}
		
		public override float Evaluate (GameState state)
		{
			return min + (float)rando.NextDouble()*(max-min);
		}

		public override Evaluator Clone ()
		{
			return new EvaluatorRandom(min,max);
		}
	}
}


