using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;

namespace GenericTurnBasedAI
{
	
	public class TurnEngineSingleThreadedWithHashing : TurnEngine
	{
		

		Dictionary<HashableGameState, HashSet<HashableGameState>> stateGenerationTable;
		Dictionary<HashableGameState, float> evaluationTable;
		int tableSize = 1000000;

		public TurnEngineSingleThreadedWithHashing(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
			InitHashing();
		}
		
		void InitHashing()
		{
			stateGenerationTable = new Dictionary<HashableGameState, HashSet<HashableGameState>>(tableSize);
			evaluationTable = new Dictionary<HashableGameState, float>(tableSize);
		}

		protected override void TurnSearchDelegate(object state)
		{
			DateTime startTime = new DateTime(DateTime.Now.Ticks);
			bool exit = false;
			List<Turn> results = null;
			float resultsValue = eval.minValue;
			HashableGameState root = state as HashableGameState;
			if(root == null) {
				Debug.LogError("Must provide HashableGameState when using Hashing enabled TurnEngines");
				return;
			}
			
			
			
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
				Debug.Log ("Starting depth " + depth);
				List<Turn> potentialTurns = new List<Turn>();
				
				float bestValue = eval.minValue;
				foreach(Turn turn in rootTurns) {
					//Debug.Log("Searching turn " + turn.ToString() + " to depth " + depth);
					if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
						exit = true;
						break;
					}
					HashableGameState nextState = turn.ApplyTurn(root.Clone()) as HashableGameState;
					if(nextState == null) {
						Debug.LogError("Apply turn did not produce a HashableGameState");
						return;
					}
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
				Debug.Log ("State Generation Table has " + stateGenerationTable.Count + " entries");
			}
			if(collectStats)
				Stats.Log(depth,DateTime.Now.Subtract(startTime).Seconds);
		}

		public float AlphaBeta(HashableGameState state, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				if(!evaluationTable.ContainsKey(state)) {

					evaluationTable.Add(state,eval.Evaluate(state));
				}
				return evaluationTable[state];

			}
			if(ourTurn) {
				float bestValue = eval.minValue;

				if(!stateGenerationTable.ContainsKey(state)) {
					foreach(Turn turn in state.GeneratePossibleTurns()) {
						HashableGameState nextState = turn.ApplyTurn(state.Clone()) as HashableGameState;
						AddStateGeneration(state,nextState);
					}
				}
				foreach(HashableGameState nextState in stateGenerationTable[state]) {
					//HashableGameState nextState = turn.ApplyTurn(state.Clone()) as HashableGameState;
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

				if(!stateGenerationTable.ContainsKey(state)) {
					foreach(Turn turn in state.GeneratePossibleTurns()) {
						HashableGameState nextState = turn.ApplyTurn(state.Clone()) as HashableGameState;
						AddStateGeneration(state,nextState);
					}
				}
				foreach(HashableGameState nextState in stateGenerationTable[state]) {
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
		

		void AddStateGeneration(HashableGameState parent, HashableGameState child)
		{
			if(!stateGenerationTable.ContainsKey(parent)) {
				//Debug.Log ("Adding new hash set for " + parent.GetHashCode());
				stateGenerationTable.Add(parent, new HashSet<HashableGameState>());
			}
			stateGenerationTable[parent].Add(child);
		}

		public class StateTurnPair
		{
			HashableGameState state;
			HashableTurn turn;

			public StateTurnPair (HashableGameState state, HashableTurn turn)
			{
				this.state = state;
				this.turn = turn;
			}

			public override int GetHashCode ()
			{
				int hash = 17;
				hash = hash*23 + state.GetHashCode();
				hash = hash*23 + turn.GetHashCode();
				return hash;
			}

		}

		
		
	}
	
}

