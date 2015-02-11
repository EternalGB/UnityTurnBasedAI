using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenericTurnBasedAI
{
	
	public class TurnEngineSingleThreaded : TurnEngine
	{
		
		
		public TurnEngineSingleThreaded (Evaluator eval, int timeLimit, int depthLimit, bool collectStats = false)
		{
			InitEngine(eval,timeLimit,depthLimit,true,collectStats);
		}
		
		public TurnEngineSingleThreaded (Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			if(timeLimited)
				InitEngine(eval,limit,int.MaxValue,timeLimited,collectStats);
			else
				InitEngine(eval,int.MaxValue,limit,timeLimited,collectStats);
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
			foreach(Turn turn in root.GeneratePossibleTurns()) {
				rootTurns.Add(turn);
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
				
				List<Turn> potentialTurns = new List<Turn>();
				
				float bestValue = eval.minValue;
				foreach(Turn turn in rootTurns) {
					
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
					alpha = Math.Max(alpha,bestValue);
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
					beta = Math.Min(beta,worstValue);
					if(beta <= alpha) {
						break;
					}
				}
				return worstValue;
			}
		}
		
		
	}
	
}
