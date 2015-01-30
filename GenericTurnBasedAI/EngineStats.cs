namespace GenericTurnBasedAI
{
	public class EngineStats 
	{
		int iterations;
		
		int minDepth = int.MinValue;
		int maxDepth = int.MaxValue;
		float totalDepth;
		
		int minTime = int.MinValue;
		int maxTime = int.MaxValue;
		float totalTime;
		
		public float AverageDepth
		{
			get {return totalDepth/iterations;}
		}
		
		public float AverageTime
		{
			get {return totalTime/iterations;}
		}
		
		internal void Log(int depth, int time)
		{
			if(depth < minDepth)
				minDepth = depth;
			if(depth > maxDepth)
				maxDepth = depth;
			if(time < minTime)
				minTime = time;
			if(time > maxTime)
				maxTime = time;
			totalDepth += depth;
			totalTime  += time;
			iterations++;
		}
		
		public override string ToString ()
		{
			return string.Format ("Min/Max Depth = ({0},{1}), Average Depth={2}, Min/Max Time = ({3},{4}), Average Time={5}, ", 
			                      minDepth, maxDepth, AverageDepth, minTime, maxTime, AverageTime);
		}
		
	}
}


