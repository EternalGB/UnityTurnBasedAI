
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// A multi-threaded implementation of <see cref="TurnEngine"/>. Uses the same search algorithm as <see cref="TurnEngineSingleThreaded"/>
	/// but runs each initial branch in a separate thread.
	/// 
	/// <seealso cref="TurnEngine"/>
	/// <seealso cref="TurnEngineSingleThreaded"/>
	/// </summary>
	public class TurnEngineMultiThreaded : TurnEngine
	{

		List<ManualResetEvent> lastDoneEvents;
		List<MinimaxWorker> lastThreadWorkers;

		public TurnEngineMultiThreaded(Evaluator eval, int timeLimit, int depthLimit, bool collectStats = false)
		{
			InitEngine(eval,timeLimit, depthLimit, true,collectStats);
		}

		public TurnEngineMultiThreaded(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			if(timeLimited)
				InitEngine(eval,limit,int.MaxValue,timeLimited,collectStats);
			else
				InitEngine(eval,int.MaxValue,limit,timeLimited,collectStats);
		}

		public TurnEngineMultiThreaded(Evaluator eval, int limit, bool timeLimited, int minThreads, int maxThreads, bool collectStats = false)
		{
			if(timeLimited)
				InitEngine(eval,limit,int.MaxValue,timeLimited,collectStats);
			else
				InitEngine(eval,int.MaxValue,limit,timeLimited,collectStats);
			//this.maxThreads = maxThreads;
			ThreadPool.SetMinThreads(minThreads,minThreads);
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
				if(Exit) {
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
				List<MinimaxWorker> threadWorkers = new List<MinimaxWorker>();

				int timeOut;
				if(timeLimited)
					timeOut = (int)(maxTime*1000);
				else
					timeOut = Timeout.Infinite;

				foreach(Turn turn in rootTurns) {

					ManualResetEvent waitHandle = new ManualResetEvent(false);
					MinimaxWorker nextWorker = new MinimaxWorker(root.Clone(), turn, eval, depth, false, waitHandle);
					threadWorkers.Add(nextWorker);
					doneEvents.Add(waitHandle);
					ThreadPool.QueueUserWorkItem(nextWorker.EvaluateState, turn);
				}

				lastThreadWorkers = threadWorkers;
				lastDoneEvents = doneEvents;

				float bestValue = eval.minValue;
				if(WaitHandle.WaitAll(doneEvents.ToArray(),timeOut) && !stopped) {
					foreach(MinimaxWorker mm in threadWorkers) {
						if(mm.Value >= bestValue) {
							if(mm.Value > bestValue) {
								bestValue = mm.Value;
								potentialTurns.Clear();
							}
							potentialTurns.Add(mm.firstTurn);
						}
					}
				} else {
					foreach(MinimaxWorker mm in threadWorkers) {
						mm.Stop();
					}
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
				lastThreadWorkers = null;
				lastDoneEvents = null;
			}
			if(collectStats)
				Stats.Log(depth,DateTime.Now.Subtract(startTime).Seconds);
		}

		public override void Stop ()
		{
			base.Stop ();
			if(lastThreadWorkers != null && lastDoneEvents != null) {
				foreach(ManualResetEvent mre in lastDoneEvents) {
					mre.Set();
				}
				foreach(MinimaxWorker mm in lastThreadWorkers) {
					mm.Stop();
				}

			}
		}
		
	}
	
}
