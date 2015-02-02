using System;

namespace GenericTurnBasedAI
{


	public class Minimax
	{

		Evaluator eval;

		public float MinValue
		{
			get {return eval.minValue;}
		}

		public float MaxValue
		{
			get {return eval.maxValue;}
		}

		public Minimax(Evaluator eval)
		{
			this.eval = eval;
		}

		public float EvaluateState(GameState state, int maxDepth, bool ourTurn)
		{
			return AlphaBeta(state,maxDepth,eval.minValue,eval.maxValue,ourTurn);
		}

		float AlphaBeta(GameState state, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				return eval.Evaluate(state);
			}
			if(ourTurn) {
				float bestValue = eval.minValue;
				foreach(Turn turn in state.GeneratePossibleTurns()) {
					GameState nextState = turn.ApplyTurn(state.Clone());
					float value = AlphaBeta(nextState,depth-1,alpha,beta,false);
					if(value > bestValue) {
						
						bestValue = value;
					}
					value = Math.Max(value,bestValue);
					alpha = Math.Max(alpha,value);
					if(beta <= alpha) {
						break;
					}
					
				}
				return bestValue;
			} else {
				float worstValue = eval.maxValue;
				foreach(Turn turn in state.GeneratePossibleTurns()) {
					GameState nextState = turn.ApplyTurn(state.Clone());
					float value = AlphaBeta(nextState,depth-1,alpha,beta,true);
					if(value < worstValue) {
						worstValue = value;
					}
					value = Math.Min(value,worstValue);
					beta = Math.Min(beta,value);
					if(beta <= alpha) {
						break;
					}
				}
				return worstValue;
			}
		}
	}
}


