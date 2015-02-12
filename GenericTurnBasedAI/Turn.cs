using System.Collections.Generic;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// A super-class for Turns. Represents the sum of actions that a player can take on their turn
	/// in the game.
	/// 
	/// <seealso cref="GameState"/>
	/// <seealso cref="TurnEngine"/>
	/// </summary>
	public abstract class Turn 
	{
		/// <summary>
		/// Applies this <see cref="Turn"/> to <paramref name="state"/> giving the resulting <see cref="GameState"/>.
		/// The <see cref="TurnEngine"/> clones <paramref name="state"/> before passing it to this function when called internally
		/// to prevent the original GameState from being overridden
		/// <seealso cref="GameState"/>
		/// <seealso cref="TurnEngine"/>
		/// </summary>
		/// <returns>The GameState that is a result of applying this turn to <paramref name="state"/>.</returns>
		/// <param name="state">The state to apply the turn to.</param>
		public abstract GameState ApplyTurn(GameState state);
	}
}



