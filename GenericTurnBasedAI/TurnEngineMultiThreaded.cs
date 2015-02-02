
using System;
using System.Collections.Generic;
using System.Threading;


namespace GenericTurnBasedAI
{
	
	public class TurnEngineMultiThreaded : TurnEngine
	{

		int maxThreads;
		
		public TurnEngineMultiThreaded(Evaluator eval, int limit, bool timeLimited, int maxThreads, bool collectStats = false)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
			this.maxThreads = maxThreads;
			ThreadPool.SetMaxThreads(maxThreads,maxThreads);
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

				List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();
				List<Minimax> threadWorkers = new List<Minimax>();

				float bestValue = eval.maxValue;
				foreach(Turn turn in rootTurns) {
					ManualResetEvent waitHandle = new ManualResetEvent(false);
					Minimax nextWorker = new Minimax(root.Clone(), turn, eval, maxDepth, false, waitHandle);
					threadWorkers.Add(nextWorker);
					doneEvents.Add(waitHandle);
					ThreadPool.QueueUserWorkItem(nextWorker.EvaluateState);
				}

				int timeOut;
				if(timeLimited)
					timeOut = (int)(maxTime*1000);
				else
					timeOut = Timeout.Infinite;

				if(WaitHandle.WaitAll(doneEvents.ToArray(),timeOut)) {
					foreach(Minimax mm in threadWorkers) {
						if(mm.Value >= bestValue) {
							if(mm.Value > bestValue) {
								bestValue = mm.Value;
								potentialTurns.Clear();
							}
							potentialTurns.Add(mm.firstTurn);
						}
					}
				} else {
					exit = true;
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
		
		
	}
	
}
