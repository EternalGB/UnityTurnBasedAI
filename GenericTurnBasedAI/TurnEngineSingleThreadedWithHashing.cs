using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenericTurnBasedAI
{
	
	public class TurnEngineSingleThreadedWithHashing : TurnEngine
	{
		
		Dictionary<HashableGameState, List<HashableTurn>> turnGenerationTable;
		Dictionary<StateTurnPair, HashableGameState> stateGenerationTable;


		public TurnEngineSingleThreadedWithHashing(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
			turnGenerationTable = new Dictionary<HashableGameState, List<HashableTurn>>();
			stateGenerationTable = new Dictionary<StateTurnPair, HashableGameState>();
		}
		
		protected override void TurnSearchDelegate(object state)
		{
			DateTime startTime = new DateTime(DateTime.Now.Ticks);
			bool exit = false;
			List<Turn> results = null;
			float resultsValue = eval.minValue;
			HashableGameState root = (HashableGameState)state;
			
			
			
			//precompute the first level so we don't have to every time
			List<Turn> rootTurns = new List<Turn>();
			foreach(Turn turn in root.GeneratePossibleTurns()) {
				rootTurns.Add(turn);

				if(timeLimited && results != null && DateTime.Now.Subtract(startTime).Seconds >= maxTime) {
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
					if(timeLimited && results != null && DateTime.Now.Subtract(startTime).Seconds >= maxTime) {
						exit = true;
						break;
					}
					
					//if we have the state in a table, just grab it!
					HashableTurn hTurn = (HashableTurn)turn;
					HashableGameState nextState;

					StateTurnPair key = new StateTurnPair(root,hTurn);
					if(stateGenerationTable.ContainsKey(key)) {
						Debug.Log("Got something out of the stateGenerationTable!");
						nextState = stateGenerationTable[key];
					} else {
						nextState = (HashableGameState)turn.ApplyTurn(root.Clone());
						stateGenerationTable.Add(key,nextState);
					}
					//HashableGameState nextState = (HashableGameState)turn.ApplyTurn(root.Clone());
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

		public float AlphaBeta(HashableGameState state, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				return eval.Evaluate(state);
			}
			if(ourTurn) {
				float bestValue = eval.minValue;

				if(!turnGenerationTable.ContainsKey(state)) {
					foreach(Turn turn in state.GeneratePossibleTurns()) {
						AddTurnGeneration(state,(HashableTurn)turn);
					}
				}
				IEnumerable<Turn> turns = turnGenerationTable[state];
				foreach(Turn turn in turns) {
					//if we have the state in a table, just grab it!
					HashableTurn hTurn = (HashableTurn)turn;
					HashableGameState nextState;
					StateTurnPair key = new StateTurnPair(state,hTurn);
					if(stateGenerationTable.ContainsKey(key)) {
						Debug.Log("Got something out of the stateGenerationTable!");
						nextState = stateGenerationTable[key];
					} else {
					 	nextState = (HashableGameState)turn.ApplyTurn(state.Clone());
						stateGenerationTable.Add(key,nextState);
					}
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
				if(!turnGenerationTable.ContainsKey(state)) {
					foreach(Turn turn in state.GeneratePossibleTurns()) {
						AddTurnGeneration(state,(HashableTurn)turn);
					}
				}
				IEnumerable<Turn> turns = turnGenerationTable[state];
				foreach(Turn turn in turns) {
					//if we have the state in a table, just grab it!
					HashableTurn hTurn = (HashableTurn)turn;
					HashableGameState nextState;
					StateTurnPair key = new StateTurnPair(state,hTurn);
					if(stateGenerationTable.ContainsKey(key)) {
						Debug.Log("Got something out of the stateGenerationTable!");
						nextState = stateGenerationTable[key];
					} else {
						nextState = (HashableGameState)turn.ApplyTurn(state.Clone());
						stateGenerationTable.Add(key,nextState);
					}
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

		void AddTurnGeneration(HashableGameState state, HashableTurn turn)
		{

			HashableTurn hTurn;
			if(!turn.GetType().IsAssignableFrom(typeof(HashableTurn))) {
				Debug.LogError("Wrong turn type");
				throw new ArgumentException("Must generate a hashable turn");
			} else
				hTurn = (HashableTurn)turn;
			if(!turnGenerationTable.ContainsKey(state))
				turnGenerationTable.Add(state, new List<HashableTurn>());
			turnGenerationTable[state].Add(hTurn);

		}

		void AddStateGeneration(HashableGameState parent, HashableTurn turn, HashableGameState child)
		{
			stateGenerationTable.Add(new StateTurnPair(parent,turn),child);
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

