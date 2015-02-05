using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenericTurnBasedAI
{

	public class TurnEngineSingleThreaded : TurnEngine
	{


		public TurnEngineSingleThreaded(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
		}
	
		protected override void TurnSearchDelegate(object state)
		{

			DateTime startTime = new DateTime(DateTime.Now.Ticks);
			bool exit = false;
			List<Turn> results = null;
			float resultsValue = eval.minValue;
			GameState root = (GameState)state;
			
			
			
			//precompute the first level so we don't have to every time
			List<Turn> rootTurns = new List<Turn>();
			List<TurnValuePair> rootValues = new List<TurnValuePair>();
			foreach(Turn turn in root.GeneratePossibleTurns()) {
				rootTurns.Add(turn);
				rootValues.Add(new TurnValuePair(turn,eval.minValue));
				if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
					exit = true;
					break;
				}
			}
			//this is so we can bail out without evaluating any turns
			results = rootTurns;
			if(exit) {
				bestTurn = GetRandomElement<Turn>(results);
				return;
			}
			
			int depth;
			for(depth = 1; depth <= maxDepth && !exit; depth++) {
				rootValues.Sort();
				List<Turn> potentialTurns = new List<Turn>();
				float bestValue = eval.minValue;
				for(int i = 0; i < rootTurns.Count; i++) {
					Turn turn = rootValues[i].turn;

					//Debug.Log("Searching turn " + turn.ToString() + " to depth " + depth);
					if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
						exit = true;
						break;
					}
					
					
					GameState nextState = turn.ApplyTurn(root.Clone());
					float value = AlphaBeta(nextState,eval,depth-1,eval.minValue,eval.maxValue,false);
					if(value >= bestValue) {
						if(value > bestValue) {
							bestValue = value;
							potentialTurns.Clear();
						}
						potentialTurns.Add(turn);
					}
					rootValues[i].value = value;
				}
				//only overwrite the results if we haven't aborted mid search
				if(!exit) {
					results = potentialTurns;
					resultsValue = bestValue;
				} else if(timeLimited)
					//for debugging/logging purposes
					depth--;
				bestTurn = GetRandomElement<Turn>(results);
			}
			if(collectStats)
				Stats.Log(depth,DateTime.Now.Subtract(startTime).Seconds);
		}

		public float AlphaBeta(GameState state, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				return eval.Evaluate(state);
			}
			if(ourTurn) {
				float bestValue = eval.minValue;
				foreach(Turn turn in state.GeneratePossibleTurns()) {
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

		public class TurnValuePair : IComparable<TurnValuePair>
		{
			public Turn turn;
			public float value;

			public TurnValuePair (Turn turn, float value)
			{
				this.turn = turn;
				this.value = value;
			}

			public int CompareTo (TurnValuePair other)
			{
				if(other == null) return 1;

				float diff = other.value - value;
				if(diff < 0)
					return -1;
				else if(diff > 0)
					return 1;
				else
					return 0;
			}
			
		}

	}

}
