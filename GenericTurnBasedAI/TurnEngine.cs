﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UniversalTurnBasedAI
{

	/// <summary>
	/// The super-class for all Turn Engines. Implementations of this class control the search for the best <see cref="Turn"/>.
	/// Provides an entry point for Unity with <see cref="GetNextTurn"/> which can be used
	/// in a familiar coroutine pattern. Defines attributes common to all Turn Engines such as depth and time
	/// limits. Also provides the <see cref="TurnReadyEvent"/>  which is triggered after a turn search has been completed and
	/// returns the best turn found.
	/// 
	/// <seealso cref="GameState"/>
	/// <seealso cref="Turn"/>
	/// <seealso cref="Evaluator"/>
	/// </summary>
	public abstract class TurnEngine
	{
		
		protected int maxDepth;
		protected int maxTime;
		protected bool timeLimited = false;
		protected Evaluator eval;
		protected System.Random rando;
		EngineStats _stats;
		/// <summary>
		/// Property for accessing stats if there were collect.
		/// </summary>
		/// <value>If stat collection enabled: returns any collected statisics collected since the last 
		/// ResetStatisticsLog call otherwise returns a new, empty EngineStats</value>
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

		protected DateTime startTime;
		protected bool Exit
		{
			get
			{
				return (timeLimited && DateTime.Now.Subtract(startTime).Seconds >= maxTime) || stopped;
			}
		}

		protected bool collectStats = false;
		protected bool stopped = true;

		protected Turn bestTurn;

		public delegate void TurnReady(Turn bestTurn);
		/// <summary>
		/// Triggered after <see cref="GetNextTurn"/> has been called and the found turn is ready to be returned.
		/// <see cref="bestTurn"/>  will be the best turn discovered by the engine
		/// </summary>
		public event TurnReady TurnReadyEvent;

		/// <summary>
		/// Initialises the common engine elements. If time or depth limit are less than 0 they are set to 1
		/// </summary>
		/// <param name="eval">The class used to evaluate GameStates searched by this engine</param>
		/// <param name="timeLimit">The maximum time allowed for search, in seconds. Must be at least 1</param>
		/// <param name="depthLimit">The maximum depth to search in the GameState search tree. Also called "ply". Must be at least 1</param>
		/// <param name="timeLimited">If set to <c>true</c> Search will end after the set timeLimit, otherwise
		/// search will complete to the set depthLimit</param>
		/// <param name="collectStats">If set to <c>true</c> collect statistics.</param>
		protected void InitEngine(Evaluator eval, int timeLimit, int depthLimit, bool timeLimited, bool collectStats)
		{

			if(timeLimit <= 0) {
				throw new ArgumentOutOfRangeException("timeLimit","Must be at least 1");
			}
			if(depthLimit <= 0) {
				throw new ArgumentOutOfRangeException("depthLimit","Must be at least 1");
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

		/// <summary>
		/// The entry point to the engine. Starts a new thread to run the search in and waits on it. Once the search
		/// is completed or timed out, calls the <see cref="TurnReadyEvent"/> to return the best turn found.
		/// 
		/// <example>
		/// Typical usage from Unity is:
		/// <code>
		/// StartCoroutine(engine.GetNextTurn(state));
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="state">State.</param>
		public System.Collections.IEnumerator GetNextTurn(GameState state) 
		{
			bestTurn = null;
			Thread thread = new Thread(() => ExecuteAndCatch(TurnSearchDelegate, state, ExceptionHandler));
			stopped = false;
			startTime = new DateTime(DateTime.Now.Ticks);
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

		/// <summary>
		/// Wrapper for catching exceptions from another thread
		/// </summary>
		/// <param name="action">The action to run</param>
		/// <param name="arg">The action's arguments</param>
		/// <param name="exceptionHandler">A handler for the exceptions</param>
		protected void ExecuteAndCatch(Action<object> action, object arg, Action<Exception> exceptionHandler)
		{
			try
			{
				action(arg);
			}
			catch (Exception ex)
			{
				exceptionHandler(ex);
			}
		}

		/// <summary>
		/// Writes .NET exceptions to the Unity Debug Log
		/// </summary>
		/// <param name="ex">The exception</param>
		protected void ExceptionHandler(Exception ex)
		{
			Debug.LogError (ex.Message);
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

