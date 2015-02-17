using System.Collections;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// The super-class for all Evaluators. An Evaluator defines an evaluation function to determine the
	/// value of a <see cref="GameState"/> from the point of view of a particular player.
	/// 
	/// <seealso cref="TurnEngine"/>
	/// <seealso cref="GameState"/>
	/// <seealso cref="Turn"/>
	/// </summary>
	public abstract class Evaluator 
	{
		
		public float minValue = float.MinValue;
		public float maxValue = float.MaxValue;

		/// <summary>
		/// Evaluate the specified GameState. Good evaluation functions should return <see cref="maxValue"/>
		/// on a winning state and <see cref="minValue"/> on a losing state. This method must also provide
		/// value to non-terminal states that give the engine some indication of whether the player is closer
		/// or further away from winning.
		/// 
		/// As this will need to be called on every searched <see cref="GameState"/> the efficiency of this
		/// method is directly related to the performance of a <see cref="TurnEngine"/>. 
		/// </summary>
		/// <param name="state">The state to evaluate</param>
		public abstract float Evaluate(GameState state);

	}
}



