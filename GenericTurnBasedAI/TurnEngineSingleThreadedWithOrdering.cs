using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenericTurnBasedAI
{

	public class TurnEngineSingleThreadedWithOrdering : TurnEngine
	{

		DateTime startTime;
		int startingDepth = 0;

		public TurnEngineSingleThreadedWithOrdering(Evaluator eval, int timeLimit, int depthLimit, bool collectStats = false)
		{
			InitEngine(eval,timeLimit,depthLimit,true,collectStats);
		}

		public TurnEngineSingleThreadedWithOrdering(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			if(timeLimited)
				InitEngine(eval,limit,int.MaxValue,timeLimited,collectStats);
			else
				InitEngine(eval,int.MaxValue,limit,timeLimited,collectStats);
		}
	
		protected override void TurnSearchDelegate(object state)
		{

			startTime = new DateTime(DateTime.Now.Ticks);
			bool exit = false;
			List<Turn> results = null;
			float resultsValue = eval.minValue;
			GameState root = state as GameState;

			Node rootNode = new Node(root,null,eval.minValue);
			int depth;
			for(depth = 1; depth <= maxDepth && !exit; depth++) {

				startingDepth = depth;
				float abBest = AlphaBeta(rootNode,eval,depth,eval.minValue,eval.maxValue,true);
				//Debug.Log ("Best value from AB : " + abBest);
				if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
					exit = true;
					break;
				}

				List<Turn> potentialTurns = new List<Turn>();
				float bestValue = eval.minValue;
				for(int i = 0; i < rootNode.children.Count; i++) {
					float value = rootNode.children[i].value;
					if(value >= bestValue) {
						if(value > bestValue) {
							bestValue = value;
							potentialTurns.Clear();
						}
						potentialTurns.Add(rootNode.children[i].generatedBy);
					}
					//Debug.Log ("Root child " + i + " value " + value);
				}
				//Debug.Log ("At depth " + depth + " selected " + potentialTurns.Count + " potentials from " + rootNode.children.Count + " with value " + bestValue);
				//Debug.Log ("Best value found at depth " + depth + " : " + bestValue);
				//only overwrite the results if we haven't aborted mid search
				if(!exit) {
					results = potentialTurns;
					resultsValue = bestValue;
				}
				bestTurn = GetRandomElement<Turn>(results);
			}
			if(exit)
				depth--;
			depth--;
			if(collectStats)
				Stats.Log(depth,DateTime.Now.Subtract(startTime).Seconds);
		}

		public float AlphaBeta(Node current, Evaluator eval, int depth, float alpha, float beta, bool ourTurn)
		{
			/*
			if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
				if(ourTurn)
					return eval.minValue;
				else
					return eval.maxValue;
			}
			*/
			if(depth == 0 || current.state.IsTerminal()) {
				current.value = eval.Evaluate(current.state);
				return current.value;
			}
			if(ourTurn) {
				if(current.children.Count > 0) {
					float bestValue = eval.minValue;

					for(int i = 0; i < current.children.Count; i++) {
						Node child = current.children[i];
						float value = AlphaBeta(child,eval,depth-1,alpha,beta,false);
						if(value > bestValue) {
							bestValue = value;

						}
						alpha = Math.Max(alpha,bestValue);
						if(beta <= alpha) {
							current.SwapKiller(i);
							break;
						}
					}
					current.value = bestValue;
					return bestValue;
				} else {
					//Debug.Log ("Max building children");
					float bestValue = eval.minValue;
					foreach(Turn turn in current.state.GeneratePossibleTurns()) {
						GameState nextState = turn.ApplyTurn(current.state.Clone());
						Node next = new Node(nextState,turn,eval.maxValue); 
						//current.AddChild(next);
						float value = AlphaBeta(next,eval,depth-1,alpha,beta,false);
						if(value > bestValue) {
							bestValue = value;
							//current.value = value;
						}
						alpha = Math.Max(alpha,bestValue);

						if(beta <= alpha) {
							current.AddKiller(next);
							//break;
						} else
							current.AddChild(next);

						
					}
					current.value = bestValue;
					return bestValue;
				}

			} else {
				if(current.children.Count > 0) {
					float worstValue = eval.maxValue;
					for(int i = 0; i < current.children.Count; i++) {
						Node child = current.children[i];
						float value = AlphaBeta(child,eval,depth-1,alpha,beta,true);
						if(value < worstValue) {
							worstValue = value;
							//current.value = value;
						}
						beta = Math.Min(beta,worstValue);
						if(beta <= alpha) {
							current.SwapKiller(i);
							break;
						}
					}
					current.value = worstValue;
					return worstValue;
				} else {
					//Debug.Log ("Min building children");
					float worstValue = eval.maxValue;
					foreach(Turn turn in current.state.GeneratePossibleTurns()) {
						GameState nextState = turn.ApplyTurn(current.state.Clone());
						Node next = new Node(nextState,turn,eval.minValue); 

						float value = AlphaBeta(next,eval,depth-1,alpha,beta,true);
						if(value < worstValue) {
							worstValue = value;
							//current.value = value;
						}
						beta = Math.Min(beta,worstValue);

						if(beta <= alpha) {
							current.AddKiller(next);
							//break;
						} else
							current.AddChild(next);

					}
					current.value = worstValue;
					return worstValue;
				}
				
			}
		}
		
		public class Node 
		{
			public GameState state;
			public Turn generatedBy;
			public float value;
			public List<Node> children;

			public Node (GameState state, Turn generatedBy, float value)
			{
				this.state = state;
				this.generatedBy = generatedBy;
				this.value = value;
				this.children = new List<Node>();
			}

			public void AddChild(Node child)
			{
				children.Add(child);
			}

			public void AddKiller(Node killer)
			{
				children.Insert(0,killer);
			}

			public void SwapKiller(int newIndex)
			{
				Node killer = children[newIndex];
				children.RemoveAt(newIndex);
				children.Insert(0,killer);
			}

		}

	}

}
