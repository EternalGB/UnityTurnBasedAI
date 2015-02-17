using System.Collections;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// An Evaluator defines an evaluation function to determine the
	/// value of a <see cref="IGameState"/> from the point of view of a particular player.
	/// 
	/// <seealso cref="TurnEngine"/>
	/// <seealso cref="IGameState"/>
	/// <seealso cref="ITurn"/>
	/// </summary>
	public interface IEvaluator 
	{

		/// <summary>
		/// Gets the minimum value any state can have
		/// </summary>
		/// <returns>The minimum value.</returns>
		float GetMinValue();

		/// <summary>
		/// Gets the max value any state can have
		/// </summary>
		/// <returns>The max value.</returns>
		float GetMaxValue();

		/// <summary>
		/// Evaluate the specified GameState. Good evaluation functions should return <see cref="GetMaxValue"/>
		/// on a winning state and <see cref="GetMinValue"/> on a losing state. This method must also provide
		/// value to non-terminal states that give the engine some indication of whether the player is closer
		/// or further away from winning.
		/// 
		/// As this will need to be called on every searched <see cref="IGameState"/> the efficiency of this
		/// method is directly related to the performance of a <see cref="TurnEngine"/>. 
		/// </summary>
		/// <param name="state">The state to evaluate</param>
		float Evaluate(IGameState state);

	}
}



