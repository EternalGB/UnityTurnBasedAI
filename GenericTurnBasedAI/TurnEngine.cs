﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace GenericTurnBasedAI
{

	public class TurnEngine
	{
		
		int maxDepth;
		Evaluator eval;
		Turn bestTurn;
		bool timeLimited = false;
		float maxTime;
		System.Random rando;
		public EngineStats Stats
		{
			get; private set;
		}

		bool collectStats = false;
		
		public delegate void TurnReady(Turn bestTurn);
		public event TurnReady TurnReadyEvent;
		
		
		public TurnEngine(Evaluator eval, int limit, bool timeLimited, bool collectStats)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
		}



		
		void InitEngine(Evaluator eval, int limit, bool timeLimited, bool collectStats)
		{
			if(limit <= 0)
				throw new ArgumentOutOfRangeException("limit");
			this.timeLimited = timeLimited;
			this.collectStats = collectStats;
			if(timeLimited) {
				this.maxTime = limit;
				maxDepth = int.MaxValue;
			} else
				this.maxDepth = limit;
			if(collectStats) {
				Stats = new EngineStats();
			}
			this.eval = eval;


			rando = new System.Random((int)DateTime.Now.Ticks);
		}
		
		public System.Collections.IEnumerator GetNextTurn(GameState state) 
		{
			bestTurn = null;
			Thread thread = new Thread(MinimaxCaller);
			thread.Start(state);
			while(thread.IsAlive) {
				yield return 0;
			}
			if(TurnReadyEvent != null)
				TurnReadyEvent(bestTurn);
		}
		
		public void MinimaxCaller(object state)
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
			}
			//this is so we can bail out without evaluating any turns
			results = rootTurns;

			int depth;
			for(depth = 1; depth <= maxDepth && !exit; depth++) {
				List<Turn> potentialTurns = new List<Turn>();
				
				float bestValue = eval.minValue;
				foreach(Turn turn in rootTurns) {
					if(timeLimited && results != null && DateTime.Now.Subtract(startTime).Seconds >= maxTime) {
						exit = true;
						break;
					}
					
					
					GameState nextState = turn.ApplyTurn(root.Clone());
					float value = AlphaBeta(nextState,depth-1,eval.minValue,eval.maxValue,false);
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
				} else 
					//for debugging/logging purposes
					depth--;
				bestTurn = GetRandomElement<Turn>(results);
			}
			if(collectStats)
				Stats.Log(depth,DateTime.Now.Subtract(startTime).Seconds);
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
		
		static T GetRandomElement<T>(IList<T> list)
		{
			System.Random rando = new System.Random((int)DateTime.Now.Ticks);
			return list[rando.Next (list.Count)];
		}
		
		
		public EngineStats ResetStatisticsLog()
		{
			if(collectStats) {
				EngineStats old = Stats;
				Stats = new EngineStats();
				return old;
			} else
				return null;
		}
		
	}

}

