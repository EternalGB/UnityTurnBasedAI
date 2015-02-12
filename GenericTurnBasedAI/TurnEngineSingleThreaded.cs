using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// A single threaded implementation of <see cref="TurnEngine"/>. Uses an implementation of
	/// the Minimax algorithm with Alpha-Beta pruning.
	/// 
	/// <seealso cref="TurnEngine"/> 
	/// <seealso cref="TurnEngineMultiThreaded"/>
	/// </summary>
	public class TurnEngineSingleThreaded : TurnEngine
	{
		
		/// <summary>
		/// Initializes a new instance of the <see cref="UniversalTurnBasedAI.TurnEngineSingleThreaded"/> class.
		/// </summary>
		/// <param name="eval">The <see cref="Evaluator"/> to use</param>
		/// <param name="timeLimit">Time limit in seconds. Must be at least 1</param>
		/// <param name="depthLimit">Depth limit or maximum "ply". Must be at least 1</param>
		/// <param name="collectStats">If set to <c>true</c> collect stats.</param>
		public TurnEngineSingleThreaded (Evaluator eval, int timeLimit, int depthLimit, bool collectStats = false)
		{
			InitEngine(eval,timeLimit,depthLimit,true,collectStats);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UniversalTurnBasedAI.TurnEngineSingleThreaded"/> class.
		/// </summary>
		/// <param name="eval">Eval.</param>
		/// <param name="limit">Limit.</param>
		/// <param name="timeLimited">If set to <c>true</c> then <paramref name="limit"/> indicates .</param>
		/// <param name="collectStats">If set to <c>true</c> collect stats.</param>
		public TurnEngineSingleThreaded (Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			if(timeLimited)
				InitEngine(eval,limit,int.MaxValue,timeLimited,collectStats);
			else
				InitEngine(eval,int.MaxValue,limit,timeLimited,collectStats);
		}

		/// <summary>
		/// A wrapper for the Minimax algorithm. Initialises the first branch of turns so that
		/// they can be given values and the best possible returned. Always generates at least one
		/// possible turns so that at least some sensible result can be returned. When the search is
		/// completed or timed out <see cref="bestTurn"/> will be assigned to the best found turn.
		/// </summary>
		/// <param name="state">The starting state</param>
		protected override void TurnSearchDelegate(object state)
		{
			

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
				
				float bestValue = eval.minValue;
				foreach(Turn turn in rootTurns) {
					
					//Debug.Log("Searching turn " + turn.ToString() + " to depth " + depth);
					if(Exit) {
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

		/// <summary>
		/// An implementation of the Minimax algorithm with Alpha-Beta pruning
		/// </summary>
		/// <returns>The beta.</returns>
		/// <param name="state">Starting state</param>
		/// <param name="eval">Used to evaluate leaf states</param>
		/// <param name="depth">The current depth (counting down)</param>
		/// <param name="alpha">The current upper bound of values found</param>
		/// <param name="beta">The current lower bound of values found</param>
		/// <param name="ourTurn">Whether or not it is the searching player's turn</param>
		float AlphaBeta(GameState state, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			if(depth == 0 || state.IsTerminal()) {
				return eval.Evaluate(state);
			}
			if(ourTurn) {
				float bestValue = eval.minValue;
				foreach(Turn turn in state.GeneratePossibleTurns()) {
					if(Exit)
						break;
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
					if(Exit)
						break;
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
