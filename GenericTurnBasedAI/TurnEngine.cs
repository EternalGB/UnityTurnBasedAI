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
		protected int maxTime;
		protected System.Random rando;
		EngineStats _stats;
		public EngineStats Stats
		{
			get
			{
				if(collectStats)
					return _stats;
				else 
					return new EngineStats();
			} 
			private set
			{
				_stats = value;
			}
		}

		protected bool collectStats = false;
		protected bool stopped = true;
		
		public delegate void TurnReady(Turn bestTurn);
		public event TurnReady TurnReadyEvent;



		protected void InitEngine(Evaluator eval, int timeLimit, int depthLimit, bool timeLimited, bool collectStats)
		{
			if(timeLimit <= 0) {
				timeLimit = 1;
			}
			if(depthLimit <= 0) {
				depthLimit = 1;
			}
			this.timeLimited = timeLimited;
			this.collectStats = collectStats;
			this.maxTime = timeLimit;
			this.maxDepth = depthLimit;
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
			stopped = false;
			thread.Start(state);
			while(thread.IsAlive) {
				if(stopped)
					thread.Abort();
				yield return 0;
			}
			if(TurnReadyEvent != null)
				TurnReadyEvent(bestTurn);
			stopped = true;
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

		public virtual void Stop()
		{
			stopped = true;
		}

	}

}

