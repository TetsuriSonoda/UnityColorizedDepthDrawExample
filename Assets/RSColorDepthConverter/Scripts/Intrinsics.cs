namespace Intel.RealSense
{
	public struct Intrinsics
	{
		public float[] coeffs;
		public float fx;
		public float fy;
		public int height;
		public Distortion model;
		public float ppx;
		public float ppy;
		public int width;

		//		public override string ToString();
	}

	public enum Distortion
	{
		None = 0,
		ModifiedBrownConrady = 1,
		InverseBrownConrady = 2,
		Ftheta = 3,
		BrownConrady = 4
	}
}