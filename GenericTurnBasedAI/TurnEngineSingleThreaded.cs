using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenericTurnBasedAI
{

	public class TurnEngineSingleThreaded : TurnEngine
	{

		DateTime startTime;

		public TurnEngineSingleThreaded(Evaluator eval, int limit, bool timeLimited, bool collectStats = false)
		{
			InitEngine(eval,limit,timeLimited,collectStats);
		}
	
		protected override void TurnSearchDelegate(object state)
		{

			startTime = new DateTime(DateTime.Now.Ticks);
			bool exit = false;
			List<Turn> results = null;
			float resultsValue = eval.minValue;
			GameState root = (GameState)state;

			Node rootNode = new Node(root,null,eval.minValue);
			int depth;
			for(depth = 1; depth <= maxDepth && !exit; depth++) {

				AlphaBeta(rootNode,eval,depth,eval.minValue,eval.maxValue,true);
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
				}

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
			if((timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped) {
				if(ourTurn)
					return eval.minValue;
				else
					return eval.maxValue;
			}
			if(depth == 0 || current.state.IsTerminal()) {
				current.value = eval.Evaluate(current.state);
				return current.value;
			}
			if(ourTurn) {
				if(current.children.Count > 0) {
					//Debug.Log ("Max Using premade children");
					current.children.Sort(Node.DescendingNodeSort);
					float bestValue = eval.minValue;
					foreach(Node child in current.children) {

						float value = AlphaBeta(child,eval,depth-1,alpha,beta,false);
						if(value > bestValue) {
							bestValue = value;
							current.value = value;
						}
						value = Math.Max(value,bestValue);
						alpha = Math.Max(alpha,value);
						if(beta <= alpha) {
							break;
						}
					}
					return bestValue;
				} else {
					//Debug.Log ("Max building children");
					float bestValue = eval.minValue;
					foreach(Turn turn in current.state.GeneratePossibleTurns()) {
						GameState nextState = turn.ApplyTurn(current.state.Clone());
						Node next = new Node(nextState,turn,eval.minValue); 
						current.AddChild(next);
						float value = AlphaBeta(next,eval,depth-1,alpha,beta,false);
						if(value > bestValue) {
							bestValue = value;
							current.value = value;
						}
						value = Math.Max(value,bestValue);
						alpha = Math.Max(alpha,value);
						if(beta <= alpha) {
							break;
						}
						
					}
					return bestValue;
				}

			} else {
				if(current.children.Count > 0) {
					//Debug.Log ("Min Using premade children");
					current.children.Sort(Node.AscendingNodeSort);
					float worstValue = eval.maxValue;
					foreach(Node child in current.children) {
						float value = AlphaBeta(child,eval,depth-1,alpha,beta,false);
						if(value < worstValue) {
							worstValue = value;
							current.value = value;
						}
						value = Math.Min(value,worstValue);
						beta = Math.Min(beta,value);
						if(beta <= alpha) {
							break;
						}
					}
					return worstValue;
				} else {
					//Debug.Log ("Min building children");
					float worstValue = eval.maxValue;
					foreach(Turn turn in current.state.GeneratePossibleTurns()) {
						GameState nextState = turn.ApplyTurn(current.state.Clone());
						Node next = new Node(nextState,turn,eval.maxValue); 
						current.AddChild(next);
						float value = AlphaBeta(next,eval,depth-1,alpha,beta,true);
						if(value < worstValue) {
							worstValue = value;
							current.value = value;
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
				//we have to do some additional checks because of possible overflows
				if(other.value == value)
					return 0;

				if(value == float.MinValue)
					return -1;
				if(other.value == float.MinValue)
					return 1;
				if(value == float.MaxValue)
					return 1;
				if(other.value == float.MaxValue)
					return -1;
				float diff = other.value - value;
				if(diff < 0)
					return -1;
				else if(diff > 0)
					return 1;
				else
					return 0;
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

			public static int DescendingNodeSort(Node n1, Node n2)
			{
				if(n2 == null) return 1;
				//we have to do some additional checks because of possible overflows
				if(n2.value == n1.value)
					return 0;
				
				if(n1.value == float.MinValue)
					return 1;
				if(n2.value == float.MinValue)
					return -1;
				if(n1.value == float.MaxValue)
					return -1;
				if(n2.value == float.MaxValue)
					return 1;
				float diff = n2.value - n1.value;
				if(diff < 0)
					return -1;
				else if(diff > 0)
					return 1;
				else
					return 0;
			}

			public static int AscendingNodeSort(Node n1, Node n2)
			{
				if(n2 == null) return 1;
				//we have to do some additional checks because of possible overflows
				if(n2.value == n1.value)
					return 0;
				
				if(n1.value == float.MinValue)
					return -1;
				if(n2.value == float.MinValue)
					return 1;
				if(n1.value == float.MaxValue)
					return 1;
				if(n2.value == float.MaxValue)
					return -1;
				float diff = n1.value - n2.value;
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
