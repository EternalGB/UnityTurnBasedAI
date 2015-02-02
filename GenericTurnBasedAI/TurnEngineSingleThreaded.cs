using System;
using System.Collections.Generic;
using System.Threading;


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
			foreach(Turn turn in root.GeneratePossibleTurns()) {
				rootTurns.Add(turn);
			}
			//this is so we can bail out without evaluating any turns
			results = rootTurns;
			
			int depth;
			for(depth = 1; depth <= maxDepth && !exit; depth++) {
				List<Turn> potentialTurns = new List<Turn>();
				
				float bestValue = eval.maxValue;
				foreach(Turn turn in rootTurns) {
					if(timeLimited && results != null && DateTime.Now.Subtract(startTime).Seconds >= maxTime) {
						exit = true;
						break;
					}
					
					
					GameState nextState = turn.ApplyTurn(root.Clone());
					float value = Minimax.AlphaBeta(nextState,eval,depth-1,eval.minValue,eval.maxValue,false);
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
		

	}

}
