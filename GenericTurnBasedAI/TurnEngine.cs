using System;
using System.Collections.Generic;
using System.Threading;

namespace GenericTurnBasedAI
{

	public abstract class TurnEngine
	{
		
		protected int maxDepth;
		protected Evaluator eval;
		protected Turn bestTurn;
		protected bool timeLimited = false;
		protected float maxTime;
		protected System.Random rando;
		public EngineStats Stats
		{
			get; private set;
		}

		protected bool collectStats = false;
		
		public delegate void TurnReady(Turn bestTurn);
		public event TurnReady TurnReadyEvent;
		

		protected void InitEngine(Evaluator eval, int limit, bool timeLimited, bool collectStats)
		{
			if(limit <= 0)
				throw new ArgumentOutOfRangeException("limit - must be at least 1");
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
			Thread thread = new Thread(TurnSearchDelegate);
			thread.Start(state);
			while(thread.IsAlive) {
				yield return 0;
			}
			if(TurnReadyEvent != null)
				TurnReadyEvent(bestTurn);
		}

		protected abstract void TurnSearchDelegate(object state);

		protected static T GetRandomElement<T>(IList<T> list)
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

