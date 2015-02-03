using System;
using System.Threading;

namespace GenericTurnBasedAI
{


	public class Minimax
	{

		GameState rootState;
		public Turn firstTurn;
		int maxDepth;
		bool ourTurn;
		Evaluator eval;
		EventWaitHandle waitHandle;
		public float Value
		{
			get; private set;
		}
		bool stop;

		public float MinValue
		{
			get {return eval.minValue;}
		}

		public float MaxValue
		{
			get {return eval.maxValue;}
		}

		public Minimax (GameState rootState, Turn firstTurn, Evaluator eval, int maxDepth, bool ourTurn, EventWaitHandle waitHandle)
		{
			this.rootState = rootState;
			this.firstTurn = firstTurn;
			this.maxDepth = maxDepth;
			this.ourTurn = ourTurn;
			this.eval = eval;
			this.waitHandle = waitHandle;
			stop = false;
		}
		

		public void EvaluateState(object threadState)
		{
			GameState state = firstTurn.ApplyTurn(rootState.Clone());
			Value = AlphaBeta(state,eval,maxDepth,eval.minValue,eval.maxValue,ourTurn);
			waitHandle.Set();
		}

		public float AlphaBeta(GameState state, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				return eval.Evaluate(state);
			}
			if(ourTurn) {
				float bestValue = eval.minValue;
				foreach(Turn turn in state.GeneratePossibleTurns()) {
					if(stop)
						return eval.minValue;
					GameState nextState = turn.ApplyTurn(state.Clone());
					float value = AlphaBeta(nextState,eval,depth-1,alpha,beta,false);
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
					if(stop)
						return eval.minValue;
					GameState nextState = turn.ApplyTurn(state.Clone());
					float value = AlphaBeta(nextState,eval,depth-1,alpha,beta,true);
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

		public void Stop()
		{
			stop = true;
		}
	}


}


